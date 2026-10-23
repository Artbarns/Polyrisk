using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using PolyRisk.Systems;
using PolyRisk.UI;

namespace PolyRisk.Multiplayer
{
    public sealed class MainMenuController : MonoBehaviour
    {
        private UIDocument document;
        private VisualElement homeView;
        private VisualElement responsiveRoot;
        private VisualElement lobbyView;
        private VisualElement pauseView;
        private VisualElement quitConfirmView;
        private Label playerRoster;
        private TextField playerName;
        private TextField gameCode;
        private TextField serverUrl;
        private Label lobbyCode;
        private Label lobbyStatus;
        private Label errorLabel;
        private Button readyButton;
        private Button startButton;
        private Button createButton;
        private Button joinButton;
        private Toggle reduceMotionHome;
        private Toggle reduceMotionLobby;
        private Label quitConfirmMessage;
        private Button confirmQuitButton;
        private Coroutine rosterResponsiveRoutine;
        private bool renderedLandscape;
        private bool returningToTitle;

        public static MainMenuController Instance { get; private set; }

        public static bool SamePlayerRows(LobbyPlayerDto[] previous, LobbyPlayerDto[] current)
        {
            if (previous == null || current == null || previous.Length != current.Length) return false;
            for (int i = 0; i < current.Length; i++)
                if (previous[i].playerId != current[i].playerId || previous[i].displayName != current[i].displayName
                    || previous[i].isHost != current[i].isHost || previous[i].isReady != current[i].isReady)
                    return false;
            return true;
        }

        public static bool ShouldUseDenseLobbyRoster(int playerCount, bool landscape)
            => playerCount > 0 && landscape;

        public static string PreservePlayerNameOnRoster(string displayName)
            => string.IsNullOrEmpty(displayName) ? string.Empty : displayName.Replace(' ', '\u00A0');

        public static string FormatLobbyRoster(LobbyPlayerDto[] players, int playersPerRow)
        {
            if (players == null || players.Length == 0) return string.Empty;

            string[] entries = players.Select(player =>
                $"{PreservePlayerNameOnRoster(player.displayName)}{(player.isHost ? " · HOST" : string.Empty)} · {(player.isReady ? "READY" : "WAITING")}")
                .ToArray();
            int rowSize = playersPerRow <= 0 ? entries.Length : playersPerRow;
            if (rowSize >= entries.Length) return string.Join("    •    ", entries);

            int rowCount = (entries.Length + rowSize - 1) / rowSize;
            return string.Join("\n", Enumerable.Range(0, rowCount)
                .Select(row => string.Join("    •    ", entries.Skip(row * rowSize).Take(rowSize))));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void DeferGameBehindMenu()
        {
            if (!Application.isBatchMode) GameBootstrapper.DeferGameLoop = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureExists()
        {
            if (Application.isBatchMode || FindObjectOfType<MainMenuController>() != null) return;

            var template = Resources.Load<VisualTreeAsset>("MainMenu");
            if (template == null)
            {
                Debug.LogError("[MainMenu] Assets/UI/Resources/MainMenu.uxml is missing.");
                return;
            }

            var go = new GameObject("[Auto] Main Menu");
            var controller = go.AddComponent<MainMenuController>();
            var doc = go.AddComponent<UIDocument>();
            doc.sortingOrder = 1000;
            var anyDoc = FindObjectsOfType<UIDocument>().FirstOrDefault(d => d != doc && d.panelSettings != null);
            if (anyDoc != null) doc.panelSettings = anyDoc.panelSettings;
            controller.document = doc;
            template.CloneTree(doc.rootVisualElement);
            DontDestroyOnLoad(go);

            var sessionObject = new GameObject("[Auto] Lobby Session");
            sessionObject.AddComponent<LobbySession>();
            sessionObject.AddComponent<OnlineGameController>();
        }

        private void Start()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            var root = document.rootVisualElement;
            responsiveRoot = root.Q<VisualElement>(className: "main-menu-overlay") ?? root;
            UIUXRuntime.PrepareRoot(root);
            UIUXRuntime.PrepareView(root);

            homeView = root.Q<VisualElement>("HomeView");
            lobbyView = root.Q<VisualElement>("LobbyView");
            pauseView = root.Q<VisualElement>("PauseView");
            quitConfirmView = root.Q<VisualElement>("QuitConfirmView");
            quitConfirmMessage = root.Q<Label>("Lbl_QuitConfirmMessage");
            confirmQuitButton = root.Q<Button>("Btn_ConfirmQuit");
            playerRoster = root.Q<Label>("Lbl_PlayerRoster");
            playerName = root.Q<TextField>("Input_PlayerName");
            gameCode = root.Q<TextField>("Input_GameCode");
            serverUrl = root.Q<TextField>("Input_ServerUrl");
            lobbyCode = root.Q<Label>("Lbl_LobbyCode");
            lobbyStatus = root.Q<Label>("Lbl_LobbyStatus");
            errorLabel = root.Q<Label>("Lbl_MenuError");
            errorLabel.pickingMode = PickingMode.Ignore;
            readyButton = root.Q<Button>("Btn_Ready");
            startButton = root.Q<Button>("Btn_StartOnline");
            createButton = root.Q<Button>("Btn_CreateGame");
            joinButton = root.Q<Button>("Btn_JoinGame");
            reduceMotionHome = root.Q<Toggle>("Toggle_ReduceMotionHome");
            reduceMotionLobby = root.Q<Toggle>("Toggle_ReduceMotionLobby");

            bool reducedMotion = UIUXRuntime.ReducedMotion;
            reduceMotionHome?.SetValueWithoutNotify(reducedMotion);
            reduceMotionLobby?.SetValueWithoutNotify(reducedMotion);
            reduceMotionHome?.RegisterValueChangedCallback(evt => SetReducedMotion(evt.newValue));
            reduceMotionLobby?.RegisterValueChangedCallback(evt => SetReducedMotion(evt.newValue));

            playerName.value = PlayerPrefs.GetString("PolyRisk.PlayerName", "Player");
            serverUrl.value = LobbySession.Instance.ServerUrl;

            playerName.RegisterValueChangedCallback(evt =>
            {
                PlayerPrefs.SetString("PolyRisk.PlayerName", evt.newValue.Trim());
                ShowError(string.Empty);
            });
            serverUrl.RegisterValueChangedCallback(evt =>
            {
                LobbySession.Instance.ServerUrl = evt.newValue;
                ShowError(string.Empty);
            });
            gameCode.RegisterValueChangedCallback(evt =>
            {
                string normalized = new string((evt.newValue ?? string.Empty)
                    .ToUpperInvariant().Where(char.IsLetterOrDigit).Take(6).ToArray());
                if (normalized != evt.newValue) gameCode.SetValueWithoutNotify(normalized);
                ShowError(string.Empty);
            });

            createButton.clicked += () =>
            {
                string name = playerName.value;
                DismissTextEntry();
                LobbySession.Instance.CreateLobby(name);
            };
            joinButton.clicked += () =>
            {
                string code = gameCode.value;
                string name = playerName.value;
                DismissTextEntry();
                LobbySession.Instance.JoinLobby(code, name);
            };
            root.Q<Button>("Btn_LocalGame").clicked += () =>
            {
                DismissTextEntry();
                StartCoroutine(StartLocalGame());
            };
            readyButton.clicked += ToggleReady;
            startButton.clicked += () => LobbySession.Instance.StartGame();
            root.Q<Button>("Btn_LeaveLobby").clicked += () => LobbySession.Instance.LeaveLobby();
            root.Q<Button>("Btn_ResumeGame").clicked += ResumeGame;
            root.Q<Button>("Btn_QuitGame").clicked += BeginQuitConfirmation;
            root.Q<Button>("Btn_CancelQuit").clicked += CancelQuitConfirmation;
            confirmQuitButton.clicked += ConfirmQuit;

            LobbySession.Instance.LobbyChanged += RenderLobby;
            LobbySession.Instance.ErrorChanged += ShowError;
            LobbySession.Instance.BusyChanged += SetBusy;
            LobbySession.Instance.GameStarted += HandleGameStarted;
            LobbySession.Instance.GameAbandoned += HandleGameAbandoned;
            OnlineGameController.Instance.GameReady += HandleOnlineGameReady;

            RenderLobby(LobbySession.Instance.CurrentLobby);
            LobbySession.Instance.ResumeSavedSession();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (LobbySession.Instance != null)
            {
                LobbySession.Instance.LobbyChanged -= RenderLobby;
                LobbySession.Instance.ErrorChanged -= ShowError;
                LobbySession.Instance.BusyChanged -= SetBusy;
                LobbySession.Instance.GameStarted -= HandleGameStarted;
                LobbySession.Instance.GameAbandoned -= HandleGameAbandoned;
            }
            if (OnlineGameController.Instance != null)
                OnlineGameController.Instance.GameReady -= HandleOnlineGameReady;
        }

        private IEnumerator StartLocalGame()
        {
            SetBusy(true);
            GameBootstrapper bootstrapper = null;
            float deadline = Time.realtimeSinceStartup + 10f;
            while (Time.realtimeSinceStartup < deadline)
            {
                bootstrapper = FindObjectOfType<GameBootstrapper>();
                if (bootstrapper != null && bootstrapper.CanStartDeferredGame) break;
                yield return null;
            }

            if (bootstrapper == null || !bootstrapper.StartDeferredLocalGame())
            {
                SetBusy(false);
                ShowError("The board is still loading. Please try again.");
                yield break;
            }

            document.rootVisualElement.style.display = DisplayStyle.None;
            SetBusy(false);
        }

        private void ToggleReady()
        {
            var lobby = LobbySession.Instance.CurrentLobby;
            var local = lobby?.players?.FirstOrDefault(p => p.playerId == LobbySession.Instance.LocalPlayerId);
            if (local != null) LobbySession.Instance.SetReady(!local.isReady);
        }

        private void SetReducedMotion(bool reduced)
        {
            UIUXRuntime.SetReducedMotion(reduced);
            reduceMotionHome?.SetValueWithoutNotify(reduced);
            reduceMotionLobby?.SetValueWithoutNotify(reduced);
        }

        private void RenderLobby(LobbySnapshotDto lobby)
        {
            bool inLobby = lobby != null;
            homeView.style.display = inLobby ? DisplayStyle.None : DisplayStyle.Flex;
            lobbyView.style.display = inLobby ? DisplayStyle.Flex : DisplayStyle.None;
            if (!inLobby)
            {
                playerRoster.text = string.Empty;
                if (rosterResponsiveRoutine != null)
                {
                    StopCoroutine(rosterResponsiveRoutine);
                    rosterResponsiveRoutine = null;
                }
                return;
            }

            if (rosterResponsiveRoutine == null)
                rosterResponsiveRoutine = StartCoroutine(CheckLobbyViewportRoutine());

            lobbyCode.text = lobby.code;
            lobbyStatus.text = lobby.status == "started" ? "GAME STARTED" : $"{lobby.players.Length}/{lobby.maxPlayers} PLAYERS";
            bool landscape = IsLandscapeViewport();
            bool denseLobbyRoster = lobby.players != null
                && ShouldUseDenseLobbyRoster(lobby.players.Length, landscape);
            renderedLandscape = landscape;
            lobbyView.EnableInClassList("lobby-view--roster-dense", denseLobbyRoster);
            playerRoster.EnableInClassList("lobby-player-list--dense", denseLobbyRoster);
            int playersPerRow = landscape && lobbyView.resolvedStyle.width <= 1000f ? 2 : lobby.players.Length;
            playerRoster.text = FormatLobbyRoster(lobby.players, playersPerRow);

            var localPlayer = lobby.players.FirstOrDefault(p => p.playerId == LobbySession.Instance.LocalPlayerId);
            readyButton.text = localPlayer != null && localPlayer.isReady ? "NOT READY" : "I'M READY";
            readyButton.SetEnabled(lobby.status == "waiting");
            bool allReady = lobby.players.Length >= 2 && lobby.players.All(p => p.isReady);
            startButton.style.display = LobbySession.Instance.IsHost ? DisplayStyle.Flex : DisplayStyle.None;
            startButton.SetEnabled(lobby.status == "waiting" && allReady);
        }

        private IEnumerator CheckLobbyViewportRoutine()
        {
            var interval = new WaitForSecondsRealtime(0.5f);
            while (true)
            {
                yield return interval;
                if (LobbySession.Instance?.CurrentLobby == null)
                {
                    rosterResponsiveRoutine = null;
                    yield break;
                }
                CheckLobbyViewportOrientation();
            }
        }

        private void CheckLobbyViewportOrientation()
        {
            var currentLobby = LobbySession.Instance?.CurrentLobby;
            if (currentLobby == null || responsiveRoot == null) return;

            bool landscape = IsLandscapeViewport();
            if (landscape != renderedLandscape) RenderLobby(currentLobby);
        }

        private bool IsLandscapeViewport()
        {
            if (Screen.width > 0 && Screen.height > 0)
                return Screen.width > Screen.height;

            var root = document.rootVisualElement;
            var viewport = responsiveRoot != null ? responsiveRoot.contentRect : root.contentRect;
            return viewport.width > viewport.height;
        }

        private void HandleGameStarted()
        {
            DismissTextEntry();
            lobbyStatus.text = "PREPARING THE BOARD…";
            readyButton.SetEnabled(false);
            startButton.SetEnabled(false);
            OnlineGameController.Instance.BeginOnlineGame();
        }

        private void HandleOnlineGameReady()
        {
            DismissTextEntry();
            if (rosterResponsiveRoutine != null)
            {
                StopCoroutine(rosterResponsiveRoutine);
                rosterResponsiveRoutine = null;
            }
            document.rootVisualElement.style.display = DisplayStyle.None;
        }

        public void ShowInGameMenu()
        {
            if (document == null) return;

            var root = document.rootVisualElement;
            pauseView ??= root.Q<VisualElement>("PauseView");
            quitConfirmView ??= root.Q<VisualElement>("QuitConfirmView");
            if (pauseView == null || quitConfirmView == null)
            {
                Debug.LogError("[MainMenu] In-game menu views are missing from MainMenu.uxml.");
                return;
            }
            root.style.display = DisplayStyle.Flex;
            homeView.style.display = DisplayStyle.None;
            lobbyView.style.display = DisplayStyle.None;
            quitConfirmView.style.display = DisplayStyle.None;
            pauseView.style.display = DisplayStyle.Flex;
            ShowError(string.Empty);
        }

        private void ResumeGame()
        {
            pauseView.style.display = DisplayStyle.None;
            quitConfirmView.style.display = DisplayStyle.None;
            document.rootVisualElement.style.display = DisplayStyle.None;
        }

        private void BeginQuitConfirmation()
        {
            bool online = OnlineGameController.Instance != null && OnlineGameController.Instance.IsOnlineGame;
            quitConfirmMessage.text = online
                ? "This will end the online match for everyone and return all players to the title screen."
                : "This will reset the local game and return you to the title screen.";
            pauseView.style.display = DisplayStyle.None;
            quitConfirmView.style.display = DisplayStyle.Flex;
            confirmQuitButton.text = online ? "END MATCH" : "QUIT GAME";
            confirmQuitButton.SetEnabled(true);
        }

        private void CancelQuitConfirmation()
        {
            quitConfirmView.style.display = DisplayStyle.None;
            pauseView.style.display = DisplayStyle.Flex;
            confirmQuitButton.SetEnabled(true);
        }

        private void ConfirmQuit()
        {
            bool online = OnlineGameController.Instance != null && OnlineGameController.Instance.IsOnlineGame;
            if (!online)
            {
                ReturnToTitle();
                return;
            }

            confirmQuitButton.SetEnabled(false);
            confirmQuitButton.text = "ENDING MATCH…";
            LobbySession.Instance.AbandonGame(() =>
            {
                confirmQuitButton.SetEnabled(true);
                confirmQuitButton.text = "END MATCH";
            });
        }

        private void HandleGameAbandoned() => ReturnToTitle();

        private void ReturnToTitle()
        {
            if (returningToTitle) return;
            returningToTitle = true;
            OnlineGameController.Instance?.ResetOnlineSessionForExit();
            LobbySession.Instance?.ClearSessionForAbandonedGame();
            GameBootstrapper.DeferGameLoop = true;
            var root = document.rootVisualElement;
            root.style.display = DisplayStyle.Flex;
            homeView.style.display = DisplayStyle.Flex;
            lobbyView.style.display = DisplayStyle.None;
            pauseView.style.display = DisplayStyle.None;
            quitConfirmView.style.display = DisplayStyle.None;
            StartCoroutine(ReturnToTitleRoutine());
        }

        private IEnumerator ReturnToTitleRoutine()
        {
            yield return GameSession.Reload();
            returningToTitle = false;
        }

        private void DismissTextEntry()
        {
            playerName?.Blur();
            gameCode?.Blur();
            serverUrl?.Blur();
        }

        private void SetBusy(bool busy)
        {
            if (createButton == null) return;
            createButton.SetEnabled(!busy);
            joinButton.SetEnabled(!busy);
        }

        private void ShowError(string message)
        {
            if (errorLabel == null) return;
            errorLabel.text = message ?? string.Empty;
            errorLabel.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
