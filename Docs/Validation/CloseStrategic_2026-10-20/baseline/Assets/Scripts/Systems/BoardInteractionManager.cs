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
        private int framedWidth, framedHeight;
        public Rect BoardViewport { get; private set; }
        public event Action FramingChanged;
        public const float DefaultElevation = 40f;
        public enum CameraState { StrategicOverview, MovementFocus, ReturnToOverview }
        public CameraState State { get; private set; }
        public bool IsArrivalHold { get; private set; }
        public Transform ObservedPawn => trackedPawn;
        public float OverviewOrthographicSize => overviewSize;
        public bool ReducedMotionEnabled => PolyRisk.UI.UIUXRuntime.ReducedMotion;
        [Header("Strategic movement camera")]
        [SerializeField, Range(1.15f,1.25f)] private float movementZoomFactor = 1.20f;
        [SerializeField, Min(.05f)] private float focusEntryDuration = .20f;
        [SerializeField, Min(.1f)] private float followHorizontalStrength = 1f;
        [SerializeField, Min(.1f)] private float followVerticalCorrectionStrength = .55f;
        [SerializeField, Range(.2f,.95f)] private float comfortZoneWidth = .62f;
        [SerializeField, Range(.2f,.95f)] private float comfortZoneHeight = .82f;
        [SerializeField, Min(.02f)] private float cameraDamping = .12f;
        [SerializeField, Range(.2f,.3f)] private float arrivalHoldDuration = .25f;
        [SerializeField, Range(.3f,.45f)] private float returnDuration = .375f;
        private const float CompactCockpitHeight = 236f, PawnRadius = .35f, PawnHeadroom = 1.45f;
        private const float PawnBoundsPadding = 1.04f, PawnHopAllowance = .6f;
        private Vector2 pawnPlaneMin, pawnPlaneMax;
        private Vector3 overviewPosition, previousPawnPosition, returnStartPosition;
        private Transform trackedPawn;
        private Vector3 movementDestination;
        private float overviewSize, entryStartSize, entryStarted, entryDuration, floorY, holdDeadline, returnStarted, returnStartSize;
        private int trackedPlayer, startLogicalPosition;
        private TurnManager.TurnState movementPhase;
        private Rect framedSafeArea;

        public static Quaternion DefaultRotation(Quaternion authored)
        {
            Vector3 forward = authored * Vector3.forward;
            return Quaternion.Euler(DefaultElevation, Mathf.Atan2(forward.x,forward.z)*Mathf.Rad2Deg,0f);
        }

        private void OnEnable()
        {
            PolyRisk.Visuals.PlayerVisualManager.VisualMovementStarted += BeginVisualMovement;
            GameEvents.OnPawnMovementFinished += HandleVisualMovementFinished;
            GameEvents.OnTurnStarted += HandleTurnStarted;
            PolyRisk.Multiplayer.OnlineGameController.PresentationStateApplied += RestoreOverview;
        }
        private void OnDisable()
        {
            PolyRisk.Visuals.PlayerVisualManager.VisualMovementStarted -= BeginVisualMovement;
            GameEvents.OnPawnMovementFinished -= HandleVisualMovementFinished;
            GameEvents.OnTurnStarted -= HandleTurnStarted;
            PolyRisk.Multiplayer.OnlineGameController.PresentationStateApplied -= RestoreOverview;
            RestoreOverview();
        }
        private void HandleTurnStarted(int _) { if (State != CameraState.StrategicOverview) RestoreOverview(); }

        // Presentation observers only. Neither method owns or invokes the gameplay completion callback.
        public void BeginVisualMovement(int playerId, PolyRisk.Visuals.PawnMovement pawn, System.Collections.Generic.List<Vector3> path)
        {
            if (!hasFramingTarget || ReducedMotionEnabled || pawn == null || path == null || path.Count == 0 || GameBootstrapper.SkipMovementAnimation) return;
            if (turnManager?.CurrentPlayer == null || playerId != turnManager.CurrentPlayer.PlayerId) return;
            if ((path[0]-pawn.transform.position).sqrMagnitude < .0001f) return;
            trackedPawn = pawn.transform; trackedPlayer = playerId;
            previousPawnPosition = trackedPawn.position; movementDestination = path[path.Count-1];
            CachePawnFootprint();
            floorY = path[0].y; startLogicalPosition = turnManager.GetPlayerPosition(turnManager.CurrentPlayer);
            movementPhase = turnManager.CurrentState;
            entryStartSize = mainCamera.orthographicSize; entryStarted = Time.unscaledTime;
            entryDuration = Mathf.Min(focusEntryDuration, Mathf.Max(.05f,path.Count*.1f));
            IsArrivalHold = false;
            SetState(CameraState.MovementFocus);
        }
        private void HandleVisualMovementFinished() => CompleteVisualMovement(trackedPawn);
        public void CompleteVisualMovement(Transform pawn)
        {
            if (State != CameraState.MovementFocus || IsArrivalHold || pawn == null || pawn != trackedPawn) return;
            if ((pawn.position-movementDestination).sqrMagnitude > .0025f) return;
            IsArrivalHold = true;
            holdDeadline = Time.unscaledTime + arrivalHoldDuration;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("CAMERA_HOLD t="+Time.unscaledTime.ToString("F4",System.Globalization.CultureInfo.InvariantCulture)+" size="+mainCamera.orthographicSize.ToString("R",System.Globalization.CultureInfo.InvariantCulture));
#endif
        }
        public void RestoreOverview()
        {
            trackedPawn = null; IsArrivalHold = false;
            SetState(CameraState.StrategicOverview);
            if (mainCamera == null || !hasFramingTarget) return;
            WriteCamera(overviewPosition,overviewSize);
        }
        private void SetState(CameraState state)
        {
            if (State == state) return;
            State = state;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("CAMERA_STATE "+state+" t="+Time.unscaledTime.ToString("F4",System.Globalization.CultureInfo.InvariantCulture));
#endif
        }

        // This component is the sole camera writer. Cockpit content/height never enters the calibration key.
        private void LateUpdate()
        {
            if (mainCamera == null || !mainCamera.orthographic) return;
            if (!hasBoardBounds && !CacheBoardBounds()) return;
            Rect safe = PolyRisk.UI.UIUXRuntime.SafeArea;
            if (!hasFramingTarget || framedWidth != Screen.width || framedHeight != Screen.height || safe != framedSafeArea)
            {
                if (!TryMeasureViewport(out Rect viewport)) return;
                BoardViewport = viewport;
                overviewSize = FitCameraSize(Screen.height,viewport,boardPlaneMax-boardPlaneMin);
                overviewPosition = PositionForPlane((boardPlaneMin+boardPlaneMax)*.5f,overviewSize,viewport);
                framedWidth=Screen.width;framedHeight=Screen.height;framedSafeArea=safe;
                hasFramingTarget=true;
                PolyRisk.UI.UIUXRuntime.FitBoardPresentation(actionButtonsWidget.uiDocument.rootVisualElement,viewport);
                RestoreOverview();
            }
            if (ReducedMotionEnabled) { if (State != CameraState.StrategicOverview) RestoreOverview(); return; }
            if (State == CameraState.StrategicOverview) return;
            if (trackedPawn == null || turnManager?.CurrentPlayer == null || turnManager.CurrentPlayer.PlayerId != trackedPlayer)
            { RestoreOverview(); return; }
            float now=Time.unscaledTime, dt=Time.unscaledDeltaTime;
            if (State == CameraState.MovementFocus)
            {
                if (IsArrivalHold)
                {
                    if(now>=holdDeadline)
                    {
                        returnStarted=now;returnStartSize=mainCamera.orthographicSize;returnStartPosition=mainCamera.transform.position;
                        IsArrivalHold=false;
                        SetState(CameraState.ReturnToOverview);
                    }
                    return;
                }
                Vector3 pawnPosition=trackedPawn.position;
                if(turnManager.CurrentState!=movementPhase || turnManager.GetPlayerPosition(turnManager.CurrentPlayer)!=startLogicalPosition ||
                    Vector3.Distance(pawnPosition,previousPawnPosition)>Mathf.Max(2.5f,dt*20f))
                { RestoreOverview();return; }
                previousPawnPosition=pawnPosition;
                float t=Mathf.Clamp01((now-entryStarted)/entryDuration);
                float size=Mathf.Lerp(entryStartSize,overviewSize/movementZoomFactor,t*t*(3f-2f*t));
                Vector2 center=PlaneAtViewportCenter(mainCamera.transform.position,size);
                Vector3 ground=new Vector3(pawnPosition.x,floorY,pawnPosition.z);
                Vector2 point=ProjectPlane(ground);
                Vector2 min=point+pawnPlaneMin, max=point+pawnPlaneMax;
                Vector2 half=Vector2.Scale(BoardViewport.size,new Vector2(comfortZoneWidth,comfortZoneHeight))*size/Screen.height;
                Vector2 correction=ComfortCorrection(center,min,max,half);
                center.x+=correction.x*(1f-Mathf.Exp(-dt*followHorizontalStrength/cameraDamping));
                center.y+=correction.y*(1f-Mathf.Exp(-dt*followVerticalCorrectionStrength/cameraDamping));
                // Visibility guard for a long frame; ground tracking does not follow the pawn's hop oscillation.
                Vector2 safetyHalf=(BoardViewport.size-Vector2.one*8f)*size/Screen.height;
                center+=ComfortCorrection(center,min,max,safetyHalf);
                WriteCamera(PositionForPlane(center,size,BoardViewport),size);
            }
            else
            {
                float t=Mathf.Clamp01((now-returnStarted)/returnDuration), ease=t*t*(3f-2f*t);
                WriteCamera(Vector3.Lerp(returnStartPosition,overviewPosition,ease),Mathf.Lerp(returnStartSize,overviewSize,ease));
                if(t>=1f) RestoreOverview();
            }
        }
        // Cache once per sequence. A fixed envelope includes the existing hop/bounce without following it vertically.
        private void CachePawnFootprint()
        {
            float verticalRadius=PawnRadius*Mathf.Sin(DefaultElevation*Mathf.Deg2Rad);
            pawnPlaneMin=new Vector2(-PawnRadius,-verticalRadius);
            pawnPlaneMax=new Vector2(PawnRadius,verticalRadius+PawnHeadroom*mainCamera.transform.up.y);
            Vector2 origin=ProjectPlane(trackedPawn.position);
            foreach(var renderer in trackedPawn.GetComponentsInChildren<Renderer>())
            {
                Bounds bounds=renderer.bounds;
                bounds.extents*=PawnBoundsPadding;
                for(int n=0;n<8;n++)
                {
                    Vector3 corner=new Vector3((n&1)==0?bounds.min.x:bounds.max.x,
                        (n&2)==0?bounds.min.y:bounds.max.y,(n&4)==0?bounds.min.z:bounds.max.z);
                    Vector2 offset=ProjectPlane(corner)-origin;
                    pawnPlaneMin=Vector2.Min(pawnPlaneMin,offset);
                    pawnPlaneMax=Vector2.Max(pawnPlaneMax,offset);
                }
            }
            pawnPlaneMax.y+=PawnHopAllowance*mainCamera.transform.up.y;
        }
        public static Vector2 ComfortCorrection(Vector2 center,Vector2 min,Vector2 max,Vector2 half) => new Vector2(
            AxisCorrection(center.x,min.x,max.x,half.x),AxisCorrection(center.y,min.y,max.y,half.y));
        private static float AxisCorrection(float center,float min,float max,float half) =>
            min<center-half ? min-(center-half) : max>center+half ? max-(center+half) : 0f;
        private Vector2 ProjectPlane(Vector3 point) => new Vector2(Vector3.Dot(point,mainCamera.transform.right),Vector3.Dot(point,mainCamera.transform.up));
        private Vector2 PlaneAtViewportCenter(Vector3 position,float size) => ProjectPlane(position)+
            (BoardViewport.center-new Vector2(Screen.width,Screen.height)*.5f)*(size*2f/Screen.height);
        private Vector3 PositionForPlane(Vector2 center,float size,Rect viewport)
        {
            float units=size*2f/Screen.height;
            return normalCameraPosition+mainCamera.transform.right*(center.x-Vector3.Dot(normalCameraPosition,mainCamera.transform.right)-(viewport.center.x-Screen.width*.5f)*units)+
                mainCamera.transform.up*(center.y-Vector3.Dot(normalCameraPosition,mainCamera.transform.up)-(viewport.center.y-Screen.height*.5f)*units);
        }
        private void WriteCamera(Vector3 position,float size)
        {
            if(mainCamera.transform.position==position && mainCamera.orthographicSize==size) return;
            mainCamera.transform.position=position;mainCamera.orthographicSize=size;FramingChanged?.Invoke();
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
            if (hud.height <= 0) return false;
            float scale=Screen.width/hudRoot.worldBound.width;
            bool portrait=hudRoot.worldBound.width<hudRoot.worldBound.height;
            Rect safe=PolyRisk.UI.UIUXRuntime.SafeArea;
            // Reference compact reservation, independent of the actual cockpit state or tab contents.
            Rect cockpit=portrait ? new Rect(safe.x,safe.y,safe.width,CompactCockpitHeight*scale) :
                new Rect(safe.xMax-safe.width*.54f-18f*scale,safe.y,safe.width*.54f,CompactCockpitHeight*scale);
            float margin=(portrait?8f:12f)*scale;
            viewport=ChooseBoardViewport(safe,hud,cockpit,boardPlaneMax-boardPlaneMin,margin);
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
