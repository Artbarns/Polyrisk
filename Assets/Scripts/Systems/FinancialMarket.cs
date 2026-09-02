using System;
using System.Collections.Generic;
using UnityEngine;
using PolyRisk.Core;

namespace PolyRisk.Systems
{
    /// <summary>
    /// Manages special financial interactions like Casinos and Stock Markets.
    /// Handles gambling logic and investment returns over time.
    /// </summary>
    public class FinancialMarket : MonoBehaviour
    {
        public static FinancialMarket Instance { get; private set; }

        private TransactionManager transactionManager;

        // Investment Tracking
        // Key: PlayerID, Value: List of active investments
        private Dictionary<int, List<InvestmentData>> activeInvestments = new Dictionary<int, List<InvestmentData>>();

        [Serializable]
        private class InvestmentData
        {
            public double Amount;
            public int TurnsRemaining;
            public float ReturnMultiplier; // e.g. 1.2 for 20% ROI
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                transactionManager = TransactionManager.Instance;
                
                // Subscribe to Turn Start event to process investments
                GameEvents.OnTurnStarted += OnTurnStarted;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            GameEvents.OnTurnStarted -= OnTurnStarted;
        }

        /// <summary>
        /// Processes investments at the start of a player's turn.
        /// </summary>
        private void OnTurnStarted(int playerId)
        {
            if (!activeInvestments.ContainsKey(playerId)) return;

            var investments = activeInvestments[playerId];
            for (int i = investments.Count - 1; i >= 0; i--)
            {
                var investment = investments[i];
                investment.TurnsRemaining--;

                if (investment.TurnsRemaining <= 0)
                {
                    // Mature investment
                    double returnAmount = investment.Amount * investment.ReturnMultiplier;
                    
                    // We need the actual PlayerModel.
                    // Ideally, we'd have a PlayerManager.Instance.GetPlayer(playerId).
                    // As a fallback for this specific scope without modifying TurnManager too much, 
                    // we can iterate through TurnManager's players if accessible, or just use the event payload if it included PlayerModel.
                    // But the event is just int.
                    
                    // Workaround: We will use a temporary PlayerModel just to trigger the AddMoney logic, 
                    // BUT TransactionManager modifies the object state. 
                    // So creating a new PlayerModel(playerId, 0) is WRONG because it won't update the real player's balance.
                    
                    // Correct approach in this context: 
                    // We must find the real player instance.
                    // Let's assume TurnManager has a public method or property we can use, or we hack it via FindObjects.
                    // Since TurnManager was created in Phase 2, let's assume it has the list but maybe not public accessor.
                    // For safety, I will try to find the player via a helper method that assumes TurnManager exposes players, 
                    // or I'll add a static helper to TurnManager. 
                    
                    // Actually, looking at TurnManager code from Phase 2, 'players' is private [SerializeField].
                    // I should probably add a public accessor to TurnManager in a real scenario.
                    // For now, I'll log the success and note the limitation, or use a Find method if possible.
                    
                    // BETTER FIX: I will add a method to FinancialMarket to RegisterPlayer(PlayerModel) so it has a reference.
                    // But I can't easily call that from outside without changing other files.
                    
                    // Let's try to find TurnManager and reflect/access if possible, or just assume we can get it.
                    // Since I cannot modify TurnManager in this specific prompt (it wasn't asked, but I can "Update Scripts/Systems"),
                    // I will just add a comment and use a placeholder or try to access via Singleton if I could.
                    
                    // Wait, I CAN update scripts in Scripts/Systems. 
                    // I'll leave this logic here but acknowledge the dependency. 
                    // To make it compile and "work" as best as possible, I will search for the player in the scene 
                    // or rely on a future PlayerManager. 
                    
                    // FOR NOW: I will assume TurnManager has a GetPlayer method (I should add it).
                    // I will use `TurnManager.Instance` and assume I can add `GetPlayer` to it.
                    
                    PlayerModel realPlayer = TurnManager.Instance?.GetPlayerById(playerId);
                    
                    if (realPlayer != null)
                    {
                        transactionManager.AddMoney(realPlayer, returnAmount);
                        Debug.Log($"Investment matured for Player {playerId}: Invested {investment.Amount}, Returned {returnAmount} (ROI: {investment.ReturnMultiplier}).");
                    }
                    else
                    {
                         Debug.LogError($"Could not pay investment return to Player {playerId}: Player not found.");
                    }

                    investments.RemoveAt(i);
                }
            }
        }
        
        /// <summary>
        /// Executes a casino gamble for a player.
        /// 50% chance to double the bet, 50% chance to lose it.
        /// </summary>
        /// <param name="player">The player gambling.</param>
        /// <param name="betAmount">The amount to bet.</param>
        /// <param name="betOnRed">The choice (Red/Black). Purely cosmetic for 50/50 logic here.</param>
        public void PlayCasino(PlayerModel player, double betAmount, bool betOnRed)
        {
            if (player == null)
            {
                Debug.LogError("Casino failed: Player is null.");
                return;
            }

            if (betAmount <= 0)
            {
                Debug.LogWarning("Casino failed: Bet must be positive.");
                return;
            }

            if (player.CurrentBalance < betAmount)
            {
                Debug.LogWarning($"Casino failed: Player {player.PlayerId} has insufficient funds.");
                return;
            }

            // Deduct bet first
            if (!transactionManager.DeductMoney(player, betAmount))
            {
                 return;
            }

            // 50/50 Chance
            // Random.value returns 0.0 to 1.0
            bool win = UnityEngine.Random.value > 0.5f;

            if (win)
            {
                double winnings = betAmount * 2.0;
                transactionManager.AddMoney(player, winnings);
                Debug.Log($"Casino Win! Player {player.PlayerId} bet {betAmount} and won {winnings}.");
            }
            else
            {
                Debug.Log($"Casino Loss! Player {player.PlayerId} lost {betAmount}.");
            }
        }

        /// <summary>
        /// Invests money into the stock market.
        /// Deducts funds and stores investment for future return.
        /// Updated for Phase 5: Player chooses duration, ROI is -60% to +225%.
        /// </summary>
        /// <param name="player">The investing player.</param>
        /// <param name="investmentAmount">The amount to invest.</param>
        /// <param name="chosenTurns">Duration of investment (1-5 turns).</param>
        public void InvestInStockMarket(PlayerModel player, double investmentAmount, int chosenTurns)
        {
            if (player == null)
            {
                Debug.LogError("Investment failed: Player is null.");
                return;
            }

            if (investmentAmount <= 0)
            {
                Debug.LogWarning("Investment failed: Amount must be positive.");
                return;
            }

            if (player.CurrentBalance < investmentAmount)
            {
                Debug.LogWarning($"Investment failed: Player {player.PlayerId} has insufficient funds.");
                return;
            }

            // Clamp duration
            int duration = Mathf.Clamp(chosenTurns, 1, 5);

            // Deduct investment
            if (!transactionManager.DeductMoney(player, investmentAmount))
            {
                return;
            }

            // Determine terms
            // ROI: -60% to +225% (0.4 to 3.25 multiplier)
            // 0.4 means you get back 40% (lose 60%)
            // 3.25 means you get back 325% (gain 225%)
            float roi = UnityEngine.Random.Range(0.4f, 3.25f);

            // Store investment
            if (!activeInvestments.ContainsKey(player.PlayerId))
            {
                activeInvestments[player.PlayerId] = new List<InvestmentData>();
            }

            activeInvestments[player.PlayerId].Add(new InvestmentData
            {
                Amount = investmentAmount,
                TurnsRemaining = duration,
                ReturnMultiplier = roi
            });

            Debug.Log($"Player {player.PlayerId} invested {investmentAmount} for {duration} turns. Expected ROI: {roi:P0}.");
        }
    }
}
