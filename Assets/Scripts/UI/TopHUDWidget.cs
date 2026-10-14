using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using PolyRisk.Core;
using PolyRisk.Systems;
using PolyRisk.Multiplayer;

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
        private readonly List<GameActivityEntry> pendingActivities = new List<GameActivityEntry>();
        // Wide layouts show two messages side by side; narrow layouts keep one priority message.
        private const int MaxVisibleActivities = 2;

        // ─────────────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            GameEvents.OnTurnStarted    += OnTurnStarted;
            GameEvents.OnBalanceChanged += OnBalanceChanged;
            GameEvents.OnPropertyPurchased += OnPropertyPurchased;
            GameEvents.OnActivityPublished += OnActivityPublished;
            UpdateHumanPlayerUI();
        }

        private void OnDisable()
        {
            GameEvents.OnTurnStarted    -= OnTurnStarted;
            GameEvents.OnBalanceChanged -= OnBalanceChanged;
            GameEvents.OnPropertyPurchased -= OnPropertyPurchased;
            GameEvents.OnActivityPublished -= OnActivityPublished;
            if (moneyAnimation != null) StopCoroutine(moneyAnimation);
            moneyAnimation = null;
            moneyInitialized = false;
        }

        private void Start()
        {
            TryBindUIToolkit();
            UpdateHumanPlayerUI();
        }

        private void TryBindUIToolkit()
        {
            if (uiDocument == null) return;
            var root = uiDocument.rootVisualElement;
            if (root == null) return;

            // Si le template n'est pas encore monté, on l'instancie
            if (topHudTemplate != null && root.Q<VisualElement>("TopHUD_Bar") == null)
                topHudTemplate.CloneTree(root);

            UIUXRuntime.PrepareRoot(root);
            root.RegisterCallback<GeometryChangedEvent>(_ => TrimActivityRows());

            lblCapital  = root.Q<Label>("Lbl_Capital");
            lblNetWorth = root.Q<Label>("Lbl_NetWorth");
            lblTurn     = root.Q<Label>("Lbl_Turn");
            lblMarket   = root.Q<Label>("Lbl_Market");
            activityFeed = root.Q<VisualElement>("ActivityFeed");
            activityFeedStack = root.Q<VisualElement>("ActivityFeedStack");

            if (activityFeedStack != null && pendingActivities.Count > 0)
            {
                foreach (var entry in pendingActivities.Skip(Mathf.Max(0, pendingActivities.Count - VisibleActivityCapacity)))
                    AddActivityRow(entry);
                pendingActivities.Clear();
            }
        }

        private void OnTurnStarted(int playerId) => UpdateHumanPlayerUI();

        private void OnBalanceChanged(int playerId, double newAmount)
        {
            int localPlayerId = OnlineGameController.Instance != null && OnlineGameController.Instance.IsOnlineGame
                ? OnlineGameController.Instance.LocalSeatIndex
                : 0;
            if (playerId == localPlayerId) UpdateHumanPlayerUI();
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
            activityFeed.style.display = DisplayStyle.Flex;

            var row = new Label($"{ResolvePlayerName(entry.playerId)}  {entry.text}");
            row.pickingMode = PickingMode.Ignore;
            row.AddToClassList("activity-feed-item");
            row.AddToClassList("activity-feed-item--" + SanitizeKind(entry.kind));
            // Decisions must survive the automatic turn/dice/movement notifications.
            if (entry.emphasis || (entry.kind != "turn" && entry.kind != "dice" && entry.kind != "move"))
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
                if (activityFeedStack.childCount == 0)
                    activityFeed.style.display = DisplayStyle.None;
            }).StartingIn(visibleMs + 260);
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
            return playerId >= 0 ? $"Joueur {playerId + 1}" : "Partie";
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

            // Net Worth = cash + valeur d'achat propriétés
            double netWorth = turnManager.GetNetWorth(humanPlayer);
            UpdateMoneyDisplay(humanPlayer.CurrentBalance, netWorth);

            // Turn x/MAX (tour de plateau global pour le joueur humain)
            int currentTurn = humanPlayer.LapsCompleted + 1;
            string turnStr = $"{currentTurn}/{maxTurns}";
            if (lblTurn != null) lblTurn.text = turnStr;

            // Market state (cycle macro)
            string marketState = (turnManager.financeSystem != null)
                ? turnManager.financeSystem.CurrentMarketState
                : "STABLE";
            if (string.IsNullOrEmpty(marketState)) marketState = "STABLE";
            ApplyMarketStyling(marketState);
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
            if (lblCapital != null) lblCapital.text = CurrencyFormatter.Format(shownCapital);
            if (lblNetWorth != null) lblNetWorth.text = CurrencyFormatter.Format(shownNetWorth);
        }

        /// <summary>Applique la couleur néon adaptée selon BOOM/CRASH/WAR/NORMAL/STABLE.</summary>
        private void ApplyMarketStyling(string marketState)
        {
            if (lblMarket == null) return;
            lblMarket.text = marketState;

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
