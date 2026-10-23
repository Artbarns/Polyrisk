using System.Collections.Generic;
using UnityEngine;
using PolyRisk.Core;

namespace PolyRisk.Systems
{
    /// <summary>
    /// Manages the logical representation of the game board.
    /// Holds the collection of all 40 tiles and provides access methods.
    /// </summary>
    public class BoardManager : MonoBehaviour
    {
        public static BoardManager Instance { get; private set; }

        [Header("Board Configuration")]
        [Tooltip("The total number of tiles on the board.")]
        [SerializeField] private int totalTiles = 40;

        // The internal list of board tiles.
        private List<BoardTile> tiles;
        private readonly List<PropertyData> runtimeProperties = new List<PropertyData>();

        private void OnDestroy()
        {
            foreach (var property in runtimeProperties) if (property != null) Destroy(property);
        }

        // Public read-only access to tiles.
        public IReadOnlyList<BoardTile> Tiles => tiles.AsReadOnly();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                // Removed automatic InitializeBoard call.
                // It will be called by GameBootstrapper.
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Initializes the board with standard layout.
        /// Publicly accessible for Bootstrapper.
        /// </summary>
        public void InitializeStandardBoard()
        {
            InitializeBoard();
        }

        /// <summary>
        /// Internal initialization logic.
        /// </summary>
        private void InitializeBoard()
        {
            tiles = new List<BoardTile>(totalTiles);
            
            // If we have a full board definition in the list, use it
            if (tileDataList != null && tileDataList.Count > 0)
            {
                // Sort by index to ensure order
                tileDataList.Sort((a, b) => a.TileIndex.CompareTo(b.TileIndex));

                // Never mutate project assets during a game or retain state across batches.
                tileDataList = new List<BoardTileData>(tileDataList);
                for (int i = 0; i < tileDataList.Count; i++)
                {
                    if (!(tileDataList[i] is PropertyData source)) continue;
                    var runtime = Instantiate(source);
                    runtime.ResetRuntimeState();
                    runtimeProperties.Add(runtime);
                    tileDataList[i] = runtime;
                }
                foreach (var tileData in tileDataList)
                {
                    // Pass the generic BoardTileData (which might be PropertyData or BoardTileData)
                    tiles.Add(new BoardTile(tileData.TileIndex, tileData.TileType, tileData));
                }
            }
            else
            {
                // Fallback / Manual Init (Legacy)
                for (int i = 0; i < totalTiles; i++)
                {
                    BoardTileData data = null;
                    TileType type = i switch
                    {
                        0 => TileType.Go_Start,
                        10 => TileType.Prison,
                        20 => TileType.GlobalAuction,
                        30 => TileType.SafeZone,
                        _ => TileType.Property
                    };

                    tiles.Add(new BoardTile(i, type, data));
                }
            }
            
            Debug.Log($"Board initialized with {tiles.Count} tiles.");
        }

        [Header("Data References")]
        [Tooltip("Database of tiles to populate the board.")]
        public List<BoardTileData> tileDataList;

        [Header("Visual References")]
        [Tooltip("Direct reference to visual tile transforms.")]
        public Transform[] visualTiles = new Transform[40];
        
        // Cache visual transforms for pathfinding (now using visualTiles array)
        // private List<Transform> tileTransforms; // Deprecated by visualTiles array

        private void Start()
        {
            // Removed auto-discovery to rely on explicit assignment from BoardGenerator or Inspector
        }

        private void InitializeVisualsCache()
        {
             // Deprecated
        }

        /// <summary>
        /// Calculates the physical path (positions) for pawn movement.
        /// </summary>
        /// <param name="startIndex">The current tile index.</param>
        /// <param name="steps">Number of steps to move.</param>
        /// <returns>List of world positions for the path.</returns>
        public List<Vector3> GetPath(int startIndex, int steps)
        {
            List<Vector3> path = new List<Vector3>();
            
            if (visualTiles == null || visualTiles.Length == 0)
            {
                Debug.LogWarning("BoardManager: visualTiles array is empty.");
                return path;
            }

            for (int i = 1; i <= steps; i++)
            {
                int nextIndex = (startIndex + i) % 40; // Modulo 40
                
                if (visualTiles[nextIndex] != null)
                {
                    path.Add(visualTiles[nextIndex].position);
                }
                else
                {
                    Debug.LogWarning($"BoardManager: visualTiles[{nextIndex}] is null!");
                    // Add current position or skip? Adding zero might be confusing. 
                    // Better to skip or add last known.
                }
            }

            return path;
        }
        /// <summary>
        /// Retrieves the tile data at a specific index.
        /// Handles wrapping for movement logic if needed, but primarily for direct access.
        /// </summary>
        /// <param name="index">The index of the tile (0-39).</param>
        /// <returns>The BoardTile object, or null if index is invalid (though it handles modulo).</returns>
        public BoardTile GetTileAt(int index)
        {
            if (tiles == null || tiles.Count == 0) return null;

            // Ensure index wraps around the board (e.g., index 40 becomes 0)
            int safeIndex = index % tiles.Count;
            if (safeIndex < 0) safeIndex += tiles.Count;

            return tiles[safeIndex];
        }
    }
}
