using System;
using UnityEngine;
using PolyRisk.Core;

namespace PolyRisk.Systems
{
    /// <summary>
    /// Handles user interaction with the 3D board (Raycasting).
    /// Detects clicks on tiles and triggers selection events.
    /// </summary>
    public class BoardInteractionManager : MonoBehaviour
    {
        private Camera mainCamera;
        private BoardManager boardManager;

        [Header("System References")]
        public TurnManager turnManager;
        public PolyRisk.UI.Marketplace_UIManager marketplaceUIManager;

        private void Start()
        {
            mainCamera = Camera.main;
            boardManager = BoardManager.Instance;
            
            if (turnManager == null) turnManager = FindObjectOfType<TurnManager>();
            if (marketplaceUIManager == null) marketplaceUIManager = FindObjectOfType<PolyRisk.UI.Marketplace_UIManager>();
            
            if (mainCamera == null)
            {
                Debug.LogError("Main Camera not found!");
            }
        }

        private void Update()
        {
            // Check for left mouse click or touch
            if (Input.GetMouseButtonDown(0))
            {
                HandleInput(Input.mousePosition);
            }
        }

        /// <summary>
        /// Raycasts from screen point to board.
        /// </summary>
        private void HandleInput(Vector3 screenPosition)
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null) return;
            }

            Ray ray = mainCamera.ScreenPointToRay(screenPosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Try to find a tile identifier.
                // Assuming the hit object represents a tile and we can get its index.
                
                string objectName = hit.collider.gameObject.name;
                
                // Naming convention: "Tile_0", "Tile_1", ...
                if (objectName.StartsWith("Tile_"))
                {
                    string indexStr = objectName.Replace("Tile_", "");
                    if (int.TryParse(indexStr, out int index))
                    {
                        if (boardManager != null)
                        {
                            BoardTile tile = boardManager.GetTileAt(index);
                            if (tile != null)
                            {
                                Debug.Log($"Tile Selected: {index}");

                                if (turnManager != null && turnManager.IsInSelectionMode) 
                                {
                                    if (tile.Data is PropertyData propData)
                                    {
                                        PolyRisk.Visuals.TileVisuals tileVisuals = hit.collider.GetComponentInParent<PolyRisk.Visuals.TileVisuals>();
                                        // TENDER MODE: Route to the Marketplace UI
                                        if (marketplaceUIManager != null) 
                                        {
                                            marketplaceUIManager.OnTileSelected(propData, tileVisuals);
                                            UnityEngine.Debug.Log("[Input Gateway] Routed click to Marketplace UI.");
                                        }
                                    }
                                } 
                                else 
                                {
                                    // NORMAL MODE: Broadcast for inspection/default behavior
                                    GameEvents.OnTileSelected?.Invoke(tile);
                                    UnityEngine.Debug.Log("[Input Gateway] Routed click to GameEvents.");
                                }
                                return;
                            }
                        }
                    }
                }
                
                Debug.Log($"Clicked on {objectName}, but it's not a recognized tile.");
            }
        }
    }
}
