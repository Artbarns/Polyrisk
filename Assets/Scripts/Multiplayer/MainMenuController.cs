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
        private VisualElement lobbyView;
        private VisualElement playerList;
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
        private LobbyPlayerDto[] renderedPlayers;
        private string renderedLocalPlayerId;

        public static bool SamePlayerRows(LobbyPlayerDto[] previous, LobbyPlayerDto[] current)
        {
            if (previous == null || current == null || previous.Length != current.Length) return false;
            for (int i = 0; i < current.Length; i++)
                if (previous[i].playerId != current[i].playerId || previous[i].displayName != current[i].displayName
                    || previous[i].isHost != current[i].isHost || previous[i].isReady != current[i].isReady)
                    return false;
            return true;
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
            var root = document.rootVisualElement;
            UIUXRuntime.PrepareRoot(root);
            UIUXRuntime.PrepareView(root);

            homeView = root.Q<VisualElement>("HomeView");
            lobbyView = root.Q<VisualElement>("LobbyView");
            playerList = root.Q<VisualElement>("PlayerList");
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

            createButton.clicked += () => LobbySession.Instance.CreateLobby(playerName.value);
            joinButton.clicked += () => LobbySession.Instance.JoinLobby(gameCode.value, playerName.value);
            root.Q<Button>("Btn_LocalGame").clicked += () => StartCoroutine(StartLocalGame());
            readyButton.clicked += ToggleReady;
            startButton.clicked += () => LobbySession.Instance.StartGame();
            root.Q<Button>("Btn_LeaveLobby").clicked += () => LobbySession.Instance.LeaveLobby();

            LobbySession.Instance.LobbyChanged += RenderLobby;
            LobbySession.Instance.ErrorChanged += ShowError;
            LobbySession.Instance.BusyChanged += SetBusy;
            LobbySession.Instance.GameStarted += HandleGameStarted;
            OnlineGameController.Instance.GameReady += HandleOnlineGameReady;

            RenderLobby(LobbySession.Instance.CurrentLobby);
            LobbySession.Instance.ResumeSavedSession();
        }

        private void OnDestroy()
        {
            if (LobbySession.Instance == null) return;
            LobbySession.Instance.LobbyChanged -= RenderLobby;
            LobbySession.Instance.ErrorChanged -= ShowError;
            LobbySession.Instance.BusyChanged -= SetBusy;
            LobbySession.Instance.GameStarted -= HandleGameStarted;
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
            if (!inLobby) { renderedPlayers = null; return; }

            lobbyCode.text = lobby.code;
            lobbyStatus.text = lobby.status == "started" ? "GAME STARTED" : $"{lobby.players.Length}/{lobby.maxPlayers} PLAYERS";
            bool rebuildRows = renderedLocalPlayerId != LobbySession.Instance.LocalPlayerId
                || !SamePlayerRows(renderedPlayers, lobby.players);
            var scroll = playerList as ScrollView;
            var previousOffset = scroll != null ? scroll.scrollOffset : Vector2.zero;
            if (rebuildRows) playerList.Clear();

            foreach (var player in rebuildRows ? lobby.players : System.Array.Empty<LobbyPlayerDto>())
            {
                var row = new VisualElement();
                row.AddToClassList("lobby-player-row");
                if (player.playerId == LobbySession.Instance.LocalPlayerId) row.AddToClassList("lobby-player-row--local");

                var name = new Label(player.displayName + (player.isHost ? "  · HOST" : string.Empty));
                name.AddToClassList("lobby-player-name");
                var state = new Label(player.isReady ? "READY" : "WAITING");
                state.AddToClassList(player.isReady ? "lobby-player-ready" : "lobby-player-waiting");
                row.Add(name);
                row.Add(state);
                playerList.Add(row);
            }
            if (rebuildRows)
            {
                renderedPlayers = lobby.players;
                renderedLocalPlayerId = LobbySession.Instance.LocalPlayerId;
                if (scroll != null) scroll.schedule.Execute(() => scroll.scrollOffset = previousOffset);
            }

            var localPlayer = lobby.players.FirstOrDefault(p => p.playerId == LobbySession.Instance.LocalPlayerId);
            readyButton.text = localPlayer != null && localPlayer.isReady ? "NOT READY" : "I'M READY";
            readyButton.SetEnabled(lobby.status == "waiting");
            bool allReady = lobby.players.Length >= 2 && lobby.players.All(p => p.isReady);
            startButton.style.display = LobbySession.Instance.IsHost ? DisplayStyle.Flex : DisplayStyle.None;
            startButton.SetEnabled(lobby.status == "waiting" && allReady);
        }

        private void HandleGameStarted()
        {
            lobbyStatus.text = "PREPARING THE BOARD…";
            readyButton.SetEnabled(false);
            startButton.SetEnabled(false);
            OnlineGameController.Instance.BeginOnlineGame();
        }

        private void HandleOnlineGameReady()
        {
            document.rootVisualElement.style.display = DisplayStyle.None;
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
