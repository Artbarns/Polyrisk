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

        /// <summary>
        /// Rolls two dice and updates the consecutive doubles counter.
        /// </summary>
        /// <returns>An integer array containing the two dice results.</returns>
        public int[] RollDice()
        {
            int die1 = Random.Range(1, dieSides + 1);
            int die2 = Random.Range(1, dieSides + 1);
            int[] result = new int[] { die1, die2 };

            // Trigger the event for UI or other listeners
            GameEvents.OnDiceRolled?.Invoke(result);

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
