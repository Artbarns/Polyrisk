using System;
using UnityEngine;
using PolyRisk.Systems;
using System.Linq;

namespace PolyRisk.Core
{
    [Serializable]
    public class DebtSettlementContext
    {
        public string kind;
        public int propertyTileIndex = -1;
        public int hijackerPlayerId = -1;
        public double creditorBalanceBefore;
    }

    /// <summary>
    /// Manages all monetary transactions in the game.
    /// Updated to support 'double' for currency.
    /// Converted to MonoBehaviour for Unity Dependency Injection.
    /// </summary>
    public class TransactionManager : MonoBehaviour
    {
        public static TransactionManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void PayRequired(PlayerModel player, double amount, PlayerModel creditor, Action<bool> completed,
            DebtSettlementContext context = null)
        {
            bool Pay() => creditor == null ? DeductMoney(player, amount) : TransferMoney(player, creditor, amount);
            if (amount <= 0) { completed?.Invoke(true); return; }
            if (player.CurrentBalance >= amount) { completed?.Invoke(Pay()); return; }
            if (player.IsAI)
            {
                // Economic simulation must not depend on a UI component being subscribed.
                LiquidationHelper.LiquidateToCover(player, amount);
                bool paid = player.CurrentBalance >= amount && Pay();
                if (!paid) ProcessBankruptcyCash(player, creditor);
                completed?.Invoke(paid);
                return;
            }
            if (GameEvents.OnInsolvency == null)
            {
                Debug.LogError("No insolvency UI available for a human debt.");
                return; // Do not silently forgive the debt or eliminate a human.
            }
            GameEvents.OnInsolvency.Invoke(player, amount, creditor, completed, context);
        }

        // Removed Lazy singleton logic
        // private static readonly Lazy<TransactionManager> lazy = ...
        // private TransactionManager() { }

        /// <summary>
        /// Transfers money from one player to another.
        /// </summary>
        /// <param name="sender">The player sending the money.</param>
        /// <param name="receiver">The player receiving the money.</param>
        /// <param name="amount">The amount to transfer (double).</param>
        /// <returns>True if successful, false otherwise.</returns>
        public bool TransferMoney(PlayerModel sender, PlayerModel receiver, double amount,
            GameEventReason reason = GameEventReason.Other, long correlationId = 0, int tileId = -1)
        {
            if (sender == null || receiver == null)
            {
                Debug.LogError("Transfer failed: Sender or Receiver is null.");
                return false;
            }

            if (amount <= 0)
            {
                Debug.LogWarning("Transfer failed: Amount must be positive.");
                return false;
            }

            if (sender.CurrentBalance < amount)
            {
                Debug.LogWarning($"Transfer failed: Player {sender.PlayerId} has insufficient funds ({sender.CurrentBalance} < {amount}).");
                return false;
            }

            try
            {
                sender.SetBalance(sender.CurrentBalance - amount);
                receiver.SetBalance(receiver.CurrentBalance + amount);

                GameEvents.OnBalanceChanged?.Invoke(sender.PlayerId, sender.CurrentBalance);
                GameEvents.OnBalanceChanged?.Invoke(receiver.PlayerId, receiver.CurrentBalance);

                GameEvents.PublishGameEvent(GameEventPhase.Resolution,
                    GameEventKind.TransferConfirmed, GameEventPriority.Normal,
                    sender.PlayerId, receiver.PlayerId, tileId, -amount, reason,
                    correlationId, splits: new[] { new GameEventSplit(receiver.PlayerId, amount) },
                    coalescible: reason == GameEventReason.Rent);

                Debug.Log($"Transfer successful: {amount} from Player {sender.PlayerId} to Player {receiver.PlayerId}.");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Transfer error: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Deducts money from a player (e.g., paying the bank, penalties).
        /// </summary>
        /// <param name="player">The player to deduct from.</param>
        /// <param name="amount">The amount to deduct.</param>
        /// <returns>True if successful, false if insufficient funds.</returns>
        public bool DeductMoney(PlayerModel player, double amount)
        {
            if (player == null)
            {
                Debug.LogError("Deduct failed: Player is null.");
                return false;
            }

            if (amount <= 0)
            {
                Debug.LogWarning("Deduct failed: Amount must be positive.");
                return false;
            }

            if (player.CurrentBalance < amount)
            {
                Debug.LogWarning($"Deduct failed: Player {player.PlayerId} has insufficient funds.");
                return false;
            }

            player.SetBalance(player.CurrentBalance - amount);
            GameEvents.OnBalanceChanged?.Invoke(player.PlayerId, player.CurrentBalance);
            
            Debug.Log($"Deducted {amount} from Player {player.PlayerId}. New Balance: {player.CurrentBalance}");
            return true;
        }

        /// <summary>
        /// Adds money to a player (e.g., passive income, Go bonus).
        /// </summary>
        /// <param name="player">The player receiving money.</param>
        /// <param name="amount">The amount to add.</param>
        public void AddMoney(PlayerModel player, double amount)
        {
             if (player == null)
            {
                Debug.LogError("AddMoney failed: Player is null.");
                return;
            }
            
            if (amount <= 0)
            {
                 Debug.LogWarning("AddMoney failed: Amount must be positive.");
                 return;
            }

            player.SetBalance(player.CurrentBalance + amount);
            GameEvents.OnBalanceChanged?.Invoke(player.PlayerId, player.CurrentBalance);
            
            Debug.Log($"Added {amount} to Player {player.PlayerId}. New Balance: {player.CurrentBalance}");
        }

        /// <summary>
        /// Handles the bankruptcy process for a debtor.
        /// Transfers remaining cash to the creditor and clears the debtor's balance.
        /// </summary>
        /// <param name="debtor">The bankrupt player.</param>
        /// <param name="creditor">The player owed money.</param>
        public void ProcessBankruptcyCash(PlayerModel debtor, PlayerModel creditor)
        {
            if (debtor == null || debtor.IsBankrupt) return;
            double remainingCash = debtor.CurrentBalance;
            debtor.SetBalance(0);
            GameEvents.OnBalanceChanged?.Invoke(debtor.PlayerId, 0);
            if (creditor != null && remainingCash > 0) AddMoney(creditor, remainingCash);
            foreach (var property in debtor.OwnedProperties.ToList())
            {
                debtor.RemoveProperty(property);
                property.ResetRuntimeState();
            }
            debtor.IsBankrupt = true;
            // Sabotage on someone else's property must not keep paying an eliminated player.
            var board = BoardManager.Instance;
            if (board != null && board.tileDataList != null)
                foreach (var property in board.tileDataList.OfType<PropertyData>())
                    if (property.HijackedBy == debtor)
                    {
                        property.HijackedBy = null;
                        property.HijackTurnsRemaining = 0;
                    }
            GameEvents.OnPlayerBankruptcy?.Invoke(debtor.PlayerId, debtor.LapsCompleted);
            GameEvents.PublishActivity("bankruptcy", debtor.PlayerId, "est en faillite", true);
        }
    }
}
