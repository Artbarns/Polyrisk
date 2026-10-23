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
        // PASS 2F.1 uses the same board/pawn envelope and compact viewport. Keep this
        // reference separate so the permanent enlargement is inspectable on every format.
        public float StrategicReferenceOrthographicSize { get; private set; }
        public const float PermanentCloseFactor = 1.40f;
        public bool IsManuallyExploring { get; private set; }
        public bool ReducedMotionEnabled => PolyRisk.UI.UIUXRuntime.ReducedMotion;
        [Header("Strategic movement camera")]
        [SerializeField, Range(1f,1.05f)] private float movementZoomFactor = 1f;
        [SerializeField, Min(.05f)] private float focusEntryDuration = .30f;
        [SerializeField, Range(0f,1f)] private float movementAnchorX = .50f;
        [SerializeField, Range(0f,1f)] private float movementAnchorY = .52f;
        [SerializeField, Min(.02f)] private float followHorizontalDamping = .08f;
        [SerializeField, Min(.02f)] private float followVerticalDamping = .12f;
        [SerializeField] private Vector2 movementComfortZone = new Vector2(.16f,.22f);
        [SerializeField, Min(.02f)] private float cornerTransitionSmoothing = .14f;
        [SerializeField, Range(.2f,.3f)] private float arrivalHoldDuration = .25f;
        [SerializeField, Range(.3f,.45f)] private float returnDuration = .375f;
        private const float CompactCockpitHeight = 236f, PawnRadius = .35f, PawnHeadroom = 1.45f;
        private const float PawnBoundsPadding = 1.04f, PawnHopAllowance = .6f;
        [SerializeField, Range(0f,4f)] private float movementLookAhead = .75f;
        private Vector2 smoothedLookAhead;
        private System.Collections.Generic.List<Vector3> observedPath;
        private int pathSegment;
        private Vector2 entryPlaneCenter;
        private Vector2 pawnPlaneMin, pawnPlaneMax;
        private Vector3 overviewPosition, previousPawnPosition, returnStartPosition;
        private Transform trackedPawn;
        private Vector3 movementDestination;
        private float overviewSize, entryStartSize, entryStarted, entryDuration, floorY, holdDeadline, returnStarted, returnStartSize;
        private int trackedPlayer, startLogicalPosition;
        private TurnManager.TurnState movementPhase;
        private Rect framedSafeArea;
        private bool restAnchorPending = true, pointerPressed, pointerDragging, reducedMovementPending;
        private int pointerFingerId = -1;
        private Vector2 pointerStart, pointerStartCenter;
        private const float DragThresholdUiPixels = 10f;
        private Transform restPawn;
        private int restPlayerId = -1, restLogicalPosition = -1;

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
            PolyRisk.Multiplayer.OnlineGameController.PresentationStateApplied += HandlePresentationRestore;
        }
        private void OnDisable()
        {
            PolyRisk.Visuals.PlayerVisualManager.VisualMovementStarted -= BeginVisualMovement;
            GameEvents.OnPawnMovementFinished -= HandleVisualMovementFinished;
            GameEvents.OnTurnStarted -= HandleTurnStarted;
            PolyRisk.Multiplayer.OnlineGameController.PresentationStateApplied -= HandlePresentationRestore;
            RestoreOverview();
        }
        private void HandleTurnStarted(int _) => RestoreOverview();
        private void HandlePresentationRestore()
        {
            var active=turnManager?.CurrentPlayer;
            // An authoritative refresh of the same resting position must not steal an
            // exploration gesture. A changed turn/position or cancelled motion may resume follow.
            if (State==CameraState.StrategicOverview && IsManuallyExploring && active!=null &&
                active.PlayerId==restPlayerId && turnManager.GetPlayerPosition(active)==restLogicalPosition) return;
            RestoreOverview();
        }

        // Presentation observers only. Neither method owns or invokes the gameplay completion callback.
        public void BeginVisualMovement(int playerId, PolyRisk.Visuals.PawnMovement pawn, System.Collections.Generic.List<Vector3> path)
        {
            if (!hasFramingTarget || pawn == null || path == null || path.Count == 0) return;
            if (turnManager?.CurrentPlayer == null || playerId != turnManager.CurrentPlayer.PlayerId) return;
            if ((path[0]-pawn.transform.position).sqrMagnitude < .0001f) return;
            CancelPointer(); IsManuallyExploring = false;
            restPawn = pawn.transform; restPlayerId = playerId;
            movementDestination=path[path.Count-1]; reducedMovementPending=false;
            if (ReducedMotionEnabled || GameBootstrapper.SkipMovementAnimation)
            {
                // Reduced Motion still interpolates the logical root. Keep the camera fixed
                // through that interpolation and establish the new resting pose only at completion.
                reducedMovementPending=true; restAnchorPending=false;
                restLogicalPosition=turnManager.GetPlayerPosition(turnManager.CurrentPlayer);
                return;
            }
            trackedPawn = pawn.transform; trackedPlayer = playerId;
            previousPawnPosition = trackedPawn.position; movementDestination = path[path.Count-1];
            CachePawnFootprint();
            floorY = path[0].y; startLogicalPosition = turnManager.GetPlayerPosition(turnManager.CurrentPlayer);
            movementPhase = turnManager.CurrentState;
            entryStartSize = mainCamera.orthographicSize; entryStarted = Time.unscaledTime;
            entryDuration = Mathf.Min(focusEntryDuration, Mathf.Max(.08f,path.Count*.14f));
            observedPath=path;pathSegment=0;
            smoothedLookAhead=Vector2.zero;
            entryPlaneCenter=PlaneAtViewportCenter(mainCamera.transform.position,mainCamera.orthographicSize);
            IsArrivalHold = false;
            SetState(CameraState.MovementFocus);
        }
        private void HandleVisualMovementFinished()
        {
            if (reducedMovementPending) CompleteVisualMovement(restPawn);
            else if (State == CameraState.MovementFocus) CompleteVisualMovement(trackedPawn);
            else { IsManuallyExploring = false; restAnchorPending = true; }
        }
        public void CompleteVisualMovement(Transform pawn)
        {
            if(reducedMovementPending)
            {
                if(pawn==null || pawn!=restPawn || (pawn.position-movementDestination).sqrMagnitude>.0025f) return;
                reducedMovementPending=false; restAnchorPending=true;
                return;
            }
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
            trackedPawn = null; observedPath=null; IsArrivalHold = false; reducedMovementPending=false;
            CancelPointer(); IsManuallyExploring = false; restAnchorPending = true;
            SetState(CameraState.StrategicOverview);
            if (mainCamera == null || !hasFramingTarget) return;
            UpdateRestAnchor();
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
                StrategicReferenceOrthographicSize = FitCameraSize(Screen.height,new Rect(viewport.position+Vector2.one*2f,viewport.size-Vector2.one*4f),boardPlaneMax-boardPlaneMin);
                overviewSize = CloseStrategicSize(StrategicReferenceOrthographicSize);
                overviewPosition = PositionForPlane((boardPlaneMin+boardPlaneMax)*.5f,overviewSize,viewport);
                framedWidth=Screen.width;framedHeight=Screen.height;framedSafeArea=safe;
                hasFramingTarget=true;
                PolyRisk.UI.UIUXRuntime.FitBoardPresentation(actionButtonsWidget.uiDocument.rootVisualElement,viewport);
                RestoreOverview();
            }
            if (ReducedMotionEnabled && State != CameraState.StrategicOverview) RestoreOverview();
            if (State == CameraState.StrategicOverview)
            {
                // Logical teleports and roster changes are gameplay events too. Inspection,
                // cockpit expansion and ordinary replicated UI refreshes do not enter here.
                var active = turnManager?.CurrentPlayer;
                int logical = active != null ? turnManager.GetPlayerPosition(active) : -1;
                if ((!IsManuallyExploring && (restAnchorPending || restPawn == null)) ||
                    (active != null && (active.PlayerId != restPlayerId || logical != restLogicalPosition)))
                {
                    IsManuallyExploring=false; UpdateRestAnchor(); WriteCamera(overviewPosition, overviewSize);
                }
                return;
            }
            if (trackedPawn == null || turnManager?.CurrentPlayer == null || turnManager.CurrentPlayer.PlayerId != trackedPlayer)
            { RestoreOverview(); return; }
            float now=Time.unscaledTime, dt=Time.unscaledDeltaTime;
            if (State == CameraState.MovementFocus)
            {
                if (IsArrivalHold)
                {
                    if(now>=holdDeadline)
                    {
                        UpdateRestAnchor(trackedPawn);
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
                // Anchor is relative to the usable board viewport, never the full screen.
                // Ground tracking excludes the logical hop and the monolith's presentation pulse.
                Vector2 lead=ProjectPlane(SampleAhead(ground,observedPath,ref pathSegment,movementLookAhead))-point;
                smoothedLookAhead=Vector2.Lerp(smoothedLookAhead,lead,1f-Mathf.Exp(-dt/Mathf.Max(.02f,cornerTransitionSmoothing)));
                Vector2 anchorOffset=Vector2.Scale(new Vector2(.5f-movementAnchorX,.5f-movementAnchorY),BoardViewport.size)*(size*2f/Screen.height);
                Vector2 target=point+anchorOffset+smoothedLookAhead;
                float entryEase=t*t*(3f-2f*t);
                target=Vector2.Lerp(entryPlaneCenter,target,entryEase);
                center.x=Mathf.Lerp(center.x,target.x,1f-Mathf.Exp(-dt/Mathf.Max(.02f,followHorizontalDamping)));
                center.y=Mathf.Lerp(center.y,target.y,1f-Mathf.Exp(-dt/Mathf.Max(.02f,followVerticalDamping)));
                // A small soft comfort guard supplements continuous following; it never waits
                // for a screen edge before starting. Blend it in with the original focus entry.
                Vector2 half=Vector2.Scale(BoardViewport.size,movementComfortZone)*size/Screen.height;
                Vector2 correction=ComfortCorrection(center-anchorOffset,min,max,half);
                center.x+=correction.x*entryEase*(1f-Mathf.Exp(-dt/Mathf.Max(.02f,followHorizontalDamping)));
                center.y+=correction.y*entryEase*(1f-Mathf.Exp(-dt/Mathf.Max(.02f,followVerticalDamping)));
                // Visibility guard for a long frame; ground tracking does not follow the pawn's hop oscillation.
                Vector2 safetyHalf=(BoardViewport.size-Vector2.one*8f)*size/Screen.height;
                center+=ComfortCorrection(center,min,max,safetyHalf);
                WriteCamera(PositionForPlane(center,size,BoardViewport),size);
            }
            else
            {
                float t=Mathf.Clamp01((now-returnStarted)/returnDuration), ease=t*t*(3f-2f*t);
                WriteCamera(Vector3.Lerp(returnStartPosition,overviewPosition,ease),Mathf.Lerp(returnStartSize,overviewSize,ease));
                if(t>=1f)
                {
                    trackedPawn=null; observedPath=null; IsArrivalHold=false;
                    SetState(CameraState.StrategicOverview);
                }
            }
        }
        public static Vector3 SampleAhead(Vector3 ground, System.Collections.Generic.List<Vector3> path, ref int segment, float distance)
        {
            if(path==null || path.Count==0) return ground;
            segment=Mathf.Clamp(segment,0,path.Count-1);
            while(segment<path.Count-1)
            {
                Vector3 end=path[segment];end.y=ground.y;
                Vector3 next=path[segment+1];next.y=ground.y;
                if((end-ground).sqrMagnitude>.04f && Vector3.Dot(ground-end,next-end)<=.0001f) break;
                segment++;
            }
            Vector3 from=ground;
            for(int i=segment;i<path.Count;i++)
            {
                Vector3 to=path[i];to.y=ground.y;float length=Vector3.Distance(from,to);
                if(length>=distance && length>.0001f) return Vector3.Lerp(from,to,distance/length);
                distance-=length;from=to;
            }
            return from;
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

        public static float CloseStrategicSize(float referenceSize) => referenceSize / PermanentCloseFactor;

        // Retain a little route context beyond the edge rather than allowing an empty view.
        // The inset is smaller than the viewport half extent, so every board point remains reachable.
        public static Vector2 ClampExplorationCenter(Vector2 center,Vector2 min,Vector2 max,Vector2 viewportHalf)
        {
            Vector2 inset=Vector2.Min(Vector2.Max(Vector2.zero,viewportHalf)*.30f,(max-min)*.5f);
            return new Vector2(Mathf.Clamp(center.x,min.x+inset.x,max.x-inset.x),
                Mathf.Clamp(center.y,min.y+inset.y,max.y-inset.y));
        }

        private void UpdateRestAnchor(Transform arrivedPawn = null)
        {
            var active=turnManager?.CurrentPlayer;
            if (active==null || boardManager?.visualTiles==null) return;
            restPlayerId=active.PlayerId;restLogicalPosition=turnManager.GetPlayerPosition(active);
            if (arrivedPawn!=null) restPawn=arrivedPawn;
            else if(restPawn==null || restPawn.name!="Pawn_Player_"+restPlayerId)
                restPawn=GameObject.Find("Pawn_Player_"+restPlayerId)?.transform;
            if(restPawn==null || restLogicalPosition<0 || restLogicalPosition>=boardManager.visualTiles.Length) return;
            Vector3 ground=restPawn.position;
            var next=boardManager.visualTiles[(restLogicalPosition+1)%boardManager.visualTiles.Length];
            // A small forward lead retains the next decisions, while leaving the pawn central.
            Vector3 direction=next!=null ? next.position-ground : Vector3.zero;direction.y=0;
            Vector2 center=ProjectPlane(ground+Vector3.ClampMagnitude(direction,movementLookAhead));
            center.y+=PawnHeadroom*.25f*mainCamera.transform.up.y;
            center=ClampExplorationCenter(center,boardPlaneMin,boardPlaneMax,BoardViewport.size*overviewSize/Screen.height);
            overviewPosition=PositionForPlane(center,overviewSize,BoardViewport);
            restAnchorPending=false;
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
            if (boardManager?.visualTiles == null) return false;
            var pawns=FindObjectsOfType<PolyRisk.Visuals.PawnMovement>();
            if(pawns.Length==0) return false;
            Vector3 pawnMin=Vector3.zero,pawnMax=Vector3.zero;
            foreach(var pawn in pawns)
                foreach(var renderer in pawn.GetComponentsInChildren<Renderer>())
                {
                    var bounds=renderer.bounds;
                    float scale=Mathf.Max(1f,PolyRisk.Visuals.PlayerVisualManager.MaximumPawnScale/Mathf.Max(.001f,pawn.transform.localScale.x));
                    pawnMin=Vector3.Min(pawnMin,(bounds.min-pawn.transform.position)*scale);
                    pawnMax=Vector3.Max(pawnMax,(bounds.max-pawn.transform.position)*scale);
                }
            boardPlaneMin = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            boardPlaneMax = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            foreach (var anchor in boardManager.visualTiles)
            {
                if (anchor == null) return false;
                var collider = anchor.GetComponent<BoxCollider>();
                if (collider == null) return false;
                for (int n = 0; n < 8; n++)
                {
                    var sign = new Vector3((n & 1) == 0 ? -1 : 1, (n & 2) == 0 ? -1 : 1, (n & 4) == 0 ? -1 : 1);
                    IncludeInFraming(collider.transform.TransformPoint(collider.center + Vector3.Scale(collider.size * 0.5f, sign)));
                    IncludeInFraming(anchor.position+new Vector3((n&1)==0?pawnMin.x:pawnMax.x,
                        (n&2)==0?pawnMin.y:pawnMax.y,(n&4)==0?pawnMin.z:pawnMax.z));
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
            // Touch owns the gesture when present; do not process its emulated mouse tap.
            if (Input.touchCount > 0)
            {
                if (Input.touchCount != 1) { CancelPointer(); return; }
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                { BeginPointer(touch.position); pointerFingerId=touch.fingerId; }
                else if (pointerFingerId == touch.fingerId)
                {
                    if (touch.phase == TouchPhase.Canceled) CancelPointer();
                    else if (touch.phase == TouchPhase.Ended) EndPointer(touch.position);
                    else MovePointer(touch.position);
                }
                return;
            }
            if (Input.GetMouseButtonDown(0)) { pointerFingerId=-1; BeginPointer(Input.mousePosition); }
            if (Input.GetMouseButton(0)) MovePointer(Input.mousePosition);
            if (Input.GetMouseButtonUp(0)) EndPointer(Input.mousePosition);
        }

        private void BeginPointer(Vector2 position)
        {
            CancelPointer();
            if (mainCamera==null || !hasFramingTarget || IsScreenInputBlocked(position)) return;
            pointerStart=position; pointerStartCenter=PlaneAtViewportCenter(mainCamera.transform.position,mainCamera.orthographicSize);
            pointerPressed=true;
        }
        private float PointerThreshold
        {
            get
            {
                var root=topHudWidget?.uiDocument?.rootVisualElement;
                float uiScale=root!=null && root.worldBound.width>0 ? Screen.width/root.worldBound.width : 1f;
                return DragThresholdUiPixels*uiScale;
            }
        }
        public static bool ExceedsDragThreshold(Vector2 start, Vector2 current, float threshold) =>
            (current-start).sqrMagnitude >= threshold*threshold;
        private void MovePointer(Vector2 position)
        {
            if (!pointerPressed) return;
            if (!pointerDragging && ExceedsDragThreshold(pointerStart,position,PointerThreshold)) pointerDragging=true;
            if (!pointerDragging || State!=CameraState.StrategicOverview || reducedMovementPending) return;
            IsManuallyExploring=true; restAnchorPending=false;
            Vector2 center=pointerStartCenter-(position-pointerStart)*(mainCamera.orthographicSize*2f/Screen.height);
            center=ClampExplorationCenter(center,boardPlaneMin,boardPlaneMax,BoardViewport.size*mainCamera.orthographicSize/Screen.height);
            WriteCamera(PositionForPlane(center,mainCamera.orthographicSize,BoardViewport),mainCamera.orthographicSize);
        }
        private void EndPointer(Vector2 position)
        {
            if (!pointerPressed) return;
            MovePointer(position);
            bool tap=!pointerDragging && !IsScreenInputBlocked(position);
            CancelPointer();
            if (tap) HandleInput(position);
        }
        private void CancelPointer() { pointerPressed=false;pointerDragging=false;pointerFingerId=-1; }

        private bool IsScreenInputBlocked(Vector2 screenPosition)
        {
            foreach (var document in FindObjectsOfType<UIDocument>())
            {
                var panel = document.rootVisualElement?.panel;
                if (panel == null) continue;
                var position = RuntimePanelUtils.ScreenToPanel(panel,
                    new Vector2(screenPosition.x, Screen.height - screenPosition.y));
                if (IsBoardInputBlocked(panel.Pick(position))) return true;
            }
            return false;
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

            if (IsScreenInputBlocked(screenPosition)) return;
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
