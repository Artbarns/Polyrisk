using System;

namespace PolyRisk.Core
{
    [Serializable]
    public sealed class GameActivityEntry
    {
        public long id;
        public string kind;
        public int playerId;
        public string text;
        public bool emphasis;
    }

    /// <summary>
    /// Static class serving as a centralized event bus for the game's core logic.
    /// Updated to support 'double' for currency and new turn-based events.
    /// </summary>
    public static class GameEvents
    {
        private static long nextActivityId;
        private static long nextGameEventId;
        private static long nextCorrelationId;

        /// <summary>Typed, confirmed gameplay facts for presentation and history.</summary>
        public static Action<GameEvent> OnGameEventPublished;

        public static long LastGameEventId => nextGameEventId;
        public static long LastCorrelationId => nextCorrelationId;

        public static long CreateCorrelationId() => ++nextCorrelationId;

        /// <summary>Restores monotonic event IDs after an online authority reload or handoff.</summary>
        public static void AdvanceSequences(long gameEventId, long correlationId)
        {
            nextGameEventId = Math.Max(nextGameEventId, Math.Max(0, gameEventId));
            nextCorrelationId = Math.Max(nextCorrelationId, Math.Max(0, correlationId));
        }

        /// <summary>Re-emits a server-confirmed fact to local presenters without minting a new ID.</summary>
        public static void ReplayGameEvent(GameEvent gameEvent)
        {
            if (gameEvent == null || gameEvent.Id <= 0) return;
            AdvanceSequences(gameEvent.Id, gameEvent.CorrelationId);
            OnGameEventPublished?.Invoke(gameEvent);
        }

        public static GameEvent PublishGameEvent(
            GameEventPhase phase,
            GameEventKind kind,
            GameEventPriority priority,
            int actorId,
            int targetId = -1,
            int tileId = -1,
            double amount = 0,
            GameEventReason reason = GameEventReason.None,
            long correlationId = 0,
            int reasonParameterId = -1,
            double reasonParameterValue = 0,
            int valueA = 0,
            int valueB = 0,
            System.Collections.Generic.IEnumerable<GameEventSplit> splits = null,
            bool coalescible = false)
        {
            long id = ++nextGameEventId;
            if (correlationId <= 0) correlationId = id;
            var gameEvent = new GameEvent(id, correlationId, phase, kind, priority,
                actorId, targetId, tileId, amount, reason, reasonParameterId,
                reasonParameterValue, valueA, valueB, splits, coalescible);
            OnGameEventPublished?.Invoke(gameEvent);
            return gameEvent;
        }

        /// <summary>
        /// Compact, presentation-ready game activity. The authoritative online client
        /// keeps the recent entries in its replicated snapshot.
        /// </summary>
        public static Action<GameActivityEntry> OnActivityPublished;
        public static GameActivityEntry PublishActivity(
            string kind,
            int playerId,
            string text,
            bool emphasis = false)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var entry = new GameActivityEntry
            {
                id = ++nextActivityId,
                kind = string.IsNullOrWhiteSpace(kind) ? "info" : kind,
                playerId = playerId,
                text = text,
                emphasis = emphasis
            };
            OnActivityPublished?.Invoke(entry);
            return entry;
        }

        public static void ReplayActivity(GameActivityEntry entry)
        {
            if (entry == null || entry.id <= 0 || string.IsNullOrWhiteSpace(entry.text)) return;
            nextActivityId = Math.Max(nextActivityId, entry.id);
            OnActivityPublished?.Invoke(entry);
        }

        public static void BeginActivitySession()
        {
            nextActivityId = 0;
            nextGameEventId = 0;
            nextCorrelationId = 0;
        }

        /// <summary>
        /// Triggered when a player's balance changes.
        /// <param name="playerId">The ID of the player whose balance changed.</param>
        /// <param name="newBalance">The updated balance amount (double).</param>
        /// </summary>
        public static Action<int, double> OnBalanceChanged;

        /// <summary>
        /// Triggered when a property is successfully purchased.
        /// <param name="playerId">The ID of the purchasing player.</param>
        /// <param name="propertyId">The ID of the purchased property.</param>
        /// </summary>
        public static Action<int, string> OnPropertyPurchased;

        /// <summary>
        /// Triggered when a new turn starts for a specific player.
        /// <param name="playerId">The ID of the player starting their turn.</param>
        /// </summary>
        public static Action<int> OnTurnStarted;

        /// <summary>
        /// Triggered when dice are rolled.
        /// <param name="diceResult">An array of integers representing the result of each die.</param>
        /// </summary>
        public static Action<int[]> OnDiceRolled;

        /// <summary>
        /// Triggered when a board tile is selected by the user.
        /// </summary>
        public static Action<BoardTile> OnTileSelected;

        /// <summary>
        /// Triggered when a player's path is calculated after dice roll.
        /// </summary>
        public static Action<int, System.Collections.Generic.List<UnityEngine.Vector3>> OnPlayerPathCalculated;

        /// <summary>
        /// Triggered when a pawn finishes moving along its path.
        /// </summary>
        public static Action OnPawnMovementFinished;

        /// <summary>
        /// Triggered when an auction ends successfully.
        /// <param name="totalBids">Number of bids made.</param>
        /// <param name="finalPrice">The final sale price.</param>
        /// </summary>
        public static Action<int, double> OnAuctionEnded;

        /// <summary>
        /// Triggered when a player invests in the stock market.
        /// <param name="amount">Amount invested.</param>
        /// <param name="turns">Duration of investment.</param>
        /// </summary>
        public static Action<double, int> OnStockMarketInvestment;

        /// <summary>
        /// Triggered when a player goes bankrupt.
        /// <param name="playerId">The bankrupt player ID.</param>
        /// <param name="turnNumber">The turn number.</param>
        /// </summary>
        public static Action<int, int> OnPlayerBankruptcy;

        /// <summary>
        /// Phase E.3 — Victoire de fin de partie.
        /// Arg1 : joueur gagnant. Arg2 : condition affichable (Hyper-Mogul ou Time-Out).
        /// </summary>
        public static Action<PlayerModel, string> OnGameVictory;

        /// <summary>
        /// Phase E.4 (Fix #3 Phase F) — Insolvabilité détectée. Le BankruptcyScreen prend la main.
        /// Arg1 : joueur en faillite. Arg2 : montant dû. Arg3 : créancier (null si banque). Arg4 : callback après paiement/faillite.
        /// Payload asynchrone : le paiement final au créancier est effectué par le controller
        /// après fermeture du screen (retry TransferMoney ou faillite finale).
        /// </summary>
        public static Action<PlayerModel, double, PlayerModel, Action<bool>, DebtSettlementContext> OnInsolvency;
    }
}
