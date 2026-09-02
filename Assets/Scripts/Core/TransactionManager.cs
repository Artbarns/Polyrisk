using System;
using UnityEngine;

namespace PolyRisk.Core
{
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
        public bool TransferMoney(PlayerModel sender, PlayerModel receiver, double amount)
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
            if (debtor == null || creditor == null)
            {
                Debug.LogError("Bankruptcy Process failed: Debtor or Creditor is null.");
                return;
            }

            double remainingCash = debtor.CurrentBalance;
            if (remainingCash > 0)
            {
                debtor.SetBalance(0);
                creditor.SetBalance(creditor.CurrentBalance + remainingCash);
                
                GameEvents.OnBalanceChanged?.Invoke(debtor.PlayerId, 0);
                GameEvents.OnBalanceChanged?.Invoke(creditor.PlayerId, creditor.CurrentBalance);
                
                Debug.Log($"Bankruptcy: Transferred remaining {remainingCash} from Player {debtor.PlayerId} to Player {creditor.PlayerId}.");
            }
        }
    }
}
