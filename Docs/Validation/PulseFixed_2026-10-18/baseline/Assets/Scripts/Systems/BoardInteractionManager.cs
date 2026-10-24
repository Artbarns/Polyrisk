using System;
using UnityEngine;
using UnityEngine.UIElements;
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
        private PolyRisk.UI.TopHUDWidget topHudWidget;
        private float normalCameraSize;
        private Vector3 normalCameraPosition;
        private bool framedSelection;
        private bool framedCompactPlanning;
        private bool framedNarrowPortrait;
        private int framedWidth = -1, framedHeight = -1;

        public static float SelectionCameraSize(float originalSize, float aspect, float horizontalRadius, float verticalRadius)
        {
            const float tileMargin = 0.75f;
            return Mathf.Max(originalSize,
                (Mathf.Max(0f, horizontalRadius) + tileMargin) / Mathf.Max(0.01f, aspect),
                Mathf.Max(0f, verticalRadius) + tileMargin);
        }

        public static float PlanningCameraSize(float originalSize, float screenHeight,
            float usableHeight, float usableWidth, float horizontalRadius, float verticalRadius)
        {
            if (originalSize <= 0f || screenHeight <= 0f || usableHeight <= 0f || usableWidth <= 0f)
                return originalSize;
            float sizeForHeight = Mathf.Max(0f, verticalRadius) * screenHeight / usableHeight;
            float sizeForWidth = Mathf.Max(0f, horizontalRadius) * screenHeight / usableWidth;
            return Mathf.Min(originalSize, Mathf.Max(sizeForHeight, sizeForWidth));
        }

        private void LateUpdate()
        {
            if (mainCamera == null || !mainCamera.orthographic || turnManager == null) return;
            bool selecting = turnManager.IsInSelectionMode;
            bool compactPlanning = !selecting && turnManager.CurrentState == TurnManager.TurnState.Planning &&
                Screen.width > Screen.height && Screen.height <= 430;
            bool narrowPortrait = !selecting && Screen.height > Screen.width && Screen.width <= 430;
            if (selecting == framedSelection && compactPlanning == framedCompactPlanning &&
                narrowPortrait == framedNarrowPortrait &&
                Screen.width == framedWidth && Screen.height == framedHeight) return;
            framedSelection = selecting;
            framedCompactPlanning = compactPlanning;
            framedNarrowPortrait = narrowPortrait;
            framedWidth = Screen.width;
            framedHeight = Screen.height;

            mainCamera.transform.position = normalCameraPosition;
            mainCamera.orthographicSize = normalCameraSize;
            if (!selecting)
            {
                if (compactPlanning && !TryFrameCompactLandscapePlanning())
                    framedCompactPlanning = false; // Retry after UI Toolkit finishes its responsive layout.
                else if (narrowPortrait)
                    mainCamera.orthographicSize = GetFullBoardCameraSize();
                return;
            }

            mainCamera.orthographicSize = GetFullBoardCameraSize();
        }

        private float GetFullBoardCameraSize()
        {
            float horizontal = 0f, vertical = 0f;
            foreach (var tile in boardManager?.visualTiles ?? Array.Empty<Transform>())
            {
                if (tile == null) continue;
                Vector3 local = mainCamera.transform.InverseTransformPoint(tile.position);
                horizontal = Mathf.Max(horizontal, Mathf.Abs(local.x));
                vertical = Mathf.Max(vertical, Mathf.Abs(local.y));
            }
            return SelectionCameraSize(normalCameraSize, mainCamera.aspect, horizontal, vertical);
        }

        private bool TryFrameCompactLandscapePlanning()
        {
            if (boardManager == null || actionButtonsWidget == null || actionButtonsWidget.uiDocument == null ||
                topHudWidget == null || topHudWidget.uiDocument == null || Screen.height <= 0 || Screen.width <= 0)
                return false;

            var actionRoot = actionButtonsWidget.uiDocument.rootVisualElement;
            var hudRoot = topHudWidget.uiDocument.rootVisualElement;
            var actionPanel = actionRoot?.Q<VisualElement>(className: "marketplace-panel");
            var hudBar = hudRoot?.Q<VisualElement>("TopHUD_Bar");
            if (actionRoot == null || hudRoot == null || actionPanel == null || hudBar == null) return false;

            float actionRootHeight = actionRoot.resolvedStyle.height;
            float hudRootHeight = hudRoot.resolvedStyle.height;
            if (actionRootHeight <= 0f || hudRootHeight <= 0f || actionPanel.worldBound.height <= 0f || hudBar.worldBound.height <= 0f)
                return false;

            float topHudBottom = (hudBar.worldBound.yMax - hudRoot.worldBound.y) * Screen.height / hudRootHeight;
            float actionPanelTop = (actionPanel.worldBound.y - actionRoot.worldBound.y) * Screen.height / actionRootHeight;
            const float screenMargin = 12f;
            float usableBottom = Screen.height - actionPanelTop + screenMargin;
            float usableTop = Screen.height - topHudBottom - screenMargin;
            float usableHeight = usableTop - usableBottom;

            Rect safeArea = Screen.safeArea;
            float usableWidth = safeArea.width - screenMargin * 2f;
            if (usableHeight <= 0f || usableWidth <= 0f) return false;

            float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;
            foreach (var tile in boardManager.visualTiles ?? Array.Empty<Transform>())
            {
                if (tile == null) continue;
                Vector3 local = mainCamera.transform.InverseTransformPoint(tile.position);
                minX = Mathf.Min(minX, local.x); maxX = Mathf.Max(maxX, local.x);
                minY = Mathf.Min(minY, local.y); maxY = Mathf.Max(maxY, local.y);
            }
            if (float.IsInfinity(minX) || float.IsInfinity(minY)) return false;

            const float tileMargin = 0.75f;
            float centerX = (minX + maxX) * 0.5f;
            float centerY = (minY + maxY) * 0.5f;
            float horizontalRadius = (maxX - minX) * 0.5f + tileMargin;
            float verticalRadius = (maxY - minY) * 0.5f + tileMargin;
            float targetSize = PlanningCameraSize(normalCameraSize, Screen.height, usableHeight, usableWidth,
                horizontalRadius, verticalRadius);

            mainCamera.orthographicSize = targetSize;
            Vector3 boardCenter = mainCamera.transform.TransformPoint(centerX, centerY, 10f);
            Vector3 projected = mainCamera.WorldToScreenPoint(boardCenter);
            float desiredCenterX = safeArea.xMin + safeArea.width * 0.5f;
            float desiredCenterY = (usableBottom + usableTop) * 0.5f;
            float worldUnitsPerPixel = targetSize * 2f / Screen.height;
            mainCamera.transform.position = normalCameraPosition +
                mainCamera.transform.right * ((projected.x - desiredCenterX) * worldUnitsPerPixel) +
                mainCamera.transform.up * ((projected.y - desiredCenterY) * worldUnitsPerPixel);
            return true;
        }

        [Header("System References")]
        public TurnManager turnManager;
        public PolyRisk.UI.Marketplace_UIManager marketplaceUIManager;
        public PolyRisk.UI.ActionButtonsWidget actionButtonsWidget;

        private void Start()
        {
            mainCamera = Camera.main;
            normalCameraSize = mainCamera != null ? mainCamera.orthographicSize : 11f;
            normalCameraPosition = mainCamera != null ? mainCamera.transform.position : Vector3.zero;
            boardManager = BoardManager.Instance;
            topHudWidget = FindObjectOfType<PolyRisk.UI.TopHUDWidget>();
            
            if (turnManager == null) turnManager = FindObjectOfType<TurnManager>();
            if (marketplaceUIManager == null) marketplaceUIManager = FindObjectOfType<PolyRisk.UI.Marketplace_UIManager>();
            if (actionButtonsWidget == null) actionButtonsWidget = FindObjectOfType<PolyRisk.UI.ActionButtonsWidget>(true);
            
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

            foreach (var document in FindObjectsOfType<UIDocument>())
            {
                var panel = document.rootVisualElement?.panel;
                if (panel == null) continue;
                var position = RuntimePanelUtils.ScreenToPanel(panel,
                    new Vector2(screenPosition.x, Screen.height - screenPosition.y));
                if (IsBoardInputBlocked(panel.Pick(position))) return;
            }
            Ray ray = mainCamera.ScreenPointToRay(screenPosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                string objectName = hit.collider.gameObject.name;
                if (TryGetTileIndex(objectName, out int index) && RouteTileSelection(index, hit.collider))
                    return;

                // On a small landscape screen, a rendered tile can be narrower than a
                // finger. During an explicit picker only, accept a tap close to the
                // nearest tile centre. This runs once per tap across 40 cached transforms,
                // so it has no frame-time or rendering cost.
                if (turnManager != null && turnManager.IsInSelectionMode &&
                    TryRouteNearestTile(screenPosition))
                {
                    return;
                }
                
                Debug.Log($"Clicked on {objectName}, but it's not a recognized tile.");
            }
        }

        public static bool IsBoardInputBlocked(VisualElement picked)
        {
            for (var element = picked; element != null; element = element.parent)
            {
                if (element is Button || element is ScrollView
                    || element.ClassListContains("marketplace-panel")
                    || element.ClassListContains("top-hud-bar")
                    || element.ClassListContains("main-menu-overlay")) return true;
            }
            return false;
        }

        private static bool TryGetTileIndex(string objectName, out int index)
        {
            index = -1;
            return !string.IsNullOrEmpty(objectName) && objectName.StartsWith("Tile_") &&
                   int.TryParse(objectName.Substring("Tile_".Length), out index);
        }

        private bool TryRouteNearestTile(Vector3 screenPosition)
        {
            if (boardManager == null || boardManager.visualTiles == null) return false;

            float radius = Mathf.Clamp(Mathf.Min(Screen.width, Screen.height) * 0.11f, 36f, 56f);
            float bestDistanceSquared = radius * radius;
            int bestIndex = -1;
            Collider bestCollider = null;

            for (int i = 0; i < boardManager.visualTiles.Length; i++)
            {
                Transform tileTransform = boardManager.visualTiles[i];
                if (tileTransform == null) continue;
                BoardTile tile = boardManager.GetTileAt(i);
                if (!IsEligibleForCurrentSelection(tile)) continue;

                Vector3 projected = mainCamera.WorldToScreenPoint(tileTransform.position);
                if (projected.z <= 0f) continue;

                float distanceSquared = ((Vector2)projected - (Vector2)screenPosition).sqrMagnitude;
                if (distanceSquared > bestDistanceSquared) continue;

                bestDistanceSquared = distanceSquared;
                bestIndex = i;
                bestCollider = tileTransform.GetComponentInChildren<Collider>();
            }

            if (bestIndex < 0) return false;
            Debug.Log($"[Input Gateway] Touch tolerance selected nearest tile {bestIndex}.");
            return RouteTileSelection(bestIndex, bestCollider);
        }

        private bool RouteTileSelection(int index, Collider sourceCollider)
        {
            if (boardManager == null) return false;
            BoardTile tile = boardManager.GetTileAt(index);
            if (tile == null) return false;

            Debug.Log($"Tile Selected: {index}");

            if (turnManager != null && turnManager.IsInSelectionMode)
            {
                if (actionButtonsWidget != null && actionButtonsWidget.IsAbilityTargetSelectionActive)
                {
                    if (!actionButtonsWidget.IsAbilityTileEligible(tile)) return false;
                    if (!actionButtonsWidget.OnAbilityTileSelected(tile)) return false;
                    Debug.Log("[Input Gateway] Routed click to Ability target picker.");
                    return true;
                }

                if (tile.Data is PropertyData propData && marketplaceUIManager != null)
                {
                    if (!marketplaceUIManager.IsTileEligibleForCurrentSelection(propData)) return false;
                    PolyRisk.Visuals.TileVisuals tileVisuals = sourceCollider != null
                        ? sourceCollider.GetComponentInParent<PolyRisk.Visuals.TileVisuals>()
                        : null;
                    marketplaceUIManager.OnTileSelected(propData, tileVisuals);
                    Debug.Log("[Input Gateway] Routed click to Marketplace UI.");
                    return true;
                }

                return false;
            }

            if (actionButtonsWidget != null) actionButtonsWidget.InspectTile(tile);
            else GameEvents.OnTileSelected?.Invoke(tile);
            Debug.Log("[Input Gateway] Routed click to GameEvents.");
            return true;
        }

        private bool IsEligibleForCurrentSelection(BoardTile tile)
        {
            if (tile == null) return false;
            if (actionButtonsWidget != null && actionButtonsWidget.IsAbilityTargetSelectionActive)
                return actionButtonsWidget.IsAbilityTileEligible(tile);
            return tile.Data is PropertyData property && marketplaceUIManager != null &&
                   marketplaceUIManager.IsTileEligibleForCurrentSelection(property);
        }
    }
}
