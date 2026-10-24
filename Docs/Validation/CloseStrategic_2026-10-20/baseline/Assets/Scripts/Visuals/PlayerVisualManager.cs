using System.Collections.Generic;
using UnityEngine;
using PolyRisk.Core;
using PolyRisk.Systems;

namespace PolyRisk.Visuals
{
    /// <summary>
    /// Manages the visual representation of players (pawns).
    /// Handles spawning and movement coordination.
    /// </summary>
    public class PlayerVisualManager : MonoBehaviour
    {
        private static readonly Color[] PlayerColors =
        {
            new Color(0.94f, 0.31f, 0.34f),
            new Color(0.31f, 0.62f, 0.98f),
            new Color(0.34f, 0.82f, 0.58f),
            new Color(0.96f, 0.72f, 0.28f),
            new Color(0.30f, 0.80f, 0.84f),
            new Color(0.76f, 0.48f, 0.92f)
        };

        public static Color GetPlayerColor(int playerId)
        {
            int index = playerId % PlayerColors.Length;
            if (index < 0) index += PlayerColors.Length;
            return PlayerColors[index];
        }

        public static event System.Action<int,PawnMovement,List<Vector3>> VisualMovementStarted;
        internal static void NotifyVisualMovementStarted(int playerId,PawnMovement pawn,List<Vector3> path) =>
            VisualMovementStarted?.Invoke(playerId,pawn,path);

        private const float PawnBaseScale = 0.76f;
        private const float ActivePawnScale = 0.91f;

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
            GameEvents.OnTurnStarted += HandleTurnStarted;
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerPathCalculated -= HandlePlayerPath;
            GameEvents.OnTurnStarted -= HandleTurnStarted;
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

            // Clear existing pawns before rebuilding a local or online roster.
            foreach (var pawn in activePawns.Values)
                if (pawn != null) Destroy(pawn.gameObject);
            activePawns.Clear();

            foreach (var player in players)
            {
                // Instantiate pawn
                GameObject pawnObj = Instantiate(pawnPrefab, startPos, Quaternion.identity);
                pawnObj.name = $"Pawn_Player_{player.PlayerId}";
                pawnObj.transform.localScale = Vector3.one * PawnBaseScale;

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
                    renderer.material.color = GetPlayerColor(player.PlayerId);
                }

                activePawns[player.PlayerId] = movement;
            }

            int activePlayerId = TurnManager.Instance != null && TurnManager.Instance.CurrentPlayer != null
                ? TurnManager.Instance.CurrentPlayer.PlayerId : -1;
            UpdateActivePawn(activePlayerId);
            
            Debug.Log($"Spawned {activePawns.Count} player pawns at {startPos}.");
        }

        private void HandleTurnStarted(int playerId) => UpdateActivePawn(playerId);

        private void UpdateActivePawn(int activePlayerId)
        {
            foreach (var pair in activePawns)
            {
                if (pair.Value == null) continue;
                bool isActive = pair.Key == activePlayerId;
                float pawnScale = isActive ? ActivePawnScale : PawnBaseScale;
                pair.Value.transform.localScale = Vector3.one * pawnScale;
                var collider = pair.Value.GetComponent<CapsuleCollider>();
                if (collider != null)
                {
                    // Keep the original world-space hit target despite the larger mesh.
                    collider.radius = 0.25f / pawnScale;
                    collider.height = 1.0f / pawnScale;
                }

                var renderer = pair.Value.GetComponentInChildren<Renderer>();
                if (renderer == null) continue;
                var material = renderer.material;
                Color playerColor = GetPlayerColor(pair.Key);
                material.color = isActive ? Color.Lerp(playerColor, Color.white, 0.18f) : playerColor;
                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", isActive ? playerColor * 0.22f : Color.black);
                }
            }
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
                // Presentation notification at the real visual dispatch; completion contract remains unchanged.
                if (path != null && path.Count > 0 && !GameBootstrapper.SkipMovementAnimation &&
                    (path[0]-pawn.transform.position).sqrMagnitude > .0001f)
                    NotifyVisualMovementStarted(playerId,pawn,path);
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
