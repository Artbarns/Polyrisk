using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using PolyRisk.Core;
using PolyRisk.Systems;
using PolyRisk.Multiplayer;
using PolyRisk.Visuals;

namespace PolyRisk.UI
{
    /// <summary>
    /// Phase E.1 — Top HUD Cyber-Luxury.
    /// Affiche pour Player 0 (humain) : CAPITAL exact, NET WORTH, TURN x/y, MARKET.
    /// Le cash des autres joueurs est masqué (Dark Board) — visible uniquement comme
    /// pastille WealthStatus dans les autres écrans (Marketplace Select Player notamment).
    ///
    /// Les anciens champs TextMeshProUGUI (capitalText, etc.) ont été supprimés de la
    /// scène (remplacés par TopHUD.uxml) — plus de chemin legacy à maintenir ici.
    /// </summary>
    public class TopHUDWidget : MonoBehaviour
    {
        [Header("Dependencies")]
        public TurnManager turnManager;

        [Header("UI Toolkit")]
        [Tooltip("UIDocument hôte de TopHUD.uxml.")]
        public UIDocument uiDocument;
        public VisualTreeAsset topHudTemplate;

        [Header("Game Config")]
        [Tooltip("Nombre de tours max (mode tournoi). Affiché dans TURN: x/MAX")]
        [SerializeField] private int maxTurns = 50;

        // Refs UI Toolkit résolues une fois au Start
        private Label lblCapital, lblNetWorth, lblTurn, lblMarket;
        private Coroutine moneyAnimation;
        private bool moneyInitialized;
        private double shownCapital, shownNetWorth, targetCapital, targetNetWorth;
        private const float MoneyAnimationSeconds = 0.45f;
        private const float MoneyPaintIntervalSeconds = 0.05f;
        private VisualElement activityFeed, activityFeedStack;
        private VisualElement playerRoster;
        private VisualElement diceRollStage;
        private Label diceRollActor, diceRollTotal, diceRollPas, diceDoubleTag;
        private VisualElement diceRollFirstDie, diceRollSecondDie, diceRollPair, diceRollVisual;
        private AudioSource diceAudioSource;
        private AudioClip diceContactLight, diceContactTac, diceDoubleAccent;
        private int diceRollConfirmedFirst, diceRollConfirmedSecond;
        private bool diceRollIsPenaltyDouble;
        private int diceRollSequence;
        private bool diceDockSuppressed;
        private Coroutine diceMotion;
        private bool diceInCockpit;
        private Label lblLocalPlayerStatus;
        private bool localPlayerStatusVisible;
        private readonly List<GameActivityEntry> pendingActivities = new List<GameActivityEntry>();
        // Wide layouts show two messages side by side; narrow layouts keep one priority message.
        private const int MaxVisibleActivities = 2;

        // ─────────────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            GameEvents.OnTurnStarted    += OnTurnStarted;
            GameEvents.OnBalanceChanged += OnBalanceChanged;
            GameEvents.OnPropertyPurchased += OnPropertyPurchased;
            GameEvents.OnGameEventPublished += OnGameEventPublished;
            UpdateHumanPlayerUI();
        }

        private void OnDisable()
        {
            GameEvents.OnTurnStarted    -= OnTurnStarted;
            GameEvents.OnBalanceChanged -= OnBalanceChanged;
            GameEvents.OnPropertyPurchased -= OnPropertyPurchased;
            GameEvents.OnGameEventPublished -= OnGameEventPublished;
            diceRollSequence++;
            if (diceMotion != null) StopCoroutine(diceMotion);
            diceMotion = null;
            if (moneyAnimation != null) StopCoroutine(moneyAnimation);
            moneyAnimation = null;
            moneyInitialized = false;
            RestoreYieldOverlayAfterDice();
        }

        private void Start()
        {
            TryBindUIToolkit();
            UpdateHumanPlayerUI();
        }

        private static void UpdateNetworkCommandNotice(VisualElement root, string error)
        {
            var notice = root.Q<VisualElement>("NetworkCommandNotice");
            if (string.IsNullOrWhiteSpace(error))
            {
                notice?.RemoveFromHierarchy();
                return;
            }

            if (notice != null) return;
            notice = new VisualElement { name = "NetworkCommandNotice" };
            notice.AddToClassList("network-command-notice");
            notice.Add(new Label("No server confirmation. Retry the same action to resync."));
            root.Add(notice);
        }

        public static void ReportNetworkCommandStatus(string error)
        {
            foreach (var document in FindObjectsOfType<UIDocument>(true))
            {
                var root = document.rootVisualElement;
                if (root != null && root.Q<VisualElement>("TopHUD_Bar") != null)
                    UpdateNetworkCommandNotice(root, error ?? string.Empty);
            }
        }

        public static void SetBoardYieldOverlayVisible(bool visible)
        {
            foreach (var document in FindObjectsOfType<UIDocument>(true))
            {
                var overlay = document.rootVisualElement?.Q<VisualElement>("BoardTileYieldOverlay");
                if (overlay != null)
                    overlay.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void TryBindUIToolkit()
        {
            if (uiDocument == null) return;
            UIUXRuntime.PrepareDocument(uiDocument);
            var root = uiDocument.rootVisualElement;
            if (root == null) return;

            // Si le template n'est pas encore monté, on l'instancie
            if (topHudTemplate != null && root.Q<VisualElement>("TopHUD_Bar") == null)
                topHudTemplate.CloneTree(root);

            UIUXRuntime.PrepareRoot(root);
            BoardTileOverlayWidget.Mount(root);
            // Mount event cards after the tile badges so transient results stay legible.
            MountDiceRollStage(root);
            var cockpit = FindObjectOfType<ActionButtonsWidget>(true);
            if (cockpit?.DiceHost != null) AttachDiceToCockpit(cockpit.DiceHost);
            root.RegisterCallback<GeometryChangedEvent>(_ => TrimActivityRows());

            lblCapital  = root.Q<Label>("Lbl_Capital");
            lblNetWorth = root.Q<Label>("Lbl_NetWorth");
            lblTurn     = root.Q<Label>("Lbl_Turn");
            lblMarket   = root.Q<Label>("Lbl_Market");
            playerRoster = root.Q<VisualElement>("PlayerRoster");
            activityFeed = root.Q<VisualElement>("ActivityFeed");
            activityFeedStack = root.Q<VisualElement>("ActivityFeedStack");
            lblLocalPlayerStatus = root.Q<Label>("Lbl_LocalPlayerStatus");
            var gameMenuButton = root.Q<Button>("Btn_GameMenu");
            if (gameMenuButton != null)
                gameMenuButton.clicked += () =>
                {
                    var menu = MainMenuController.Instance ?? FindObjectOfType<MainMenuController>();
                    menu?.ShowInGameMenu();
                };

            if (activityFeedStack != null && pendingActivities.Count > 0)
            {
                foreach (var entry in pendingActivities.Skip(Mathf.Max(0, pendingActivities.Count - VisibleActivityCapacity)))
                    AddActivityRow(entry);
                pendingActivities.Clear();
            }
            UpdatePlayerRoster();
        }

        private void MountDiceRollStage(VisualElement root)
        {
            if (root == null || diceRollStage != null) return;

            diceRollStage = new VisualElement { name = "DiceRollStage" };
            diceRollStage.AddToClassList("dice-roll-stage");
            diceRollStage.style.display = DisplayStyle.None;
            diceRollStage.pickingMode = PickingMode.Ignore;

            var card = new VisualElement();
            card.AddToClassList("dice-roll-card");
            card.pickingMode = PickingMode.Ignore;

            diceRollActor = new Label();
            diceRollActor.AddToClassList("dice-roll-actor");
            card.Add(diceRollActor);

            diceRollVisual = new VisualElement();
            diceRollVisual.AddToClassList("dice-roll-visual");
            diceRollVisual.pickingMode = PickingMode.Ignore;
            var halo = new VisualElement();
            halo.AddToClassList("dice-double-halo");
            diceRollVisual.Add(halo);
            for (int rayIndex = 0; rayIndex < 8; rayIndex++)
            {
                var ray = new VisualElement();
                ray.AddToClassList("dice-double-ray");
                ray.AddToClassList("dice-double-ray--" + rayIndex);
                diceRollVisual.Add(ray);
            }
            for (int barIndex = 0; barIndex < 3; barIndex++)
            {
                var bar = new VisualElement();
                bar.AddToClassList("dice-double-accent-bar");
                bar.AddToClassList("dice-double-accent-bar--" + barIndex);
                diceRollVisual.Add(bar);
            }
            diceRollTotal = new Label();
            diceRollTotal.AddToClassList("dice-roll-total");
            diceRollVisual.Add(diceRollTotal);
            diceRollPas = new Label("PAS");
            diceRollPas.AddToClassList("dice-roll-pas");
            diceRollVisual.Add(diceRollPas);

            diceRollPair = new VisualElement();
            diceRollPair.AddToClassList("dice-roll-pair");
            diceRollPair.pickingMode = PickingMode.Ignore;
            diceRollFirstDie = MakeStageDie();
            diceRollSecondDie = MakeStageDie();
            diceRollPair.Add(diceRollFirstDie);
            diceRollPair.Add(diceRollSecondDie);
            diceRollVisual.Add(diceRollPair);
            card.Add(diceRollVisual);
            diceDoubleTag = new Label();
            diceDoubleTag.AddToClassList("dice-double-tag");
            card.Add(diceDoubleTag);

            var skip = new Button(SkipDiceRollAnimation) { text = "Passer" };
            skip.AddToClassList("dice-roll-skip");
            card.Add(skip);
            diceRollStage.Add(card);
            root.Add(diceRollStage);
            diceAudioSource = GetComponent<AudioSource>();
            if (diceAudioSource == null) diceAudioSource = gameObject.AddComponent<AudioSource>();
            diceAudioSource.playOnAwake = false;
            diceAudioSource.spatialBlend = 0f;
            diceContactLight = Resources.Load<AudioClip>("PolyRiskAudio/contact_light");
            diceContactTac = Resources.Load<AudioClip>("PolyRiskAudio/contact_tac");
            diceDoubleAccent = Resources.Load<AudioClip>("PolyRiskAudio/double_green");
        }

        private static VisualElement MakeStageDie() => new PulseDiceView();
        private static void SetDieFace(VisualElement die, int value)
        {
            if (die is PulseDiceView volume) volume.SetValue(value);
        }
        public void AttachDiceToCockpit(VisualElement host)
        {
            if (host == null || diceRollStage == null) return;
            if (diceRollStage.parent != host) host.Add(diceRollStage);
            diceInCockpit = true;
            diceRollStage.AddToClassList("dice-roll-stage--cockpit");
            diceRollStage.style.display = DisplayStyle.Flex;
            if (diceRollConfirmedFirst == 0) PresentDiceReady(turnManager?.CurrentPlayer?.PlayerId ?? 0);
        }
        public void PresentDiceReady(int actorId)
        {
            diceRollSequence++;
            if (diceMotion != null) StopCoroutine(diceMotion);
            diceMotion = null;
            RestoreYieldOverlayAfterDice();
            if (diceRollStage == null) return;
            ResetDiceClasses();
            diceRollStage.AddToClassList("dice-roll-stage--idle");
            diceRollActor.text = ResolvePlayerName(actorId).ToUpperInvariant();
            diceRollTotal.text = "";
            diceRollConfirmedFirst = diceRollConfirmedSecond = 0;
            diceRollPas.text = "PRÊT À LANCER";
            SetDieFace(diceRollFirstDie, 3); SetDieFace(diceRollSecondDie, 4);
            SetDicePose(false, new PulseDicePose(0, 0)); SetDicePose(true, new PulseDicePose(0, 0));
        }
        private void ResetDiceClasses()
        {
            foreach (string suffix in new[] { "settled", "double", "penalty-double", "double-release", "double-rest",
                "double-halo-fade", "double-rays-fade", "double-bars-fade", "skip", "idle" })
                diceRollStage.RemoveFromClassList("dice-roll-stage--" + suffix);
            diceRollPair.RemoveFromClassList("dice-roll-pair--double");
            diceDoubleTag.text = "";
        }
        private void SetDicePose(bool second, PulseDicePose pose)
        {
            if ((second ? diceRollSecondDie : diceRollFirstDie) is PulseDiceView volume) volume.SetPose(pose);
        }

        private void OnGameEventPublished(GameEvent gameEvent)
        {
            UpdatePlayerRoster();
            if (gameEvent == null || gameEvent.Kind != GameEventKind.DiceRolled
                || gameEvent.ValueA < 1 || gameEvent.ValueA > 6 || gameEvent.ValueB < 1 || gameEvent.ValueB > 6
                || diceRollStage == null) return;
            var cockpit = FindObjectOfType<ActionButtonsWidget>(true);
            if (cockpit?.DiceHost != null) AttachDiceToCockpit(cockpit.DiceHost);
            int sequence = ++diceRollSequence;
            if (diceMotion != null) StopCoroutine(diceMotion);
            diceRollConfirmedFirst = gameEvent.ValueA;
            diceRollConfirmedSecond = gameEvent.ValueB;
            diceRollIsPenaltyDouble = gameEvent.ValueA == gameEvent.ValueB && turnManager != null
                && turnManager.ConsecutiveDoubles >= 4 && turnManager.CurrentPlayer != null
                && !turnManager.CurrentPlayer.IsInAudit;
            ResetDiceClasses();
            diceRollActor.text = ResolvePlayerName(gameEvent.ActorId).ToUpperInvariant();
            diceRollPas.text = "PAS";
            diceRollTotal.text = "";
            SetDieFace(diceRollFirstDie, gameEvent.ValueA); SetDieFace(diceRollSecondDie, gameEvent.ValueB);
            diceRollStage.AddToClassList("dice-roll-stage--visible");
            diceRollStage.style.display = DisplayStyle.Flex;
            SuppressGameStateDockForDice();
            if (UIUXRuntime.ReducedMotion)
            {
                SetDicePose(false, new PulseDicePose(0, 0)); SetDicePose(true, new PulseDicePose(0, 0));
                RevealDiceResult(sequence);
                ScheduleDiceRollHide(sequence, 900);
                return;
            }
            diceMotion = StartCoroutine(AnimateDice(sequence));
            ScheduleDiceRollHide(sequence, 1900);
        }
        private IEnumerator AnimateDice(int sequence)
        {
            float started = Time.unscaledTime;
            bool light = false, contact = false, revealed = false;
            bool isDouble = diceRollConfirmedFirst == diceRollConfirmedSecond;
            while (sequence == diceRollSequence)
            {
                float t = Time.unscaledTime - started;
                if (UIUXRuntime.ReducedMotion)
                {
                    SetDicePose(false, new PulseDicePose(0, 0)); SetDicePose(true, new PulseDicePose(0, 0));
                    RevealDiceResult(sequence); break;
                }
                SetDicePose(false, PulseDicePose.Sample(false, t));
                SetDicePose(true, PulseDicePose.Sample(true, t));
                if (!light && t >= .52f)
                {
                    light = true;
                    if (diceAudioSource != null && diceContactLight != null && AudioListener.volume > 0)
                        diceAudioSource.PlayOneShot(diceContactLight, .55f);
                }
                if (!contact && t >= .935f)
                {
                    contact = true;
                    if (isDouble) diceRollPair.AddToClassList("dice-roll-pair--double");
                    if (diceAudioSource != null && diceContactTac != null && AudioListener.volume > 0)
                        diceAudioSource.PlayOneShot(diceContactTac, .7f);
                }
                if (!revealed && t >= (isDouble && !diceRollIsPenaltyDouble ? 1f : .935f))
                { revealed = true; RevealDiceResult(sequence); }
                if (t >= 1.3f) break;
                yield return null;
            }
            diceMotion = null;
        }
        private void RevealDiceResult(int sequence)
        {
            if (sequence != diceRollSequence) return;
            SetDiceRollResult(diceRollConfirmedFirst, diceRollConfirmedSecond);
            diceRollStage.AddToClassList("dice-roll-stage--settled");
            if (diceRollConfirmedFirst != diceRollConfirmedSecond) return;
            diceRollStage.AddToClassList(diceRollIsPenaltyDouble ? "dice-roll-stage--penalty-double" : "dice-roll-stage--double");
            diceDoubleTag.text = diceRollIsPenaltyDouble ? "DOUBLE · PENALTY" : "DOUBLE";
            if (diceRollIsPenaltyDouble || UIUXRuntime.ReducedMotion) return;
            diceRollStage.AddToClassList("dice-roll-stage--double-release");
            ScheduleDiceClass(sequence, "double-halo-fade", 40);
            ScheduleDiceClass(sequence, "double-rays-fade", 360);
            ScheduleDiceClass(sequence, "double-bars-fade", 850);
            ScheduleDiceClass(sequence, "double-rest", 280);
            if (diceAudioSource != null && diceDoubleAccent != null && AudioListener.volume > 0)
                diceAudioSource.PlayOneShot(diceDoubleAccent, .5f);
        }
        private void ScheduleDiceClass(int sequence, string suffix, long delay)
        {
            diceRollStage.schedule.Execute(() =>
            {
                if (sequence == diceRollSequence) diceRollStage.AddToClassList("dice-roll-stage--" + suffix);
            }).StartingIn(delay);
        }

        private void SetDiceRollResult(int first, int second)
        {
            SetDieFace(diceRollFirstDie, first);
            SetDieFace(diceRollSecondDie, second);
            diceRollTotal.text = (first + second).ToString(CultureInfo.InvariantCulture);
        }

        private void ScheduleDiceRollHide(int sequence, long delayMs)
        {
            diceRollStage.schedule.Execute(() =>
            {
                if (sequence != diceRollSequence || diceRollStage == null) return;
                diceRollStage.RemoveFromClassList("dice-roll-stage--visible");
                if (!diceInCockpit) diceRollStage.style.display = DisplayStyle.None;
                RestoreYieldOverlayAfterDice();
            }).StartingIn(delayMs);
        }
        private void SkipDiceRollAnimation()
        {
            if (diceRollConfirmedFirst < 1 || diceRollStage == null) return;
            diceRollSequence++;
            if (diceMotion != null) StopCoroutine(diceMotion);
            diceMotion = null;
            SetDicePose(false, new PulseDicePose(0, 0)); SetDicePose(true, new PulseDicePose(0, 0));
            RevealDiceResult(diceRollSequence);
            diceRollStage.AddToClassList("dice-roll-stage--skip");
            diceRollStage.RemoveFromClassList("dice-roll-stage--visible");
            if (!diceInCockpit) diceRollStage.style.display = DisplayStyle.None;
            RestoreYieldOverlayAfterDice();
        }

        private void SuppressGameStateDockForDice()
        {
            if (diceDockSuppressed) return;
            ActionButtonsWidget.SetTransientDicePresentation(true);
            diceDockSuppressed = true;
        }

        private void RestoreGameStateDockAfterDice()
        {
            if (!diceDockSuppressed) return;
            ActionButtonsWidget.SetTransientDicePresentation(false);
            diceDockSuppressed = false;
        }

        private void RestoreYieldOverlayAfterDice() => RestoreGameStateDockAfterDice();

        private void OnTurnStarted(int playerId)
        {
            PresentDiceReady(playerId);
            UpdateHumanPlayerUI();
            UpdatePlayerRoster();
        }

        private void OnBalanceChanged(int playerId, double newAmount)
        {
            int localPlayerId = OnlineGameController.Instance != null && OnlineGameController.Instance.IsOnlineGame
                ? OnlineGameController.Instance.LocalSeatIndex
                : 0;
            if (playerId == localPlayerId) UpdateHumanPlayerUI();
            UpdatePlayerRoster();
        }

        private void OnPropertyPurchased(int playerId, string propertyName)
        {
            int localPlayerId = OnlineGameController.Instance != null && OnlineGameController.Instance.IsOnlineGame
                ? OnlineGameController.Instance.LocalSeatIndex
                : 0;
            if (playerId == localPlayerId) UpdateHumanPlayerUI();
        }

        private void OnActivityPublished(GameActivityEntry entry)
        {
            if (entry == null) return;
            int localPlayerId = OnlineGameController.Instance != null && OnlineGameController.Instance.IsOnlineGame
                ? OnlineGameController.Instance.LocalSeatIndex
                : 0;
            if (entry.playerId == localPlayerId) UpdateHumanPlayerUI();
            if (activityFeedStack == null)
            {
                pendingActivities.Add(entry);
                while (pendingActivities.Count > VisibleActivityCapacity)
                    pendingActivities.RemoveAt(0);
                return;
            }
            AddActivityRow(entry);
        }

        private void AddActivityRow(GameActivityEntry entry)
        {
            if (activityFeed == null || activityFeedStack == null) return;
            RefreshActivityFeedVisibility();

            int firstDie = 0;
            int secondDie = 0;
            bool isDiceResult = string.Equals(entry.kind, "dice", StringComparison.OrdinalIgnoreCase)
                && TryReadDice(entry.text, out firstDie, out secondDie);
            var row = isDiceResult ? BuildDiceActivityRow(entry, firstDie, secondDie)
                : new Label($"{ResolvePlayerName(entry.playerId)}  {FormatActivityText(entry.text)}");
            row.pickingMode = PickingMode.Ignore;
            row.AddToClassList("activity-feed-item");
            row.AddToClassList("activity-feed-item--" + SanitizeKind(entry.kind));
            // The roll result must survive the near-immediate landing event on narrow screens.
            if (entry.emphasis || (entry.kind != "turn" && entry.kind != "move"))
                row.AddToClassList("activity-feed-item--priority");
            if (entry.emphasis) row.AddToClassList("activity-feed-item--emphasis");
            row.AddToClassList("activity-feed-item--enter");
            activityFeedStack.Insert(0, row);
            TrimActivityRows();

            row.schedule.Execute(() => row.RemoveFromClassList("activity-feed-item--enter")).StartingIn(20);
            long visibleMs = entry.emphasis ? 5600 : 4200;
            row.schedule.Execute(() => row.AddToClassList("activity-feed-item--exit")).StartingIn(visibleMs);
            row.schedule.Execute(() =>
            {
                row.RemoveFromHierarchy();
                RefreshActivityFeedVisibility();
            }).StartingIn(visibleMs + 260);
        }

        private VisualElement BuildDiceActivityRow(GameActivityEntry entry, int firstDie, int secondDie)
        {
            var row = new VisualElement();
            row.AddToClassList("activity-dice-result");
            var player = new Label(ResolvePlayerName(entry.playerId));
            player.AddToClassList("activity-dice-player");
            row.Add(player);

            var dice = new VisualElement();
            dice.AddToClassList("activity-dice-pair");
            dice.Add(MakeDie(firstDie));
            var plus = new Label("+");
            plus.AddToClassList("activity-dice-plus");
            dice.Add(plus);
            dice.Add(MakeDie(secondDie));
            row.Add(dice);

            var total = new Label($"{firstDie + secondDie} STEPS");
            total.AddToClassList("activity-dice-total");
            row.Add(total);
            return row;
        }

        private static Label MakeDie(int value)
        {
            var die = new Label(value.ToString(CultureInfo.InvariantCulture));
            die.AddToClassList("activity-die");
            die.AddToClassList("activity-die--enter");
            die.schedule.Execute(() => die.RemoveFromClassList("activity-die--enter")).StartingIn(20);
            return die;
        }

        private static bool TryReadDice(string text, out int firstDie, out int secondDie)
        {
            firstDie = secondDie = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;
            int start = text.IndexOf(' ');
            int plus = text.IndexOf('+', start >= 0 ? start : 0);
            if (start < 0 || plus < 0) return false;
            int middleDot = text.IndexOf('·', plus + 1);
            if (middleDot < 0) middleDot = text.Length;
            return int.TryParse(text.Substring(start + 1, plus - start - 1).Trim(), NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out firstDie)
                && int.TryParse(text.Substring(plus + 1, middleDot - plus - 1).Trim(), NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out secondDie)
                && firstDie >= 1 && firstDie <= 6 && secondDie >= 1 && secondDie <= 6;
        }

        private void RefreshActivityFeedVisibility()
        {
            if (activityFeed == null) return;
            bool hasActivity = activityFeedStack != null && activityFeedStack.childCount > 0;
            activityFeed.style.display = localPlayerStatusVisible || hasActivity
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        private void TrimActivityRows()
        {
            if (activityFeedStack == null) return;
            while (activityFeedStack.childCount > VisibleActivityCapacity)
            {
                // Preserve a highlighted purchase/power/OPA when a routine event follows it.
                VisualElement removable = null;
                for (int i = activityFeedStack.childCount - 1; i >= 0; i--)
                {
                    var candidate = activityFeedStack[i];
                    if (!candidate.ClassListContains("activity-feed-item--priority"))
                    {
                        removable = candidate;
                        break;
                    }
                }
                (removable ?? activityFeedStack[activityFeedStack.childCount - 1]).RemoveFromHierarchy();
            }

        }

        private int VisibleActivityCapacity
        {
            get
            {
                var root = uiDocument != null ? uiDocument.rootVisualElement : null;
                return root != null && root.ClassListContains("ux-narrow")
                    ? 1
                    : MaxVisibleActivities;
            }
        }

        private string ResolvePlayerName(int playerId)
        {
            var lobbyPlayers = LobbySession.Instance?.CurrentLobby?.players;
            if (lobbyPlayers != null && playerId >= 0 && playerId < lobbyPlayers.Length
                && !string.IsNullOrWhiteSpace(lobbyPlayers[playerId].displayName))
                return lobbyPlayers[playerId].displayName;
            return playerId >= 0 ? $"Player {playerId + 1}" : "Game";
        }

        private void UpdatePlayerRoster()
        {
            if (playerRoster == null || turnManager == null) return;
            var players = turnManager.Players;
            playerRoster.Clear();
            if (players == null || players.Count < 2)
            {
                playerRoster.style.display = DisplayStyle.None;
                return;
            }

            playerRoster.style.display = DisplayStyle.Flex;
            int localPlayerId = OnlineGameController.Instance != null && OnlineGameController.Instance.IsOnlineGame
                ? OnlineGameController.Instance.LocalSeatIndex : 0;
            int activePlayerId = turnManager.CurrentPlayer != null ? turnManager.CurrentPlayer.PlayerId : -1;
            double sgo = turnManager.financeSystem != null ? turnManager.financeSystem.CurrentSgo : 0;

            foreach (var player in players)
            {
                if (player == null) continue;
                if (playerRoster.childCount > 0)
                {
                    var separator = new Label("·");
                    separator.AddToClassList("player-roster-separator");
                    separator.pickingMode = PickingMode.Ignore;
                    playerRoster.Add(separator);
                }
                string displayName = player.PlayerId == localPlayerId
                    ? "Vous"
                    : ShortPlayerName(ResolvePlayerName(player.PlayerId));
                var chip = new VisualElement();
                chip.AddToClassList("player-roster-chip");
                chip.EnableInClassList("player-roster-chip--active", player.PlayerId == activePlayerId);
                chip.EnableInClassList("player-roster-chip--local", player.PlayerId == localPlayerId);
                chip.EnableInClassList("player-roster-chip--bankrupt", player.IsBankrupt);
                var wealthStatus = WealthStatusHelper.GetStatus(player, sgo);
                chip.tooltip = player.IsBankrupt
                    ? displayName + " · BANKRUPT"
                    : displayName + " · " + WealthStatusHelper.GetLabel(wealthStatus);

                var avatar = new Label(GetPlayerInitial(displayName));
                avatar.AddToClassList("player-roster-avatar");
                avatar.style.backgroundColor = player.IsBankrupt
                    ? new Color(0.25f, 0.27f, 0.31f, 1f)
                    : PlayerVisualManager.GetPlayerColor(player.PlayerId);
                chip.Add(avatar);

                var identity = new VisualElement();
                identity.AddToClassList("player-roster-identity");
                var name = new Label(displayName);
                name.AddToClassList("player-roster-name");
                identity.Add(name);
                string statusText = player.IsBankrupt ? "OUT" : WealthStatusHelper.GetLabel(wealthStatus);
                var status = new Label(statusText);
                status.AddToClassList("player-roster-wealth");
                status.AddToClassList("player-roster-wealth--" + statusText.ToLowerInvariant());
                identity.Add(status);
                chip.Add(identity);
                playerRoster.Add(chip);
            }
        }

        private static string GetPlayerInitial(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";
            return name.Trim().Substring(0, 1).ToUpperInvariant();
        }

        private static string ShortPlayerName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "PLAYER";
            string trimmed = name.Trim();
            if (trimmed.StartsWith("Player ", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(trimmed.Substring(7), NumberStyles.Integer, CultureInfo.InvariantCulture, out int playerNumber))
                return "P" + playerNumber.ToString(CultureInfo.InvariantCulture);
            return trimmed.Length <= 7 ? trimmed.ToUpperInvariant() : trimmed.Substring(0, 6).ToUpperInvariant() + "…";
        }

        public static string FormatActivityText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            // Activity entries are also stored in replicated snapshots. Translate at the
            // presentation edge so old/reconnected sessions follow the English UI too.
            if (text == "commence son tour") return "starts their turn";
            if (text.StartsWith("arrive sur ", StringComparison.Ordinal)) return "lands on " + text.Substring(11);
            if (text.StartsWith("passe l’appel d’offres", StringComparison.Ordinal)) return "passes on the tender";
            if (text.StartsWith("passe sur ", StringComparison.Ordinal)) return "passes on " + text.Substring(10);
            if (text == "passe") return "passes";
            if (text.StartsWith("lance une OPA sur ", StringComparison.Ordinal))
                return "launches a hostile takeover of " + text.Substring(18).Replace(" pour ", " for ");
            if (text.StartsWith("lance ", StringComparison.Ordinal)) return "rolls " + text.Substring(6);
            if (text.StartsWith("achète ", StringComparison.Ordinal)) return "buys " + text.Substring(7);
            if (text.StartsWith("paie ", StringComparison.Ordinal))
                return "pays " + text.Substring(5).Replace(" de loyer · ", " in rent · ")
                    .Replace(" et quitte l’Audit fiscal", " and leaves Tax Audit");
            if (text.StartsWith("règle sa dette de ", StringComparison.Ordinal))
                return "settles a debt of " + text.Substring(18);
            if (text.StartsWith("règle ", StringComparison.Ordinal))
                return "pays " + text.Substring(6).Replace(" de loyer · ", " in rent · ");
            if (text.StartsWith("investit ", StringComparison.Ordinal))
                return "invests " + text.Substring(9).Replace(" pour ", " for ").Replace(" tour", " turn");
            if (text.StartsWith("perd son investissement de ", StringComparison.Ordinal))
                return "loses their investment of " + text.Substring(27);
            if (text.StartsWith("récupère ", StringComparison.Ordinal))
                return "receives " + text.Substring(9).Replace(" en Bourse", " from the stock market");
            if (text == "est en faillite") return "is bankrupt";
            if (text.StartsWith("vend les améliorations de ", StringComparison.Ordinal))
                return "sells upgrades on " + text.Substring(26);
            if (text.StartsWith("hypothèque ", StringComparison.Ordinal)) return "mortgages " + text.Substring(11);
            if (text.StartsWith("vend ", StringComparison.Ordinal)) return "sells " + text.Substring(5).Replace(" à la banque", " to the bank");
            if (text.StartsWith("quitte la Marketplace", StringComparison.Ordinal)) return "leaves the Marketplace";
            if (text.StartsWith("annule l’appel d’offres", StringComparison.Ordinal)) return "cancels the tender";
            if (text.StartsWith("met ", StringComparison.Ordinal)) return "lists " + text.Substring(4).Replace(" en appel d’offres", " for tender");
            if (text == "soumet une offre") return "places a bid";
            if (text.StartsWith("remporte ", StringComparison.Ordinal)) return "wins " + text.Substring(9);
            if (text.StartsWith("active ", StringComparison.Ordinal)) return "activates " + text.Substring(7);
            if (text.StartsWith("complète la famille ", StringComparison.Ordinal))
                return "completes the " + text.Substring(20).Replace(" · pouvoir débloqué", " family · power unlocked");
            if (text.StartsWith("cycle ", StringComparison.Ordinal))
                return "market cycle " + text.Substring(6).Replace(" actif pour ", " active for ").Replace(" tours", " turns");
            if (text == "retour à un marché normal") return "returns to a normal market";
            if (text.StartsWith("gagne ", StringComparison.Ordinal)) return "wins " + text.Substring(6).Replace(" au Casino", " at the Casino");
            if (text.StartsWith("perd ", StringComparison.Ordinal)) return "loses " + text.Substring(5).Replace(" au Casino", " at the Casino");
            if (text == "tente de sortir de l’Audit fiscal") return "tries to leave Tax Audit";
            if (text == "peut activer Transit Express") return "can activate Express Transit";
            if (text.StartsWith("utilise Transit Express · case ", StringComparison.Ordinal))
                return "uses Express Transit · tile " + text.Substring(31);
            if (text == "reste sur son Hub") return "stays at their Hub";
            if (text == "reste en Audit fiscal") return "stays in Tax Audit";
            if (text == "utilise sa carte de sortie d’Audit") return "uses a Tax Audit exit card";
            if (text.StartsWith("pioche Venture #", StringComparison.Ordinal)) return "draws Venture #" + text.Substring(16);
            if (text.StartsWith("améliore ", StringComparison.Ordinal)) return "upgrades " + text.Substring(9).Replace(" · niveau ", " · level ");
            if (text.StartsWith("place un embargo sur ", StringComparison.Ordinal)) return "places an embargo on " + text.Substring(21);
            if (text.StartsWith("déclenche un audit ciblé contre Joueur ", StringComparison.Ordinal))
                return "launches a targeted audit against Player " + text.Substring(39);
            if (text.StartsWith("déclenche une grève chez Joueur ", StringComparison.Ordinal))
                return "starts a strike at Player " + text.Substring(32);
            if (text.StartsWith("sabote ", StringComparison.Ordinal)) return "sabotages " + text.Substring(7);
            return text;
        }

        private static string SanitizeKind(string kind)
        {
            if (string.IsNullOrWhiteSpace(kind)) return "info";
            switch (kind.ToLowerInvariant())
            {
                case "purchase":
                case "rent":
                case "upgrade":
                case "tax":
                case "casino":
                case "income":
                case "pass":
                case "audit":
                case "bankruptcy":
                case "power":
                case "family":
                case "marketplace":
                case "venture":
                case "investment":
                case "cycle":
                case "network":
                case "dice":
                    return kind.ToLowerInvariant();
                default:
                    return "info";
            }
        }

        // ─────────────────────────────────────────────────────────────────────

        private void UpdateHumanPlayerUI()
        {
            if (turnManager == null) return;
            int localPlayerId = OnlineGameController.Instance != null && OnlineGameController.Instance.IsOnlineGame
                ? OnlineGameController.Instance.LocalSeatIndex
                : 0;
            var humanPlayer = turnManager.GetPlayerById(localPlayerId);
            if (humanPlayer == null) return;
            UpdateLocalPlayerStatus(humanPlayer);

            // Net Worth = cash + valeur d'achat propriétés
            double netWorth = turnManager.GetNetWorth(humanPlayer);
            UpdateMoneyDisplay(humanPlayer.CurrentBalance, netWorth);

            // Turn x/MAX (tour de plateau global pour le joueur humain)
            int currentTurn = humanPlayer.LapsCompleted + 1;
            string turnStr = $"{currentTurn} / {maxTurns}";
            if (lblTurn != null) lblTurn.text = turnStr;

            // Market state (cycle macro)
            string marketState = (turnManager.financeSystem != null)
                ? turnManager.financeSystem.CurrentMarketState
                : "STABLE";
            if (string.IsNullOrEmpty(marketState)) marketState = "STABLE";
            ApplyMarketStyling(marketState);
        }

        private void UpdateLocalPlayerStatus(PlayerModel player)
        {
            if (lblLocalPlayerStatus == null) return;
            bool hasPharmaImmunity = player.ImmunityTurnsRemaining > 0;
            localPlayerStatusVisible = hasPharmaImmunity;
            lblLocalPlayerStatus.text = hasPharmaImmunity
                ? $"PHARMA IMMUNITY · {player.ImmunityTurnsRemaining} TURN{(player.ImmunityTurnsRemaining == 1 ? string.Empty : "S")}"
                : string.Empty;
            lblLocalPlayerStatus.style.display = hasPharmaImmunity
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            RefreshActivityFeedVisibility();
        }

        private void UpdateMoneyDisplay(double capital, double netWorth)
        {
            if (lblCapital == null && lblNetWorth == null) return;
            if (!moneyInitialized || UIUXRuntime.ReducedMotion)
            {
                if (moneyAnimation != null) StopCoroutine(moneyAnimation);
                moneyAnimation = null;
                moneyInitialized = true;
                shownCapital = targetCapital = capital;
                shownNetWorth = targetNetWorth = netWorth;
                PaintMoney();
                return;
            }

            if (Math.Abs(capital - targetCapital) < 0.005 && Math.Abs(netWorth - targetNetWorth) < 0.005)
                return;

            if (moneyAnimation != null) StopCoroutine(moneyAnimation);
            targetCapital = capital;
            targetNetWorth = netWorth;
            moneyAnimation = StartCoroutine(AnimateMoney());
        }

        private IEnumerator AnimateMoney()
        {
            double startCapital = shownCapital;
            double startNetWorth = shownNetWorth;
            float elapsed = 0f;
            float sincePaint = 0f;

            while (elapsed < MoneyAnimationSeconds && !UIUXRuntime.ReducedMotion)
            {
                float step = Time.unscaledDeltaTime;
                elapsed += step;
                sincePaint += step;
                if (sincePaint >= MoneyPaintIntervalSeconds || elapsed >= MoneyAnimationSeconds)
                {
                    float t = Mathf.Clamp01(elapsed / MoneyAnimationSeconds);
                    double eased = t * t * (3f - 2f * t);
                    shownCapital = startCapital + (targetCapital - startCapital) * eased;
                    shownNetWorth = startNetWorth + (targetNetWorth - startNetWorth) * eased;
                    PaintMoney();
                    sincePaint = 0f;
                }
                yield return null;
            }

            shownCapital = targetCapital;
            shownNetWorth = targetNetWorth;
            PaintMoney();
            moneyAnimation = null;
        }

        private void PaintMoney()
        {
            if (lblCapital != null) lblCapital.text = CurrencyFormatter.FormatExact(shownCapital);
            if (lblNetWorth != null) lblNetWorth.text = CurrencyFormatter.FormatExact(shownNetWorth);
        }

        /// <summary>Applique la couleur néon adaptée selon BOOM/CRASH/WAR/NORMAL/STABLE.</summary>
        private void ApplyMarketStyling(string marketState)
        {
            if (lblMarket == null) return;
            lblMarket.text = "MARCHÉ " + marketState.ToUpperInvariant();

            // Nettoyer
            lblMarket.RemoveFromClassList("top-hud-value--green");
            lblMarket.RemoveFromClassList("top-hud-value--red");
            lblMarket.RemoveFromClassList("top-hud-value--orange");
            lblMarket.RemoveFromClassList("top-hud-value--purple");

            string s = marketState.ToUpperInvariant();
            if      (s.Contains("BOOM"))                       lblMarket.AddToClassList("top-hud-value--green");
            else if (s.Contains("CRASH"))                      lblMarket.AddToClassList("top-hud-value--red");
            else if (s.Contains("WAR"))                        lblMarket.AddToClassList("top-hud-value--orange");
            else                                               lblMarket.AddToClassList("top-hud-value--green");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Notifications transitoires (Marketplace etc.)
        // ─────────────────────────────────────────────────────────────────────

        public void ShowNotification(string message, float duration = 3f)
        {
            if (lblMarket != null) StartCoroutine(NotificationRoutineUIToolkit(message, duration));
        }

        private IEnumerator NotificationRoutineUIToolkit(string message, float duration)
        {
            lblMarket.text = message;
            lblMarket.AddToClassList("top-hud-value--green");
            yield return new WaitForSecondsRealtime(duration);
            UpdateHumanPlayerUI();
        }
    }
}
