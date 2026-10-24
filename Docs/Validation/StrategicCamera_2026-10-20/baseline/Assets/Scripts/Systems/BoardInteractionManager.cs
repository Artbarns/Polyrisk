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
        private Vector3 normalCameraPosition;
        private VisualElement hudBar, cockpitPanel;
        private Vector2 boardPlaneMin, boardPlaneMax;
        private bool hasBoardBounds, hasFramingTarget;
        private Rect previousViewport;
        private int framedWidth, framedHeight;
        private Vector3 framingTargetPosition;
        private float framingTargetSize;
        public Rect BoardViewport { get; private set; }
        public event Action FramingChanged;
        public const float DefaultElevation = 40f;
        public const float ConsultationZoom = 1.25f;
        public bool FocusActive => focusTile >= 0;
        public int FocusedTileIndex => focusTile;
        private int focusTile = -1, manuallyInspectedTile = -1, inspectionPlayer = -1, focusPlayerPosition = -1;
        private TurnManager.TurnState inspectionState;
        private Button focusButton;
        private bool focusDirty, snapNextFrame;

        public static Quaternion DefaultRotation(Quaternion authored)
        {
            Vector3 forward = authored * Vector3.forward;
            return Quaternion.Euler(DefaultElevation, Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg, 0f);
        }

        public static void FitConsultation(float screenHeight, Rect viewport, Vector2 boardMin, Vector2 boardMax,
            Vector2 interestMin, Vector2 interestMax, float padding, out float size, out Vector2 center)
        {
            float overview = FitCameraSize(screenHeight, viewport, boardMax - boardMin);
            var inner = new Rect(viewport.x + padding, viewport.y + padding,
                Mathf.Max(1,viewport.width - 2 * padding), Mathf.Max(1,viewport.height - 2 * padding));
            size = Mathf.Min(overview, Mathf.Max(overview / ConsultationZoom,
                FitCameraSize(screenHeight, inner, interestMax - interestMin)));
            Vector2 half = inner.size * size / screenHeight;
            Vector2 origin = (boardMin + boardMax) * 0.5f;
            center = new Vector2(ClampFocusCenter(origin.x,interestMax.x-half.x,interestMin.x+half.x),
                ClampFocusCenter(origin.y,interestMax.y-half.y,interestMin.y+half.y));
        }

        private static float ClampFocusCenter(float value, float min, float max) =>
            min <= max ? Mathf.Clamp(value,min,max) : (min+max)*0.5f;

        public void ReturnToOverview()
        {
            focusTile = -1;
            focusDirty = true;
            snapNextFrame = true;
            if (hasBoardBounds && BoardViewport.width > 0)
            {
                SetFramingTarget(BoardViewport);
                ApplyFraming(1f);
                snapNextFrame = false;
            }
            RefreshFocusButton();
        }

        private void ToggleConsultation()
        {
            if (FocusActive) { ReturnToOverview(); return; }
            if (!CanConsult()) return;
            focusTile = manuallyInspectedTile;
            focusPlayerPosition = turnManager.GetPlayerPosition(turnManager.CurrentPlayer);
            focusDirty = true;
            RefreshFocusButton();
        }

        private bool CanConsult() => manuallyInspectedTile >= 0 && actionButtonsWidget != null &&
            actionButtonsWidget.CanInspect && actionButtonsWidget.InspectedTileIndex == manuallyInspectedTile &&
            turnManager != null && !turnManager.IsInSelectionMode && !turnManager.IsGameOver &&
            turnManager.CurrentPlayer?.PlayerId == inspectionPlayer && turnManager.CurrentState == inspectionState;

        private void RefreshFocusButton()
        {
            if (focusButton == null)
            {
                focusButton = topHudWidget?.uiDocument?.rootVisualElement?.Q<Button>("Btn_BoardFocus");
                if (focusButton == null) return;
                focusButton.clicked += ToggleConsultation;
            }
            string text = FocusActive ? "Vue entière" : "Approcher";
            if (focusButton.text != text) focusButton.text = text;
            bool enabled = FocusActive || CanConsult();
            if (focusButton.enabledSelf != enabled) focusButton.SetEnabled(enabled);
            focusButton.EnableInClassList("board-camera-control--focused", FocusActive);
            focusButton.tooltip = FocusActive ? "Rétablir immédiatement la vue des 40 cases" :
                enabled ? "Rapprocher la case inspectée, en conservant le pion actif visible" : "Touchez une case pour activer le rapprochement";
        }

        private void OnDestroy()
        {
            if (focusButton != null) focusButton.clicked -= ToggleConsultation;
        }

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

        // Sole camera writer. Focus only follows an explicit consultation command or manual tile tap.
        private void LateUpdate()
        {
            if (mainCamera == null || !mainCamera.orthographic) return;
            if (manuallyInspectedTile >= 0 && (!CanConsult() || (FocusActive &&
                turnManager.GetPlayerPosition(turnManager.CurrentPlayer) != focusPlayerPosition)))
            {
                manuallyInspectedTile = -1;
                if (FocusActive) { focusTile = -1; focusDirty = true; }
            }
            RefreshFocusButton();
            if (!hasBoardBounds && !CacheBoardBounds()) return;
            if (!TryMeasureViewport(out Rect viewport)) return;
            BoardViewport = viewport;
            if (!hasFramingTarget || focusDirty || viewport != previousViewport || framedWidth != Screen.width || framedHeight != Screen.height)
            {
                PolyRisk.UI.UIUXRuntime.FitBoardPresentation(actionButtonsWidget.uiDocument.rootVisualElement, viewport);
                SetFramingTarget(viewport);
                previousViewport = viewport;
                framedWidth = Screen.width; framedHeight = Screen.height;
                focusDirty = false;
            }
            float blend = snapNextFrame || PolyRisk.UI.UIUXRuntime.ReducedMotion ? 1f : 1f - Mathf.Exp(-Time.unscaledDeltaTime * 18f);
            ApplyFraming(blend);
            snapNextFrame = false;
        }

        private void SetFramingTarget(Rect viewport)
        {
            framingTargetSize = FitCameraSize(Screen.height, viewport, boardPlaneMax - boardPlaneMin);
            Vector2 center = (boardPlaneMin + boardPlaneMax) * 0.5f;
            if (FocusActive && boardManager?.visualTiles != null)
            {
                Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity;
                IncludeInterest(boardManager.visualTiles[focusTile], false, ref min, ref max);
                int active = turnManager.GetPlayerPosition(turnManager.CurrentPlayer);
                IncludeInterest(boardManager.visualTiles[active], true, ref min, ref max);
                float padding = 12f * Screen.width / topHudWidget.uiDocument.rootVisualElement.worldBound.width;
                FitConsultation(Screen.height, viewport, boardPlaneMin, boardPlaneMax, min, max, padding,
                    out framingTargetSize, out center);
            }
            float unitsPerPixel = framingTargetSize * 2f / Screen.height;
            framingTargetPosition = normalCameraPosition +
                mainCamera.transform.right * (center.x - Vector3.Dot(normalCameraPosition, mainCamera.transform.right)
                    - (viewport.center.x - Screen.width * 0.5f) * unitsPerPixel) +
                mainCamera.transform.up * (center.y - Vector3.Dot(normalCameraPosition, mainCamera.transform.up)
                    - (viewport.center.y - Screen.height * 0.5f) * unitsPerPixel);
            hasFramingTarget = true;
        }

        private void IncludeInterest(Transform anchor, bool pawn, ref Vector2 min, ref Vector2 max)
        {
            var col = anchor.GetComponent<BoxCollider>();
            for (int n = 0; n < 8; n++)
            {
                var sign = new Vector3((n & 1) == 0 ? -1 : 1,(n & 2) == 0 ? -1 : 1,(n & 4) == 0 ? -1 : 1);
                var p = col.transform.TransformPoint(col.center + Vector3.Scale(col.size * .5f,sign));
                var plane = new Vector2(Vector3.Dot(p,mainCamera.transform.right),Vector3.Dot(p,mainCamera.transform.up));
                min = Vector2.Min(min,plane); max = Vector2.Max(max,plane);
            }
            if (!pawn) return;
            var top = anchor.position + Vector3.up * 1.45f;
            var projected = new Vector2(Vector3.Dot(top,mainCamera.transform.right),Vector3.Dot(top,mainCamera.transform.up));
            min = Vector2.Min(min,projected); max = Vector2.Max(max,projected);
        }

        private void ApplyFraming(float blend)
        {
            if (mainCamera.transform.position == framingTargetPosition && mainCamera.orthographicSize == framingTargetSize) return;
            mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, framingTargetSize, blend);
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, framingTargetPosition, blend);
            if (blend >= 1f || (Mathf.Abs(mainCamera.orthographicSize - framingTargetSize) < 0.001f &&
                (mainCamera.transform.position - framingTargetPosition).sqrMagnitude < 0.000001f))
            {
                mainCamera.orthographicSize = framingTargetSize;
                mainCamera.transform.position = framingTargetPosition;
            }
            FramingChanged?.Invoke();
        }

        public static float FitCameraSize(float screenHeight, Rect viewport, Vector2 projectedExtent) =>
            Mathf.Max(0.01f, screenHeight * 0.5f * Mathf.Max(
                projectedExtent.x / Mathf.Max(1f, viewport.width), projectedExtent.y / Mathf.Max(1f, viewport.height)));

        public static Rect ChooseBoardViewport(Rect safe, Rect hud, Rect cockpit, Vector2 extent, float margin)
        {
            var area = Rect.MinMaxRect(safe.xMin + margin, safe.yMin + margin,
                safe.xMax - margin, Mathf.Min(safe.yMax - margin, hud.yMin - margin));
            Rect best = area;
            if (cockpit.width > 0 && cockpit.height > 0)
            {
                best = Rect.MinMaxRect(area.xMin, Mathf.Max(area.yMin, cockpit.yMax + margin), area.xMax, area.yMax);
                var left = Rect.MinMaxRect(area.xMin, area.yMin, Mathf.Min(area.xMax, cockpit.xMin - margin), area.yMax);
                var right = Rect.MinMaxRect(Mathf.Max(area.xMin, cockpit.xMax + margin), area.yMin, area.xMax, area.yMax);
                if (ViewportScale(left, extent) > ViewportScale(best, extent)) best = left;
                if (ViewportScale(right, extent) > ViewportScale(best, extent)) best = right;
            }
            return best;
        }

        private static float ViewportScale(Rect rect, Vector2 extent) => rect.width > 0 && rect.height > 0
            ? Mathf.Min(rect.width / Mathf.Max(0.01f, extent.x), rect.height / Mathf.Max(0.01f, extent.y)) : 0;

        private bool CacheBoardBounds()
        {
            var board = FindObjectOfType<PolyRiskBoard>();
            if (board == null) return false;
            var meshes = board.GetComponentsInChildren<MeshFilter>();
            if (meshes.Length == 0 || boardManager?.visualTiles == null) return false;
            boardPlaneMin = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            boardPlaneMax = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            // Fit the actual fused surfaces, rather than the empty corners of a world AABB.
            foreach (var mesh in meshes)
                foreach (var vertex in mesh.sharedMesh.vertices) IncludeInFraming(mesh.transform.TransformPoint(vertex));
            // Preserve the existing pawn and its existing hop at every legal anchor.
            foreach (var anchor in boardManager.visualTiles)
            {
                if (anchor == null) return false;
                IncludeInFraming(anchor.position + Vector3.up * 1.45f);
                var collider = anchor.GetComponent<BoxCollider>();
                if (collider == null) continue;
                for (int n = 0; n < 8; n++)
                {
                    var sign = new Vector3((n & 1) == 0 ? -1 : 1, (n & 2) == 0 ? -1 : 1, (n & 4) == 0 ? -1 : 1);
                    IncludeInFraming(collider.transform.TransformPoint(collider.center + Vector3.Scale(collider.size * 0.5f, sign)));
                }
            }
            hasBoardBounds = true;
            return true;
        }

        private void IncludeInFraming(Vector3 point)
        {
            var plane = new Vector2(Vector3.Dot(point, mainCamera.transform.right), Vector3.Dot(point, mainCamera.transform.up));
            boardPlaneMin = Vector2.Min(boardPlaneMin, plane); boardPlaneMax = Vector2.Max(boardPlaneMax, plane);
        }

        private bool TryMeasureViewport(out Rect viewport)
        {
            viewport = default;
            var hudRoot = topHudWidget?.uiDocument?.rootVisualElement;
            var cockpitRoot = actionButtonsWidget?.uiDocument?.rootVisualElement;
            if (hudRoot?.panel == null || cockpitRoot?.panel == null || Screen.width <= 0 || Screen.height <= 0) return false;
            if (hudBar?.panel == null) hudBar = hudRoot.Q<VisualElement>("TopHUD_Bar");
            if (cockpitPanel?.panel == null) cockpitPanel = cockpitRoot.Q<VisualElement>(className: "marketplace-panel");
            if (hudBar == null || cockpitPanel == null || hudRoot.worldBound.width <= 0 || cockpitRoot.worldBound.width <= 0) return false;
            Rect hud = ScreenRect(hudRoot, hudBar);
            Rect cockpit = ScreenRect(cockpitRoot, cockpitPanel);
            if (hud.height <= 0 || cockpit.height <= 0) return false;
            bool portrait = hudRoot.worldBound.width < hudRoot.worldBound.height;
            float margin = (portrait ? 8f : 12f) * Screen.width / hudRoot.worldBound.width;
            viewport = ChooseBoardViewport(PolyRisk.UI.UIUXRuntime.SafeArea, hud, cockpit, boardPlaneMax - boardPlaneMin, margin);
            return viewport.width > 1 && viewport.height > 1;
        }

        private static Rect ScreenRect(VisualElement root, VisualElement element)
        {
            var r = root.worldBound; var e = element.worldBound;
            return new Rect((e.xMin - r.xMin) * Screen.width / r.width,
                Screen.height - (e.yMax - r.yMin) * Screen.height / r.height,
                e.width * Screen.width / r.width, e.height * Screen.height / r.height);
        }

        [Header("System References")]
        public TurnManager turnManager;
        public PolyRisk.UI.Marketplace_UIManager marketplaceUIManager;
        public PolyRisk.UI.ActionButtonsWidget actionButtonsWidget;

        private void Start()
        {
            mainCamera = Camera.main;
            if (mainCamera != null) mainCamera.transform.rotation = DefaultRotation(mainCamera.transform.rotation);
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

            if (actionButtonsWidget != null && !actionButtonsWidget.CanInspect) return false;
            if (actionButtonsWidget != null) actionButtonsWidget.InspectTile(tile);
            else GameEvents.OnTileSelected?.Invoke(tile);
            // Presentation only: remember a successfully inspected manual tap. No implicit zoom.
            if (actionButtonsWidget != null && actionButtonsWidget.InspectedTileIndex == index)
            {
                manuallyInspectedTile = index;
                inspectionPlayer = turnManager?.CurrentPlayer?.PlayerId ?? -1;
                inspectionState = turnManager != null ? turnManager.CurrentState : default;
                if (FocusActive) { focusTile = index; focusDirty = true; }
            }
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
