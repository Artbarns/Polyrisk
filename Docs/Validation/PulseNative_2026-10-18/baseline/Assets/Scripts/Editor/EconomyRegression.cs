using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using PolyRisk.Core;
using PolyRisk.Systems;
using PolyRisk.Multiplayer;

namespace PolyRisk.Editor
{
    // Execute with -batchmode -executeMethod PolyRisk.Editor.EconomyRegression.Run.
    // Uses real production classes; no mocked economy or copied formulas.
    public static class EconomyRegression
    {
        private static int assertions;
        private static readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        private static T Component<T>() where T : Component
        {
            var go = new GameObject(typeof(T).Name); owned.Add(go);
            var component = go.AddComponent<T>();
            typeof(T).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.SetValue(null, component);
            return component;
        }
        private static PropertyData Property(double price, FamilyData family = null)
        {
            var p = ScriptableObject.CreateInstance<PropertyData>(); owned.Add(p);
            Set(p, "basePrice", price); Set(p, "baseRent", price * 0.1); Set(p, "familyData", family);
            return p;
        }
        private static void Set(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
        private static void Check(bool condition, string label)
        {
            assertions++;
            if (!condition) throw new Exception("REGRESSION FAILED: " + label);
            Debug.Log("PASS: " + label);
        }
        public static void Run()
        {
            assertions = 0;
            GameBootstrapper.DeferGameLoop = true;
            GameEvents.OnInsolvency = null;
            GameEvents.OnGameVictory = null;
            GameEvents.OnPlayerBankruptcy = null;
            try
            {
                long matchStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
                Check(!FinanceSystem.IsAggressiveGrowthModeAt(matchStart,
                        matchStart + TimeSpan.FromMinutes(90).Ticks),
                    "Aggressive Growth stays off at exactly 90 minutes");
                Check(FinanceSystem.IsAggressiveGrowthModeAt(matchStart,
                        matchStart + TimeSpan.FromMinutes(90).Ticks + 1),
                    "Aggressive Growth activates strictly after 90 minutes");
                Check(FinanceSystem.CalculateSgoGrowth(2, 1, false) == 500
                    && FinanceSystem.CalculateSgoGrowth(6, 1, false) == 166,
                    "standard Sgo growth remains indexed to active player count");
                Check(FinanceSystem.CalculateSgoGrowth(2, 1, true) == 5000
                    && FinanceSystem.CalculateSgoGrowth(6, 1.3, true) == 6500,
                    "Aggressive Growth uses $5,000 per GO and retains the active cycle multiplier");
                Check(FinanceSystem.CalculateLiquidityShieldThreshold(2000, false) == 20000
                    && FinanceSystem.CalculateLiquidityShieldThreshold(2000, true) == 60000,
                    "Liquidity Shield changes from 10×Sgo to 30×Sgo");
                Check(!FinanceSystem.HasLiquidityShield(20000, 2000, false)
                    && FinanceSystem.HasLiquidityShield(20001, 2000, false)
                    && !FinanceSystem.HasLiquidityShield(60000, 2000, true)
                    && FinanceSystem.HasLiquidityShield(60001, 2000, true),
                    "Liquidity Shield keeps the strict greater-than boundary in both modes");
                var matchClockRoundTrip = JsonUtility.FromJson<OnlineGameSnapshotDto>(JsonUtility.ToJson(
                    new OnlineGameSnapshotDto { matchStartedUtcTicks = matchStart }));
                Check(matchClockRoundTrip.matchStartedUtcTicks == matchStart,
                    "match start clock survives online snapshot serialization");

                var oldRows = new[] { new LobbyPlayerDto { playerId = "a", displayName = "Alice", isHost = true } };
                var freshRows = new[] { new LobbyPlayerDto { playerId = "a", displayName = "Alice", isHost = true } };
                Check(MainMenuController.SamePlayerRows(oldRows, freshRows), "unchanged lobby poll retains existing rows");
                freshRows[0].isReady = true;
                Check(!MainMenuController.SamePlayerRows(oldRows, freshRows), "ready change refreshes lobby rows");
                Check(!MainMenuController.SamePlayerRows(oldRows, System.Array.Empty<LobbyPlayerDto>()), "membership change refreshes lobby rows");
                Check(LobbySession.IsValidServerUrl("http://127.0.0.1:5091"), "local HTTP lobby URL accepted");
                Check(LobbySession.IsValidServerUrl("https://play.example.com/api"), "HTTPS lobby URL with base path accepted");
                Check(LobbySession.DefaultServerUrlForPage("https://play.example.com/game/?room=ABCDEF") == "https://play.example.com",
                    "public WebGL defaults to its own HTTPS origin without query or path");
                Check(LobbySession.DefaultServerUrlForPage("http://127.0.0.1:8792/") == "http://127.0.0.1:5080",
                    "local WebGL retains the standalone development API default");
                Check(LobbySession.DefaultServerUrlForPage("") == "http://127.0.0.1:5080",
                    "non-page execution retains the development API default");
                foreach (var invalidUrl in new[] { "", "localhost:5091", "file:///tmp/lobby", "http://127.0.0.1:5091http://127.0.0.1:5091", "https://user:password@example.com", "https://example.com?token=secret", "https://example.com#fragment" })
                    Check(!LobbySession.IsValidServerUrl(invalidUrl), "invalid lobby URL rejected: " + invalidUrl);
                var urlSession = Component<LobbySession>();
                string savedServerUrl = urlSession.ServerUrl;
                try
                {
                    urlSession.ServerUrl = "http://127.0.0.1:5091http://127.0.0.1:5091";
                    string reportedUrlError = null;
                    string callbackUrlError = null;
                    urlSession.ErrorChanged += error => reportedUrlError = error;
                    var postMethod = typeof(LobbySession).GetMethod("Post", BindingFlags.NonPublic | BindingFlags.Instance)
                        .MakeGenericMethod(typeof(LobbySnapshotDto));
                    var invalidRequest = (System.Collections.IEnumerator)postMethod.Invoke(urlSession,
                        new object[] { "/api/lobbies", new CreateLobbyRequestDto(), null,
                            new Action<string>(error => callbackUrlError = error), true, true, null, 10 });
                    Check(!invalidRequest.MoveNext() && !urlSession.IsBusy,
                        "invalid URL completes without request or stuck busy state");
                    Check(!string.IsNullOrEmpty(reportedUrlError) && reportedUrlError == callbackUrlError,
                        "invalid URL reports a recoverable error to UI and caller");
                    var resumeMethod = typeof(LobbySession).GetMethod("ResumeSavedRoutine",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    int resumeRevision = (int)typeof(LobbySession).GetField("resumeGeneration",
                        BindingFlags.NonPublic | BindingFlags.Instance).GetValue(urlSession);
                    var retryResume = (System.Collections.IEnumerator)resumeMethod.Invoke(urlSession,
                        new object[] { "QAONLY", "qa-player", "qa-token", resumeRevision });
                    Check(retryResume.MoveNext(), "saved resume begins its snapshot request");
                    var resumeRequest = (System.Collections.IEnumerator)retryResume.Current;
                    Check(!resumeRequest.MoveNext() && retryResume.MoveNext()
                        && retryResume.Current is WaitForSecondsRealtime,
                        "temporary resume failure schedules a retry rather than clearing session");
                    typeof(LobbySession).GetMethod("CancelSavedResume",
                        BindingFlags.NonPublic | BindingFlags.Instance).Invoke(urlSession, null);
                    Check(!retryResume.MoveNext(), "starting another session cancels the old resume attempt");
                }
                finally { urlSession.ServerUrl = savedServerUrl; }
                foreach (long temporaryStatus in new long[] { 0, 408, 429, 500, 502, 503, 504 })
                    Check(!LobbySession.IsDefinitiveResumeFailure(temporaryStatus), "temporary resume failure preserves credentials: " + temporaryStatus);
                foreach (long rejectedStatus in new long[] { 400, 401, 403, 404, 410 })
                    Check(LobbySession.IsDefinitiveResumeFailure(rejectedStatus), "definitive resume rejection invalidates session: " + rejectedStatus);
                var transactions = Component<TransactionManager>();
                var board = Component<BoardManager>();
                var turns = Component<TurnManager>();
                var finance = Component<FinanceSystem>();
                finance.transactionManager = transactions;
                var family = ScriptableObject.CreateInstance<FamilyData>(); owned.Add(family);
                var first = Property(1000, family); var second = Property(2000, family);
                first.TotalUpgradeCostInvested = 500;
                var player = new PlayerModel(0, 10000, true);
                player.AddProperty(first);
                Check(NetWorth.Calculate(player, new[] { first, second }) == 11250, "partial family has no bonus");
                player.AddProperty(second);
                Check(NetWorth.Calculate(player, new[] { first, second }) == 63250, "full GDD net worth");

                var creditor = new PlayerModel(1, 100, true);
                bool? paid = null;
                transactions.PayRequired(player, 300, creditor, ok => paid = ok);
                Check(paid == true && player.CurrentBalance == 9700 && creditor.CurrentBalance == 400, "debt paid once");

                var poor = new PlayerModel(2, 100, true);
                var collateral = Property(1000); collateral.SetLevel(1); collateral.TotalUpgradeCostInvested = 500;
                poor.AddProperty(collateral);
                transactions.PayRequired(poor, 700, null, ok => paid = ok);
                Check(paid == true && !poor.IsBankrupt && poor.CurrentBalance == 150 && collateral.IsMortgaged,
                    "AI liquidates upgrades and mortgage without UI");
                transactions.PayRequired(poor, 5000, creditor, ok => paid = ok);
                Check(paid == false && poor.IsBankrupt && poor.CurrentBalance == 0 && poor.OwnedProperties.Count == 0,
                    "insolvent AI eliminated and assets returned");
                Check(creditor.CurrentBalance == 550, "creditor receives remaining cash once");
                transactions.ProcessBankruptcyCash(poor, creditor);
                Check(creditor.CurrentBalance == 550, "bankruptcy idempotent");

                var upkeep = Property(1000); upkeep.SetLevel(5); upkeep.WasVisitedSinceLastGO = true;
                var broke = new PlayerModel(3, 100, true); broke.AddProperty(upkeep);
                finance.CurrentSgo = 2000;
                finance.ProcessGoIncome(broke);
                Check(broke.IsBankrupt && broke.CurrentBalance == 0 && !finance.IsSettlingGo,
                    "unaffordable GO maintenance cannot be forgiven");

                var human = new PlayerModel(4, 100);
                var humanProp = Property(1000); humanProp.SetLevel(5); humanProp.WasVisitedSinceLastGO = true;
                human.AddProperty(humanProp);
                Action<bool> resolve = null;
                double debt = 0;
                GameEvents.OnInsolvency = (p, amount, c, callback, context) => { debt = amount; resolve = callback; };
                finance.ProcessGoIncome(human);
                Check(finance.IsSettlingGo && debt == 35000 && resolve != null, "human GO waits for debt decision");
                human.SetBalance(40000);
                transactions.DeductMoney(human, debt); resolve(true);
                Check(!finance.IsSettlingGo && human.CurrentBalance == 5000, "human settlement resumes after payment");
                GameEvents.OnInsolvency = null;

                int victories = 0;
                GameEvents.OnGameVictory = (winner, reason) => victories++;
                var rich = new PlayerModel(0, 9999000, true);
                turns.StartGame(new List<PlayerModel> { rich, creditor });
                finance.ProcessGoIncome(rich);
                Check(turns.IsGameOver && victories == 1, "10M cash stops game");
                turns.DeclareVictory(rich, "duplicate");
                Check(victories == 1, "victory emitted only once");
                var dead = new PlayerModel(5, 0, true) { IsBankrupt = true };
                turns.StartGame(new List<PlayerModel> { poor, dead });
                Check(turns.CheckEndConditions() && turns.IsGameOver, "zero survivors terminates without loop");

                board.tileDataList = new List<BoardTileData> { first, second };
                first.SetLevel(5); first.IsMortgaged = true; first.IsEmbargoed = true;
                board.InitializeStandardBoard();
                var clone = (PropertyData)board.tileDataList[0]; owned.Add(clone); owned.Add(board.tileDataList[1]);
                Check(clone != first && clone.CurrentLevel == 0 && !clone.IsMortgaged && !clone.IsEmbargoed
                    && clone.TotalUpgradeCostInvested == 0, "new game owns clean property instances");
                clone.SetLevel(2);
                Check(first.CurrentLevel == 5, "runtime changes do not mutate source assets");
                // The board normally destroys clones on shutdown; tests own their edit-mode cleanup.
                ((List<PropertyData>)typeof(BoardManager).GetField("runtimeProperties", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(board)).Clear();
                for (int count = 2; count <= 6; count++)
                {
                    turns.StartGame(Enumerable.Range(0, count).Select(i => new PlayerModel(i, 18000, true)).ToList());
                    Check(turns.Players.Count == count && !turns.CheckEndConditions(), $"{count}-player start");
                }
                var cashLeader = new PlayerModel(0, 60000, true) { LapsCompleted = 50 };
                var assetLeader = new PlayerModel(1, 1000, true) { LapsCompleted = 50 };
                assetLeader.AddProperty(clone); assetLeader.AddProperty((PropertyData)board.tileDataList[1]);
                clone.TotalUpgradeCostInvested = 20000;
                PlayerModel actualWinner = null;
                GameEvents.OnGameVictory = (winner, reason) => actualWinner = winner;
                turns.StartGame(new List<PlayerModel> { cashLeader, assetLeader, dead });
                Check(turns.CheckEndConditions() && actualWinner == assetLeader,
                    "lap 50 ranks full net worth and ignores eliminated lap counters");
                turns.StartGame(new List<PlayerModel> { cashLeader, assetLeader });
                actualWinner = null;
                var taxedRich = new PlayerModel(0, 9999000, true);
                var costly = Property(1000); costly.SetLevel(5); costly.WasVisitedSinceLastGO = true;
                taxedRich.AddProperty(costly);
                finance.ProcessGoIncome(taxedRich);
                Check(actualWinner == null && taxedRich.CurrentBalance == 9966000,
                    "gross crossing of 10M before maintenance does not win");
                var venture = Component<VentureSystem>();
                UnityEngine.Random.InitState(1234); venture.InitializeDeck();
                var pile = typeof(VentureSystem).GetField("drawPile", BindingFlags.NonPublic | BindingFlags.Instance);
                var firstDeck = ((List<int>)pile.GetValue(venture)).ToArray();
                UnityEngine.Random.InitState(1234); venture.InitializeDeck();
                Check(firstDeck.SequenceEqual((List<int>)pile.GetValue(venture)), "Venture deck reproducible with seed");
                for (int card = 0; card < 5; card++) venture.DrawCardId(player);
                var deckSnapshot = JsonUtility.FromJson<OnlineGameSnapshotDto>(JsonUtility.ToJson(
                    new OnlineGameSnapshotDto { hasVentureDeckState = true, ventureDeck = venture.CaptureDeckState() }));
                Check(deckSnapshot.ventureDeck.drawPile.Length == 15
                    && deckSnapshot.ventureDeck.discardPile.Length == 5
                    && VentureSystem.IsValidDeckState(deckSnapshot.ventureDeck),
                    "Venture draw and discard piles survive snapshot serialization");
                UnityEngine.Random.InitState(5678); venture.InitializeDeck();
                Check(venture.ApplyReplicatedDeck(deckSnapshot.ventureDeck)
                    && venture.DrawCardId(player) == firstDeck[5],
                    "Venture restore continues original draw order instead of new deck");
                var invalidDeck = new VentureDeckState
                {
                    drawPile = Enumerable.Repeat(1, 20).ToArray(), discardPile = new int[0]
                };
                var deckBeforeInvalid = venture.CaptureDeckState();
                Check(!venture.ApplyReplicatedDeck(invalidDeck)
                    && venture.CaptureDeckState().drawPile.SequenceEqual(deckBeforeInvalid.drawPile),
                    "Venture duplicate cards rejected without modifying live deck");
                var exhaustedDeck = new VentureDeckState
                    { drawPile = new int[0], discardPile = Enumerable.Range(1, 20).ToArray() };
                Check(venture.ApplyReplicatedDeck(exhaustedDeck)
                    && venture.DrawCardId(player) >= 1 && venture.CaptureDeckState().drawPile.Length == 19,
                    "Venture restored exhausted pile reshuffles discard normally");


                var hijacker = new PlayerModel(0, 50, true);
                var landlord = new PlayerModel(1, 0, true);
                var visitor = new PlayerModel(2, 1000, true);
                var sabotaged = Property(1000);
                sabotaged.Initialize("Sabotaged property", 1, TileType.Property);
                sabotaged.HijackedBy = hijacker;
                sabotaged.HijackTurnsRemaining = 3;
                landlord.AddProperty(sabotaged);
                board.tileDataList = new List<BoardTileData> { sabotaged };
                turns.StartGame(new List<PlayerModel> { hijacker, landlord, visitor });
                var rules = Component<RulesEngine>();
                rules.transactionManager = transactions; rules.turnManager = turns;
                rules.financeSystem = finance; rules.boardManager = board;
                rules.upgradeSystem = Component<UpgradeSystem>();
                var tile = new BoardTile(1, TileType.Property, sabotaged);
                rules.ExecutePrimaryAction(hijacker, tile);
                Check(hijacker.IsBankrupt && hijacker.CurrentBalance == 0 && landlord.CurrentBalance == 50,
                    "bankrupt hijacker receives no kickback from their own unpaid rent");
                Check(sabotaged.HijackedBy == null && sabotaged.HijackTurnsRemaining == 0,
                    "bankruptcy clears sabotage on other players' properties");
                Check(sabotaged.WasVisitedSinceLastGO,
                    "partial rent recovered at bankruptcy counts as income for maintenance");
                rules.ExecutePrimaryAction(visitor, tile);
                Check(visitor.CurrentBalance == 900 && landlord.CurrentBalance == 150 && hijacker.CurrentBalance == 0,
                    "later rent goes entirely to the owner after hijacker elimination");
                // A restored continuation receives a debt that was already collected.
                // It must only apply the diversion and visit marker, never invoice again.
                var restoredDebtor = new PlayerModel(3, 400, false);
                var restoredOwner = new PlayerModel(4, 1100, false);
                var restoredHijacker = new PlayerModel(5, 200, false);
                var restoredProperty = Property(1000);
                double settlementCash = restoredDebtor.CurrentBalance
                    + restoredOwner.CurrentBalance + restoredHijacker.CurrentBalance;
                rules.CompleteRentSettlement(restoredDebtor, restoredProperty,
                    restoredOwner, restoredHijacker, 1000, true);
                Check(restoredDebtor.CurrentBalance == 400
                    && restoredOwner.CurrentBalance == 1075 && restoredHijacker.CurrentBalance == 225,
                    "restored rent continuation splits collected debt without charging debtor again");
                Check(restoredProperty.WasVisitedSinceLastGO
                    && restoredDebtor.CurrentBalance + restoredOwner.CurrentBalance
                        + restoredHijacker.CurrentBalance == settlementCash,
                    "restored rent continuation marks visit and conserves cash");
                double goCash = restoredDebtor.CurrentBalance;
                finance.CompleteGoSettlement(restoredDebtor, true);
                Check(!finance.IsSettlingGo && restoredDebtor.CurrentBalance == goCash,
                    "restored GO completion does not repeat income or maintenance");
                turns.financeSystem = finance;
                long beforeNewGameClock = DateTime.UtcNow.Ticks;
                turns.StartGame(new List<PlayerModel> { restoredDebtor, restoredOwner, restoredHijacker });
                Check(finance.MatchStartedUtcTicks >= beforeNewGameClock,
                    "starting a fresh game initializes its persistent UTC match clock");
                restoredDebtor.OilGasBoostTurnsRemaining = 2;
                restoredDebtor.ImmunityTurnsRemaining = 1;
                restoredDebtor.HasImmunityCard = true;
                int lapsBeforeResume = restoredDebtor.LapsCompleted;
                double sgoBeforeResume = finance.CurrentSgo;
                turns.ApplyReplicaTurnState(restoredDebtor.PlayerId, "MovingPawn", 3, 7, false);
                Check(turns.ResumeGoDebtSettlement(restoredDebtor, true)
                    && turns.CurrentState == TurnManager.TurnState.ResolveTile && turns.PendingDiceTotal == 0,
                    "restored GO debt resumes arrival rather than waiting for lost coroutine");
                Check(restoredDebtor.CurrentBalance == goCash
                    && restoredDebtor.LapsCompleted == lapsBeforeResume
                    && finance.CurrentSgo == sgoBeforeResume,
                    "restored GO debt does not repeat cash, lap or inflation");
                Check(restoredDebtor.OilGasBoostTurnsRemaining == 1
                    && restoredDebtor.ImmunityTurnsRemaining == 0 && !restoredDebtor.HasImmunityCard,
                    "restored GO debt applies remaining buff durations");
                Check(!turns.ResumeGoDebtSettlement(restoredDebtor, true)
                    && restoredDebtor.OilGasBoostTurnsRemaining == 1,
                    "duplicate GO resume is rejected after arrival");
                var online = Component<OnlineGameController>();
                typeof(OnlineGameController).GetField("<IsAuthority>k__BackingField",
                    BindingFlags.Instance | BindingFlags.NonPublic).SetValue(online, true);
                var restoredServerState = new GameStateDto
                {
                    version = 50, lastAppliedCommandSequence = 19,
                    payloadJson = JsonUtility.ToJson(new OnlineGameSnapshotDto
                    {
                        currentPlayerId = restoredDebtor.PlayerId, turnState = "Planning",
                        currentSgo = 2300, turnsStarted = 12,
                        players = new[] { new OnlinePlayerStateDto
                            { playerId = restoredDebtor.PlayerId, balance = 5000, position = 7 } },
                        activities = Enumerable.Range(1, 12).Select(id =>
                            new GameActivityEntry { id = id, kind = "turn", text = "historique" }).ToArray()
                    })
                };
                var restoreAuthority = typeof(OnlineGameController).GetMethod("RestoreAuthorityState",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Check((bool)restoreAuthority.Invoke(online, new object[] { restoredServerState })
                    && online.StateVersion == 50 && restoredDebtor.CurrentBalance == 5000
                    && turns.GetPlayerPosition(restoredDebtor) == 7,
                    "authority startup restores server version, cash and position");
                Check(!turns.HasPendingMovement,
                    "absent movement metadata remains absent after Unity JSON round trip");
                Check((long)typeof(OnlineGameController).GetField("lastAppliedCommandSequence",
                    BindingFlags.Instance | BindingFlags.NonPublic).GetValue(online) == 19,
                    "authority startup resumes commands after last acknowledged sequence");
                var history = (List<GameActivityEntry>)typeof(OnlineGameController).GetField("recentActivities",
                    BindingFlags.Instance | BindingFlags.NonPublic).GetValue(online);
                Check(history.Count == 12 && history[0].id == 1 && history[11].id == 12,
                    "authority startup preserves full activity history beyond last three UI messages");
                Check(!(bool)restoreAuthority.Invoke(online, new object[] { new GameStateDto
                    { version = 51, payloadJson = "invalid" } }) && online.StateVersion == 50,
                    "authority startup rejects unreadable server state without acknowledging it");
                turns.rulesEngine = rules;
                rules.ventureSystem = venture;
                var restoredWithDeck = JsonUtility.FromJson<OnlineGameSnapshotDto>(restoredServerState.payloadJson);
                restoredWithDeck.hasVentureDeckState = true;
                restoredWithDeck.ventureDeck = deckSnapshot.ventureDeck;
                venture.InitializeDeck();
                Check((bool)restoreAuthority.Invoke(online, new object[] { new GameStateDto
                    { version = 52, lastAppliedCommandSequence = 19,
                        payloadJson = JsonUtility.ToJson(restoredWithDeck) } })
                    && venture.DrawCardId(restoredDebtor) == firstDeck[5],
                    "authority snapshot restores Venture deck through production apply path");
                restoredWithDeck.ventureDeck = invalidDeck;
                double balanceBeforeInvalidDeck = restoredDebtor.CurrentBalance;
                restoredWithDeck.players[0].balance = 1;
                Check(!(bool)restoreAuthority.Invoke(online, new object[] { new GameStateDto
                    { version = 53, payloadJson = JsonUtility.ToJson(restoredWithDeck) } })
                    && online.StateVersion == 52 && restoredDebtor.CurrentBalance == balanceBeforeInvalidDeck,
                    "invalid Venture snapshot rejected before changing player balances");

                var selectedOrder = new List<string>();
                BoardTile selectedAfterApply = null;
                Action<int> clearSelection = id => { selectedOrder.Add("turn"); selectedAfterApply = null; };
                Action<BoardTile> restoreSelection = selected => { selectedOrder.Add("tile"); selectedAfterApply = selected; };
                var selectionTile = new BoardTile(1, TileType.Property, sabotaged);
                typeof(BoardManager).GetField("tiles", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(board, new List<BoardTile> { new BoardTile(0, TileType.Go_Start, null), selectionTile });
                GameEvents.OnTurnStarted += clearSelection;
                GameEvents.OnTileSelected += restoreSelection;
                try
                {
                    var selectionPayload = JsonUtility.ToJson(new OnlineGameSnapshotDto
                    {
                        currentPlayerId = restoredDebtor.PlayerId, turnState = "ResolveTile", turnsStarted = 13,
                        players = new[] { new OnlinePlayerStateDto
                            { playerId = restoredDebtor.PlayerId, balance = 5000, position = 1 } }
                    });
                    Check((bool)restoreAuthority.Invoke(online, new object[] { new GameStateDto
                        { version = 54, payloadJson = selectionPayload } })
                        && selectedOrder.SequenceEqual(new[] { "turn", "tile" }) && selectedAfterApply == selectionTile,
                        "replica restores tile selection after the new-turn reset");
                    selectedOrder.Clear();
                    Check((bool)restoreAuthority.Invoke(online, new object[] { new GameStateDto
                        { version = 55, payloadJson = selectionPayload } })
                        && selectedOrder.SequenceEqual(new[] { "tile" }) && selectedAfterApply == selectionTile,
                        "same-turn replica update does not reset selection or decision pickers");
                }
                finally
                {
                    GameEvents.OnTurnStarted -= clearSelection;
                    GameEvents.OnTileSelected -= restoreSelection;
                }
                restoredDebtor.AutoPrecisionAvailable = true;
                turns.ApplyReplicaTurnState(restoredDebtor.PlayerId, "AdjustDice", 12, 7, false, 2);
                var waitChoice = (System.Collections.IEnumerator)typeof(TurnManager)
                    .GetMethod("WaitForAutoPrecisionChoice", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(turns, new object[] { restoredDebtor });
                Check(waitChoice.MoveNext() && turns.PendingDiceTotal == 7,
                    "restored precision choice waits with original dice total");
                Check(turns.ConfirmAutoPrecision(0) && !waitChoice.MoveNext()
                    && turns.CurrentState == TurnManager.TurnState.RollDice
                    && turns.PendingDiceTotal == 7 && turns.ConsecutiveDoubles == 2,
                    "restored precision confirmation keeps dice total and double history");
                Check(!turns.ConfirmAutoPrecision(0),
                    "restored precision confirmation cannot be submitted twice");
                turns.ApplyReplicaTurnState(restoredDebtor.PlayerId, "MovingPawn", 12, 7, false);
                Check(turns.ApplyReplicatedMovement(new MovementResumeState
                    { startPosition = 38, targetPosition = 3 }),
                    "restored movement accepts source and destination");
                double cashBeforeMovement = restoredDebtor.CurrentBalance;
                int lapsBeforeMovement = restoredDebtor.LapsCompleted;
                Check(turns.ApplyPendingMovementEconomics(restoredDebtor)
                    && turns.GetPlayerPosition(restoredDebtor) == 3
                    && restoredDebtor.LapsCompleted == lapsBeforeMovement + 1
                    && restoredDebtor.CurrentBalance == cashBeforeMovement + finance.CurrentSgo,
                    "restored movement crossing GO applies destination, lap and salary");
                double settledCash = restoredDebtor.CurrentBalance;
                double settledSgo = finance.CurrentSgo;
                Check(turns.ApplyPendingMovementEconomics(restoredDebtor)
                    && restoredDebtor.CurrentBalance == settledCash
                    && finance.CurrentSgo == settledSgo
                    && restoredDebtor.LapsCompleted == lapsBeforeMovement + 1,
                    "restored movement cannot repeat GO salary, inflation or lap");
                var movementCopy = turns.CaptureMovementState();
                movementCopy.targetPosition = 9;
                Check(turns.CaptureMovementState().targetPosition == 3,
                    "captured movement metadata is independent from live state");
                var movementRoundTrip = JsonUtility.FromJson<OnlineGameSnapshotDto>(JsonUtility.ToJson(
                    new OnlineGameSnapshotDto { movement = turns.CaptureMovementState() }));
                Check(movementRoundTrip.movement.goEconomicsApplied
                    && movementRoundTrip.movement.startPosition == 38
                    && movementRoundTrip.movement.targetPosition == 3,
                    "movement destination and GO phase survive snapshot serialization");
                Check(!turns.ApplyReplicatedMovement(new MovementResumeState
                    { startPosition = 38, targetPosition = 40 })
                    && turns.CaptureMovementState().targetPosition == 3,
                    "invalid restored movement destination is rejected");
                var resumeMovement = (System.Collections.IEnumerator)typeof(TurnManager)
                    .GetMethod("ResumePendingMovement", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(turns, new object[] { restoredDebtor });
                Check(!resumeMovement.MoveNext() && turns.CurrentState == TurnManager.TurnState.ResolveTile
                    && restoredDebtor.CurrentBalance == settledCash
                    && turns.CaptureMovementState() == null && turns.PendingDiceTotal == 0,
                    "restored movement coroutine arrives without repeating settled GO and clears metadata");
                turns.ApplyReplicaTurnState(restoredDebtor.PlayerId, "MovingPawn", 12, 6, false);
                turns.ApplyReplicatedMovement(new MovementResumeState { startPosition = 3, targetPosition = 9 });
                restoredDebtor.OilGasBoostTurnsRemaining = 2;
                int nonGoLaps = restoredDebtor.LapsCompleted;
                var resumeWithoutGo = (System.Collections.IEnumerator)typeof(TurnManager)
                    .GetMethod("ResumePendingMovement", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(turns, new object[] { restoredDebtor });
                Check(!resumeWithoutGo.MoveNext() && turns.GetPlayerPosition(restoredDebtor) == 9
                    && turns.CurrentState == TurnManager.TurnState.ResolveTile
                    && restoredDebtor.CurrentBalance == settledCash
                    && finance.CurrentSgo == settledSgo && restoredDebtor.LapsCompleted == nonGoLaps
                    && restoredDebtor.OilGasBoostTurnsRemaining == 2,
                    "restored movement without GO arrives without salary, inflation or buff decrement");
                int victoryNotifications = 0;
                PlayerModel restoredWinner = null;
                GameEvents.OnGameVictory = (winner, reason) =>
                    { victoryNotifications++; restoredWinner = winner; };
                long restoredMatchStart = matchStart - TimeSpan.FromMinutes(91).Ticks;
                var endedPayload = JsonUtility.ToJson(new OnlineGameSnapshotDto
                {
                    currentPlayerId = restoredDebtor.PlayerId, turnState = "EndTurn",
                    isGameOver = true, winnerPlayerId = restoredOwner.PlayerId,
                    victoryReason = "TIME OUT — HIGHEST NET WORTH",
                    matchStartedUtcTicks = restoredMatchStart,
                    players = new[] { new OnlinePlayerStateDto
                        { playerId = restoredDebtor.PlayerId, balance = settledCash, position = 9 } }
                });
                var endedServerState = new GameStateDto
                    { version = 60, lastAppliedCommandSequence = 25, payloadJson = endedPayload };
                Check((bool)restoreAuthority.Invoke(online, new object[] { endedServerState })
                    && turns.IsGameOver && turns.WinnerPlayerId == restoredOwner.PlayerId
                    && turns.VictoryReason == "TIME OUT — HIGHEST NET WORTH"
                    && finance.MatchStartedUtcTicks == restoredMatchStart && finance.IsAggressiveGrowthMode
                    && restoredWinner == restoredOwner && victoryNotifications == 1,
                    "restored authority snapshot preserves match clock, aggressive-growth mode and victory event");
                endedServerState.version = 61;
                Check((bool)restoreAuthority.Invoke(online, new object[] { endedServerState })
                    && victoryNotifications == 1 && turns.CurrentState == TurnManager.TurnState.EndTurn,
                    "repeated ended snapshot does not repeat victory notification");
                turns.DeclareVictory(restoredDebtor, "another result");
                Check(turns.WinnerPlayerId == restoredOwner.PlayerId && victoryNotifications == 1,
                    "completed game cannot replace its winner");
                Check(!turns.ApplyReplicatedOutcome(true, 999, "unknown winner")
                    && turns.WinnerPlayerId == restoredOwner.PlayerId,
                    "unknown winner is rejected without changing result");
                Check(turns.ApplyReplicatedOutcome(true, -1, "NO SURVIVORS")
                    && restoredWinner == null && victoryNotifications == 2,
                    "ended game with no survivors restores a result without winner");
                turns.StartGame(new List<PlayerModel> { restoredDebtor, restoredOwner, restoredHijacker }, false);
                Check(!turns.IsGameOver && turns.WinnerPlayerId == -1 && turns.VictoryReason == "",
                    "new game clears previous victory metadata");
                typeof(OnlineGameController).GetMethod("OnEnable",
                    BindingFlags.Instance | BindingFlags.NonPublic).Invoke(online, null);
                typeof(OnlineGameController).GetField("<IsOnlineGame>k__BackingField",
                    BindingFlags.Instance | BindingFlags.NonPublic).SetValue(online, true);
                var publishPolicy = typeof(OnlineGameController).GetMethod("ShouldPublishAutonomousState",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var revision = typeof(OnlineGameController).GetField("mutationGeneration",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var publishedRevision = typeof(OnlineGameController).GetField("publishedGeneration",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                publishedRevision.SetValue(online, revision.GetValue(online));
                turns.ApplyReplicaTurnState(restoredDebtor.PlayerId, "Planning", 1, 0, false);
                Check(!(bool)publishPolicy.Invoke(online, null),
                    "idle online authority does not serialize unchanged state");
                GameEvents.PublishActivity("move", restoredDebtor.PlayerId, "automatic arrival");
                Check((bool)publishPolicy.Invoke(online, null),
                    "autonomous activity schedules a stable snapshot publication");
                var processing = typeof(OnlineGameController).GetField("processingCommand",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                processing.SetValue(online, true);
                Check(!(bool)publishPolicy.Invoke(online, null),
                    "autonomous publication waits while a command is being processed");
                processing.SetValue(online, false);
                turns.ApplyReplicaTurnState(restoredDebtor.PlayerId, "MovingPawn", 1, 6, false);
                Check(!(bool)publishPolicy.Invoke(online, null),
                    "autonomous publication waits for movement effects to settle");
                typeof(OnlineGameController).GetField("pendingBankruptcyPlayerId",
                    BindingFlags.Instance | BindingFlags.NonPublic).SetValue(online, restoredDebtor.PlayerId);
                typeof(OnlineGameController).GetField("pendingBankruptcyToken",
                    BindingFlags.Instance | BindingFlags.NonPublic).SetValue(online, 1L);
                Check((bool)publishPolicy.Invoke(online, null),
                    "GO debt is published while movement awaits a human decision");
                typeof(OnlineGameController).GetMethod("ClearPendingBankruptcy",
                    BindingFlags.Instance | BindingFlags.NonPublic).Invoke(online, null);
                turns.ApplyReplicaTurnState(restoredDebtor.PlayerId, "Planning", 1, 0, false);
                long capturedRevision = (long)revision.GetValue(online);
                GameEvents.PublishActivity("turn", restoredDebtor.PlayerId, "change during publication");
                typeof(OnlineGameController).GetMethod("AcceptPublishedState",
                    BindingFlags.Instance | BindingFlags.NonPublic).Invoke(online, new object[]
                    { new GameStateDto { version = 70, lastAppliedCommandSequence = 30 },
                        "captured result", capturedRevision, null });
                Check((bool)publishPolicy.Invoke(online, null) && online.StateVersion == 70,
                    "changes during publication remain dirty after acknowledgement");
                var publicationPending = typeof(OnlineGameController).GetField("statePublicationPending",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                publicationPending.SetValue(online, true);
                Check(!(bool)publishPolicy.Invoke(online, null),
                    "only one snapshot publication may be in flight");
                publicationPending.SetValue(online, false);
                typeof(OnlineGameController).GetField("<IsOnlineGame>k__BackingField",
                    BindingFlags.Instance | BindingFlags.NonPublic).SetValue(online, false);
                typeof(OnlineGameController).GetMethod("OnDisable",
                    BindingFlags.Instance | BindingFlags.NonPublic).Invoke(online, null);
                var nextHuman = new PlayerModel(99, 18000, false);
                turns.StartGame(new List<PlayerModel> { player, nextHuman }, false);
                turns.ApplyReplicaTurnState(player.PlayerId, "Planning", 1, 0, false);
                var actionWidget = Component<PolyRisk.UI.ActionButtonsWidget>();
                actionWidget.turnManager = turns;
                var primaryAction = new UnityEngine.UIElements.Button();
                Set(actionWidget, "btnPrimary", primaryAction);
                Set(actionWidget, "btnSecondary", new UnityEngine.UIElements.Button());
                Set(actionWidget, "btnTertiary", new UnityEngine.UIElements.Button());
                typeof(PolyRisk.UI.ActionButtonsWidget).GetMethod("OnEnable",
                    BindingFlags.Instance | BindingFlags.NonPublic).Invoke(actionWidget, null);
                actionWidget.UpdateButtonsState(null, player);
                turns.ApplyReplicaTurnState(nextHuman.PlayerId, "Planning", 2, 0, false);
                GameEvents.OnTurnStarted?.Invoke(nextHuman.PlayerId);
                Check(primaryAction.text == "Lancer les dés" && primaryAction.enabledSelf,
                    "Planning-to-Planning turn enables the next human's roll button immediately");
                actionWidget.SetPresentationVisible(false);
                Check(actionWidget.gameObject.activeSelf && actionWidget.enabled,
                    "modal hiding keeps action widget active and subscribed");
                primaryAction.text = "STALE";
                actionWidget.SetPresentationVisible(true);
                Check(primaryAction.text == "Lancer les dés" && primaryAction.enabledSelf,
                    "closing a modal refreshes actions for the current turn");
                Debug.Log($"ECONOMY REGRESSION PASS: {assertions} assertions");
            }
            finally
            {
                GameEvents.OnInsolvency = null; GameEvents.OnGameVictory = null;
                GameBootstrapper.DeferGameLoop = false;
                foreach (var item in owned.AsEnumerable().Reverse()) if (item != null) UnityEngine.Object.DestroyImmediate(item);
                owned.Clear();
            }
        }
    }
}
