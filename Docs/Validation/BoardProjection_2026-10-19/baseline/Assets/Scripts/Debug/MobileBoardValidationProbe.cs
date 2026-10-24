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
    /// <summary>Development-only layout evidence. Never writes camera, UI or economic state.</summary>
    public sealed class MobileBoardValidationProbe : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [System.Serializable] sealed class Evidence
        {
            public int width, height, indices, anchorsVisible, collidersInside, financialBadges, identityMarkers, ownedMarkers, overlaps, selected;
            public bool analysis, expanded;
            public Rect viewport, hud, cockpit, primary, toggle, presentation;
            public Vector2 tile31, tile0, tile10, tile20, tile30;
            public string economy;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Mount()
        {
            if (Application.isBatchMode) return;
            new GameObject("[QA] Mobile board evidence").AddComponent<MobileBoardValidationProbe>();
        }
        IEnumerator Start()
        {
            string previous = "";
            while (true)
            {
                yield return new WaitForSecondsRealtime(0.75f);
                var framing = FindObjectOfType<BoardInteractionManager>();
                var board = BoardManager.Instance; var camera = Camera.main;
                var turns = TurnManager.Instance;
                var cockpit = FindObjectOfType<ActionButtonsWidget>();
                var hud = FindObjectOfType<TopHUDWidget>();
                if (framing == null || camera == null || board?.visualTiles == null || turns?.CurrentPlayer == null || cockpit == null || hud == null) continue;
                var root = hud.uiDocument.rootVisualElement; var actionRoot = cockpit.uiDocument.rootVisualElement;
                var panel = actionRoot.Q<VisualElement>(className:"marketplace-panel");
                var e = new Evidence { width = Screen.width, height = Screen.height, viewport = framing.BoardViewport,
                    analysis = root.ClassListContains("board-analysis"), expanded = actionRoot.Q<VisualElement>("Panel_TileDetail").ClassListContains("pulse-panel--expanded"),
                    selected = cockpit.InspectedTileIndex, identityMarkers = root.Query<VisualElement>(className:"board-tile-identity").ToList().Count,
                    ownedMarkers = root.Query<VisualElement>(className:"board-owner-marker").ToList().Count(m => m.resolvedStyle.display != DisplayStyle.None),
                    hud = ScreenRect(root, root.Q<VisualElement>("TopHUD_Bar")), cockpit = ScreenRect(actionRoot,panel),
                    presentation = ScreenRect(actionRoot,actionRoot.Q<VisualElement>("PulseCenterStage")),
                    primary = ScreenRect(actionRoot,actionRoot.Q<Button>("Btn_Primary")), toggle = ScreenRect(root,root.Q<Button>("Btn_BoardAnalysis")),
                    economy = string.Join(";",turns.Players.Select(p => p.PlayerId + ":" + p.CurrentBalance.ToString("R",System.Globalization.CultureInfo.InvariantCulture) + ":" + turns.GetPlayerPosition(p) + ":" + p.OwnedProperties.Count)) };
                foreach (var anchor in board.visualTiles)
                {
                    if (anchor == null) continue;
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
                var badges = root.Query<VisualElement>(className:"board-tile-analysis-badge").ToList().Where(b => b.resolvedStyle.display != DisplayStyle.None).ToArray();
                e.financialBadges = badges.Length;
                for(int a = 0;a < badges.Length;a++) for(int b = a + 1;b < badges.Length;b++)
                    if(badges[a].worldBound.Overlaps(badges[b].worldBound)) e.overlaps++;
                e.tile31 = ScreenPoint(camera,board.visualTiles[31]); e.tile0 = ScreenPoint(camera,board.visualTiles[0]);
                e.tile10 = ScreenPoint(camera,board.visualTiles[10]); e.tile20 = ScreenPoint(camera,board.visualTiles[20]); e.tile30 = ScreenPoint(camera,board.visualTiles[30]);
                string data = JsonUtility.ToJson(e);
                if(data != previous) { previous = data; Debug.Log("BOARD_QA " + data); }
            }
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
