using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Linq;
using PolyRisk.Systems;
using PolyRisk.UI;
using UnityEngine.UIElements;
#endif

namespace PolyRisk.DebugTools
{
    /// <summary>Development-only projection/layout evidence. Never writes game, camera or UI state.</summary>
    public sealed class MobileBoardValidationProbe : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [System.Serializable] public sealed class Projection
        {
            public Rect boardPixels;
            public float elevation, azimuth, orthographicSize, minTileEdgePixels, minTileAltitudePixels, minTileAreaPixels;
        }
        [System.Serializable] sealed class Evidence
        {
            public int width, height, indices, anchorsVisible, collidersInside, identityMarkers, ownedMarkers, selected;
            public bool expanded, focused, reducedMotion;
            public Rect viewport, hud, cockpit, primary, presentation, focusControl;
            public Vector2 tile31, tile32, tile0, tile10, tile20, tile30;
            public string economy, turnState, inspectionName, rent, risk, yield, ownership;
            public Projection projection;
        }
        public static Projection MeasureProjection(Camera camera, PolyRiskBoard board, Transform[] anchors)
        {
            Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity;
            foreach (var mesh in board.GetComponentsInChildren<MeshFilter>())
                foreach (var vertex in mesh.sharedMesh.vertices)
                {
                    Vector2 p = camera.WorldToScreenPoint(mesh.transform.TransformPoint(vertex));
                    min = Vector2.Min(min, p); max = Vector2.Max(max, p);
                }
            var m = new Projection { boardPixels = Rect.MinMaxRect(min.x,min.y,max.x,max.y),
                elevation = Mathf.Asin(-camera.transform.forward.y) * Mathf.Rad2Deg,
                azimuth = Mathf.Atan2(camera.transform.forward.x,camera.transform.forward.z) * Mathf.Rad2Deg,
                orthographicSize = camera.orthographicSize, minTileEdgePixels = float.PositiveInfinity,
                minTileAltitudePixels = float.PositiveInfinity, minTileAreaPixels = float.PositiveInfinity };
            foreach(var anchor in anchors)
            {
                var col = anchor.GetComponent<BoxCollider>();
                var center = col.center;
                // The collider's XZ footprint is the authored tile footprint. Translation in Y does not change its projected size.
                Vector2 p = camera.WorldToScreenPoint(col.transform.TransformPoint(center));
                Vector2 x = (Vector2)camera.WorldToScreenPoint(col.transform.TransformPoint(center + Vector3.right * col.size.x)) - p;
                Vector2 z = (Vector2)camera.WorldToScreenPoint(col.transform.TransformPoint(center + Vector3.forward * col.size.z)) - p;
                float area = Mathf.Abs(x.x * z.y - x.y * z.x);
                m.minTileEdgePixels = Mathf.Min(m.minTileEdgePixels, Mathf.Min(x.magnitude,z.magnitude));
                m.minTileAltitudePixels = Mathf.Min(m.minTileAltitudePixels, area / Mathf.Max(x.magnitude,z.magnitude));
                m.minTileAreaPixels = Mathf.Min(m.minTileAreaPixels,area);
            }
            return m;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Mount()
        {
            if (Application.isBatchMode) return;
            new GameObject("[QA] Mobile board evidence").AddComponent<MobileBoardValidationProbe>();
        }
        IEnumerator Start()
        {
            string previous = ""; bool checkedMovement = false;
            while (true)
            {
                yield return new WaitForSecondsRealtime(0.75f);
                var framing = FindObjectOfType<BoardInteractionManager>();
                var board = BoardManager.Instance; var camera = Camera.main;
                var surfaces = FindObjectOfType<PolyRiskBoard>();
                var turns = TurnManager.Instance;
                var cockpit = FindObjectOfType<ActionButtonsWidget>();
                var hud = FindObjectOfType<TopHUDWidget>();
                if (framing == null || camera == null || surfaces == null || board?.visualTiles == null || turns?.CurrentPlayer == null || cockpit == null || hud == null) continue;
                if(board.visualTiles.Any(t => t == null)) continue;
                if (!checkedMovement) { checkedMovement = true; StartCoroutine(CheckAnimatedMovement(board.visualTiles)); }
                var root = hud.uiDocument.rootVisualElement; var actionRoot = cockpit.uiDocument.rootVisualElement;
                var panel = actionRoot.Q<VisualElement>(className:"marketplace-panel");
                var e = new Evidence { width = Screen.width, height = Screen.height, viewport = framing.BoardViewport, focused = framing.FocusActive, reducedMotion = UIUXRuntime.ReducedMotion, turnState = turns.CurrentState.ToString(),
                    expanded = actionRoot.Q<VisualElement>("Panel_TileDetail").ClassListContains("pulse-panel--expanded"),
                    selected = cockpit.InspectedTileIndex, identityMarkers = root.Query<VisualElement>(className:"board-tile-identity").ToList().Count,
                    ownedMarkers = root.Query<VisualElement>(className:"board-owner-marker").ToList().Count(m => m.resolvedStyle.display != DisplayStyle.None),
                    hud = ScreenRect(root, root.Q<VisualElement>("TopHUD_Bar")), cockpit = ScreenRect(actionRoot,panel),
                    presentation = ScreenRect(actionRoot,actionRoot.Q<VisualElement>("PulseCenterStage")),
                    primary = ScreenRect(actionRoot,actionRoot.Q<Button>("Btn_Primary")),
                    focusControl = ScreenRect(root,root.Q<Button>("Btn_BoardFocus")),
                    projection = MeasureProjection(camera,surfaces,board.visualTiles),
                    inspectionName = actionRoot.Q<Label>("Lbl_TileName")?.text, rent = actionRoot.Q<Label>("Lbl_TileRent")?.text,
                    risk = actionRoot.Q<Label>("Lbl_TileRisk")?.text, yield = actionRoot.Q<Label>("Lbl_TileRoi")?.text,
                    ownership = actionRoot.Q<Label>("Lbl_PulseOwnership")?.text,
                    economy = string.Join(";",turns.Players.Select(p => p.PlayerId + ":" + p.CurrentBalance.ToString("R",System.Globalization.CultureInfo.InvariantCulture) + ":" + turns.GetPlayerPosition(p) + ":" + p.OwnedProperties.Count)) };
                foreach (var anchor in board.visualTiles)
                {
                    e.indices++;
                    var point = camera.WorldToScreenPoint(anchor.position);
                    if (e.viewport.Contains(point)) e.anchorsVisible++;
                    var collider = anchor.GetComponent<BoxCollider>();
                    bool inside = collider != null;
                    if (collider != null) for (int n = 0; n < 8; n++)
                    {
                        var sign = new Vector3((n & 1) == 0 ? -1 : 1,(n & 2) == 0 ? -1 : 1,(n & 4) == 0 ? -1 : 1);
                        var corner = collider.transform.TransformPoint(collider.center + Vector3.Scale(collider.size * 0.5f,sign));
                        inside &= e.viewport.Contains(camera.WorldToScreenPoint(corner));
                    }
                    if (inside) e.collidersInside++;
                }
                e.tile31 = ScreenPoint(camera,board.visualTiles[31]); e.tile32 = ScreenPoint(camera,board.visualTiles[32]); e.tile0 = ScreenPoint(camera,board.visualTiles[0]);
                e.tile10 = ScreenPoint(camera,board.visualTiles[10]); e.tile20 = ScreenPoint(camera,board.visualTiles[20]); e.tile30 = ScreenPoint(camera,board.visualTiles[30]);
                string data = JsonUtility.ToJson(e);
                if(data != previous) { previous = data; Debug.Log("BOARD_QA " + data); }
            }
        }
        IEnumerator CheckAnimatedMovement(Transform[] anchors)
        {
            var host = new GameObject("[QA] Invisible movement contract");
            var motion = host.AddComponent<PolyRisk.Visuals.PawnMovement>();
            host.transform.position = anchors[1].position;
            int callbacks = 0;
            motion.MoveAlongPath(new System.Collections.Generic.List<Vector3> { anchors[2].position, anchors[3].position }, () => callbacks++);
            float deadline = Time.realtimeSinceStartup + 4f;
            while (callbacks == 0 && Time.realtimeSinceStartup < deadline) yield return null;
            bool passed = callbacks == 1 && Vector3.Distance(host.transform.position,anchors[3].position) < .001f && host.transform.localScale == Vector3.one;
            Debug.Log("CAMERA_MOTION_QA " + (passed ? "PASS" : "FAIL") + " animated path 1->2->3; callbacks=" + callbacks + "; reducedMotion=" + UIUXRuntime.ReducedMotion);
            Destroy(host);
        }
        static Vector2 ScreenPoint(Camera camera,Transform point)
        {
            var p = camera.WorldToScreenPoint(point.position); return new Vector2(p.x,Screen.height-p.y);
        }
        static Rect ScreenRect(VisualElement root,VisualElement element)
        {
            if (element == null) return default;
            var r=root.worldBound; var e=element.worldBound;
            return new Rect((e.xMin-r.xMin)*Screen.width/r.width,Screen.height-(e.yMax-r.yMin)*Screen.height/r.height,
                e.width*Screen.width/r.width,e.height*Screen.height/r.height);
        }
#endif
    }
}
