using UnityEngine;
using PolyRisk.Core;

namespace PolyRisk.Systems
{
    /// <summary>
    /// Handles the randomization logic for dice rolls.
    /// Tracks consecutive doubles to enforce penalties.
    /// </summary>
    public class DiceSystem : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("The number of sides on each die.")]
        [SerializeField] private int dieSides = 6;
        
        [Tooltip("The threshold for consecutive doubles before penalty.")]
        [SerializeField] private int maxConsecutiveDoubles = 5;

        // State tracking
        private int consecutiveDoubles = 0;

        public int ConsecutiveDoubles => consecutiveDoubles;

        public void ApplyReplicatedDoubles(int count)
        {
            consecutiveDoubles = Mathf.Clamp(count, 0, maxConsecutiveDoubles);
        }

        /// <summary>
        /// Rolls two dice and updates the consecutive doubles counter.
        /// </summary>
        /// <returns>An integer array containing the two dice results.</returns>
        public int[] RollDice()
        {
            return CommitRoll(GenerateRoll());
        }

        // Candidate rolls have no gameplay or presentation side effects.
        public int[] GenerateRoll()
        {
            return new[] { Random.Range(1, dieSides + 1), Random.Range(1, dieSides + 1) };
        }

        public int[] CommitRoll(int[] candidate)
        {
            if (candidate == null || candidate.Length != 2
                || candidate[0] < 1 || candidate[0] > dieSides
                || candidate[1] < 1 || candidate[1] > dieSides)
                throw new System.ArgumentException("A roll must contain two valid dice.", nameof(candidate));
            int die1 = candidate[0];
            int die2 = candidate[1];
            int[] result = new[] { die1, die2 };

            // Trigger the event for UI or other listeners
            GameEvents.OnDiceRolled?.Invoke(result);
            var currentPlayer = TurnManager.Instance != null ? TurnManager.Instance.CurrentPlayer : null;
            if (currentPlayer != null)
            {
                GameEvents.PublishGameEvent(GameEventPhase.Action,
                    GameEventKind.DiceRolled, GameEventPriority.Major,
                    currentPlayer.PlayerId, reason: GameEventReason.DiceRoll,
                    valueA: die1, valueB: die2);
                GameEvents.PublishActivity("dice", currentPlayer.PlayerId,
                    $"lance {die1} + {die2} · total {die1 + die2}", die1 == die2);
            }

            if (die1 == die2)
            {
                consecutiveDoubles++;
                Debug.Log($"Doubles rolled! ({die1}, {die2}). Consecutive count: {consecutiveDoubles}");
            }
            else
            {
                consecutiveDoubles = 0;
                Debug.Log($"Rolled {die1} and {die2}.");
            }

            return result;
        }

        /// <summary>
        /// Resets the consecutive doubles counter (e.g., at the end of a turn).
        /// </summary>
        public void ResetDoubles()
        {
            consecutiveDoubles = 0;
        }

        /// <summary>
        /// Checks if the player has exceeded the allowed consecutive doubles limit.
        /// </summary>
        public bool HasExceededDoublesLimit()
        {
            return consecutiveDoubles >= maxConsecutiveDoubles;
        }
    }
}
