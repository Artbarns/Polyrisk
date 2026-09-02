using UnityEngine;
using PolyRisk.Core;

namespace PolyRisk.Systems
{
    /// <summary>
    /// Tracks key game events for analytics purposes.
    /// Logs data points like purchases, auctions, investments, and bankruptcy.
    /// </summary>
    public class AnalyticsTracker : MonoBehaviour
    {
        private void Awake()
        {
            // Subscribe to events
            GameEvents.OnPropertyPurchased += TrackPropertyPurchased;
            GameEvents.OnAuctionEnded += TrackAuctionParticipation;
            GameEvents.OnStockMarketInvestment += TrackStockMarketInvestment;
            GameEvents.OnPlayerBankruptcy += TrackPlayerBankruptcy;
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            GameEvents.OnPropertyPurchased -= TrackPropertyPurchased;
            GameEvents.OnAuctionEnded -= TrackAuctionParticipation;
            GameEvents.OnStockMarketInvestment -= TrackStockMarketInvestment;
            GameEvents.OnPlayerBankruptcy -= TrackPlayerBankruptcy;
        }

        /// <summary>
        /// Logs property purchases.
        /// </summary>
        private void TrackPropertyPurchased(int playerId, string propertyName)
        {
            // Note: The event only passes ID and Name. Price is not in the event payload currently.
            // We'll log what we have.
            Debug.Log($"[ANALYTICS] Player {playerId} purchased {propertyName}.");
        }

        /// <summary>
        /// Logs auction results.
        /// </summary>
        private void TrackAuctionParticipation(int totalBids, double finalPrice)
        {
            Debug.Log($"[ANALYTICS] Auction ended with {totalBids} bids at price {finalPrice}.");
        }

        /// <summary>
        /// Logs stock market investments.
        /// </summary>
        private void TrackStockMarketInvestment(double amount, int turns)
        {
            Debug.Log($"[ANALYTICS] Stock Market Investment: {amount} for {turns} turns.");
        }

        /// <summary>
        /// Logs player bankruptcy.
        /// </summary>
        private void TrackPlayerBankruptcy(int playerId, int turnNumber)
        {
            Debug.Log($"[ANALYTICS] Player {playerId} went bankrupt on turn {turnNumber}.");
        }
    }
}
