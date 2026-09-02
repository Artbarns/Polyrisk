using System;

namespace PolyRisk.Core
{
    /// <summary>
    /// Static class serving as a centralized event bus for the game's core logic.
    /// Updated to support 'double' for currency and new turn-based events.
    /// </summary>
    public static class GameEvents
    {
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
        /// Arg1 : joueur en faillite. Arg2 : montant dû. Arg3 : créancier (null si banque).
        /// Payload asynchrone : le paiement final au créancier est effectué par le controller
        /// après fermeture du screen (retry TransferMoney ou faillite finale).
        /// </summary>
        public static Action<PlayerModel, double, PlayerModel> OnInsolvency;
    }
}
