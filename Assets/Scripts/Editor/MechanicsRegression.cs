using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using PolyRisk.Core;
using PolyRisk.Multiplayer;
using PolyRisk.Systems;

namespace PolyRisk.Editor
{
    // Execute with -batchmode -executeMethod PolyRisk.Editor.MechanicsRegression.Run.
    // Exercises production family powers, hostile takeover and current sanctions.
    public static class MechanicsRegression
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

        private static FieldInfo Field(Type type, string name)
        {
            while (type != null)
            {
                var field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (field != null) return field;
                type = type.BaseType;
            }
            throw new MissingFieldException(name);
        }

        private static void Set(object target, string name, object value) => Field(target.GetType(), name).SetValue(target, value);

        private static FamilyData Family(string name, int rank)
        {
            var family = ScriptableObject.CreateInstance<FamilyData>(); owned.Add(family);
            Set(family, "familyName", name); Set(family, "powerRank", rank);
            Set(family, "rentGrowthCurve", AnimationCurve.Linear(0, 1, 1, 200));
            return family;
        }

        private static PropertyData Property(string name, int index, double price, double rent, FamilyData family)
        {
            var property = ScriptableObject.CreateInstance<PropertyData>(); owned.Add(property);
            property.Initialize(name, index, TileType.Property);
            Set(property, "basePrice", price); Set(property, "baseRent", rent); Set(property, "familyData", family);
            return property;
        }

        private static void Check(bool condition, string label)
        {
            assertions++;
            if (!condition) throw new Exception("MECHANICS REGRESSION FAILED: " + label);
            Debug.Log("PASS: " + label);
        }

        private static bool Near(double actual, double expected, double tolerance = 0.01)
            => Math.Abs(actual - expected) <= tolerance;

        public static void Run()
        {
            assertions = 0;
            GameBootstrapper.DeferGameLoop = true;
            GameEvents.OnInsolvency = null;
            GameEvents.OnGameVictory = null;
            GameEvents.OnActivityPublished = null;
            try
            {
                Check(MainMenuController.ShouldUseDenseLobbyRoster(1, true)
                    && MainMenuController.ShouldUseDenseLobbyRoster(2, true)
                    && MainMenuController.ShouldUseDenseLobbyRoster(6, true),
                    "Landscape lobby compacts one-to-six players beside pinned actions");
                Check(!MainMenuController.ShouldUseDenseLobbyRoster(6, false)
                    && !MainMenuController.ShouldUseDenseLobbyRoster(0, true),
                    "Portrait and empty lobbies keep the standard layout");
                Check(MainMenuController.PreservePlayerNameOnRoster("QA Player 4") == "QA\u00A0Player\u00A04"
                    && MainMenuController.PreservePlayerNameOnRoster(null) == string.Empty,
                    "Lobby name wrapping keeps each complete player name together");
                var sixSeatRoster = Enumerable.Range(1, 6).Select(index => new LobbyPlayerDto
                {
                    displayName = index == 1 ? "Host" : $"QA{index}",
                    isHost = index == 1,
                    isReady = true
                }).ToArray();
                string compactRoster = MainMenuController.FormatLobbyRoster(sixSeatRoster, 2);
                Check(compactRoster.Split('\n').Length == 3
                    && Enumerable.Range(2, 5).All(index => compactRoster.Contains($"QA{index} · READY")),
                    "Compact six-player lobby keeps every Ready status on three complete rows");
                Check(TurnManager.IsValidTurboCandidates(Array.Empty<int>(), Array.Empty<int>(), false)
                    && !TurnManager.IsValidTurboCandidates(Array.Empty<int>(), Array.Empty<int>(), true),
                    "Legacy empty Turbo arrays accepted only outside pending choice");
                Check(Near(BoardInteractionManager.SelectionCameraSize(11, 0.5f, 9, 5), 19.5),
                    "Portrait selection camera frames horizontal tile extent with margin");
                Check(Near(BoardInteractionManager.SelectionCameraSize(11, 2, 9, 5), 11),
                    "Landscape selection camera preserves sufficient original zoom");
                Check(Near(BoardInteractionManager.SelectionCameraSize(11, 1, 5, 15), 15.75),
                    "Selection camera also frames vertical tile extent");
                Check(Near(BoardInteractionManager.PlanningCameraSize(11, 430, 236, 900, 5, 4.35f), 7.93),
                    "Compact landscape planning camera uses free vertical viewport to frame the board");
                Check(Near(BoardInteractionManager.PlanningCameraSize(11, 430, 150, 900, 5, 4.35f), 11),
                    "Compact landscape planning camera never zooms out beyond the authored view");
                var dice = Component<DiceSystem>();
                int diceEvents = 0;
                Action<int[]> diceListener = result => diceEvents++;
                GameEvents.OnDiceRolled += diceListener;
                try
                {
                    dice.ApplyReplicatedDoubles(4);
                    dice.GenerateRoll(); dice.GenerateRoll();
                    Check(dice.ConsecutiveDoubles == 4 && diceEvents == 0,
                        "Turbo candidates do not change doubles or publish dice events");
                    dice.CommitRoll(new[] { 6, 6 });
                    Check(dice.ConsecutiveDoubles == 5 && dice.HasExceededDoublesLimit() && diceEvents == 1,
                        "Selected Turbo double counts once and triggers fifth-double penalty");
                    dice.ApplyReplicatedDoubles(4);
                    dice.CommitRoll(new[] { 5, 6 });
                    Check(dice.ConsecutiveDoubles == 0 && !dice.HasExceededDoublesLimit() && diceEvents == 2,
                        "Selected non-double resets streak and emits exactly one event");
                    bool invalidRejected = false;
                    try { dice.CommitRoll(new[] { 0, 6 }); }
                    catch (ArgumentException) { invalidRejected = true; }
                    Check(invalidRejected && dice.ConsecutiveDoubles == 0 && diceEvents == 2,
                        "Invalid dice cannot mutate counters or publish events");
                }
                finally { GameEvents.OnDiceRolled -= diceListener; }

                var capturedActivities = new List<GameActivityEntry>();
                GameEvents.OnActivityPublished += entry => capturedActivities.Add(entry);
                GameEvents.BeginActivitySession();
                var activityOne = GameEvents.PublishActivity("turn", 0, "commence son tour");
                var activityTwo = GameEvents.PublishActivity("pass", 1, "passe");
                Check(activityOne.id == 1 && activityTwo.id == 2 && capturedActivities.Count == 2,
                    "activity feed assigns ordered session identifiers");
                string activityJson = JsonUtility.ToJson(new OnlineGameSnapshotDto
                {
                    activities = capturedActivities.ToArray(),
                    pendingVentureCardId = 19,
                    pendingVenturePlayerId = 1,
                    pendingVentureToken = 7,
                    bankruptcy = new OnlineBankruptcyStateDto
                    {
                        playerId = 3,
                        creditorPlayerId = 1,
                        debt = 4250,
                        token = 9
                    },
                    marketState = "BOOM",
                    cycle = new OnlineCycleStateDto { cycleType = "BOOM", turnsRemaining = 4 },
                    investments = new[]
                    {
                        new OnlineStockInvestmentDto
                        {
                            playerId = 2, amount = 5000, turnsRemaining = 2, totalDuration = 3
                        }
                    }
                });
                var activityRoundTrip = JsonUtility.FromJson<OnlineGameSnapshotDto>(activityJson);
                Check(activityRoundTrip.activities.Length == 2
                    && activityRoundTrip.activities[1].kind == "pass"
                    && activityRoundTrip.activities[1].playerId == 1,
                    "activity feed survives multiplayer snapshot serialization");
                Check(activityRoundTrip.pendingVentureCardId == 19
                    && activityRoundTrip.pendingVenturePlayerId == 1
                    && activityRoundTrip.pendingVentureToken == 7,
                    "pending Venture confirmation survives multiplayer snapshot serialization");
                Check(activityRoundTrip.bankruptcy != null
                    && activityRoundTrip.bankruptcy.playerId == 3
                    && activityRoundTrip.bankruptcy.creditorPlayerId == 1
                    && Near(activityRoundTrip.bankruptcy.debt, 4250)
                    && activityRoundTrip.bankruptcy.token == 9,
                    "pending bankruptcy survives multiplayer snapshot serialization");
                Check(activityRoundTrip.marketState == "BOOM"
                    && activityRoundTrip.cycle.cycleType == "BOOM"
                    && activityRoundTrip.cycle.turnsRemaining == 4
                    && activityRoundTrip.investments.Length == 1
                    && activityRoundTrip.investments[0].playerId == 2
                    && Near(activityRoundTrip.investments[0].amount, 5000),
                    "cycle and investments survive multiplayer snapshot serialization");
                var stockCommandRoundTrip = JsonUtility.FromJson<StockInvestmentCommandDto>(
                    JsonUtility.ToJson(new StockInvestmentCommandDto { amount = 7500, duration = 5 }));
                Check(Near(stockCommandRoundTrip.amount, 7500) && stockCommandRoundTrip.duration == 5,
                    "stock investment command survives multiplayer serialization");
                var sanctionCommand = new MarketplaceSanctionCommandDto
                {
                    sanction = "sabotage",
                    targetPlayerId = 4,
                    targetTileIndex = 18
                };
                var sanctionRoundTrip = JsonUtility.FromJson<MarketplaceSanctionCommandDto>(
                    JsonUtility.ToJson(sanctionCommand));
                Check(sanctionRoundTrip.sanction == "sabotage"
                    && sanctionRoundTrip.targetPlayerId == 4
                    && sanctionRoundTrip.targetTileIndex == 18,
                    "marketplace sanction command survives multiplayer serialization");
                var tenderRoundTrip = JsonUtility.FromJson<OnlineMarketplaceStateDto>(
                    JsonUtility.ToJson(new OnlineMarketplaceStateDto
                    {
                        auctionState = "Reviewing",
                        sellerPlayerId = 2,
                        propertyTileIndex = 11,
                        bids = new[]
                        {
                            new OnlineMarketplaceBidDto
                            {
                                playerId = 4,
                                cashOffer = 3750,
                                offeredTileIndices = new[] { 8, 19 }
                            }
                        }
                    }));
                Check(tenderRoundTrip.auctionState == "Reviewing"
                    && tenderRoundTrip.sellerPlayerId == 2
                    && tenderRoundTrip.bids.Length == 1
                    && Near(tenderRoundTrip.bids[0].cashOffer, 3750)
                    && tenderRoundTrip.bids[0].offeredTileIndices.SequenceEqual(new[] { 8, 19 }),
                    "complete marketplace tender survives snapshot serialization");
                var transitCommandRoundTrip = JsonUtility.FromJson<TransitChoiceCommandDto>(
                    JsonUtility.ToJson(new TransitChoiceCommandDto { takeTransit = true }));
                Check(transitCommandRoundTrip.takeTransit,
                    "optional Transit Express choice survives multiplayer command serialization");

                var transactions = Component<TransactionManager>();
                var board = Component<BoardManager>();
                var turns = Component<TurnManager>();
                var finance = Component<FinanceSystem>();
                var upgrades = Component<UpgradeSystem>();
                var abilities = Component<AbilitySystem>();
                var rules = Component<RulesEngine>();
                var marketplace = Component<MarketplaceSystem>();
                var venture = Component<VentureSystem>();
                var cycles = Component<CycleManager>();

                finance.transactionManager = transactions; finance.boardManager = board; finance.CurrentSgo = 1000;
                upgrades.transactionManager = transactions; upgrades.boardManager = board;
                abilities.transactionManager = transactions; abilities.turnManager = turns; abilities.boardManager = board;
                abilities.financeSystem = finance; abilities.upgradeSystem = upgrades;
                rules.transactionManager = transactions; rules.turnManager = turns; rules.financeSystem = finance;
                rules.boardManager = board; rules.upgradeSystem = upgrades;
                turns.rulesEngine = rules;
                marketplace.transactionManager = transactions; marketplace.turnManager = turns; marketplace.financeSystem = finance;
                venture.transactionManager = transactions; venture.turnManager = turns; venture.financeSystem = finance;
                venture.boardManager = board; venture.rulesEngine = rules;

                var investor = new PlayerModel(90, 50000, false);
                capturedActivities.Clear();
                Check(finance.InvestInStockMarket(investor, 25000, 3)
                    && Near(investor.CurrentBalance, 35000)
                    && Near(finance.GetActiveInvestmentsForPlayer(investor.PlayerId).Single().Amount, 15000),
                    "stock investment caps at 15 Sgo and deducts the authoritative amount");
                Check(capturedActivities.Any(item => item.kind == "investment" && item.playerId == investor.PlayerId),
                    "stock investment publishes shared activity feedback");
                finance.ClearInvestmentsForReplica();
                finance.SetInvestmentsForReplica(investor.PlayerId, new[]
                {
                    new StockInvestment { Amount = 4200, TurnsRemaining = 2, TotalDuration = 5 }
                });
                var replicatedInvestment = finance.GetActiveInvestmentsForPlayer(investor.PlayerId).Single();
                Check(Near(replicatedInvestment.Amount, 4200)
                    && replicatedInvestment.TurnsRemaining == 2
                    && replicatedInvestment.TotalDuration == 5,
                    "replica investment state replaces local state without economic side effects");
                cycles.ApplyReplicatedState(CycleType.CRASH, 3);
                Check(cycles.CurrentCycle == CycleType.CRASH && cycles.TurnsRemaining == 3
                    && finance.CurrentMarketState == "CRASH",
                    "replica cycle state updates gameplay state and market HUD");
                Check(Near(cycles.GetGoIncomeMultiplier(), 0.60)
                    && Near(cycles.GetStockShiftMin(), -0.20)
                    && Near(cycles.GetStockShiftMax(1), -0.20)
                    && Near(cycles.GetStockShiftMax(3), -0.28)
                    && Near(cycles.GetStockShiftMax(5), -0.48),
                    "CRASH applies the documented GO-income and duration-specific stock modifiers");
                var crashFamily = Family("Real Estate", 1);
                var crashProperty = Property("QA Crash Dividend", 37, 1000, 100, crashFamily);
                crashProperty.SetLevel(3);
                crashProperty.WasVisitedSinceLastGO = true;
                board.tileDataList = new List<BoardTileData> { crashProperty };
                var crashOwner = new PlayerModel(91, 10000, false);
                crashOwner.AddProperty(crashProperty);
                finance.CurrentSgo = 1000;
                finance.ProcessGoIncome(crashOwner);
                Check(Near(crashOwner.CurrentBalance, 7744),
                    "CRASH reduces GO salary and family dividends by 40 percent but keeps full active Burn Rate");
                cycles.ApplyReplicatedState(CycleType.NORMAL, 0);
                Check(Near(cycles.GetGoIncomeMultiplier(), 1.0)
                    && Near(cycles.GetStockShiftMax(1), 0)
                    && Near(cycles.GetStockShiftMax(3), 0)
                    && Near(cycles.GetStockShiftMax(5), 0),
                    "NORMAL cycle has no GO-income or stock-return shift");

                var definitions = new[]
                {
                    ("Real Estate", 1, 3), ("Pharma", 2, 3), ("Automobile", 3, 3),
                    ("Oil & Gas", 4, 3), ("Luxe", 5, 3), ("Tech", 6, 3),
                    ("Aerospace", 7, 2), ("Armement", 8, 2)
                };
                var families = definitions.ToDictionary(d => d.Item1, d => Family(d.Item1, d.Item2));
                var familyProperties = new Dictionary<string, List<PropertyData>>();
                var allTiles = new List<BoardTileData>();
                int tileIndex = 1;
                foreach (var definition in definitions)
                {
                    var props = new List<PropertyData>();
                    for (int i = 0; i < definition.Item3; i++)
                    {
                        var prop = Property($"{definition.Item1} {i + 1}", tileIndex++, 1000 + i * 100, 100 + i * 10, families[definition.Item1]);
                        props.Add(prop); allTiles.Add(prop);
                    }
                    familyProperties[definition.Item1] = props;
                }
                var marketData = ScriptableObject.CreateInstance<BoardTileData>(); owned.Add(marketData);
                marketData.Initialize("Marketplace", 20, TileType.GlobalAuction); allTiles.Add(marketData);
                var startData = ScriptableObject.CreateInstance<BoardTileData>(); owned.Add(startData);
                startData.Initialize("Start", 0, TileType.Go_Start);
                var boardData = new List<BoardTileData> { startData };
                boardData.AddRange(allTiles);
                board.tileDataList = boardData;
                var boardTiles = Enumerable.Range(0, 40).Select(index =>
                {
                    BoardTileData data = index == 0
                        ? startData
                        : index == 20
                            ? marketData
                        : allTiles.FirstOrDefault(tile => tile.TileIndex == index && tile is PropertyData);
                    return data != null
                        ? new BoardTile(index, data.TileType, data)
                        : new BoardTile(index, TileType.Go_Start, null);
                }).ToList();
                Set(board, "tiles", boardTiles);

                var logistics = Family("Logistics", 0);
                var hubOwner = new PlayerModel(97, 50000, true);
                var logisticsHubs = new[] { 5, 15, 25, 35 }
                    .Select(index => Property($"Logistics Hub {index}", index, 1000, 100, logistics))
                    .ToArray();
                foreach (var hub in logisticsHubs) hubOwner.AddProperty(hub);
                Check(TurnManager.TryGetTransitExpressDestination(hubOwner, 5, out int transitFrom5)
                    && transitFrom5 == 15,
                    "Transit Express offers the next clockwise Hub when all four are owned");
                Check(TurnManager.TryGetTransitExpressDestination(hubOwner, 35, out int transitFrom35)
                    && transitFrom35 == 5,
                    "Transit Express 35→5 does not apply a second GO movement or salary");
                Check(!TurnManager.TryGetTransitExpressDestination(hubOwner, 6, out _),
                    "Transit Express is offered only on Hub landings");
                hubOwner.RemoveProperty(logisticsHubs[3]);
                Check(!TurnManager.TryGetTransitExpressDestination(hubOwner, 5, out _),
                    "Transit Express requires all four Logistics Hubs");
                hubOwner.AddProperty(logisticsHubs[3]);

                var transitStandby = new PlayerModel(98, 50000, true);
                turns.StartGame(new List<PlayerModel> { hubOwner, transitStandby }, false);
                var positions = (Dictionary<int, int>)Field(typeof(TurnManager), "playerPositions").GetValue(turns);
                double transitCashBefore = hubOwner.CurrentBalance;
                positions[hubOwner.PlayerId] = 35;
                Set(turns, "currentState", TurnManager.TurnState.WaitingForTransitChoice);
                Check(turns.ResolveTransitExpressChoice(true)
                    && turns.GetPlayerPosition(hubOwner) == 5
                    && turns.CurrentState == TurnManager.TurnState.ResolveTile
                    && Near(hubOwner.CurrentBalance, transitCashBefore),
                    "Transit Express can be accepted from Hub 35 without collecting GO salary");
                positions[hubOwner.PlayerId] = 5;
                Set(turns, "currentState", TurnManager.TurnState.WaitingForTransitChoice);
                Check(turns.ResolveTransitExpressChoice(false)
                    && turns.GetPlayerPosition(hubOwner) == 5
                    && turns.CurrentState == TurnManager.TurnState.ResolveTile,
                    "Transit Express can be declined and the original Hub is resolved");

                var activityBuyer = new PlayerModel(99, 10000, true);
                activityBuyer.AddProperty(familyProperties["Real Estate"][0]);
                activityBuyer.AddProperty(familyProperties["Real Estate"][1]);
                capturedActivities.Clear();
                Check(rules.TryBuyProperty(activityBuyer, familyProperties["Real Estate"][2]),
                    "activity purchase setup completes a family");
                Check(capturedActivities.Any(item => item.kind == "purchase")
                    && capturedActivities.Any(item => item.kind == "family" && item.emphasis),
                    "purchase publishes acquisition and family-power activity");
                Check(abilities.HasFamilyMonopoly(activityBuyer, "Real Estate"),
                    "third family title immediately makes its power available");
                abilities.ChargeEnergyOnGo(activityBuyer);
                Check(abilities.HasEnoughEnergy(activityBuyer),
                    "completed Tier 1 family charges its power on GO");

                var tier1 = new PlayerModel(10, 100000, true);
                foreach (var p in familyProperties["Real Estate"]) tier1.AddProperty(p);
                abilities.ChargeEnergyOnGo(tier1);
                Check(Near(abilities.GetEnergy(tier1), 100), "Tier 1 reaches 100 energy in one GO");

                var tier2 = new PlayerModel(11, 100000, true);
                foreach (var p in familyProperties["Oil & Gas"]) tier2.AddProperty(p);
                abilities.ChargeEnergyOnGo(tier2); abilities.ChargeEnergyOnGo(tier2);
                Check(Near(abilities.GetEnergy(tier2), 100), "Tier 2 reaches 100 energy in two GO");

                var tier3 = new PlayerModel(12, 100000, true);
                foreach (var p in familyProperties["Tech"]) tier3.AddProperty(p);
                abilities.ChargeEnergyOnGo(tier3); abilities.ChargeEnergyOnGo(tier3);
                Check(!abilities.HasEnoughEnergy(tier3), "Tier 3 cannot activate after two GO");
                abilities.ChargeEnergyOnGo(tier3);
                Check(abilities.HasEnoughEnergy(tier3) && Near(abilities.GetEnergy(tier3), 100),
                    "Tier 3 reaches exactly 100 energy in three GO");

                var owner = new PlayerModel(0, 1_000_000, true);
                foreach (var prop in familyProperties.Values.SelectMany(x => x)) owner.AddProperty(prop);
                var opponent = new PlayerModel(1, 1_000_000, true);
                var visitor = new PlayerModel(2, 1_000_000, true);
                var dummyFamily = Family("Dummy", 1);
                var opponentProp = Property("Opponent Asset", 30, 2000, 200, dummyFamily);
                opponentProp.SetLevel(2); opponent.AddProperty(opponentProp);
                board.tileDataList.Add(opponentProp);
                ((List<BoardTile>)Field(typeof(BoardManager), "tiles").GetValue(board))[opponentProp.TileIndex] =
                    new BoardTile(opponentProp.TileIndex, opponentProp.TileType, opponentProp);
                turns.StartGame(new List<PlayerModel> { owner, opponent, visitor });

                Check(turns.ApplyReplicaTurnState(owner.PlayerId, "ResolveTile", 7, 8, false, 2),
                    "restored turn accepts pending decision and doubles state");
                turns.CompleteTileResolution();
                Check(turns.CurrentState == TurnManager.TurnState.RollDice,
                    "restored doubles preserve the extra roll after a decision");
                turns.ApplyReplicaTurnState(owner.PlayerId, "ResolveTile", 7, 8, false, 0);
                turns.CompleteTileResolution();
                Check(turns.CurrentState == TurnManager.TurnState.EndTurn,
                    "restored non-double ends the turn after a decision");
                var restoredCounters = JsonUtility.FromJson<OnlineGameSnapshotDto>(JsonUtility.ToJson(
                    new OnlineGameSnapshotDto
                    {
                        consecutiveDoubles = 2,
                        players = new[] { new OnlinePlayerStateDto { playerId = 0, turnsStarted = 9 } }
                    }));
                Check(restoredCounters.consecutiveDoubles == 2 && restoredCounters.players[0].turnsStarted == 9,
                    "authority turn counters survive snapshot serialization");
                var debtSnapshot = JsonUtility.FromJson<OnlineBankruptcyStateDto>(JsonUtility.ToJson(
                    new OnlineBankruptcyStateDto
                    {
                        playerId = 0, creditorPlayerId = 1, debt = 100, token = 4,
                        continuation = new DebtSettlementContext
                        {
                            kind = "rent", propertyTileIndex = 7,
                            hijackerPlayerId = 2, creditorBalanceBefore = 1200
                        }
                    }));
                Check(debtSnapshot.continuation.kind == "rent"
                    && debtSnapshot.continuation.propertyTileIndex == 7
                    && debtSnapshot.continuation.hijackerPlayerId == 2
                    && Near(debtSnapshot.continuation.creditorBalanceBefore, 1200),
                    "rent debt continuation survives snapshot serialization");
                turns.StartGame(new List<PlayerModel> { owner, opponent, visitor });

                foreach (var definition in definitions)
                    Check(abilities.HasFamilyMonopoly(owner, definition.Item1), $"full {definition.Item1} family detected ({definition.Item3} assets)");
                owner.RemoveProperty(familyProperties["Automobile"][2]);
                Check(!abilities.HasFamilyMonopoly(owner, "Automobile"), "family power disabled after losing one asset");
                owner.AddProperty(familyProperties["Automobile"][2]);

                double standardUpgrade = upgrades.CalculateUpgradeCost(familyProperties["Pharma"][0], owner);
                double techUpgrade = upgrades.CalculateUpgradeCost(familyProperties["Tech"][0], owner);
                Check(Near(standardUpgrade, 425), "Real Estate passive discounts every upgrade by 15%");
                Check(Near(techUpgrade, 382), "Real Estate and Tech upgrade discounts stack to rounded 382");

                double beforeTax = owner.CurrentBalance;
                rules.ExecutePrimaryAction(owner, new BoardTile(4, TileType.Tax, null));
                Check(Near(beforeTax - owner.CurrentBalance, 225), "Pharma passive reduces 0.3 Sgo tax by 25%");

                owner.AutoPrecisionAvailable = true;
                Check(turns.UseAutoPrecision(owner, 1) && owner.PendingDiceDelta == 1, "Automobile passive accepts one legal precision adjustment");
                owner.RemoveProperty(familyProperties["Automobile"][2]); owner.AutoPrecisionAvailable = true;
                Check(!turns.UseAutoPrecision(owner, -1), "Automobile passive rejects player without full family");
                owner.AddProperty(familyProperties["Automobile"][2]);

                Set(owner, "<IsAI>k__BackingField", false);
                Check(turns.ApplyReplicaTurnState(owner.PlayerId, "ChooseTurbo", 1, 0, false, 4)
                    && turns.ApplyTurboCandidates(new[] { 6, 6 }, new[] { 2, 3 }),
                    "Turbo decision restores both candidates and previous doubles streak");
                Check(!turns.ConfirmTurboChoice(2) && turns.ConsecutiveDoubles == 4,
                    "Turbo rejects invalid choice without consuming candidates");
                var turboSnapshot = JsonUtility.FromJson<OnlineGameSnapshotDto>(JsonUtility.ToJson(new OnlineGameSnapshotDto
                { turboFirst = turns.TurboFirst, turboSecond = turns.TurboSecond }));
                Check(turboSnapshot.turboFirst.SequenceEqual(new[] { 6, 6 })
                    && turboSnapshot.turboSecond.SequenceEqual(new[] { 2, 3 }),
                    "Turbo snapshot round-trips individual dice, not only totals");
                Check(turns.ConfirmTurboChoice(1) && turns.PendingDiceTotal == 5
                    && turns.ConsecutiveDoubles == 0 && turns.CurrentState == TurnManager.TurnState.ResolveDice,
                    "Human can select lower total and avoid rejected candidate double penalty");
                Check(!turns.ConfirmTurboChoice(0) && turns.TurboFirst == null,
                    "Repeated Turbo choice cannot commit another roll");
                Check(turns.ApplyReplicaTurnState(owner.PlayerId, "ChooseTurbo", 1, 0, false)
                    && !turns.ApplyTurboCandidates(null, null)
                    && !turns.ApplyTurboCandidates(new[] { 0, 6 }, new[] { 2, 3 }),
                    "Restored Turbo decision rejects missing or invalid candidates");
                owner.AutoPrecisionAvailable = true; owner.PendingDiceDelta = 0;
                Set(turns, "currentState", TurnManager.TurnState.AdjustDice);
                Check(turns.ConfirmAutoPrecision(-1) && owner.PendingDiceDelta == -1
                    && turns.CurrentState == TurnManager.TurnState.RollDice,
                    "post-roll Automobile choice applies before movement and resumes dice flow");
                owner.AutoPrecisionAvailable = true; owner.PendingDiceDelta = 1;
                Set(turns, "currentState", TurnManager.TurnState.AdjustDice);
                Check(turns.ConfirmAutoPrecision(0) && owner.PendingDiceDelta == 0 && !owner.AutoPrecisionAvailable,
                    "post-roll Automobile keep choice consumes the turn option without changing dice");
                Set(owner, "<IsAI>k__BackingField", true);

                Check(Near(rules.CalculateRent(familyProperties["Oil & Gas"][0]), 240), "Oil passive produces monopoly x2 and family x1.20 rent");
                Check(Near(rules.CalculateRent(familyProperties["Luxe"][0]), 250), "Luxe passive produces monopoly x2 and family x1.25 rent");

                cycles.StartCycle(CycleType.WAR, 5);
                Check(Near(rules.CalculateRent(familyProperties["Armement"][0]), 460), "Armement passive produces x2 WAR and x1.15 family rent");
                Check(Near(rules.CalculateRent(familyProperties["Luxe"][0]), 125), "WAR halves Luxe rent after its family passive");
                cycles.EndCycle();

                var aero = familyProperties["Aerospace"][0]; aero.SetLevel(2);
                double sanctionCash = opponent.CurrentBalance;
                marketplace.ExecuteSabotage(opponent, aero);
                Check(aero.CurrentLevel == 2 && Near(opponent.CurrentBalance, sanctionCash), "Aerospace passive blocks sabotage without charging attacker");

                double ownerBefore = owner.CurrentBalance;
                abilities.AddEnergy(owner, 100); Check(abilities.RealEstate_RappelDividendes(owner), "Real Estate ultimate activates");
                Check(owner.CurrentBalance > ownerBefore && Near(abilities.GetEnergy(owner), 0), "Real Estate ultimate pays dividends and consumes energy");

                abilities.AddEnergy(owner, 100); Check(abilities.Pharma_ProtocoleSanitaire(owner), "Pharma ultimate activates");
                Check(owner.ImmunityTurnsRemaining == 1 && owner.HasImmunityCard, "Pharma ultimate grants one-turn immunity");

                abilities.AddEnergy(owner, 100); Check(abilities.Automobile_MoteurTurbo(owner) && owner.AutoTurboReady,
                    "Automobile ultimate arms best-of-two dice roll");

                abilities.AddEnergy(owner, 100); Check(abilities.OilGas_BoomEnergetique(owner), "Oil ultimate activates");
                Check(Near(rules.CalculateRent(familyProperties["Oil & Gas"][0]), 432), "Oil ultimate raises full-family rent factor to x4.32");

                double opponentBefore = opponent.CurrentBalance;
                abilities.AddEnergy(owner, 100); Check(abilities.Luxe_TaxeSomptuaire(owner), "Luxe ultimate activates");
                Check(Near(opponentBefore - opponent.CurrentBalance, Math.Min(opponentBefore * 0.01, 8 * finance.CurrentSgo)),
                    "Luxe ultimate transfers 1% opponent cash within the 8 Sgo cap");

                abilities.AddEnergy(owner, 100); Check(abilities.Tech_MaJSysteme(owner), "Tech ultimate activates");
                Check(familyProperties["Tech"].Count(p => p.CurrentLevel == 1) == 2, "Tech ultimate upgrades exactly two lowest properties for free");

                int aeroDestination = familyProperties["Tech"][0].TileIndex;
                int positionBeforeInvalidAero = turns.GetPlayerPosition(owner);
                abilities.AddEnergy(owner, 100);
                Check(!abilities.Aerospace_SautQuantique(owner, opponentProp.TileIndex)
                    && Near(abilities.GetEnergy(owner), 100)
                    && turns.GetPlayerPosition(owner) == positionBeforeInvalidAero,
                    "Aerospace rejects an ineligible destination without spending energy or moving");
                abilities.AddEnergy(owner, 100); Check(abilities.Aerospace_SautQuantique(owner, aeroDestination), "Aerospace ultimate accepts eligible chosen destination");
                Check(turns.GetPlayerPosition(owner) == aeroDestination, "Aerospace ultimate changes logical board position");

                abilities.AddEnergy(owner, 100); Check(abilities.Armement_CyberSabotage(owner, opponentProp), "Armement ultimate activates on non-Aerospace asset");
                Check(opponentProp.HijackedBy == owner && opponentProp.HijackTurnsRemaining == 3, "Armement sabotage starts with three owner turns");
                abilities.AddEnergy(owner, 100);
                Check(!abilities.Armement_CyberSabotage(owner, aero)
                    && Near(abilities.GetEnergy(owner), 100)
                    && aero.HijackedBy == null && aero.HijackTurnsRemaining == 0,
                    "Armement rejects an Aerospace-protected target without spending energy or applying sabotage");
                double visitorBefore = visitor.CurrentBalance;
                double originalOwnerBeforeRent = opponent.CurrentBalance;
                double hijackerBeforeRent = owner.CurrentBalance;
                double hijackedRent = rules.CalculateRent(opponentProp);
                var confirmedRentTransfers = new List<GameEvent>();
                Action<GameEvent> captureRentTransfer = gameEvent =>
                {
                    if (gameEvent != null && gameEvent.Kind == GameEventKind.TransferConfirmed
                        && gameEvent.Reason == GameEventReason.Rent)
                        confirmedRentTransfers.Add(gameEvent);
                };
                using (var rentPresenter = new GameEventPresenter
                {
                    ShouldQueue = gameEvent => gameEvent.Kind == GameEventKind.TransferConfirmed
                        && gameEvent.Reason == GameEventReason.Rent
                })
                {
                    GameEvents.OnGameEventPublished += captureRentTransfer;
                    try
                    {
                        rules.ExecutePrimaryAction(visitor, new BoardTile(30, TileType.Property, opponentProp));
                    }
                    finally
                    {
                        GameEvents.OnGameEventPublished -= captureRentTransfer;
                    }

                    bool mergedReceiptStarted = rentPresenter.TryBeginNext(out var mergedRentReceipt);
                    Check(rentPresenter.History.Count == 2 && mergedReceiptStarted
                        && mergedRentReceipt.Amount == -hijackedRent && mergedRentReceipt.MergedCount == 2
                        && mergedRentReceipt.Splits.Count == 2
                        && Near(mergedRentReceipt.Splits.Sum(split => split.Amount), hijackedRent),
                        "Actual Armement transfers coalesce into one Dock receipt with exact total and payees");
                }
                Check(Near(visitorBefore - visitor.CurrentBalance, hijackedRent)
                    && Near(owner.CurrentBalance - hijackerBeforeRent, hijackedRent * 0.25)
                    && Near(opponent.CurrentBalance - originalOwnerBeforeRent, hijackedRent * 0.75)
                    && opponentProp.HijackTurnsRemaining == 3,
                    "Armement sabotage splits rent without consuming duration per landing");
                Check(confirmedRentTransfers.Count == 2
                    && confirmedRentTransfers.All(gameEvent => gameEvent.ActorId == visitor.PlayerId
                        && gameEvent.TileId == opponentProp.TileIndex && gameEvent.CorrelationId > 0
                        && gameEvent.Coalescible)
                    && confirmedRentTransfers.Select(gameEvent => gameEvent.CorrelationId).Distinct().Count() == 1
                    && Near(confirmedRentTransfers.Sum(gameEvent => -gameEvent.Amount), hijackedRent)
                    && confirmedRentTransfers.Any(gameEvent => gameEvent.TargetId == owner.PlayerId
                        && Near(-gameEvent.Amount, hijackedRent * 0.25)
                        && gameEvent.Splits.Count == 1 && gameEvent.Splits[0].PayeeId == owner.PlayerId
                        && Near(gameEvent.Splits[0].Amount, hijackedRent * 0.25))
                    && confirmedRentTransfers.Any(gameEvent => gameEvent.TargetId == opponent.PlayerId
                        && Near(-gameEvent.Amount, hijackedRent * 0.75)
                        && gameEvent.Splits.Count == 1 && gameEvent.Splits[0].PayeeId == opponent.PlayerId
                        && Near(gameEvent.Splits[0].Amount, hijackedRent * 0.75)),
                    "Armement split emits two confirmed, coalescible rent facts with exact payees and one correlation");
                finance.ProcessGoIncome(opponent);
                Check(opponentProp.HijackTurnsRemaining == 2, "Armement sabotage duration decrements once at owner GO");

                // Verify that live ultimate effects are captured and restored by the
                // production multiplayer snapshot path, not only by DTO round trips.
                double visitorBalanceBeforeForceAudit = visitor.CurrentBalance;
                marketplace.ExecuteSanction(visitor, opponent, SanctionType.ForceAudit);
                Check(turns.GetPlayerPosition(opponent) == 10
                    && opponent.IsInAudit && opponent.AuditTurnsRemaining == 3
                    && Near(visitorBalanceBeforeForceAudit - visitor.CurrentBalance, 4 * finance.CurrentSgo),
                    "Force Audit sanction charges four Sgo and places its target in Audit for three turns");

                owner.HasImmunityCard = true;
                owner.ImmunityTurnsRemaining = 1;
                abilities.SetEnergyForReplica(owner, 42);
                var snapshotLobbySession = Component<LobbySession>();
                Set(snapshotLobbySession, "<CurrentLobby>k__BackingField", new LobbySnapshotDto
                {
                    code = "SNAPQA", maxPlayers = 6, status = "started", hostPlayerId = "qa-0",
                    players = new[]
                    {
                        new LobbyPlayerDto { playerId = "qa-0", displayName = "QA Owner", isHost = true },
                        new LobbyPlayerDto { playerId = "qa-1", displayName = "QA Opponent" },
                        new LobbyPlayerDto { playerId = "qa-2", displayName = "QA Visitor" }
                    }
                });
                var snapshotController = Component<OnlineGameController>();
                var captureOnlineSnapshot = typeof(OnlineGameController).GetMethod("CaptureSnapshot",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var capturedSnapshot = (OnlineGameSnapshotDto)captureOnlineSnapshot.Invoke(snapshotController, null);
                string capturedSnapshotJson = JsonUtility.ToJson(capturedSnapshot);
                var ultimateSnapshot = JsonUtility.FromJson<OnlineGameSnapshotDto>(capturedSnapshotJson);
                var ownerSnapshot = ultimateSnapshot.players.Single(player => player.playerId == owner.PlayerId);
                var techSnapshot = ultimateSnapshot.properties.Single(property =>
                    property.tileIndex == familyProperties["Tech"][0].TileIndex);
                var sabotageSnapshot = ultimateSnapshot.properties.Single(property =>
                    property.tileIndex == opponentProp.TileIndex);
                Check(ownerSnapshot.energy == 42 && ownerSnapshot.balance == owner.CurrentBalance
                    && ownerSnapshot.position == turns.GetPlayerPosition(owner)
                    && ownerSnapshot.hasImmunityCard && ownerSnapshot.immunityTurnsRemaining == 1
                    && ownerSnapshot.autoTurboReady && ownerSnapshot.oilGasBoostTurnsRemaining == 1,
                    "production online snapshot captures energy, cash, position and Pharma/Automobile/Oil ultimate effects");
                var auditSnapshot = ultimateSnapshot.players.Single(player => player.playerId == opponent.PlayerId);
                Check(auditSnapshot.position == 10 && auditSnapshot.isInAudit
                    && auditSnapshot.auditTurnsRemaining == 3,
                    "production online snapshot captures the active Force Audit sanction and remaining duration");
                Check(techSnapshot.ownerPlayerId == owner.PlayerId && techSnapshot.currentLevel == 1
                    && sabotageSnapshot.ownerPlayerId == opponent.PlayerId
                    && sabotageSnapshot.hijackedByPlayerId == owner.PlayerId
                    && sabotageSnapshot.hijackTurnsRemaining == 2,
                    "production online snapshot captures Tech upgrades and Armement sabotage on the opponent property");

                // Corrupt only replica-side values, then apply the serialized snapshot.
                abilities.SetEnergyForReplica(owner, 0);
                owner.HasImmunityCard = false; owner.ImmunityTurnsRemaining = 0;
                owner.AutoTurboReady = false; owner.OilGasBoostTurnsRemaining = 0;
                turns.TeleportPlayer(owner, 0);
                opponent.IsInAudit = false; opponent.AuditTurnsRemaining = 0;
                turns.TeleportPlayer(opponent, 0);
                familyProperties["Tech"][0].SetLevel(0);
                opponentProp.HijackedBy = null; opponentProp.HijackTurnsRemaining = 0;
                ultimateSnapshot.currentPlayerId = owner.PlayerId;
                ultimateSnapshot.turnState = TurnManager.TurnState.Planning.ToString();
                ultimateSnapshot.turnsStarted = turns.TurnsStarted;
                ultimateSnapshot.pendingDiceTotal = 0;
                ultimateSnapshot.turboFirst = Array.Empty<int>();
                ultimateSnapshot.turboSecond = Array.Empty<int>();
                ultimateSnapshot.hasMovementState = false;
                ultimateSnapshot.movement = null;
                var applyOnlineSnapshot = typeof(OnlineGameController).GetMethod("ApplyState",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                bool snapshotApplied = (bool)applyOnlineSnapshot.Invoke(snapshotController, new object[]
                {
                    new GameStateDto { version = 1, payloadJson = JsonUtility.ToJson(ultimateSnapshot) }
                });
                Check(snapshotApplied && abilities.GetEnergy(owner) == 42
                    && owner.HasImmunityCard && owner.ImmunityTurnsRemaining == 1
                    && owner.AutoTurboReady && owner.OilGasBoostTurnsRemaining == 1
                    && turns.GetPlayerPosition(owner) == ownerSnapshot.position,
                    "replica application restores Pharma, Automobile, Oil, energy and Aerospace position from the server snapshot");
                Check(familyProperties["Tech"][0].CurrentLevel == 1
                    && opponentProp.HijackedBy == owner && opponentProp.HijackTurnsRemaining == 2,
                    $"replica application restores Tech/Sabotage (Tech level {familyProperties["Tech"][0].CurrentLevel}, "
                    + $"hijacker {opponentProp.HijackedBy?.PlayerId}, turns {opponentProp.HijackTurnsRemaining})");
                Check(opponent.IsInAudit && opponent.AuditTurnsRemaining == 3
                    && turns.GetPlayerPosition(opponent) == 10,
                    "replica application restores Force Audit state, remaining turns and board position");

                // Exercise invalid power targets through the host's production
                // command handler, not only by calling AbilitySystem directly.
                Set(snapshotLobbySession, "<LocalPlayerId>k__BackingField", "qa-0");
                Set(snapshotController, "<IsOnlineGame>k__BackingField", true);
                Set(snapshotController, "<IsAuthority>k__BackingField", true);
                Set(snapshotController, "<LocalSeatIndex>k__BackingField", owner.PlayerId);
                Set(snapshotController, "<StateVersion>k__BackingField", 1L);
                Set(snapshotController, "statePublicationPending", true);
                turns.ApplyReplicaTurnState(owner.PlayerId, TurnManager.TurnState.Planning.ToString(),
                    turns.TurnsStarted, 0, false);
                var applyOnlineCommand = typeof(OnlineGameController).GetMethod("ApplyCommand",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                int activityCountBeforeRejectedCommands = capturedActivities.Count;
                double ownerCashBeforeRejectedCommands = owner.CurrentBalance;
                double opponentCashBeforeRejectedCommands = opponent.CurrentBalance;
                int ownerPositionBeforeRejectedCommands = turns.GetPlayerPosition(owner);
                abilities.SetEnergyForReplica(owner, 100);
                var invalidAerospaceCommand = (System.Collections.IEnumerator)applyOnlineCommand.Invoke(snapshotController, new object[]
                {
                    new GameCommandDto
                    {
                        sequence = 2, expectedStateVersion = 1, playerId = "qa-0", type = "ultimate.use",
                        payloadJson = JsonUtility.ToJson(new UltimateCommandDto
                        {
                            family = "Aerospace", targetTileIndex = opponentProp.TileIndex
                        })
                    }
                });
                Check(!invalidAerospaceCommand.MoveNext()
                    && Near(abilities.GetEnergy(owner), 100)
                    && turns.GetPlayerPosition(owner) == ownerPositionBeforeRejectedCommands
                    && Near(owner.CurrentBalance, ownerCashBeforeRejectedCommands)
                    && capturedActivities.Count == activityCountBeforeRejectedCommands,
                    "host rejects invalid Aerospace target command without movement, charge or activity");

                var protectedAero = familyProperties["Aerospace"][0];
                owner.RemoveProperty(protectedAero);
                opponent.AddProperty(protectedAero);
                abilities.SetEnergyForReplica(owner, 100);
                var protectedArmementCommand = (System.Collections.IEnumerator)applyOnlineCommand.Invoke(snapshotController, new object[]
                {
                    new GameCommandDto
                    {
                        sequence = 3, expectedStateVersion = 1, playerId = "qa-0", type = "ultimate.use",
                        payloadJson = JsonUtility.ToJson(new UltimateCommandDto
                        {
                            family = "Armement", targetTileIndex = protectedAero.TileIndex
                        })
                    }
                });
                Check(!protectedArmementCommand.MoveNext()
                    && Near(abilities.GetEnergy(owner), 100)
                    && protectedAero.HijackedBy == null && protectedAero.HijackTurnsRemaining == 0
                    && Near(owner.CurrentBalance, ownerCashBeforeRejectedCommands)
                    && Near(opponent.CurrentBalance, opponentCashBeforeRejectedCommands)
                    && capturedActivities.Count == activityCountBeforeRejectedCommands,
                    "host rejects Armement command against protected Aerospace target without charge or sabotage");

                // Exercise a successful rent collection through the production host
                // command handler, including both confirmed transfers emitted by the
                // real RulesEngine for an active Cyber-sabotage effect.
                turns.TeleportPlayer(visitor, opponentProp.TileIndex);
                Check(turns.ApplyReplicaTurnState(visitor.PlayerId, TurnManager.TurnState.ResolveTile.ToString(),
                    turns.TurnsStarted, 0, false),
                    "online split-rent fixture selects the visiting seat in ResolveTile");
                int visitorCommandSeat = (int)typeof(OnlineGameController)
                    .GetMethod("FindSeat", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(snapshotController, new object[] { "qa-2" });
                Check(turns.CurrentPlayer?.PlayerId == visitor.PlayerId && visitorCommandSeat == visitor.PlayerId
                    && snapshotController.StateVersion == 1 && !turns.IsGameOver
                    && turns.CurrentState == TurnManager.TurnState.ResolveTile,
                    $"online split-rent command preconditions align (visitor={visitor.PlayerId}, active={turns.CurrentPlayer?.PlayerId}, seat={visitorCommandSeat}, version={snapshotController.StateVersion}, gameOver={turns.IsGameOver})");
                var onlineRentTile = BoardManager.Instance?.GetTileAt(turns.GetPlayerPosition(visitor));
                Check(onlineRentTile != null && onlineRentTile.Type == TileType.Property
                    && ReferenceEquals(onlineRentTile.Data, opponentProp),
                    $"online split-rent command resolves the sabotaged asset (tile={onlineRentTile?.TileIndex}, type={onlineRentTile?.Type}, data={onlineRentTile?.Data?.name})");
                double visitorBeforeOnlineHijackedRent = visitor.CurrentBalance;
                double ownerBeforeOnlineHijackedRent = owner.CurrentBalance;
                double opponentBeforeOnlineHijackedRent = opponent.CurrentBalance;
                double expectedOnlineHijackedRent = rules.CalculateRent(opponentProp);
                var onlineHijackedRentEvents = new List<GameEvent>();
                Action<GameEvent> captureOnlineHijackedRent = gameEvent =>
                {
                    if (gameEvent != null && gameEvent.Kind == GameEventKind.TransferConfirmed
                        && gameEvent.Reason == GameEventReason.Rent)
                        onlineHijackedRentEvents.Add(gameEvent);
                };
                GameEvents.OnGameEventPublished += captureOnlineHijackedRent;
                var onlineHijackedRentCommand = (System.Collections.IEnumerator)applyOnlineCommand.Invoke(snapshotController, new object[]
                {
                    new GameCommandDto
                    {
                        sequence = 4, expectedStateVersion = 1, playerId = "qa-2", type = "action.primary",
                        payloadJson = "{}"
                    }
                });
                bool onlineHijackedRentStarted;
                try { onlineHijackedRentStarted = onlineHijackedRentCommand.MoveNext(); }
                finally
                {
                    (onlineHijackedRentCommand as IDisposable)?.Dispose();
                    GameEvents.OnGameEventPublished -= captureOnlineHijackedRent;
                }
                double onlineVisitorRentPaid = visitorBeforeOnlineHijackedRent - visitor.CurrentBalance;
                double onlineSaboteurShare = owner.CurrentBalance - ownerBeforeOnlineHijackedRent;
                double onlineOwnerShare = opponent.CurrentBalance - opponentBeforeOnlineHijackedRent;
                Check(onlineHijackedRentStarted
                    && Near(onlineVisitorRentPaid, expectedOnlineHijackedRent)
                    && Near(onlineSaboteurShare, expectedOnlineHijackedRent * 0.25)
                    && Near(onlineOwnerShare, expectedOnlineHijackedRent * 0.75)
                    && opponentProp.HijackedBy == owner && opponentProp.HijackTurnsRemaining == 2,
                    $"host action.primary command applies the hijacked rent split once (25/75; commandStarted={onlineHijackedRentStarted}, rent={expectedOnlineHijackedRent}, paid={onlineVisitorRentPaid}, saboteur={onlineSaboteurShare}, owner={onlineOwnerShare}, hijacker={opponentProp.HijackedBy?.PlayerId}, duration={opponentProp.HijackTurnsRemaining})");
                Check(onlineHijackedRentEvents.Count == 2
                    && onlineHijackedRentEvents.All(gameEvent => gameEvent.ActorId == visitor.PlayerId
                        && gameEvent.TileId == opponentProp.TileIndex && gameEvent.CorrelationId > 0)
                    && onlineHijackedRentEvents.Select(gameEvent => gameEvent.CorrelationId).Distinct().Count() == 1
                    && onlineHijackedRentEvents.Any(gameEvent => gameEvent.TargetId == owner.PlayerId
                        && Near(-gameEvent.Amount, expectedOnlineHijackedRent * 0.25))
                    && onlineHijackedRentEvents.Any(gameEvent => gameEvent.TargetId == opponent.PlayerId
                        && Near(-gameEvent.Amount, expectedOnlineHijackedRent * 0.75)),
                    "host action.primary command emits both correlated rent receipts to their beneficiaries");

                var marketplaceTile = board.GetTileAt(20);
                Check(marketplaceTile != null && marketplaceTile.Type == TileType.GlobalAuction,
                    "online sanction command fixture resolves the real Marketplace tile");
                owner.HasImmunityCard = false;
                opponent.HasImmunityCard = false;
                opponent.ImmunityTurnsRemaining = 1;
                turns.TeleportPlayer(owner, 20);
                turns.ApplyReplicaTurnState(owner.PlayerId, TurnManager.TurnState.ResolveTile.ToString(),
                    turns.TurnsStarted, 0, false);
                double onlineAuditAttackerBefore = owner.CurrentBalance;
                double onlineAuditTargetBefore = opponent.CurrentBalance;
                int onlineAuditTargetPositionBefore = turns.GetPlayerPosition(opponent);
                int onlineAuditTurnsBefore = opponent.AuditTurnsRemaining;
                int onlineAuditActivitiesBefore = capturedActivities.Count;
                var immuneMarketplaceAuditCommand = (System.Collections.IEnumerator)applyOnlineCommand.Invoke(snapshotController, new object[]
                {
                    new GameCommandDto
                    {
                        sequence = 4, expectedStateVersion = 1, playerId = "qa-0", type = "marketplace.sanction",
                        payloadJson = JsonUtility.ToJson(new MarketplaceSanctionCommandDto
                        {
                            sanction = "audit", targetPlayerId = opponent.PlayerId
                        })
                    }
                });
                bool onlineAuditStarted = immuneMarketplaceAuditCommand.MoveNext();
                Check(onlineAuditStarted && turns.CurrentState == TurnManager.TurnState.EndTurn
                    && Near(owner.CurrentBalance, onlineAuditAttackerBefore)
                    && Near(opponent.CurrentBalance, onlineAuditTargetBefore)
                    && turns.GetPlayerPosition(opponent) == onlineAuditTargetPositionBefore
                    && opponent.IsInAudit && opponent.AuditTurnsRemaining == onlineAuditTurnsBefore
                    && capturedActivities.Count == onlineAuditActivitiesBefore,
                    "host command consumes an immune online Marketplace Audit without charge, movement or activity");
                (immuneMarketplaceAuditCommand as IDisposable)?.Dispose();
                opponent.ImmunityTurnsRemaining = 0;

                turns.ApplyReplicaTurnState(owner.PlayerId, TurnManager.TurnState.ResolveTile.ToString(),
                    turns.TurnsStarted, 0, false);
                double onlineAuditPaidAttackerBefore = owner.CurrentBalance;
                double onlineAuditPaidTargetBefore = opponent.CurrentBalance;
                int onlineAuditPaidActivitiesBefore = capturedActivities.Count;
                var paidMarketplaceAuditCommand = (System.Collections.IEnumerator)applyOnlineCommand.Invoke(snapshotController, new object[]
                {
                    new GameCommandDto
                    {
                        sequence = 5, expectedStateVersion = 1, playerId = "qa-0", type = "marketplace.sanction",
                        payloadJson = JsonUtility.ToJson(new MarketplaceSanctionCommandDto
                        {
                            sanction = "audit", targetPlayerId = opponent.PlayerId
                        })
                    }
                });
                bool paidOnlineAuditStarted = paidMarketplaceAuditCommand.MoveNext();
                Check(paidOnlineAuditStarted && turns.CurrentState == TurnManager.TurnState.EndTurn
                    && Near(onlineAuditPaidAttackerBefore - owner.CurrentBalance, finance.CurrentSgo)
                    && Near(onlineAuditPaidTargetBefore - opponent.CurrentBalance, onlineAuditPaidTargetBefore * 0.05)
                    && turns.GetPlayerPosition(opponent) == onlineAuditTargetPositionBefore
                    && opponent.IsInAudit && opponent.AuditTurnsRemaining == onlineAuditTurnsBefore
                    && capturedActivities.Count == onlineAuditPaidActivitiesBefore + 1,
                    "host command applies online Marketplace Audit once with the documented charge and target tax");
                (paidMarketplaceAuditCommand as IDisposable)?.Dispose();
                opponent.RemoveProperty(protectedAero);
                owner.AddProperty(protectedAero);
                Set(snapshotController, "statePublicationPending", false);
                snapshotController.ResetOnlineSessionForExit();

                owner.ImmunityTurnsRemaining = 0; owner.HasImmunityCard = false;
                double embargoBefore = opponent.CurrentBalance;
                marketplace.ExecuteEmbargo(opponent, familyProperties["Real Estate"][0]);
                Check(Near(embargoBefore - opponent.CurrentBalance, 5000), "Embargo costs exactly 5 Sgo");
                Check(familyProperties["Real Estate"][0].IsEmbargoed
                    && Near(rules.CalculateRent(familyProperties["Real Estate"][0]), 0), "Embargo suppresses rent");

                double auditAttackerBefore = visitor.CurrentBalance;
                double auditTargetBefore = opponent.CurrentBalance;
                marketplace.ExecuteAudit(visitor, opponent);
                Check(Near(auditAttackerBefore - visitor.CurrentBalance, 1000)
                    && Near(auditTargetBefore - opponent.CurrentBalance, auditTargetBefore * 0.05),
                    "targeted audit charges 1 Sgo and removes 5% target cash");

                double strikeBefore = visitor.CurrentBalance;
                marketplace.ExecuteStrike(visitor, opponentProp);
                Check(Near(strikeBefore - visitor.CurrentBalance, 2000) && opponent.DoubleBurnRateNextGo,
                    "production strike charges 2 Sgo and doubles next burn rate");

                int levelBeforeSabotage = opponentProp.CurrentLevel;
                double sabotageBefore = visitor.CurrentBalance;
                marketplace.ExecuteSabotage(visitor, opponentProp);
                Check(Near(sabotageBefore - visitor.CurrentBalance, 5000)
                    && opponentProp.CurrentLevel == levelBeforeSabotage - 1,
                    "sabotage charges 5 Sgo and removes one upgrade");
                Check(capturedActivities.Any(item => item.kind == "marketplace"
                    && item.playerId == visitor.PlayerId
                    && item.text.Contains(opponentProp.PropertyName)),
                    "successful marketplace sanction publishes shared activity feedback");

                opponent.ImmunityTurnsRemaining = 1;
                double immuneAttackerBefore = visitor.CurrentBalance;
                double immuneTargetBefore = opponent.CurrentBalance;
                marketplace.ExecuteAudit(visitor, opponent);
                Check(Near(visitor.CurrentBalance, immuneAttackerBefore) && Near(opponent.CurrentBalance, immuneTargetBefore),
                    "Pharma immunity blocks sanction before either side is charged");
                double immuneForceAuditAttackerBefore = visitor.CurrentBalance;
                double immuneForceAuditTargetBefore = opponent.CurrentBalance;
                int immuneForceAuditPositionBefore = turns.GetPlayerPosition(opponent);
                int immuneForceAuditTurnsBefore = opponent.AuditTurnsRemaining;
                int immuneForceAuditActivitiesBefore = capturedActivities.Count;
                marketplace.ExecuteSanction(visitor, opponent, SanctionType.ForceAudit);
                Check(Near(visitor.CurrentBalance, immuneForceAuditAttackerBefore)
                    && Near(opponent.CurrentBalance, immuneForceAuditTargetBefore)
                    && turns.GetPlayerPosition(opponent) == immuneForceAuditPositionBefore
                    && opponent.IsInAudit && opponent.AuditTurnsRemaining == immuneForceAuditTurnsBefore
                    && capturedActivities.Count == immuneForceAuditActivitiesBefore,
                    "Pharma immunity blocks Force Audit without payment, relocation, duration reset or activity");
                opponent.ImmunityTurnsRemaining = 0;

                var poorAttacker = new PlayerModel(19, 100, true);
                bool embargoStateBefore = opponentProp.IsEmbargoed;
                marketplace.ExecuteEmbargo(poorAttacker, opponentProp);
                Check(Near(poorAttacker.CurrentBalance, 100) && opponentProp.IsEmbargoed == embargoStateBefore,
                    "unaffordable embargo has no economic or property effect");

                owner.IsInAudit = true;
                turns.EnterAuditDecisionMode();
                turns.StayInAudit();
                Check(turns.CurrentState == TurnManager.TurnState.EndTurn,
                    "staying in Audit immediately after landing ends the turn");
                owner.IsInAudit = false;

                var seller = new PlayerModel(20, 1000, true);
                var buyer = new PlayerModel(21, 10000, true);
                var takeover = Property("OPA Target", 31, 1000, 100, dummyFamily);
                takeover.TotalUpgradeCostInvested = 1000; takeover.SetLevel(2); seller.AddProperty(takeover);
                board.tileDataList.Add(takeover);
                turns.StartGame(new List<PlayerModel> { seller, buyer });
                Check(Near(VentureSystem.CalculateHostileTakeoverPrice(takeover), 1750), "OPA price uses upgrade branch when higher than 150%");
                double moneyBefore = seller.CurrentBalance + buyer.CurrentBalance;
                Check(venture.ExecuteHostileTakeover(buyer, takeover), "affordable OPA succeeds");
                Check(buyer.OwnedProperties.Contains(takeover) && !seller.OwnedProperties.Contains(takeover) && takeover.CurrentLevel == 2,
                    "OPA transfers ownership and preserves upgrades");
                Check(Near(seller.CurrentBalance, 2750) && Near(buyer.CurrentBalance, 8250)
                    && Near(seller.CurrentBalance + buyer.CurrentBalance, moneyBefore), "OPA pays seller and conserves total cash");
                Check(capturedActivities.Any(item => item.kind == "venture"
                    && item.playerId == buyer.PlayerId
                    && item.text.Contains(takeover.PropertyName)),
                    "OPA publishes shared Venture activity feedback");

                var expensive = Property("Unaffordable OPA", 32, 10000, 100, dummyFamily); seller.AddProperty(expensive);
                buyer.SetBalance(100);
                Check(!venture.ExecuteHostileTakeover(buyer, expensive) && seller.OwnedProperties.Contains(expensive),
                    "unaffordable OPA leaves cash and ownership unchanged");

                var opaFamilyBuyer = new PlayerModel(25, 50000, true);
                var opaFamilySeller = new PlayerModel(26, 50000, true);
                var pharmaTitles = familyProperties["Pharma"];
                foreach (var title in pharmaTitles) owner.RemoveProperty(title);
                opaFamilyBuyer.AddProperty(pharmaTitles[0]);
                opaFamilyBuyer.AddProperty(pharmaTitles[1]);
                opaFamilySeller.AddProperty(pharmaTitles[2]);
                turns.StartGame(new List<PlayerModel> { opaFamilyBuyer, opaFamilySeller });
                capturedActivities.Clear();
                Check(venture.ExecuteHostileTakeover(opaFamilyBuyer, pharmaTitles[2])
                    && abilities.HasFamilyMonopoly(opaFamilyBuyer, "Pharma"),
                    "OPA acquisition of the third Pharma title completes the family");
                Check(capturedActivities.Any(item => item.kind == "family"
                    && item.playerId == opaFamilyBuyer.PlayerId && item.text.Contains("Pharma")),
                    "OPA family completion publishes the unlock activity");
                abilities.ChargeEnergyOnGo(opaFamilyBuyer);
                Check(abilities.HasEnoughEnergy(opaFamilyBuyer),
                    "OPA-completed Tier 1 family charges after GO");

                var tradeFamilyBuyer = new PlayerModel(27, 50000, true);
                var tradeFamilySeller = new PlayerModel(28, 50000, true);
                var autoTitles = familyProperties["Automobile"];
                foreach (var title in autoTitles) owner.RemoveProperty(title);
                tradeFamilyBuyer.AddProperty(autoTitles[0]);
                tradeFamilyBuyer.AddProperty(autoTitles[1]);
                tradeFamilySeller.AddProperty(autoTitles[2]);
                turns.StartGame(new List<PlayerModel> { tradeFamilyBuyer, tradeFamilySeller });
                capturedActivities.Clear();
                Check(marketplace.ExecuteTrade(tradeFamilyBuyer, tradeFamilySeller,
                    autoTitles[2], 1000)
                    && abilities.HasFamilyMonopoly(tradeFamilyBuyer, "Automobile"),
                    "Marketplace acquisition of the third Automobile title completes the family");
                Check(capturedActivities.Any(item => item.kind == "family"
                    && item.playerId == tradeFamilyBuyer.PlayerId && item.text.Contains("Automobile")),
                    "Marketplace family completion publishes the unlock activity");

                var liquidating = new PlayerModel(22, 100, false);
                var liquidatedProperty = Property("Liquidation Target", 33, 2000, 100, dummyFamily);
                liquidatedProperty.TotalUpgradeCostInvested = 1000;
                liquidatedProperty.SetLevel(2);
                liquidating.AddProperty(liquidatedProperty);
                Check(Near(upgrades.SellAllUpgrades(liquidating, liquidatedProperty), 500)
                    && Near(liquidating.CurrentBalance, 600)
                    && liquidatedProperty.CurrentLevel == 0,
                    "bankruptcy liquidation refunds upgrades at 50 percent");
                Check(Near(LiquidationHelper.MortgageProperty(liquidating, liquidatedProperty), 1000)
                    && Near(liquidating.CurrentBalance, 1600)
                    && liquidatedProperty.IsMortgaged,
                    "bankruptcy mortgage refunds property at 50 percent");
                Check(Near(LiquidationHelper.MortgageProperty(liquidating, liquidatedProperty), 0)
                    && Near(liquidating.CurrentBalance, 1600),
                    "bankruptcy mortgage cannot be collected twice");
                Check(Near(LiquidationHelper.SellPropertyToBank(liquidating, liquidatedProperty), 0)
                    && !liquidating.OwnedProperties.Contains(liquidatedProperty)
                    && Near(liquidating.CurrentBalance, 1600),
                    "selling a mortgaged title returns it without a second refund");

                var bankruptDebtor = new PlayerModel(23, 350, false);
                var bankruptcyCreditor = new PlayerModel(24, 100, false);
                var returnedProperty = Property("Returned On Bankruptcy", 34, 3000, 100, dummyFamily);
                returnedProperty.SetLevel(1);
                bankruptDebtor.AddProperty(returnedProperty);
                transactions.ProcessBankruptcyCash(bankruptDebtor, bankruptcyCreditor);
                Check(bankruptDebtor.IsBankrupt
                    && Near(bankruptDebtor.CurrentBalance, 0)
                    && Near(bankruptcyCreditor.CurrentBalance, 450)
                    && !bankruptDebtor.OwnedProperties.Contains(returnedProperty)
                    && returnedProperty.CurrentLevel == 0,
                    "final bankruptcy transfers remaining cash and returns titles to the bank");

                var offerSeller = new PlayerModel(0, 18000, false);
                var offerBuyer = new PlayerModel(1, 18000, false);
                var sellerTitle = Property("Seller Title", 1, 800, 80, dummyFamily);
                var buyerTitle = Property("Buyer Title", 3, 900, 90, dummyFamily);
                offerSeller.AddProperty(sellerTitle); offerBuyer.AddProperty(buyerTitle);
                turns.StartGame(new List<PlayerModel> { offerSeller, offerBuyer });
                var offerOnline = Component<OnlineGameController>();
                Set(offerOnline, "<IsOnlineGame>k__BackingField", true);
                Set(offerOnline, "<LocalSeatIndex>k__BackingField", 1);
                var offerUi = Component<PolyRisk.UI.Marketplace_UIManager>();
                offerUi.turnManager = turns;
                var contextType = Field(offerUi.GetType(), "selectionContext").FieldType;
                Set(offerUi, "selectionContext", Enum.Parse(contextType, "BidProperty"));
                Check(offerUi.IsTileEligibleForCurrentSelection(buyerTitle),
                    "Online bid selection permits local buyer title during seller turn");
                Check(!offerUi.IsTileEligibleForCurrentSelection(sellerTitle),
                    "Online bid selection rejects seller title during seller turn");
                Set(offerUi, "selectionContext", Enum.Parse(contextType, "TenderProperty"));
                Check(offerUi.IsTileEligibleForCurrentSelection(sellerTitle)
                    && !offerUi.IsTileEligibleForCurrentSelection(buyerTitle),
                    "Tender selection still follows active seller ownership");
                var draftRoot = new UnityEngine.UIElements.VisualElement();
                var draftPanel = new UnityEngine.UIElements.VisualElement { name = "Panel_Marketplace_BidderForm" };
                draftRoot.Add(draftPanel);
                Set(offerUi, "root", draftRoot);
                offerUi.marketplaceSystem = marketplace;
                marketplace.ApplyReplicatedTenderState(offerSeller, sellerTitle,
                    MarketplaceSystem.AuctionState.InvitingPlayers,
                    new List<KeyValuePair<int, MarketplaceSystem.TenderBid>>());
                typeof(PolyRisk.UI.Marketplace_UIManager).GetMethod("HandleAuctionStateChanged",
                    BindingFlags.NonPublic | BindingFlags.Instance).Invoke(offerUi,
                    new object[] { MarketplaceSystem.AuctionState.InvitingPlayers });
                Check(draftRoot.childCount == 1 && draftRoot[0] == draftPanel,
                    "Another buyer response does not reopen invitation over existing offer draft");
                Set(offerOnline, "<IsOnlineGame>k__BackingField", false);

                var auctionWinner = new PlayerModel(29, 50000, true);
                var luxeTitles = familyProperties["Luxe"];
                foreach (var title in luxeTitles) owner.RemoveProperty(title);
                auctionWinner.AddProperty(luxeTitles[0]);
                auctionWinner.AddProperty(luxeTitles[1]);
                turns.StartGame(new List<PlayerModel>
                {
                    auctionWinner,
                    new PlayerModel(32, 50000, true)
                });
                var auction = Component<AuctionManager>();
                Set(auction, "transactionManager", transactions);
                Set(auction, "currentAuctionProperty", luxeTitles[2]);
                Set(auction, "currentHighestBid", 1000d);
                Set(auction, "currentHighestBidder", auctionWinner);
                Set(auction, "isAuctionActive", true);
                capturedActivities.Clear();
                auction.EndAuction();
                Check(abilities.HasFamilyMonopoly(auctionWinner, "Luxe"),
                    "bank auction acquisition of the third Luxe title completes the family");
                Check(capturedActivities.Any(item => item.kind == "family"
                    && item.playerId == auctionWinner.PlayerId && item.text.Contains("Luxe")),
                    "bank auction family completion publishes the unlock activity");

                var tenderBuyer = new PlayerModel(30, 50000, true);
                var tenderSeller = new PlayerModel(31, 50000, true);
                var techTitles = familyProperties["Tech"];
                foreach (var title in techTitles) owner.RemoveProperty(title);
                tenderBuyer.AddProperty(techTitles[0]);
                tenderBuyer.AddProperty(techTitles[1]);
                tenderSeller.AddProperty(techTitles[2]);
                turns.StartGame(new List<PlayerModel> { tenderBuyer, tenderSeller });
                marketplace.ApplyReplicatedTenderState(tenderSeller, techTitles[2],
                    MarketplaceSystem.AuctionState.Reviewing,
                    new List<KeyValuePair<int, MarketplaceSystem.TenderBid>>());
                var tenderBid = new MarketplaceSystem.TenderBid
                {
                    bidder = tenderBuyer,
                    cashOffer = 1000
                };
                capturedActivities.Clear();
                Check(marketplace.SealTenderDeal(tenderBid)
                    && abilities.HasFamilyMonopoly(tenderBuyer, "Tech"),
                    "tender acquisition of the third Tech title completes the family");
                Check(capturedActivities.Any(item => item.kind == "family"
                    && item.playerId == tenderBuyer.PlayerId && item.text.Contains("Tech")),
                    "tender family completion publishes the unlock activity");

                Debug.Log($"MECHANICS REGRESSION PASS: {assertions} assertions");
            }
            finally
            {
                GameEvents.OnInsolvency = null; GameEvents.OnGameVictory = null;
                GameEvents.OnActivityPublished = null;
                GameBootstrapper.DeferGameLoop = false;
                foreach (var item in owned.AsEnumerable().Reverse()) if (item != null) UnityEngine.Object.DestroyImmediate(item);
                owned.Clear();
            }
        }
    }
}
