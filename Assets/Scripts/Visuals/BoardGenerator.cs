using UnityEngine;
using PolyRisk.Core;

namespace PolyRisk.Visuals
{
    /// <summary>
    /// Generates the 3D visual representation of the game board.
    /// Can be executed in the Unity Editor via ContextMenu.
    /// </summary>
    public class BoardGenerator : MonoBehaviour
    {
        [Header("Board Settings")]
        [Tooltip("The prefab to use for each tile.")]
        public GameObject tilePrefab;

        [Tooltip("The spacing between tiles.")]
        public float tileSpacing = 1.5f;

        [Tooltip("Reference to the BoardManager to auto-assign tiles.")]
        public PolyRisk.Systems.BoardManager boardManager;

        [Tooltip("Number of tiles per side (excluding corners for calculation simplicity, but total is 40).")]
        // Standard Monopoly board has 10 tiles per side (including corners). 
        // 0-9 (Bottom), 10-19 (Left), 20-29 (Top), 30-39 (Right).
        // Side length is 10 tiles.
        private const int tilesPerSide = 10;

        /// <summary>
        /// Generates the board visuals.
        /// Can be called from the Inspector context menu.
        /// </summary>
        [ContextMenu("Generate Board Visual")]
        public void GenerateBoard()
        {
            if (tilePrefab == null)
            {
                Debug.LogError("BoardGenerator: Tile Prefab is missing!");
                return;
            }

            // 1. Clear existing board
            // We use a while loop because DestroyImmediate modifies the child count
            while (transform.childCount > 0)
            {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }

            Debug.Log("Generating 40-tile board...");

            // 2. Generate 40 tiles
            for (int i = 0; i < 40; i++)
            {
                Vector3 position = CalculateTilePosition(i);
                Quaternion rotation = CalculateTileRotation(i);

                GameObject newTile = Instantiate(tilePrefab, position, rotation, transform);
                newTile.name = $"Tile_{i}";

                // 3. Setup BoardTile component
                // Since BoardTile is a pure C# class in Core, we can't AddComponent<BoardTile>().
                // We need a visual wrapper or we assume the prefab has a script that holds the data.
                // The prompt says "Add the BoardTile.cs component". 
                // But BoardTile.cs was defined as [Serializable] public class BoardTile (not MonoBehaviour) in Phase 3.
                // This is a conflict. 
                // Resolution: I will check BoardTile.cs. If it's not a MonoBehaviour, I cannot AddComponent it.
                // I will assume for this task that I should creating a `TileVisual` component 
                // OR the user implies BoardTile SHOULD be a MonoBehaviour.
                // However, based on previous context, BoardTile is a data class.
                // To fulfill the request "Add the BoardTile.cs component", I would strictly need it to be a MonoBehaviour.
                // If it is not, I will add a `TileIdentifier` script (created inline here or just name it).
                
                // Let's look at the file BoardTile.cs content if possible, but I recall it being a pure class.
                // "1. BoardTile.cs (Core): A pure C# class representing a single square..."
                
                // So I cannot add it as a component. 
                // I will create a small MonoBehaviour wrapper inside this file (or separate) to hold the index,
                // satisfying the intent of "identifying the tile".
                
                TileIdentifier identifier = newTile.GetComponent<TileIdentifier>();
                if (identifier == null)
                {
                    identifier = newTile.AddComponent<TileIdentifier>();
                }
                identifier.SetIndex(i);

                // 4. Assign to BoardManager if available
                if (boardManager != null)
                {
                    if (boardManager.visualTiles == null || boardManager.visualTiles.Length != 40)
                    {
                        boardManager.visualTiles = new Transform[40];
                    }
                    boardManager.visualTiles[i] = newTile.transform;
                }
            }
            
            Debug.Log("Board generation complete.");
        }

        /// <summary>
        /// Calculates the position of a tile based on its index.
        /// Layout: 
        /// 0-9: Bottom Side (Right to Left? Or Left to Right? Monopoly starts Bottom Right and goes CCW usually).
        /// Prompt says: "Clockwise".
        /// Side 1 (0-9): Bottom edge. Let's assume starting from Bottom-Left going to Top-Left?
        /// Wait, usually "Clockwise" means:
        /// Bottom-Left -> Top-Left -> Top-Right -> Bottom-Right -> Bottom-Left.
        /// 
        /// Let's define the corners for a 40 tile board (10 per side).
        /// Let's center the board at (0,0,0).
        /// Total width = 10 * spacing.
        /// Offset = (10 * spacing) / 2.
        /// 
        /// Let's verify standard indices for "Clockwise":
        /// 0: Start (Bottom Left corner?)
        /// 0-9: Left Edge (going Up) -> No, prompt says "Side 1 (0-9): Bottom edge".
        /// If moving Clockwise on Bottom Edge:
        /// That means Left to Right? Or Right to Left?
        /// Let's assume standard math grid:
        /// Side 1 (Bottom): y=min, x goes min to max (Left to Right).
        /// Side 2 (Right?): x=max, y goes min to max.
        /// 
        /// Prompt specifics:
        /// Side 1 (0-9): Bottom edge
        /// Side 2 (10-19): Left edge (Wait, Bottom to Left is NOT clockwise if Bottom is y=min).
        /// 
        /// Visualizing Clockwise from Bottom:
        /// Bottom Edge (Left to Right? No, that leads to Right Edge).
        /// Bottom Edge (Right to Left?) -> Leads to Left Edge.
        /// YES.
        /// Start at Bottom-Right (Index 0).
        /// Move Left along Bottom Edge (Indices 0-9).
        /// Move Up along Left Edge (Indices 10-19).
        /// Move Right along Top Edge (Indices 20-29).
        /// Move Down along Right Edge (Indices 30-39).
        /// This forms a Clockwise loop.
        /// 
        /// Let's implement this logic.
        /// </summary>
        private Vector3 CalculateTilePosition(int index)
        {
            // The board is a square of 40 tiles.
            // 11 tiles per side, but corners are shared.
            // 0 is corner (Bottom Right).
            // 10 is corner (Bottom Left).
            // 20 is corner (Top Left).
            // 30 is corner (Top Right).
            
            // Side length in terms of spacing units: 10 units.
            // We want 0 at (5, -5), 10 at (-5, -5), 20 at (-5, 5), 30 at (5, 5).
            // (Assuming center 0,0)
            
            float halfSide = 5f * tileSpacing;

            // Side 1: Bottom (0-10). Moving Left (Negative X).
            if (index < 10)
            {
                // Index 0 to 9.
                // 0 is at X=5. 9 is at X=5 - (9 * 1) = -4? No.
                // We want 0 at X=5. 10 at X=-5. Distance is 10.
                // Spacing is 10 / 10 = 1.
                // Pos = Start - (index * spacing)
                return new Vector3(halfSide - (index * tileSpacing), 0, -halfSide);
            }
            // Side 2: Left (10-20). Moving Up (Positive Z).
            else if (index < 20)
            {
                int sideIndex = index - 10;
                // 10 is at Z=-5. 20 is at Z=5.
                // Pos = Start + (sideIndex * spacing)
                return new Vector3(-halfSide, 0, -halfSide + (sideIndex * tileSpacing));
            }
            // Side 3: Top (20-30). Moving Right (Positive X).
            else if (index < 30)
            {
                int sideIndex = index - 20;
                // 20 is at X=-5. 30 is at X=5.
                // Pos = Start + (sideIndex * spacing)
                return new Vector3(-halfSide + (sideIndex * tileSpacing), 0, halfSide);
            }
            // Side 4: Right (30-39). Moving Down (Negative Z).
            else
            {
                int sideIndex = index - 30;
                // 30 is at Z=5. 0 (wrapped 40) is at Z=-5.
                // Pos = Start - (sideIndex * spacing)
                return new Vector3(halfSide, 0, halfSide - (sideIndex * tileSpacing));
            }
        }

        private Quaternion CalculateTileRotation(int index)
        {
            // Orient tiles to face center or along the path?
            // Usually text faces "outward" or "inward".
            // Let's assume standard inward facing.
            
            if (index < 10) return Quaternion.Euler(0, 0, 0); // Bottom facing inward
            else if (index < 20) return Quaternion.Euler(0, 90, 0); // Left facing inward
            else if (index < 30) return Quaternion.Euler(0, 180, 0); // Top facing inward
            else return Quaternion.Euler(0, -90, 0); // Right facing inward
        }
    }
}
