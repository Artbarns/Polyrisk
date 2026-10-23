using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PolyRisk.Core;
using PolyRisk.Systems;
using PolyRisk.UI;
using UnityEngine;

namespace PolyRisk.Multiplayer
{
    public sealed class OnlineGameController : MonoBehaviour
    {
        public static OnlineGameController Instance { get; private set; }
        public bool IsOnlineGame { get; private set; }
        public bool IsAuthority { get; private set; }
        public bool HasPendingDebt => pendingBankruptcyPlayerId >= 0 && pendingBankruptcyToken > 0;
        public int LocalSeatIndex { get; private set; } = -1;
        public long StateVersion { get; private set; }

        public event Action GameReady;
        public static event Action PresentationStateApplied;

        private bool beginning;
        private int diceA = -1;
        private int diceB = -1;
        private int pendingVentureCardId = -1;
        private int pendingVenturePlayerId = -1;
        private long pendingVentureToken;
        private long nextVentureToken;
        private long lastPresentedVentureToken;
        private int pendingBankruptcyPlayerId = -1;
        private int pendingBankruptcyCreditorPlayerId = -1;
        private double pendingBankruptcyDebt;
        private long pendingBankruptcyToken;
        private long nextBankruptcyToken;
        private Action<bool> pendingBankruptcyCompletion;
        private DebtSettlementContext pendingBankruptcyContext;
        private long lastAppliedCommandSequence;
        private long commandSubmittedForVersion = -1;
        private string pendingClientCommandId;
        private string pendingClientCommandType;
        private string pendingClientCommandPayload;
        private long pendingClientCommandVersion = -1;
        private bool processingCommand;
        private bool statePublicationPending;
        private long mutationGeneration;
        private long publishedGeneration;
        private string lastPublishedPayload;
        private readonly List<GameActivityEntry> recentActivities = new List<GameActivityEntry>();
        private readonly List<GameEventDto> recentGameEvents = new List<GameEventDto>();
        private long lastSeenActivityId;
        private long lastSeenGameEventId;
        private bool hasSeenGameEventSnapshot;
        private const int MaxReplicatedActivities = 12;
        private const int MaxReplicatedGameEvents = 12;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            GameEvents.OnDiceRolled += HandleDiceRolled;
            GameEvents.OnActivityPublished += HandleActivityPublished;
            GameEvents.OnGameEventPublished += HandleGameEventPublished;
            GameEvents.OnInsolvency += HandleOnlineInsolvency;
            GameEvents.OnBalanceChanged += HandleBalanceMutation;
            GameEvents.OnTurnStarted += HandleTurnMutation;
            GameEvents.OnGameVictory += HandleVictoryMutation;
        }

        private void OnDisable()
        {
            GameEvents.OnDiceRolled -= HandleDiceRolled;
            GameEvents.OnActivityPublished -= HandleActivityPublished;
            GameEvents.OnGameEventPublished -= HandleGameEventPublished;
            GameEvents.OnInsolvency -= HandleOnlineInsolvency;
            GameEvents.OnBalanceChanged -= HandleBalanceMutation;
            GameEvents.OnTurnStarted -= HandleTurnMutation;
            GameEvents.OnGameVictory -= HandleVictoryMutation;
        }

        public void BeginOnlineGame()
        {
            if (beginning || IsOnlineGame) return;
            StartCoroutine(BeginRoutine());
        }

        public void ResetOnlineSessionForExit()
        {
            StopAllCoroutines();
            IsOnlineGame = false;
            IsAuthority = false;
            LocalSeatIndex = -1;
            StateVersion = 0;
            beginning = false;
            diceA = -1;
            diceB = -1;
            pendingVentureCardId = -1;
            pendingVenturePlayerId = -1;
            pendingVentureToken = 0;
            pendingBankruptcyPlayerId = -1;
            pendingBankruptcyCreditorPlayerId = -1;
            pendingBankruptcyDebt = 0;
            pendingBankruptcyToken = 0;
            pendingBankruptcyCompletion = null;
            pendingBankruptcyContext = null;
            processingCommand = false;
            statePublicationPending = false;
            mutationGeneration = 0;
            publishedGeneration = 0;
            lastPublishedPayload = null;
            lastAppliedCommandSequence = 0;
            commandSubmittedForVersion = -1;
            ClearPendingClientCommand();
            recentActivities.Clear();
            lastSeenActivityId = 0;
            recentGameEvents.Clear();
            lastSeenGameEventId = 0;
            hasSeenGameEventSnapshot = false;
        }

        private IEnumerator BeginRoutine()
        {
            beginning = true;
            var session = LobbySession.Instance;
            var lobby = session != null ? session.CurrentLobby : null;
            if (lobby?.players == null || lobby.players.Length < 2)
            {
                beginning = false;
                yield break;
            }

            LocalSeatIndex = Array.FindIndex(lobby.players, player => player.playerId == session.LocalPlayerId);
            IsAuthority = session.IsHost;
            recentActivities.Clear();
            recentGameEvents.Clear();
            statePublicationPending = false;
            mutationGeneration = 0;
            publishedGeneration = 0;
            lastPublishedPayload = null;
            lastSeenActivityId = 0;
            lastSeenGameEventId = 0;
            hasSeenGameEventSnapshot = false;
            ClearPendingClientCommand();
            pendingVentureCardId = -1;
            pendingVenturePlayerId = -1;
            pendingVentureToken = 0;
            nextVentureToken = 0;
            lastPresentedVentureToken = 0;
            pendingBankruptcyPlayerId = -1;
            pendingBankruptcyCreditorPlayerId = -1;
            pendingBankruptcyDebt = 0;
            pendingBankruptcyToken = 0;
            nextBankruptcyToken = 0;
            pendingBankruptcyCompletion = null;
            pendingBankruptcyContext = null;
            if (LocalSeatIndex < 0)
            {
                beginning = false;
                yield break;
            }

            GameBootstrapper bootstrapper = null;
            float deadline = Time.realtimeSinceStartup + 10f;
            while (Time.realtimeSinceStartup < deadline)
            {
                bootstrapper = FindObjectOfType<GameBootstrapper>();
                if (bootstrapper != null && bootstrapper.CanStartDeferredGame) break;
                yield return null;
            }

            if (bootstrapper == null || !bootstrapper.StartDeferredOnlineGame(lobby.players.Length, false))
            {
                beginning = false;
                yield break;
            }

            IsOnlineGame = true;
            beginning = false;
            if (IsAuthority)
                yield return StartCoroutine(InitializeAuthority());
            else
                StartCoroutine(ReplicaPollLoop(waitForFirstState: true));
        }

        private IEnumerator InitializeAuthority()
        {
            bool freshLoopStarted = false;
            while (IsOnlineGame && IsAuthority)
            {
                GameStateDto remote = null;
                bool fetched = false;
                LobbySession.Instance.FetchGameState(state => { remote = state; fetched = true; });
                float deadline = Time.realtimeSinceStartup + 12f;
                while (!fetched && IsOnlineGame && Time.realtimeSinceStartup < deadline)
                    yield return null;
                if (!fetched || remote == null)
                {
                    yield return new WaitForSecondsRealtime(1f);
                    continue;
                }

                if (remote.version > 0)
                {
                    if (!RestoreAuthorityState(remote))
                    {
                        Debug.LogError("[Online] Impossible de restaurer l’état autoritaire ; la partie reste en attente.");
                        yield return new WaitForSecondsRealtime(1f);
                        continue;
                    }
                    if (!TurnManager.Instance.IsGameOver) TurnManager.Instance.ResumeGameLoop();
                    GameReady?.Invoke();
                    StartCoroutine(HostPollLoop());
                    yield break;
                }

                if (!freshLoopStarted)
                {
                    freshLoopStarted = true;
                    TurnManager.Instance.ResumeGameLoop();
                    // Let the new TurnStart reach its first decision before publishing.
                    yield return null;
                    yield return null;
                }
                bool published = false;
                PublishCurrentState(0, 0, _ => published = true);
                deadline = Time.realtimeSinceStartup + 12f;
                while (!published && IsOnlineGame && Time.realtimeSinceStartup < deadline)
                    yield return null;
                if (published)
                {
                    GameReady?.Invoke();
                    StartCoroutine(HostPollLoop());
                    yield break;
                }
                // A lost publish response may already have committed v1. Read it back
                // before retrying, rather than overwriting a running server state.
                yield return new WaitForSecondsRealtime(1f);
            }
        }

        private bool RestoreAuthorityState(GameStateDto state)
        {
            if (state == null || state.version <= 0 || !ApplyState(state)) return false;
            StateVersion = state.version;
            lastAppliedCommandSequence = state.lastAppliedCommandSequence;
            ClearPendingClientCommand();
            processingCommand = false;
            var snapshot = JsonUtility.FromJson<OnlineGameSnapshotDto>(state.payloadJson);
            recentActivities.Clear();
            var history = (snapshot.activities ?? Array.Empty<GameActivityEntry>())
                .Where(entry => entry != null && entry.id > 0).OrderBy(entry => entry.id).ToArray();
            recentActivities.AddRange(history.Skip(Mathf.Max(0, history.Length - MaxReplicatedActivities)));
            var gameEventHistory = (snapshot.gameEvents ?? Array.Empty<GameEventDto>())
                .Where(entry => entry != null && entry.id > 0).OrderBy(entry => entry.id).ToArray();
            recentGameEvents.AddRange(gameEventHistory.Skip(Mathf.Max(0, gameEventHistory.Length - MaxReplicatedGameEvents)));
            GameEvents.AdvanceSequences(snapshot.gameEventSequence, snapshot.correlationSequence);
            lastPublishedPayload = state.payloadJson;
            publishedGeneration = mutationGeneration;
            return true;
        }

        public void PublishCurrentState(long expectedVersion, long appliedSequence, Action<GameStateDto> onSuccess = null)
        {
            if (!IsOnlineGame || !IsAuthority || LobbySession.Instance == null || statePublicationPending) return;
            string payload = JsonUtility.ToJson(CaptureSnapshot());
            statePublicationPending = true;
            StartCoroutine(PublishCapturedState(expectedVersion, appliedSequence, payload, mutationGeneration, onSuccess));
        }

        private IEnumerator PublishCapturedState(long expectedVersion, long appliedSequence, string payload,
            long capturedGeneration, Action<GameStateDto> onSuccess)
        {
            // Retry the captured result, never re-execute the economic action on a lost response.
            while (IsOnlineGame && IsAuthority && LobbySession.Instance != null && LobbySession.Instance.IsHost)
            {
                GameStateDto response = null;
                bool completed = false;
                LobbySession.Instance.PublishGameState(expectedVersion, appliedSequence, payload,
                    state => { response = state; completed = true; }, _ => completed = true);
                while (!completed && IsOnlineGame) yield return null;
                if (response != null)
                {
                    AcceptPublishedState(response, payload, capturedGeneration, onSuccess);
                    yield break;
                }

                GameStateDto remote = null;
                completed = false;
                LobbySession.Instance.FetchGameState(state => { remote = state; completed = true; },
                    onFailure: _ => completed = true);
                while (!completed && IsOnlineGame) yield return null;
                if (remote != null && remote.version > expectedVersion)
                {
                    if (remote.lastAppliedCommandSequence >= appliedSequence && remote.payloadJson == payload)
                    {
                        AcceptPublishedState(remote, payload, capturedGeneration, onSuccess);
                        yield break;
                    }
                    // Another publication won the version race. Resume its committed state;
                    // unacknowledged commands remain in the server queue for normal replay.
                    if (RestoreAuthorityState(remote))
                    {
                        statePublicationPending = false;
                        TurnManager.Instance.ResumeGameLoop();
                        if (remote.lastAppliedCommandSequence >= appliedSequence) onSuccess?.Invoke(remote);
                        else processingCommand = false;
                        yield break;
                    }
                }
                yield return new WaitForSecondsRealtime(1f);
            }
            statePublicationPending = false;
        }

        private void AcceptPublishedState(GameStateDto state, string payload, long capturedGeneration,
            Action<GameStateDto> onSuccess)
        {
            StateVersion = state.version;
            lastAppliedCommandSequence = Math.Max(lastAppliedCommandSequence, state.lastAppliedCommandSequence);
            ClearPendingClientCommand();
            lastPublishedPayload = payload;
            publishedGeneration = capturedGeneration;
            statePublicationPending = false;
            onSuccess?.Invoke(state);
        }

        private bool ShouldPublishAutonomousState()
        {
            var turns = TurnManager.Instance;
            return IsOnlineGame && IsAuthority && !processingCommand && !statePublicationPending
                && mutationGeneration != publishedGeneration && turns != null
                && (turns.IsGameOver || HasPendingDebt || !IsTransientState(turns.CurrentState));
        }

        private void MarkReplicationDirty()
        {
            if (IsOnlineGame && IsAuthority) mutationGeneration++;
        }

        private void HandleBalanceMutation(int playerId, double amount) => MarkReplicationDirty();
        private void HandleTurnMutation(int playerId) => MarkReplicationDirty();
        private void HandleVictoryMutation(PlayerModel winner, string reason) => MarkReplicationDirty();

        public bool TrySubmitLocalCommand(string type, string payloadJson = "{}", bool allowOutOfTurn = false)
        {
            if (!IsOnlineGame) return false;
            var turns = TurnManager.Instance;
            if (turns != null && turns.IsGameOver) return true;
            if (!allowOutOfTurn
                && (turns?.CurrentPlayer == null || turns.CurrentPlayer.PlayerId != LocalSeatIndex)) return true;

            string normalizedPayload = string.IsNullOrWhiteSpace(payloadJson) ? "{}" : payloadJson;
            if (pendingClientCommandVersion == StateVersion && !string.IsNullOrEmpty(pendingClientCommandId)
                && (pendingClientCommandType != type || pendingClientCommandPayload != normalizedPayload))
            {
                LobbySession.Instance?.ReportCommandError(
                    "Une décision précédente attend encore le serveur. Réessayez la même action.");
                return true;
            }
            if (commandSubmittedForVersion == StateVersion) return true;

            if (pendingClientCommandVersion != StateVersion || string.IsNullOrEmpty(pendingClientCommandId))
            {
                pendingClientCommandVersion = StateVersion;
                pendingClientCommandId = Guid.NewGuid().ToString("N");
                pendingClientCommandType = type;
                pendingClientCommandPayload = normalizedPayload;
            }

            long expectedVersion = StateVersion;
            string commandId = pendingClientCommandId;
            commandSubmittedForVersion = StateVersion;
            LobbySession.Instance.SubmitGameCommand(type, normalizedPayload, expectedVersion, commandId,
                onFailure: _ =>
                {
                    if (StateVersion == expectedVersion && commandSubmittedForVersion == expectedVersion)
                        commandSubmittedForVersion = -1;
                });
            return true;
        }

        private void ClearPendingClientCommand()
        {
            commandSubmittedForVersion = -1;
            pendingClientCommandId = null;
            pendingClientCommandType = null;
            pendingClientCommandPayload = null;
            pendingClientCommandVersion = -1;
            TopHUDWidget.ReportNetworkCommandStatus(string.Empty);
        }

        private IEnumerator HostPollLoop()
        {
            while (IsOnlineGame && IsAuthority)
            {
                if (ShouldPublishAutonomousState())
                {
                    string payload = JsonUtility.ToJson(CaptureSnapshot());
                    if (payload == lastPublishedPayload) publishedGeneration = mutationGeneration;
                    else
                    {
                        statePublicationPending = true;
                        StartCoroutine(PublishCapturedState(StateVersion, lastAppliedCommandSequence,
                            payload, mutationGeneration, null));
                    }
                }
                if (!processingCommand && !statePublicationPending)
                    LobbySession.Instance.PollGameCommands(lastAppliedCommandSequence, HandleCommandBatch);
                yield return new WaitForSecondsRealtime(0.25f);
            }
        }

        private void HandleCommandBatch(GameCommandBatchDto batch)
        {
            if (processingCommand || statePublicationPending || batch?.commands == null || batch.commands.Length == 0) return;
            var command = batch.commands.OrderBy(item => item.sequence).First();
            processingCommand = true;
            StartCoroutine(ApplyCommand(command));
        }

        private IEnumerator ApplyCommand(GameCommandDto command)
        {
            var turns = TurnManager.Instance;
            int commandSeat = FindSeat(command.playerId);
            bool validActor = turns?.CurrentPlayer != null && turns.CurrentPlayer.PlayerId == commandSeat;
            bool isTenderResponse = command.type == "marketplace.bid.submit"
                || command.type == "marketplace.bid.skip";
            bool isBankruptcyResponse = command.type != null
                && command.type.StartsWith("bankruptcy.", StringComparison.Ordinal);
            bool validVersion = command.expectedStateVersion == StateVersion
                || (isTenderResponse && command.expectedStateVersion <= StateVersion);
            bool validBankruptcyActor = isBankruptcyResponse
                && commandSeat == pendingBankruptcyPlayerId
                && pendingBankruptcyToken > 0;
            bool accepted = turns != null && !turns.IsGameOver && commandSeat >= 0 && validVersion
                && (validActor || isTenderResponse || validBankruptcyActor);

            if (accepted && command.type == "turn.roll" && turns.CurrentState == TurnManager.TurnState.Planning)
            {
                turns.EndPlanningPhase();
                float deadline = Time.realtimeSinceStartup + 15f;
                while (IsTransientState(turns.CurrentState) && !HasPendingDebt && !turns.IsGameOver
                       && Time.realtimeSinceStartup < deadline)
                    yield return null;
            }
            else if (accepted && command.type == "transit.choose"
                     && turns.CurrentState == TurnManager.TurnState.WaitingForTransitChoice)
            {
                var request = JsonUtility.FromJson<TransitChoiceCommandDto>(command.payloadJson ?? "{}");
                if (request != null) turns.ResolveTransitExpressChoice(request.takeTransit);
                yield return null;
            }
            else if (accepted && command.type == "stock.invest"
                     && turns.CurrentState == TurnManager.TurnState.ResolveTile)
            {
                var player = turns.CurrentPlayer;
                var tile = BoardManager.Instance != null
                    ? BoardManager.Instance.GetTileAt(turns.GetPlayerPosition(player))
                    : null;
                var request = JsonUtility.FromJson<StockInvestmentCommandDto>(command.payloadJson ?? "{}");
                if (tile != null && tile.Type == TileType.StockMarket
                    && request != null && turns.financeSystem != null
                    && turns.financeSystem.InvestInStockMarket(player, request.amount, request.duration))
                {
                    turns.CompleteTileResolution();
                    float deadline = Time.realtimeSinceStartup + 15f;
                    while (IsTransientState(turns.CurrentState) && !HasPendingDebt && !turns.IsGameOver
                       && Time.realtimeSinceStartup < deadline)
                        yield return null;
                }
            }
            else if (accepted && command.type == "action.primary" && turns.CurrentState == TurnManager.TurnState.ResolveTile)
            {
                var player = turns.CurrentPlayer;
                var tile = BoardManager.Instance != null
                    ? BoardManager.Instance.GetTileAt(turns.GetPlayerPosition(player))
                    : null;
                // Marketplace needs its own replicated multi-step protocol. Keep that
                // interaction out of the generic primary action until that tranche.
                if (tile != null && tile.Type == TileType.Venture && turns.rulesEngine?.ventureSystem != null)
                {
                    int cardId = turns.rulesEngine.ventureSystem.DrawCardId(player);
                    if (cardId >= 0)
                    {
                        pendingVentureCardId = cardId;
                        pendingVenturePlayerId = player.PlayerId;
                        pendingVentureToken = ++nextVentureToken;
                    }
                }
                else if (tile != null && tile.Type != TileType.GlobalAuction && turns.rulesEngine != null)
                {
                    turns.rulesEngine.ExecutePrimaryAction(player, tile);
                    float deadline = Time.realtimeSinceStartup + 60f;
                    while (turns.CurrentState == TurnManager.TurnState.ResolveTile
                           && pendingBankruptcyPlayerId < 0
                           && Time.realtimeSinceStartup < deadline)
                        yield return null;
                    while (IsTransientState(turns.CurrentState) && !HasPendingDebt && !turns.IsGameOver
                       && Time.realtimeSinceStartup < deadline)
                        yield return null;
                }
            }
            else if (accepted && command.type == "action.secondary" && turns.CurrentState == TurnManager.TurnState.ResolveTile)
            {
                var player = turns.CurrentPlayer;
                var tile = BoardManager.Instance != null
                    ? BoardManager.Instance.GetTileAt(turns.GetPlayerPosition(player))
                    : null;
                if (tile != null && turns.rulesEngine != null
                    && (tile.Data is PropertyData || tile.Type == TileType.Casino || tile.Type == TileType.StockMarket))
                {
                    // Online auctions are a later multi-step command family. For the
                    // minimal turn slice, the visible PASS action is authoritative.
                    turns.rulesEngine.SkipOptionalAction();
                    float deadline = Time.realtimeSinceStartup + 15f;
                    while (IsTransientState(turns.CurrentState) && !HasPendingDebt && !turns.IsGameOver
                       && Time.realtimeSinceStartup < deadline)
                        yield return null;
                }
            }
            else if (accepted && command.type.StartsWith("audit.", StringComparison.Ordinal)
                     && turns.CurrentState == TurnManager.TurnState.WaitingForAuditDecision)
            {
                switch (command.type)
                {
                    case "audit.pay": turns.AuditPayFine(); break;
                    case "audit.roll": turns.AuditRollDice(); break;
                    case "audit.stay": turns.StayInAudit(); break;
                    case "audit.card": turns.UseEscapeCard(); break;
                }
                yield return null;
                float deadline = Time.realtimeSinceStartup + 15f;
                while (IsTransientState(turns.CurrentState) && !HasPendingDebt && !turns.IsGameOver
                       && Time.realtimeSinceStartup < deadline)
                    yield return null;
            }
            else if (accepted && command.type == "dice.turbo"
                     && turns.CurrentState == TurnManager.TurnState.ChooseTurbo)
            {
                var request = JsonUtility.FromJson<TurboChoiceCommandDto>(command.payloadJson ?? "{}");
                if (request != null) turns.ConfirmTurboChoice(request.choice);
                yield return null;
            }
            else if (accepted && command.type == "dice.adjust"
                     && turns.CurrentState == TurnManager.TurnState.AdjustDice)
            {
                var adjustment = JsonUtility.FromJson<DiceAdjustmentCommandDto>(command.payloadJson ?? "{}");
                turns.ConfirmAutoPrecision(Mathf.Clamp(adjustment != null ? adjustment.delta : 0, -1, 1));
                float deadline = Time.realtimeSinceStartup + 15f;
                while (IsTransientState(turns.CurrentState) && !HasPendingDebt && !turns.IsGameOver
                       && Time.realtimeSinceStartup < deadline)
                    yield return null;
            }
            else if (accepted && command.type == "ultimate.use"
                     && turns.CurrentState == TurnManager.TurnState.Planning)
            {
                var request = JsonUtility.FromJson<UltimateCommandDto>(command.payloadJson ?? "{}");
                var player = turns.CurrentPlayer;
                var abilities = AbilitySystem.Instance;
                if (request != null && abilities != null && player != null)
                {
                    PropertyData targetProperty = null;
                    if (request.targetTileIndex >= 0 && BoardManager.Instance != null)
                        targetProperty = BoardManager.Instance.GetTileAt(request.targetTileIndex)?.Property;
                    switch (request.family)
                    {
                        case "Tech": abilities.Tech_MaJSysteme(player); break;
                        case "Real Estate": abilities.RealEstate_RappelDividendes(player); break;
                        case "Pharma": abilities.Pharma_ProtocoleSanitaire(player); break;
                        case "Luxe": abilities.Luxe_TaxeSomptuaire(player); break;
                        case "Oil": abilities.OilGas_BoomEnergetique(player); break;
                        case "Automobile": abilities.Automobile_MoteurTurbo(player); break;
                        case "Aerospace": abilities.Aerospace_SautQuantique(player, request.targetTileIndex); break;
                        case "Armement": abilities.Armement_CyberSabotage(player, targetProperty); break;
                    }
                }
            }
            else if (accepted && command.type == "venture.confirm"
                     && validActor
                     && turns.CurrentState == TurnManager.TurnState.ResolveTile)
            {
                var request = JsonUtility.FromJson<VentureConfirmCommandDto>(command.payloadJson ?? "{}");
                if (request != null
                    && request.token == pendingVentureToken
                    && pendingVentureCardId >= 0
                    && pendingVenturePlayerId == commandSeat
                    && turns.rulesEngine?.ventureSystem != null)
                {
                    int cardId = pendingVentureCardId;
                    pendingVentureCardId = -1;
                    pendingVenturePlayerId = -1;
                    pendingVentureToken = 0;
                    turns.rulesEngine.ventureSystem.ApplyCardEffect(turns.CurrentPlayer, cardId);
                    yield return null;
                    float deadline = Time.realtimeSinceStartup + 15f;
                    while (IsTransientState(turns.CurrentState) && !HasPendingDebt && !turns.IsGameOver
                       && Time.realtimeSinceStartup < deadline)
                        yield return null;
                }
            }
            else if (accepted && isBankruptcyResponse && validBankruptcyActor)
            {
                var request = JsonUtility.FromJson<BankruptcyCommandDto>(command.payloadJson ?? "{}");
                if (request != null && request.token == pendingBankruptcyToken)
                    ApplyBankruptcyCommand(command.type, request);
            }
            else if (accepted && command.type.StartsWith("marketplace.", StringComparison.Ordinal)
                     && turns.CurrentState == TurnManager.TurnState.ResolveTile
                     && IsCurrentTileMarketplace(turns))
            {
                bool resolved = false;
                var marketplace = UnityEngine.Object.FindObjectOfType<MarketplaceSystem>();
                if (command.type == "marketplace.pass" && validActor)
                {
                    GameEvents.PublishActivity("pass", turns.CurrentPlayer.PlayerId, "quitte la Marketplace");
                    resolved = true;
                }
                else if (command.type == "marketplace.sanction" && validActor)
                {
                    var request = JsonUtility.FromJson<MarketplaceSanctionCommandDto>(command.payloadJson ?? "{}");
                    resolved = TryExecuteMarketplaceSanction(turns.CurrentPlayer, request);
                }
                else if (marketplace != null)
                {
                    var request = JsonUtility.FromJson<MarketplaceTenderCommandDto>(command.payloadJson ?? "{}");
                    switch (command.type)
                    {
                        case "marketplace.tender.begin" when validActor:
                            TryBeginOnlineTender(marketplace, turns.CurrentPlayer, request);
                            break;
                        case "marketplace.bid.skip":
                            TryRecordOnlineBid(marketplace, turns.GetPlayerById(commandSeat), request, skip: true);
                            resolved = marketplace.CurrentAuctionState == MarketplaceSystem.AuctionState.Closing;
                            break;
                        case "marketplace.bid.submit":
                            TryRecordOnlineBid(marketplace, turns.GetPlayerById(commandSeat), request, skip: false);
                            resolved = marketplace.CurrentAuctionState == MarketplaceSystem.AuctionState.Closing;
                            break;
                        case "marketplace.tender.cancel" when validActor:
                            resolved = marketplace.CurrentTenderSeller == turns.CurrentPlayer
                                && marketplace.CancelTender();
                            if (resolved)
                                GameEvents.PublishActivity("marketplace", turns.CurrentPlayer.PlayerId,
                                    "annule l’appel d’offres");
                            break;
                        case "marketplace.tender.seal" when validActor:
                            resolved = TrySealOnlineTender(marketplace, turns.CurrentPlayer, request);
                            break;
                    }
                }

                if (resolved)
                {
                    turns.CompleteTileResolution();
                    float deadline = Time.realtimeSinceStartup + 15f;
                    while (IsTransientState(turns.CurrentState) && !HasPendingDebt && !turns.IsGameOver
                       && Time.realtimeSinceStartup < deadline)
                        yield return null;
                }
            }

            // Publishing also acknowledges rejected or unsupported commands so one bad
            // client cannot block the queue for every following player.
            PublishCurrentState(StateVersion, command.sequence, _ =>
            {
                lastAppliedCommandSequence = Math.Max(lastAppliedCommandSequence, command.sequence);
                processingCommand = false;
                PresentPendingVentureForLocalSeat();
                PresentPendingBankruptcyForLocalSeat();
            });
        }

        private void HandleOnlineInsolvency(
            PlayerModel player,
            double debt,
            PlayerModel creditor,
            Action<bool> completed,
            DebtSettlementContext context)
        {
            if (!IsOnlineGame || !IsAuthority || player == null || debt <= 0) return;
            if (pendingBankruptcyPlayerId >= 0)
            {
                Debug.LogError("[Online] Une liquidation est déjà en cours.");
                return;
            }

            pendingBankruptcyPlayerId = player.PlayerId;
            pendingBankruptcyCreditorPlayerId = creditor != null ? creditor.PlayerId : -1;
            pendingBankruptcyDebt = debt;
            pendingBankruptcyToken = ++nextBankruptcyToken;
            pendingBankruptcyCompletion = completed;
            pendingBankruptcyContext = context;
            PresentPendingBankruptcyForLocalSeat();
        }

        private void ApplyBankruptcyCommand(string commandType, BankruptcyCommandDto request)
        {
            var turns = TurnManager.Instance;
            var player = turns?.GetPlayerById(pendingBankruptcyPlayerId);
            if (player == null || request == null) return;

            bool changed = false;
            PropertyData property = request.tileIndex >= 0 && BoardManager.Instance != null
                ? BoardManager.Instance.GetTileAt(request.tileIndex)?.Property
                : null;
            bool ownsProperty = property != null && player.OwnedProperties.Contains(property);

            switch (commandType)
            {
                case "bankruptcy.upgrades" when ownsProperty && property.CurrentLevel > 0:
                    var upgrades = UnityEngine.Object.FindObjectOfType<UpgradeSystem>();
                    changed = upgrades != null && upgrades.SellAllUpgrades(player, property) > 0;
                    if (changed)
                        GameEvents.PublishActivity("bankruptcy", player.PlayerId,
                            $"vend les améliorations de {property.PropertyName}");
                    break;
                case "bankruptcy.mortgage" when ownsProperty && !property.IsMortgaged && property.CurrentLevel == 0:
                    changed = LiquidationHelper.MortgageProperty(player, property) > 0;
                    if (changed)
                        GameEvents.PublishActivity("bankruptcy", player.PlayerId,
                            $"hypothèque {property.PropertyName}");
                    break;
                case "bankruptcy.sell" when ownsProperty && (property.IsMortgaged || property.CurrentLevel == 0):
                    LiquidationHelper.SellPropertyToBank(player, property);
                    changed = true;
                    GameEvents.PublishActivity("bankruptcy", player.PlayerId,
                        $"vend {property.PropertyName} à la banque");
                    break;
                case "bankruptcy.give_up":
                    ResolvePendingBankruptcy(forceBankruptcy: true);
                    return;
            }

            if (!changed) return;
            bool canPay = player.CurrentBalance >= pendingBankruptcyDebt;
            bool hasNoLiquidableAssets = LiquidationHelper.GetTotalLiquidableValue(player) <= 0;
            if (canPay || hasNoLiquidableAssets)
                ResolvePendingBankruptcy(forceBankruptcy: !canPay);
        }

        private void ResolvePendingBankruptcy(bool forceBankruptcy)
        {
            var turns = TurnManager.Instance;
            var player = turns?.GetPlayerById(pendingBankruptcyPlayerId);
            var creditor = turns?.GetPlayerById(pendingBankruptcyCreditorPlayerId);
            var transactions = TransactionManager.Instance;
            if (player == null || transactions == null) return;

            double debt = pendingBankruptcyDebt;
            var completed = pendingBankruptcyCompletion;
            bool paid = !forceBankruptcy && player.CurrentBalance >= debt
                && (creditor == null
                    ? transactions.DeductMoney(player, debt)
                    : transactions.TransferMoney(player, creditor, debt));

            ClearPendingBankruptcy();
            if (paid)
            {
                GameEvents.PublishActivity("bankruptcy", player.PlayerId,
                    $"règle sa dette de {debt:N0} $", true);
            }
            else
            {
                transactions.ProcessBankruptcyCash(player, creditor);
            }
            completed?.Invoke(paid);
        }

        private void ClearPendingBankruptcy()
        {
            pendingBankruptcyPlayerId = -1;
            pendingBankruptcyCreditorPlayerId = -1;
            pendingBankruptcyDebt = 0;
            pendingBankruptcyToken = 0;
            pendingBankruptcyCompletion = null;
            pendingBankruptcyContext = null;
            var screen = UnityEngine.Object.FindObjectOfType<PolyRisk.UI.BankruptcyScreenController>();
            screen?.HideOnline();
        }

        private int FindSeat(string lobbyPlayerId)
        {
            var players = LobbySession.Instance?.CurrentLobby?.players;
            return players == null ? -1 : Array.FindIndex(players, player => player.playerId == lobbyPlayerId);
        }

        private static bool IsCurrentTileMarketplace(TurnManager turns)
        {
            if (turns?.CurrentPlayer == null || BoardManager.Instance == null) return false;
            var tile = BoardManager.Instance.GetTileAt(turns.GetPlayerPosition(turns.CurrentPlayer));
            return tile != null && tile.Type == TileType.GlobalAuction;
        }

        private static bool TryExecuteMarketplaceSanction(PlayerModel attacker, MarketplaceSanctionCommandDto request)
        {
            var marketplace = UnityEngine.Object.FindObjectOfType<MarketplaceSystem>();
            var turns = TurnManager.Instance;
            if (attacker == null || request == null || marketplace == null || turns == null) return false;

            if (string.Equals(request.sanction, "audit", StringComparison.OrdinalIgnoreCase))
            {
                var target = turns.GetPlayerById(request.targetPlayerId);
                if (target == null || target == attacker || target.IsBankrupt) return false;
                marketplace.ExecuteAudit(attacker, target);
                return true;
            }

            var property = request.targetTileIndex >= 0 && BoardManager.Instance != null
                ? BoardManager.Instance.GetTileAt(request.targetTileIndex)?.Property
                : null;
            var owner = property != null ? turns.GetOwnerOfProperty(property) : null;
            if (property == null || owner == null || owner == attacker || owner.IsBankrupt) return false;

            if (string.Equals(request.sanction, "embargo", StringComparison.OrdinalIgnoreCase))
                marketplace.ExecuteEmbargo(attacker, property);
            else if (string.Equals(request.sanction, "strike", StringComparison.OrdinalIgnoreCase))
                marketplace.ExecuteStrike(attacker, property);
            else if (string.Equals(request.sanction, "sabotage", StringComparison.OrdinalIgnoreCase))
                marketplace.ExecuteSabotage(attacker, property);
            else
                return false;
            return true;
        }

        private static bool TryBeginOnlineTender(
            MarketplaceSystem marketplace, PlayerModel seller, MarketplaceTenderCommandDto request)
        {
            var property = request != null && request.targetTileIndex >= 0 && BoardManager.Instance != null
                ? BoardManager.Instance.GetTileAt(request.targetTileIndex)?.Property
                : null;
            if (seller == null || property == null || !seller.OwnedProperties.Contains(property)) return false;
            marketplace.BeginTender(seller, property);
            GameEvents.PublishActivity("marketplace", seller.PlayerId,
                $"met {property.PropertyName} en appel d’offres", true);
            return true;
        }

        private static bool TryRecordOnlineBid(
            MarketplaceSystem marketplace, PlayerModel bidder, MarketplaceTenderCommandDto request, bool skip)
        {
            if (marketplace == null || bidder == null || request == null
                || marketplace.CurrentTenderSeller == null
                || bidder == marketplace.CurrentTenderSeller
                || bidder.IsBankrupt
                || marketplace.ActiveBids.ContainsKey(bidder.PlayerId)) return false;

            if (skip)
            {
                marketplace.SkipBid(bidder.PlayerId);
                GameEvents.PublishActivity("marketplace", bidder.PlayerId, "passe l’appel d’offres");
                return true;
            }

            if (double.IsNaN(request.cashOffer) || double.IsInfinity(request.cashOffer)
                || request.cashOffer < 0 || request.cashOffer > bidder.CurrentBalance) return false;

            var bid = new MarketplaceSystem.TenderBid
            {
                bidder = bidder,
                cashOffer = request.cashOffer
            };
            var offered = request.offeredTileIndices ?? Array.Empty<int>();
            foreach (int tileIndex in offered.Distinct())
            {
                var property = BoardManager.Instance != null
                    ? BoardManager.Instance.GetTileAt(tileIndex)?.Property
                    : null;
                if (property == null || !bidder.OwnedProperties.Contains(property)
                    || property == marketplace.CurrentTenderProperty) return false;
                bid.propertyOffers.Add(property);
            }

            marketplace.SubmitBid(bidder.PlayerId, bid);
            GameEvents.PublishActivity("marketplace", bidder.PlayerId, "soumet une offre");
            return true;
        }

        private static bool TrySealOnlineTender(
            MarketplaceSystem marketplace, PlayerModel seller, MarketplaceTenderCommandDto request)
        {
            if (marketplace == null || seller == null || request == null
                || marketplace.CurrentTenderSeller != seller
                || marketplace.CurrentAuctionState != MarketplaceSystem.AuctionState.Reviewing
                || !marketplace.ActiveBids.TryGetValue(request.bidderPlayerId, out var bid)
                || bid == null || bid.bidder == null) return false;

            string propertyName = marketplace.CurrentTenderProperty != null
                ? marketplace.CurrentTenderProperty.PropertyName
                : "la propriété";
            if (!marketplace.SealTenderDeal(bid)) return false;
            GameEvents.PublishActivity("marketplace", bid.bidder.PlayerId,
                $"remporte {propertyName}", true);
            return true;
        }

        private static bool IsTransientState(TurnManager.TurnState state) =>
            state == TurnManager.TurnState.TurnStart ||
            state == TurnManager.TurnState.RollDice ||
            state == TurnManager.TurnState.ResolveDice ||
            state == TurnManager.TurnState.MovingPawn ||
            state == TurnManager.TurnState.EndTurn;

        private IEnumerator ReplicaPollLoop(bool waitForFirstState)
        {
            bool firstStateApplied = false;
            while (IsOnlineGame && !IsAuthority)
            {
                LobbySession.Instance.FetchGameState(state =>
                {
                    if (state != null && state.version > StateVersion && ApplyState(state))
                    {
                        StateVersion = state.version;
                        ClearPendingClientCommand();
                        if (!firstStateApplied)
                        {
                            firstStateApplied = true;
                            GameReady?.Invoke();
                        }
                    }
                });

                if (waitForFirstState && !firstStateApplied)
                    yield return new WaitForSecondsRealtime(0.25f);
                else
                    yield return new WaitForSecondsRealtime(1f);
            }
        }

        private OnlineGameSnapshotDto CaptureSnapshot()
        {
            var turns = TurnManager.Instance;
            var lobby = LobbySession.Instance.CurrentLobby;
            return new OnlineGameSnapshotDto
            {
                isGameOver = turns.IsGameOver,
                winnerPlayerId = turns.WinnerPlayerId,
                victoryReason = turns.VictoryReason,
                currentPlayerId = turns.CurrentPlayer != null ? turns.CurrentPlayer.PlayerId : 0,
                turnState = turns.CurrentState.ToString(),
                turnsStarted = turns.TurnsStarted,
                currentSgo = turns.financeSystem != null ? turns.financeSystem.CurrentSgo : 0,
                matchStartedUtcTicks = turns.financeSystem != null ? turns.financeSystem.MatchStartedUtcTicks : 0,
                gameEventSequence = GameEvents.LastGameEventId,
                correlationSequence = GameEvents.LastCorrelationId,
                marketState = turns.financeSystem != null ? turns.financeSystem.CurrentMarketState : "STABLE",
                cycle = CaptureCycleState(),
                hasVentureDeckState = turns.rulesEngine?.ventureSystem != null,
                ventureDeck = turns.rulesEngine?.ventureSystem?.CaptureDeckState(),
                investments = CaptureInvestments(turns),
                diceA = diceA,
                diceB = diceB,
                pendingDiceTotal = turns.PendingDiceTotal,
                turboFirst = turns.TurboFirst,
                turboSecond = turns.TurboSecond,
                consecutiveDoubles = turns.ConsecutiveDoubles,
                hasMovementState = turns.HasPendingMovement,
                movement = turns.CaptureMovementState(),
                justLandedInAudit = turns.IsJustLandedInAudit,
                players = turns.Players.Select(player =>
                {
                    var seat = player.PlayerId >= 0 && player.PlayerId < lobby.players.Length
                        ? lobby.players[player.PlayerId]
                        : null;
                    return new OnlinePlayerStateDto
                    {
                        playerId = player.PlayerId,
                        lobbyPlayerId = seat?.playerId,
                        displayName = seat?.displayName,
                        balance = player.CurrentBalance,
                        isBankrupt = player.IsBankrupt,
                        isInAudit = player.IsInAudit,
                        auditTurnsRemaining = player.AuditTurnsRemaining,
                        lapsCompleted = player.LapsCompleted,
                        turnsStarted = player.TurnsStarted,
                        position = turns.GetPlayerPosition(player),
                        hasGetOutOfAuditCard = player.HasGetOutOfAuditCard,
                        autoPrecisionAvailable = player.AutoPrecisionAvailable,
                        pendingDiceDelta = player.PendingDiceDelta,
                        energy = AbilitySystem.Instance != null ? AbilitySystem.Instance.GetEnergy(player) : 0,
                        hasGetOutOfJailCard = player.HasGetOutOfJailCard,
                        hasGoldenTicket = player.HasGoldenTicket,
                        doubleBurnRateNextGo = player.DoubleBurnRateNextGo,
                        oilGasPenaltyTurns = player.OilGasPenaltyTurns,
                        hasImmunityCard = player.HasImmunityCard,
                        immunityTurnsRemaining = player.ImmunityTurnsRemaining,
                        oilGasBoostTurnsRemaining = player.OilGasBoostTurnsRemaining,
                        autoTurboReady = player.AutoTurboReady
                    };
                }).ToArray(),
                properties = BoardManager.Instance.Tiles
                    .Where(tile => tile.Data is PropertyData)
                    .Select(tile =>
                    {
                        var property = (PropertyData)tile.Data;
                        var owner = turns.GetOwnerOfProperty(property);
                        return new OnlinePropertyStateDto
                        {
                            tileIndex = tile.TileIndex,
                            ownerPlayerId = owner != null ? owner.PlayerId : -1,
                            currentLevel = property.CurrentLevel,
                            isMortgaged = property.IsMortgaged,
                            mortgageExemptFromNextGo = property.MortgageExemptFromNextGo,
                            totalUpgradeCostInvested = property.TotalUpgradeCostInvested,
                            wasVisitedSinceLastGo = property.WasVisitedSinceLastGO,
                            isEmbargoed = property.IsEmbargoed,
                            embargoTurnsRemaining = property.EmbargoTurnsRemaining,
                            hijackedByPlayerId = property.HijackedBy != null ? property.HijackedBy.PlayerId : -1,
                            hijackTurnsRemaining = property.HijackTurnsRemaining
                        };
                    }).ToArray(),
                activities = recentActivities.ToArray(),
                gameEvents = recentGameEvents.ToArray(),
                marketplace = CaptureMarketplaceState(),
                pendingVentureCardId = pendingVentureCardId,
                pendingVenturePlayerId = pendingVenturePlayerId,
                pendingVentureToken = pendingVentureToken,
                bankruptcy = pendingBankruptcyPlayerId >= 0 && pendingBankruptcyToken > 0
                    ? new OnlineBankruptcyStateDto
                    {
                        playerId = pendingBankruptcyPlayerId,
                        creditorPlayerId = pendingBankruptcyCreditorPlayerId,
                        debt = pendingBankruptcyDebt,
                        token = pendingBankruptcyToken,
                        continuation = pendingBankruptcyContext
                    }
                    : null
            };
        }

        private static OnlineCycleStateDto CaptureCycleState()
        {
            var cycle = CycleManager.Instance;
            return new OnlineCycleStateDto
            {
                cycleType = cycle != null ? cycle.CurrentCycle.ToString() : CycleType.NORMAL.ToString(),
                turnsRemaining = cycle != null ? cycle.TurnsRemaining : 0
            };
        }

        private static OnlineStockInvestmentDto[] CaptureInvestments(TurnManager turns)
        {
            if (turns?.financeSystem == null) return Array.Empty<OnlineStockInvestmentDto>();
            return turns.Players.SelectMany(player => turns.financeSystem
                .GetActiveInvestmentsForPlayer(player.PlayerId)
                .Select(investment => new OnlineStockInvestmentDto
                {
                    playerId = player.PlayerId,
                    amount = investment.Amount,
                    turnsRemaining = investment.TurnsRemaining,
                    totalDuration = investment.TotalDuration
                })).ToArray();
        }

        private static OnlineMarketplaceStateDto CaptureMarketplaceState()
        {
            var marketplace = UnityEngine.Object.FindObjectOfType<MarketplaceSystem>();
            if (marketplace == null) return null;
            return new OnlineMarketplaceStateDto
            {
                auctionState = marketplace.CurrentAuctionState.ToString(),
                sellerPlayerId = marketplace.CurrentTenderSeller != null
                    ? marketplace.CurrentTenderSeller.PlayerId
                    : -1,
                propertyTileIndex = marketplace.CurrentTenderProperty != null
                    ? marketplace.CurrentTenderProperty.TileIndex
                    : -1,
                bids = marketplace.ActiveBids.Select(pair => new OnlineMarketplaceBidDto
                {
                    playerId = pair.Key,
                    skipped = pair.Value == null,
                    cashOffer = pair.Value != null ? pair.Value.cashOffer : 0,
                    offeredTileIndices = pair.Value != null && pair.Value.propertyOffers != null
                        ? pair.Value.propertyOffers
                            .Where(property => property != null)
                            .Select(property => property.TileIndex)
                            .ToArray()
                        : Array.Empty<int>()
                }).ToArray()
            };
        }

        private bool ApplyState(GameStateDto state)
        {
            if (state == null || string.IsNullOrWhiteSpace(state.payloadJson) || TurnManager.Instance == null) return false;
            OnlineGameSnapshotDto snapshot;
            try { snapshot = JsonUtility.FromJson<OnlineGameSnapshotDto>(state.payloadJson); }
            catch { return false; }
            if (snapshot == null || snapshot.schemaVersion != 1 || snapshot.players == null) return false;

            if (!TurnManager.IsValidTurboCandidates(snapshot.turboFirst, snapshot.turboSecond,
                    snapshot.turnState == "ChooseTurbo")) return false;
            var turns = TurnManager.Instance;
            bool newTurn = StateVersion == 0 || turns.CurrentPlayer == null
                || turns.CurrentPlayer.PlayerId != snapshot.currentPlayerId
                || turns.TurnsStarted != snapshot.turnsStarted;
            if (snapshot.isGameOver && snapshot.winnerPlayerId >= 0
                && turns.GetPlayerById(snapshot.winnerPlayerId) == null) return false;
            if (snapshot.hasVentureDeckState && (!VentureSystem.IsValidDeckState(snapshot.ventureDeck)
                || turns.rulesEngine?.ventureSystem == null)) return false;
            if (snapshot.hasVentureDeckState)
                turns.rulesEngine.ventureSystem.ApplyReplicatedDeck(snapshot.ventureDeck);
            foreach (var remote in snapshot.players)
            {
                var player = turns.GetPlayerById(remote.playerId);
                if (player == null) continue;
                player.SetBalance(remote.balance);
                player.IsBankrupt = remote.isBankrupt;
                player.IsInAudit = remote.isInAudit;
                player.AuditTurnsRemaining = remote.auditTurnsRemaining;
                player.LapsCompleted = remote.lapsCompleted;
                player.TurnsStarted = Mathf.Max(0, remote.turnsStarted);
                player.HasGetOutOfAuditCard = remote.hasGetOutOfAuditCard;
                player.AutoPrecisionAvailable = remote.autoPrecisionAvailable;
                player.PendingDiceDelta = remote.pendingDiceDelta;
                player.HasGetOutOfJailCard = remote.hasGetOutOfJailCard;
                player.HasGoldenTicket = remote.hasGoldenTicket;
                player.DoubleBurnRateNextGo = remote.doubleBurnRateNextGo;
                player.OilGasPenaltyTurns = remote.oilGasPenaltyTurns;
                player.HasImmunityCard = remote.hasImmunityCard;
                player.ImmunityTurnsRemaining = remote.immunityTurnsRemaining;
                player.OilGasBoostTurnsRemaining = remote.oilGasBoostTurnsRemaining;
                player.AutoTurboReady = remote.autoTurboReady;
                if (AbilitySystem.Instance != null)
                    AbilitySystem.Instance.SetEnergyForReplica(player, remote.energy);
                turns.TeleportPlayer(player, remote.position);
                GameEvents.OnBalanceChanged?.Invoke(player.PlayerId, player.CurrentBalance);
            }

            foreach (var player in turns.Players) player.ClearProperties();
            if (snapshot.properties != null)
            {
                foreach (var remote in snapshot.properties)
                {
                    var tile = BoardManager.Instance.GetTileAt(remote.tileIndex);
                    if (!(tile?.Data is PropertyData property)) continue;
                    property.SetLevel(remote.currentLevel);
                    property.IsMortgaged = remote.isMortgaged;
                    property.MortgageExemptFromNextGo = remote.mortgageExemptFromNextGo;
                    property.TotalUpgradeCostInvested = remote.totalUpgradeCostInvested;
                    property.WasVisitedSinceLastGO = remote.wasVisitedSinceLastGo;
                    property.IsEmbargoed = remote.isEmbargoed;
                    property.EmbargoTurnsRemaining = remote.embargoTurnsRemaining;
                    property.HijackedBy = turns.GetPlayerById(remote.hijackedByPlayerId);
                    property.HijackTurnsRemaining = remote.hijackTurnsRemaining;
                    var owner = turns.GetPlayerById(remote.ownerPlayerId);
                    if (owner != null) owner.AddProperty(property);
                }
            }

            if (snapshot.cycle != null && Enum.TryParse(snapshot.cycle.cycleType, true, out CycleType cycleType))
                CycleManager.Instance?.ApplyReplicatedState(cycleType, snapshot.cycle.turnsRemaining);
            if (turns.financeSystem != null)
            {
                if (snapshot.matchStartedUtcTicks > 0
                    && !turns.financeSystem.ApplyReplicatedMatchClock(snapshot.matchStartedUtcTicks)) return false;
                turns.financeSystem.CurrentSgo = snapshot.currentSgo;
                turns.financeSystem.CurrentMarketState = string.IsNullOrWhiteSpace(snapshot.marketState)
                    ? "STABLE"
                    : snapshot.marketState;
                turns.financeSystem.ClearInvestmentsForReplica();
                if (snapshot.investments != null)
                {
                    foreach (var group in snapshot.investments.GroupBy(item => item.playerId))
                        turns.financeSystem.SetInvestmentsForReplica(group.Key, group.Select(item => new StockInvestment
                        {
                            Amount = item.amount,
                            TurnsRemaining = item.turnsRemaining,
                            TotalDuration = item.totalDuration
                        }));
                }
            }
            if (!turns.ApplyReplicaTurnState(
                    snapshot.currentPlayerId,
                    snapshot.turnState,
                    snapshot.turnsStarted,
                    snapshot.pendingDiceTotal,
                    snapshot.justLandedInAudit,
                    snapshot.consecutiveDoubles)) return false;
            if (!turns.ApplyTurboCandidates(snapshot.turboFirst, snapshot.turboSecond)) return false;
            if (!turns.ApplyReplicatedMovement(snapshot.hasMovementState ? snapshot.movement : null)) return false;
            ApplyMarketplaceState(snapshot.marketplace, turns);
            pendingVentureCardId = snapshot.pendingVentureCardId;
            pendingVenturePlayerId = snapshot.pendingVenturePlayerId;
            pendingVentureToken = snapshot.pendingVentureToken;
            nextVentureToken = Math.Max(nextVentureToken, pendingVentureToken);
            PresentPendingVentureForLocalSeat();
            pendingBankruptcyPlayerId = snapshot.bankruptcy != null ? snapshot.bankruptcy.playerId : -1;
            pendingBankruptcyCreditorPlayerId = snapshot.bankruptcy != null
                ? snapshot.bankruptcy.creditorPlayerId
                : -1;
            pendingBankruptcyDebt = snapshot.bankruptcy != null ? snapshot.bankruptcy.debt : 0;
            pendingBankruptcyToken = snapshot.bankruptcy != null ? snapshot.bankruptcy.token : 0;
            pendingBankruptcyContext = snapshot.bankruptcy?.continuation;
            pendingBankruptcyCompletion = null;
            if (IsAuthority && pendingBankruptcyContext?.kind == "rent")
            {
                var context = pendingBankruptcyContext;
                var debtor = turns.GetPlayerById(pendingBankruptcyPlayerId);
                var creditor = turns.GetPlayerById(pendingBankruptcyCreditorPlayerId);
                var hijacker = turns.GetPlayerById(context.hijackerPlayerId);
                var property = BoardManager.Instance?.GetTileAt(context.propertyTileIndex)?.Property;
                var rules = UnityEngine.Object.FindObjectOfType<RulesEngine>();
                if (debtor != null && creditor != null && property != null && rules != null)
                    pendingBankruptcyCompletion = paid => rules.CompleteRentSettlement(
                        debtor, property, creditor, hijacker, context.creditorBalanceBefore, paid);
            }
            else if (IsAuthority && pendingBankruptcyContext?.kind == "go")
            {
                var debtor = turns.GetPlayerById(pendingBankruptcyPlayerId);
                if (debtor != null)
                    pendingBankruptcyCompletion = paid => turns.ResumeGoDebtSettlement(debtor, paid);
            }
            nextBankruptcyToken = Math.Max(nextBankruptcyToken, pendingBankruptcyToken);
            PresentPendingBankruptcyForLocalSeat();
            diceA = snapshot.diceA;
            diceB = snapshot.diceB;
            if (diceA > 0 && diceB > 0) GameEvents.OnDiceRolled?.Invoke(new[] { diceA, diceB });
            ReplayNewActivities(snapshot.activities);
            ReplayNewGameEvents(snapshot.gameEvents, snapshot.gameEventSequence,
                snapshot.correlationSequence);
            // Turn listeners clear their previous selection. Restore the tile afterwards.
            // An update within the same turn must not reset decision pickers.
            if (!snapshot.isGameOver && newTurn) GameEvents.OnTurnStarted?.Invoke(snapshot.currentPlayerId);
            if (!snapshot.isGameOver && (turns.CurrentState == TurnManager.TurnState.ResolveTile
                || turns.CurrentState == TurnManager.TurnState.WaitingForTransitChoice))
            {
                var current = turns.CurrentPlayer;
                int position = current != null ? turns.GetPlayerPosition(current) : -1;
                var tile = BoardManager.Instance != null ? BoardManager.Instance.GetTileAt(position) : null;
                if (tile != null) GameEvents.OnTileSelected?.Invoke(tile);
            }
            bool applied = turns.ApplyReplicatedOutcome(snapshot.isGameOver,
                snapshot.winnerPlayerId, snapshot.victoryReason);
            if (applied) PresentationStateApplied?.Invoke();
            return applied;
        }

        private void PresentPendingVentureForLocalSeat()
        {
            if (pendingVentureCardId < 0 || pendingVentureToken <= 0
                || pendingVenturePlayerId != LocalSeatIndex
                || lastPresentedVentureToken == pendingVentureToken) return;

            var modal = PolyRisk.UI.VentureCardModalController.Instance;
            if (modal == null) return;
            long token = pendingVentureToken;
            lastPresentedVentureToken = token;
            modal.ShowCard(pendingVentureCardId, () =>
            {
                TrySubmitLocalCommand("venture.confirm",
                    JsonUtility.ToJson(new VentureConfirmCommandDto { token = token }));
            });
        }

        private void PresentPendingBankruptcyForLocalSeat()
        {
            var screen = UnityEngine.Object.FindObjectOfType<PolyRisk.UI.BankruptcyScreenController>();
            if (screen == null) return;
            if (pendingBankruptcyPlayerId < 0 || pendingBankruptcyToken <= 0
                || pendingBankruptcyPlayerId != LocalSeatIndex)
            {
                screen.HideOnline();
                return;
            }

            var player = TurnManager.Instance?.GetPlayerById(pendingBankruptcyPlayerId);
            if (player == null) return;
            screen.ShowOnline(player, pendingBankruptcyDebt, pendingBankruptcyToken,
                (type, tileIndex, token) =>
                {
                    var payload = JsonUtility.ToJson(new BankruptcyCommandDto
                    {
                        token = token,
                        tileIndex = tileIndex
                    });
                    TrySubmitLocalCommand(type, payload);
                });
        }

        private static void ApplyMarketplaceState(OnlineMarketplaceStateDto remote, TurnManager turns)
        {
            var marketplace = UnityEngine.Object.FindObjectOfType<MarketplaceSystem>();
            if (remote == null || marketplace == null || turns == null) return;
            if (!Enum.TryParse(remote.auctionState, out MarketplaceSystem.AuctionState state))
                state = MarketplaceSystem.AuctionState.None;

            var seller = turns.GetPlayerById(remote.sellerPlayerId);
            var property = remote.propertyTileIndex >= 0 && BoardManager.Instance != null
                ? BoardManager.Instance.GetTileAt(remote.propertyTileIndex)?.Property
                : null;
            var bids = new List<KeyValuePair<int, MarketplaceSystem.TenderBid>>();
            foreach (var item in remote.bids ?? Array.Empty<OnlineMarketplaceBidDto>())
            {
                if (item == null) continue;
                if (item.skipped)
                {
                    bids.Add(new KeyValuePair<int, MarketplaceSystem.TenderBid>(item.playerId, null));
                    continue;
                }

                var bidder = turns.GetPlayerById(item.playerId);
                if (bidder == null) continue;
                var bid = new MarketplaceSystem.TenderBid
                {
                    bidder = bidder,
                    cashOffer = item.cashOffer
                };
                foreach (int tileIndex in item.offeredTileIndices ?? Array.Empty<int>())
                {
                    var offered = BoardManager.Instance != null
                        ? BoardManager.Instance.GetTileAt(tileIndex)?.Property
                        : null;
                    if (offered != null) bid.propertyOffers.Add(offered);
                }
                bids.Add(new KeyValuePair<int, MarketplaceSystem.TenderBid>(item.playerId, bid));
            }
            marketplace.ApplyReplicatedTenderState(seller, property, state, bids);
        }

        private void HandleDiceRolled(int[] result)
        {
            if (result == null || result.Length < 2) return;
            diceA = result[0];
            diceB = result[1];
            MarkReplicationDirty();
        }

        private void HandleActivityPublished(GameActivityEntry entry)
        {
            if (!IsAuthority || entry == null || entry.id <= 0) return;
            recentActivities.Add(entry);
            MarkReplicationDirty();
            while (recentActivities.Count > MaxReplicatedActivities)
                recentActivities.RemoveAt(0);
        }

        private void HandleGameEventPublished(GameEvent gameEvent)
        {
            if (!IsOnlineGame || !IsAuthority || gameEvent == null || gameEvent.Id <= 0) return;
            recentGameEvents.Add(GameEventDto.FromGameEvent(gameEvent));
            while (recentGameEvents.Count > MaxReplicatedGameEvents)
                recentGameEvents.RemoveAt(0);
            MarkReplicationDirty();
        }

        private void ReplayNewActivities(GameActivityEntry[] activities)
        {
            if (activities == null || activities.Length == 0) return;
            var unseen = activities
                .Where(item => item != null && item.id > lastSeenActivityId)
                .OrderBy(item => item.id)
                .ToArray();
            if (unseen.Length == 0) return;

            // Joining a running game should reveal context without flooding the screen.
            int start = lastSeenActivityId == 0 ? Mathf.Max(0, unseen.Length - 3) : 0;
            for (int i = start; i < unseen.Length; i++)
                GameEvents.ReplayActivity(unseen[i]);
            lastSeenActivityId = unseen[unseen.Length - 1].id;
        }

        private void ReplayNewGameEvents(GameEventDto[] events, long eventSequence, long correlationSequence)
        {
            GameEvents.AdvanceSequences(eventSequence, correlationSequence);
            var available = (events ?? Array.Empty<GameEventDto>())
                .Where(item => item != null && item.id > 0)
                .OrderBy(item => item.id)
                .ToArray();
            long latestAvailableId = available.Length > 0 ? available[available.Length - 1].id : 0;
            long watermark = Math.Max(eventSequence, latestAvailableId);

            // A newly joined/reloaded client gets the current state, but does not replay stale effects.
            if (!hasSeenGameEventSnapshot)
            {
                hasSeenGameEventSnapshot = true;
                lastSeenGameEventId = Math.Max(lastSeenGameEventId, watermark);
                return;
            }

            foreach (var item in available)
            {
                if (item.id <= lastSeenGameEventId) continue;
                var gameEvent = item.ToGameEvent();
                if (gameEvent == null) continue;
                GameEvents.ReplayGameEvent(gameEvent);
                lastSeenGameEventId = gameEvent.Id;
            }
            lastSeenGameEventId = Math.Max(lastSeenGameEventId, watermark);
        }
    }
}
