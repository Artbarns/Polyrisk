using System.Collections.Generic;
using UnityEngine;
using PolyRisk.Core;

namespace PolyRisk.Visuals
{
    /// <summary>
    /// Manages the visual representation of players (pawns).
    /// Handles spawning and movement coordination.
    /// </summary>
    public class PlayerVisualManager : MonoBehaviour
    {
        [Header("Assets")]
        [Tooltip("Prefab for the player pawn.")]
        [SerializeField] private GameObject pawnPrefab;

        [Tooltip("The starting position for new pawns (e.g., Go tile).")]
        [SerializeField] private Transform startPoint;

        // Map PlayerID to their PawnMovement script
        private Dictionary<int, PawnMovement> activePawns = new Dictionary<int, PawnMovement>();

        private void OnEnable()
        {
            GameEvents.OnPlayerPathCalculated += HandlePlayerPath;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerPathCalculated -= HandlePlayerPath;
        }

        private void Start()
        {
            // Auto-spawn players if TurnManager is ready.
            // Since TurnManager creates players in Start(), we might need to wait or check.
            // But TurnManager is created in Phase 2.
            // We can't access TurnManager.Instance.players easily (private).
            // BUT TurnManager could call us if we make SpawnPlayers public and TurnManager references us.
            // OR we can assume TurnManager.Instance.players will be available if we expose them.
            
            // For now, let's assume TurnManager will call SpawnPlayers, or we hook into OnTurnStarted.
            // But OnTurnStarted happens every turn.
            
            // Let's add a small delay or check.
            // Or better: Let TurnManager initialize this.
            // Since the prompt says "SpawnPlayers... Instantiates a pawn...", 
            // I will leave it to be called externally or I'll try to find players if possible.
            
            // To be robust:
            // I'll add a check in Update or Coroutine to spawn once.
        }

        /// <summary>
        /// Spawns pawns for all players in the game.
        /// </summary>
        /// <param name="players">List of player models.</param>
        /// <param name="startPos">The world position to spawn them at.</param>
        public void SpawnPlayers(List<PlayerModel> players, Vector3 startPos)
        {
            if (pawnPrefab == null)
            {
                Debug.LogError("Pawn Prefab is missing in PlayerVisualManager!");
                return;
            }

            // Clear existing
            activePawns.Clear();

            foreach (var player in players)
            {
                // Instantiate pawn
                GameObject pawnObj = Instantiate(pawnPrefab, startPos, Quaternion.identity);
                pawnObj.name = $"Pawn_Player_{player.PlayerId}";

                // Get PawnMovement component
                PawnMovement movement = pawnObj.GetComponent<PawnMovement>();
                if (movement == null)
                {
                    movement = pawnObj.AddComponent<PawnMovement>();
                }

                // Color tinting if supported (optional)
                var renderer = pawnObj.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    // Assign a distinct color based on ID
                    // Simple logic: Red, Blue, Green, Yellow...
                    Color[] colors = { Color.red, Color.blue, Color.green, Color.yellow, Color.cyan, Color.magenta };
                    renderer.material.color = colors[player.PlayerId % colors.Length];
                }

                activePawns[player.PlayerId] = movement;
            }
            
            Debug.Log($"Spawned {activePawns.Count} player pawns at {startPos}.");
        }

        /// <summary>
        /// Handles the movement event triggered by TurnManager.
        /// </summary>
        /// <param name="playerId">The moving player's ID.</param>
        /// <param name="path">The list of waypoints.</param>
        private void HandlePlayerPath(int playerId, List<Vector3> path)
        {
            if (activePawns.TryGetValue(playerId, out PawnMovement pawn))
            {
                // Execute movement
                pawn.MoveAlongPath(path, () => 
                {
                    // Notify system when done
                    GameEvents.OnPawnMovementFinished?.Invoke();
                });
            }
            else
            {
                Debug.LogWarning($"Pawn not found for Player {playerId}. Skipping visual movement.");
                // Ensure the game loop doesn't hang if visual is missing
                GameEvents.OnPawnMovementFinished?.Invoke();
            }
        }
    }
}
