using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using PolyRisk.UI;

namespace PolyRisk.Multiplayer
{
    public sealed class LobbySession : MonoBehaviour
    {
        private const string ServerUrlKey = "PolyRisk.LobbyServerUrl";
        private const string CodeKey = "PolyRisk.Lobby.Code";
        private const string PlayerIdKey = "PolyRisk.Lobby.PlayerId";
        private const string TokenKey = "PolyRisk.Lobby.Token";

        public static LobbySession Instance { get; private set; }
        public LobbySnapshotDto CurrentLobby { get; private set; }
        public string LocalPlayerId { get; private set; }
        public bool IsBusy { get; private set; }
        public bool IsHost => CurrentLobby != null && CurrentLobby.hostPlayerId == LocalPlayerId;

        public event Action<LobbySnapshotDto> LobbyChanged;
        public event Action<string> ErrorChanged;
        public event Action<bool> BusyChanged;
        public event Action GameStarted;
        public event Action GameAbandoned;

        private string reconnectToken;
        private Coroutine polling;
        private bool startRaised;
        private int resumeGeneration;
        private bool resumingSavedSession;
        private bool gameAbandoned;
        private bool abandonRequestPending;

        public string ServerUrl
        {
            get => PlayerPrefs.GetString(ServerUrlKey,
                Application.platform == RuntimePlatform.WebGLPlayer
                    ? DefaultServerUrlForPage(Application.absoluteURL) : "http://127.0.0.1:5080").TrimEnd('/');
            set => PlayerPrefs.SetString(ServerUrlKey, (value ?? string.Empty).Trim().TrimEnd('/'));
        }

        public static string DefaultServerUrlForPage(string pageUrl)
        {
            if (Uri.TryCreate(pageUrl, UriKind.Absolute, out var page)
                && (page.Scheme == Uri.UriSchemeHttp || page.Scheme == Uri.UriSchemeHttps)
                && !page.IsLoopback)
                return page.GetLeftPart(UriPartial.Authority);
            return "http://127.0.0.1:5080";
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void ResumeSavedSession()
        {
            if (IsBusy || CurrentLobby != null || resumingSavedSession) return;
            string code = PlayerPrefs.GetString(CodeKey, string.Empty);
            string playerId = PlayerPrefs.GetString(PlayerIdKey, string.Empty);
            string token = PlayerPrefs.GetString(TokenKey, string.Empty);
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(token)) return;

            resumingSavedSession = true;
            StartCoroutine(ResumeSavedRoutine(code, playerId, token, resumeGeneration));
        }

        public static bool IsDefinitiveResumeFailure(long status) =>
            status == 400 || status == 401 || status == 403 || status == 404 || status == 410;

        private IEnumerator ResumeSavedRoutine(string code, string playerId, string token, int generation)
        {
            try
            {
                while (generation == resumeGeneration && CurrentLobby == null)
                {
                    bool failed = false;
                    bool definitiveFailure = false;
                    yield return Post<LobbySnapshotDto>($"/api/lobbies/{code}/snapshot",
                        new SessionRequestDto { playerId = playerId, reconnectToken = token },
                        snapshot =>
                        {
                            if (generation != resumeGeneration) return;
                            LocalPlayerId = playerId;
                            reconnectToken = token;
                            ApplySnapshot(snapshot);
                        },
                        error =>
                        {
                            if (generation != resumeGeneration) return;
                            failed = true;
                            ErrorChanged?.Invoke(error);
                        }, showBusy: false, reportError: false,
                        onStatusFailure: status => definitiveFailure = IsDefinitiveResumeFailure(status));
                    if (generation != resumeGeneration || !failed) yield break;
                    if (definitiveFailure) { ClearSession(); yield break; }
                    yield return new WaitForSecondsRealtime(5f);
                }
            }
            finally
            {
                if (generation == resumeGeneration) resumingSavedSession = false;
            }
        }

        private void CancelSavedResume()
        {
            resumeGeneration++;
            resumingSavedSession = false;
        }

        public void CreateLobby(string displayName)
        {
            if (IsBusy) return;
            CancelSavedResume();
            var request = new CreateLobbyRequestDto { displayName = displayName, maxPlayers = 6 };
            StartCoroutine(Post<LobbySessionResponseDto>("/api/lobbies", request, ApplySession));
        }

        public void JoinLobby(string code, string displayName)
        {
            if (IsBusy) return;
            CancelSavedResume();
            code = (code ?? string.Empty).Trim().ToUpperInvariant();
            var request = new JoinLobbyRequestDto { displayName = displayName };
            StartCoroutine(Post<LobbySessionResponseDto>($"/api/lobbies/{code}/join", request, ApplySession));
        }

        public void SetReady(bool isReady)
        {
            if (CurrentLobby == null || IsBusy) return;
            var request = new SetReadyRequestDto
            {
                playerId = LocalPlayerId,
                reconnectToken = reconnectToken,
                isReady = isReady
            };
            StartCoroutine(Post<LobbySnapshotDto>($"/api/lobbies/{CurrentLobby.code}/ready", request, ApplySnapshot));
        }

        public void StartGame()
        {
            if (CurrentLobby == null || !IsHost || IsBusy) return;
            StartCoroutine(Post<LobbySnapshotDto>($"/api/lobbies/{CurrentLobby.code}/start", Credentials(), ApplySnapshot));
        }

        public void LeaveLobby()
        {
            CancelSavedResume();
            if (CurrentLobby == null) { ClearSession(); return; }
            string code = CurrentLobby.code;
            StartCoroutine(Post<LobbySnapshotDto>($"/api/lobbies/{code}/leave", Credentials(), _ => ClearSession(),
                _ => ClearSession()));
        }

        public void AbandonGame(Action onFailure = null)
        {
            if (CurrentLobby == null || !CanUseGameProtocol() || abandonRequestPending)
            {
                onFailure?.Invoke();
                return;
            }

            abandonRequestPending = true;
            string code = CurrentLobby.code;
            StartCoroutine(Post<LobbySnapshotDto>($"/api/lobbies/{code}/abandon", Credentials(), snapshot =>
            {
                abandonRequestPending = false;
                ApplySnapshot(snapshot);
            }, _ =>
            {
                abandonRequestPending = false;
                onFailure?.Invoke();
            }, showBusy: false));
        }

        public void ClearSessionForAbandonedGame() => ClearSession();

        public void SubmitGameCommand(
            string type,
            string payloadJson,
            long expectedStateVersion,
            string clientCommandId,
            Action<GameCommandDto> onSuccess = null,
            Action<string> onFailure = null)
        {
            if (!CanUseGameProtocol())
            {
                onFailure?.Invoke("Game session unavailable.");
                return;
            }
            var request = new SubmitGameCommandRequestDto
            {
                playerId = LocalPlayerId,
                reconnectToken = reconnectToken,
                expectedStateVersion = expectedStateVersion,
                type = type,
                payloadJson = string.IsNullOrWhiteSpace(payloadJson) ? "{}" : payloadJson,
                clientCommandId = clientCommandId
            };
            StartCoroutine(SubmitGameCommandRoutine(request, onSuccess, onFailure));
        }

        private IEnumerator SubmitGameCommandRoutine(SubmitGameCommandRequestDto request,
            Action<GameCommandDto> onSuccess, Action<string> onFailure)
        {
            if (!IsValidServerUrl(ServerUrl))
            {
                const string validationError = "Adresse serveur invalide. Utilisez une URL http:// ou https://.";
                ErrorChanged?.Invoke(validationError);
                TopHUDWidget.ReportNetworkCommandStatus(validationError);
                onFailure?.Invoke(validationError);
                yield break;
            }

            SetBusy(true);
            string path = $"/api/lobbies/{CurrentLobby.code}/game/commands";
            string finalError = "Network request failed.";
            for (int attempt = 0; attempt < 3; attempt++)
            {
                GameCommandDto response = null;
                long statusCode = 0;
                yield return Post<GameCommandDto>(path, request,
                    value => response = value,
                    error => finalError = error,
                    showBusy: false,
                    reportError: false,
                    onStatusFailure: status => statusCode = status,
                    timeoutSeconds: 5);

                if (response != null)
                {
                    SetBusy(false);
                    ErrorChanged?.Invoke(string.Empty);
                    TopHUDWidget.ReportNetworkCommandStatus(string.Empty);
                    onSuccess?.Invoke(response);
                    yield break;
                }

                bool retryable = statusCode == 0 || statusCode == 408 || statusCode == 429 || statusCode >= 500;
                if (!retryable || attempt == 2) break;
                yield return new WaitForSecondsRealtime(attempt == 0 ? 0.5f : 1f);
            }

            SetBusy(false);
            ErrorChanged?.Invoke(finalError);
            TopHUDWidget.ReportNetworkCommandStatus(finalError);
            onFailure?.Invoke(finalError);
        }

        public void ReportCommandError(string error) => ErrorChanged?.Invoke(error);

        public void PollGameCommands(
            long afterSequence,
            Action<GameCommandBatchDto> onSuccess,
            bool showBusy = false)
        {
            if (!CanUseGameProtocol() || !IsHost) return;
            var request = new PollGameCommandsRequestDto
            {
                playerId = LocalPlayerId,
                reconnectToken = reconnectToken,
                afterSequence = afterSequence
            };
            StartCoroutine(Post<GameCommandBatchDto>(
                $"/api/lobbies/{CurrentLobby.code}/game/commands/poll", request, onSuccess, showBusy: showBusy));
        }

        public void PublishGameState(
            long expectedStateVersion,
            long lastAppliedCommandSequence,
            string payloadJson,
            Action<GameStateDto> onSuccess = null,
            Action<string> onFailure = null)
        {
            if (!CanUseGameProtocol() || !IsHost)
            {
                onFailure?.Invoke("Authoritative session unavailable.");
                return;
            }
            var request = new PublishGameStateRequestDto
            {
                playerId = LocalPlayerId,
                reconnectToken = reconnectToken,
                expectedStateVersion = expectedStateVersion,
                lastAppliedCommandSequence = lastAppliedCommandSequence,
                payloadJson = string.IsNullOrWhiteSpace(payloadJson) ? "{}" : payloadJson
            };
            StartCoroutine(Post<GameStateDto>(
                $"/api/lobbies/{CurrentLobby.code}/game/state", request, onSuccess, onFailure));
        }

        public void FetchGameState(Action<GameStateDto> onSuccess, bool showBusy = false,
            Action<string> onFailure = null)
        {
            if (!CanUseGameProtocol())
            {
                onFailure?.Invoke("Game session unavailable.");
                return;
            }
            StartCoroutine(Post<GameStateDto>(
                $"/api/lobbies/{CurrentLobby.code}/game/state/get", Credentials(), onSuccess, onFailure,
                showBusy: showBusy));
        }

        private void ApplySession(LobbySessionResponseDto session)
        {
            if (session == null || session.lobby == null) return;
            gameAbandoned = false;
            abandonRequestPending = false;
            LocalPlayerId = session.playerId;
            reconnectToken = session.reconnectToken;
            PlayerPrefs.SetString(CodeKey, session.lobby.code);
            PlayerPrefs.SetString(PlayerIdKey, LocalPlayerId);
            PlayerPrefs.SetString(TokenKey, reconnectToken);
            PlayerPrefs.Save();
            ApplySnapshot(session.lobby);
            if (polling == null) polling = StartCoroutine(PollLobby());
        }

        private void ApplySnapshot(LobbySnapshotDto snapshot)
        {
            if (snapshot == null) return;
            if (gameAbandoned
                && !string.Equals(snapshot.status, "abandoned", StringComparison.OrdinalIgnoreCase))
                return;
            if (string.Equals(snapshot.status, "abandoned", StringComparison.OrdinalIgnoreCase))
            {
                if (gameAbandoned) return;
                gameAbandoned = true;
                abandonRequestPending = false;
                ClearSession();
                GameAbandoned?.Invoke();
                return;
            }
            bool changed = CurrentLobby == null || snapshot.version != CurrentLobby.version || snapshot.status != CurrentLobby.status;
            CurrentLobby = snapshot;
            if (changed) LobbyChanged?.Invoke(snapshot);
            if (!startRaised && string.Equals(snapshot.status, "started", StringComparison.OrdinalIgnoreCase))
            {
                startRaised = true;
                GameStarted?.Invoke();
            }
            if (polling == null) polling = StartCoroutine(PollLobby());
        }

        private IEnumerator PollLobby()
        {
            while (CurrentLobby != null)
            {
                // Gameplay snapshots retain their own 1s cadence. Lobby membership
                // needs fewer refreshes once all seats are fixed and the menu is hidden.
                bool started = string.Equals(CurrentLobby.status, "started", StringComparison.OrdinalIgnoreCase);
                yield return new WaitForSecondsRealtime(started ? 1.5f : 1f);
                if (CurrentLobby == null || IsBusy) continue;
                string code = CurrentLobby.code;
                yield return Post<LobbySnapshotDto>($"/api/lobbies/{code}/snapshot", Credentials(), ApplySnapshot,
                    error => ErrorChanged?.Invoke(error), showBusy: false);
            }
            polling = null;
        }

        private SessionRequestDto Credentials() => new SessionRequestDto
        {
            playerId = LocalPlayerId,
            reconnectToken = reconnectToken
        };

        private bool CanUseGameProtocol() =>
            CurrentLobby != null &&
            string.Equals(CurrentLobby.status, "started", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrEmpty(LocalPlayerId) &&
            !string.IsNullOrEmpty(reconnectToken);

        private IEnumerator Post<T>(
            string path,
            object payload,
            Action<T> onSuccess,
            Action<string> onFailure = null,
            bool showBusy = true,
            bool reportError = true,
            Action<long> onStatusFailure = null,
            int timeoutSeconds = 10)
        {
            if (!IsValidServerUrl(ServerUrl))
            {
                const string validationError = "Adresse serveur invalide. Utilisez une URL http:// ou https://.";
                if (reportError) ErrorChanged?.Invoke(validationError);
                onStatusFailure?.Invoke(0);
                onFailure?.Invoke(validationError);
                yield break;
            }
            if (showBusy) SetBusy(true);
            string json = JsonUtility.ToJson(payload);
            using var request = new UnityWebRequest(ServerUrl + path, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = timeoutSeconds;
            yield return request.SendWebRequest();
            if (showBusy) SetBusy(false);

            if (request.result == UnityWebRequest.Result.Success)
            {
                ErrorChanged?.Invoke(string.Empty);
                onSuccess?.Invoke(JsonUtility.FromJson<T>(request.downloadHandler.text));
                yield break;
            }

            string error = request.error;
            try
            {
                var apiError = JsonUtility.FromJson<ApiErrorDto>(request.downloadHandler.text);
                if (apiError != null && !string.IsNullOrWhiteSpace(apiError.error)) error = apiError.error;
            }
            catch { }
            if (reportError) ErrorChanged?.Invoke(error);
            onStatusFailure?.Invoke(request.responseCode);
            onFailure?.Invoke(error);
        }

        public static bool IsValidServerUrl(string value)
        {
            return Uri.TryCreate(value, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                && !string.IsNullOrEmpty(uri.Host)
                && string.IsNullOrEmpty(uri.UserInfo)
                && string.IsNullOrEmpty(uri.Query)
                && string.IsNullOrEmpty(uri.Fragment);
        }

        private void SetBusy(bool busy)
        {
            IsBusy = busy;
            BusyChanged?.Invoke(busy);
        }

        private void ClearSession()
        {
            CancelSavedResume();
            CurrentLobby = null;
            LocalPlayerId = null;
            reconnectToken = null;
            startRaised = false;
            abandonRequestPending = false;
            PlayerPrefs.DeleteKey(CodeKey);
            PlayerPrefs.DeleteKey(PlayerIdKey);
            PlayerPrefs.DeleteKey(TokenKey);
            PlayerPrefs.Save();
            LobbyChanged?.Invoke(null);
        }
    }
}
