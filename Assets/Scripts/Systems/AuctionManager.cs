using System;
using UnityEngine;
using PolyRisk.Core;

namespace PolyRisk.Systems
{
    /// <summary>
    /// Manages global auctions for properties.
    /// Handles bidding logic and ownership transfer upon completion.
    /// </summary>
    public class AuctionManager : MonoBehaviour
    {
        public static AuctionManager Instance { get; private set; }

        private TransactionManager transactionManager;

        // Auction State
        private PropertyData currentAuctionProperty;
        private double currentHighestBid;
        private PlayerModel currentHighestBidder;
        private bool isAuctionActive;

        // Events for UI and System communication
        public static Action<PropertyData> OnAuctionStarted;
        public static Action<PlayerModel, double> OnNewHighestBid;
        public static Action<PlayerModel, PropertyData, double> OnAuctionEnded;

        public bool IsAuctionActive => isAuctionActive;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                transactionManager = TransactionManager.Instance;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Starts a new auction for a property.
        /// </summary>
        /// <param name="property">The property to be auctioned.</param>
        public void StartAuction(PropertyData property)
        {
            if (property == null)
            {
                Debug.LogError("Auction start failed: Property is null.");
                return;
            }

            if (isAuctionActive)
            {
                Debug.LogWarning("Auction already in progress.");
                return;
            }

            currentAuctionProperty = property;
            currentHighestBid = 0; // Starting bid could be 0 or base price, rule usually implies low start.
            currentHighestBidder = null;
            isAuctionActive = true;

            Debug.Log($"Auction started for {property.PropertyName}!");
            OnAuctionStarted?.Invoke(property);
        }

        /// <summary>
        /// Places a bid on the current auction.
        /// </summary>
        /// <param name="player">The player placing the bid.</param>
        /// <param name="bidAmount">The bid amount.</param>
        /// <returns>True if the bid was accepted.</returns>
        public bool PlaceBid(PlayerModel player, double bidAmount)
        {
            if (!isAuctionActive)
            {
                Debug.LogWarning("No active auction.");
                return false;
            }

            if (player == null)
            {
                Debug.LogError("Bid failed: Player is null.");
                return false;
            }

            // Validation: Bid must be higher than current + increment
            // Minimum increment: 10 (as per requirement)
            double minimumBid = currentHighestBid + 10.0;
            
            // Special case: First bid needs to be at least 10? Or just > 0.
            if (currentHighestBid == 0 && bidAmount < 10)
            {
                 minimumBid = 10;
            }

            if (bidAmount < minimumBid)
            {
                Debug.LogWarning($"Bid rejected: {bidAmount} is too low. Minimum is {minimumBid}.");
                return false;
            }

            // Validation: Player must have funds
            if (player.CurrentBalance < bidAmount)
            {
                Debug.LogWarning($"Bid rejected: Player {player.PlayerId} has insufficient funds.");
                return false;
            }

            // Accept Bid
            currentHighestBid = bidAmount;
            currentHighestBidder = player;
            
            Debug.Log($"New highest bid: {bidAmount} by Player {player.PlayerId}.");
            OnNewHighestBid?.Invoke(player, bidAmount);
            
            return true;
        }

        /// <summary>
        /// Ends the current auction and processes the transaction.
        /// </summary>
        public void EndAuction()
        {
            if (!isAuctionActive) return;

            if (currentHighestBidder != null)
            {
                // Process payment
                bool paymentSuccess = transactionManager.DeductMoney(currentHighestBidder, currentHighestBid);
                
                if (paymentSuccess)
                {
                    // Transfer ownership
                    currentHighestBidder.AddProperty(currentAuctionProperty);
                    GameEvents.OnPropertyPurchased?.Invoke(currentHighestBidder.PlayerId, currentAuctionProperty.PropertyName);
                    AbilitySystem.PublishFamilyCompletionIfNeeded(currentHighestBidder,
                        new[] { currentAuctionProperty });

                    Debug.Log($"Auction won by Player {currentHighestBidder.PlayerId} for {currentHighestBid}.");
                    // [ACQUISITION] diagnostic temporaire — investigation rythme d'upgrade IA (tasks/todo.md).
                    Debug.LogWarning($"[ACQUISITION] {currentAuctionProperty.PropertyName} | from=Bank to=Player{currentHighestBidder.PlayerId} ({currentHighestBidder.Profile}) | channel=Auction | price={currentHighestBid:N0} | turn={currentHighestBidder.LapsCompleted}");
                    OnAuctionEnded?.Invoke(currentHighestBidder, currentAuctionProperty, currentHighestBid);
                }
                else
                {
                    // This edge case shouldn't happen if we check funds on bid, 
                    // unless funds changed during auction (e.g. random event).
                    Debug.LogError("Auction processing failed: Winner cannot pay.");
                    // Fallback logic needed (restart auction? cancel?)
                    OnAuctionEnded?.Invoke(null, currentAuctionProperty, 0);
                }
            }
            else
            {
                Debug.Log("Auction ended with no bids.");
                OnAuctionEnded?.Invoke(null, currentAuctionProperty, 0);
            }

            // Reset state
            isAuctionActive = false;
            currentAuctionProperty = null;
            currentHighestBid = 0;
            currentHighestBidder = null;
        }
    }
}
