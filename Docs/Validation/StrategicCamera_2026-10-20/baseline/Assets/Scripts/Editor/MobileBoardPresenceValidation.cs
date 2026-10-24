using System;
using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using PolyRisk.Systems;
using PolyRisk.Visuals;
using PolyRisk.UI;

namespace PolyRisk.Editor
{
    public static class MobileBoardPresenceValidation
    {
        const string Report = "Docs/Validation/CameraPresence_2026-10-19";
        static int checks;
        public static void RunAndBuild()
        {
            EconomyBatchValidation.RunAllEditorRegressions();
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            checks = 0;
            var board = UnityEngine.Object.FindObjectOfType<PolyRiskBoard>();
            var anchors = board.GetComponentsInChildren<TileIdentifier>().OrderBy(a => a.TileIndex).ToArray();
            Require(anchors.Length == 40 && anchors.Select(a => a.TileIndex).SequenceEqual(Enumerable.Range(0,40)), "40 unique tile indices");
            Physics.SyncTransforms();
            foreach (var anchor in anchors)
            {
                RaycastHit hit;
                Require(Physics.Raycast(anchor.transform.position + Vector3.up * 2, Vector3.down, out hit, 3)
                    && hit.collider.gameObject == anchor.gameObject, "Raycast " + anchor.TileIndex);
            }
            Require(board.GetComponentsInChildren<MeshFilter>().Length == 4, "Four fused render meshes");
            var camera = Camera.main;
            var authored = camera.transform.rotation;
            Require(camera.orthographic, "Orthographic projection retained");
            camera.transform.rotation = BoardInteractionManager.DefaultRotation(authored);
            var projected = PolyRisk.DebugTools.MobileBoardValidationProbe.MeasureProjection(camera,board,anchors.Select(a => a.transform).ToArray());
            Require(Mathf.Abs(projected.elevation - 40f) < .01f && Mathf.Abs(projected.azimuth - 45f) < .01f,
                "Official default is 40 degrees with unchanged azimuth");
            foreach (var anchor in anchors)
            {
                Vector3 screen = camera.WorldToScreenPoint(anchor.transform.position + Vector3.up * .1f);
                Require(Physics.Raycast(camera.ScreenPointToRay(screen), out RaycastHit hit, 100f)
                    && hit.collider.gameObject == anchor.gameObject, "Default screen raycast tile " + anchor.TileIndex);
            }
            CheckConsultationFrames(camera,board,anchors);
            CheckMovementCallbacks(anchors);
            camera.transform.rotation = authored;
            var hudTree = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>("Assets/UI/Layouts/TopHUD.uxml").CloneTree();
            Require(UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Button>(hudTree,"Btn_GameMenu") != null, "HUD menu preserved");
            Require(UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Button>(hudTree,"Btn_BoardFocus") != null, "Explicit consultation/overview control available");
            Require(!File.ReadAllText("Assets/UI/Layouts/TopHUD.uxml").Contains("BoardAnalysis"), "Analysis controls removed");
            string overlay = File.ReadAllText("Assets/Scripts/UI/BoardTileOverlayWidget.cs");
            Require(!overlay.Contains("FormatYield") && !overlay.Contains("Analysis") && !overlay.Contains("new Label"), "No financial overlay or toggle logic");
            var extent = new Vector2(21,11);
            foreach (bool expanded in new[] { false, true })
            foreach (bool portrait in new[] { false, true })
            {
                float width = portrait ? 430 : 1200, height = portrait ? 830 : 900;
                var safe = new Rect(0,0,width,height);
                var hud = new Rect(0, height - 134, width, 134);
                var cockpit = new Rect(portrait ? 0 : width * 0.46f, 0,
                    portrait ? width : width * 0.54f, expanded ? height * 0.52f : 236);
                float margin = portrait ? 8 : 12;
                var viewport = BoardInteractionManager.ChooseBoardViewport(safe, hud, cockpit, extent, margin);
                Require(viewport.width > 0 && viewport.height > 0 && !viewport.Overlaps(cockpit), "Viewport excludes cockpit");
                float size = BoardInteractionManager.FitCameraSize(height, viewport, extent);
                Require(extent.x * height / (2 * size) <= viewport.width + 0.01f &&
                    extent.y * height / (2 * size) <= viewport.height + 0.01f, "Entire board fits both axes");
                Require(viewport.xMin >= margin && viewport.xMax <= width - margin && viewport.yMax <= hud.yMin - margin, "HUD and lateral margins");
            }
            var standard = BoardInteractionManager.ChooseBoardViewport(new Rect(0,0,430,830),new Rect(0,696,430,134),new Rect(0,0,430,236),extent,12);
            var dense = BoardInteractionManager.ChooseBoardViewport(new Rect(0,0,860,1660),new Rect(0,1392,860,268),new Rect(0,0,860,472),extent,24);
            Require(Mathf.Abs(BoardInteractionManager.FitCameraSize(830,standard,extent) -
                BoardInteractionManager.FitCameraSize(1660,dense,extent)) < 0.001f, "DPR 2 preserves the same projected CSS framing");
            var notch = UIUXRuntime.IntersectSafeInsets(new Rect(0,0,430,830),430,830,new Vector4(0,34,0,44));
            Require(notch == new Rect(0,34,430,752), "Notch/home insets in render pixels");
            Require(UIUXRuntime.IntersectSafeInsets(notch,430,830,new Vector4(0,34,0,44)) == notch, "Native/browser safe insets are not added twice");
            var landscape = BoardInteractionManager.ChooseBoardViewport(new Rect(0,0,900,430), new Rect(0,336,900,94),
                new Rect(414,0,486,260),extent,12);
            Require(landscape.xMax <= 402 && landscape.height > 200, "Short landscape uses the free side instead of a tiny top strip");
            Directory.CreateDirectory(Report);
            File.WriteAllText(Report + "/geometry-and-framing.txt", "PASS: " + checks + " checks; 40 indices/raycasts, 4 meshes, portrait/desktop compact/expanded; official 40-degree projection, bounded focus with active pawn and selected tile, movement callbacks; no analysis UI, safe insets and short landscape.\n");
            Debug.Log("MOBILE BOARD REGRESSION PASS: " + checks + " checks");
            EconomyBatchValidation.BuildWebGL();
        }
        static void CheckConsultationFrames(Camera camera, PolyRiskBoard board, TileIdentifier[] anchors)
        {
            var all = new System.Collections.Generic.List<Vector3>();
            foreach(var mesh in board.GetComponentsInChildren<MeshFilter>())
                foreach(var vertex in mesh.sharedMesh.vertices) all.Add(mesh.transform.TransformPoint(vertex));
            foreach(var anchor in anchors) all.AddRange(TilePoints(anchor.transform,true));
            PlaneBounds(camera,all,out Vector2 boardMin,out Vector2 boardMax);
            foreach(bool portrait in new[]{true,false}) foreach(bool expanded in new[]{false,true})
            {
                float width=portrait?430:1200, height=portrait?830:900, margin=portrait?8:12;
                var viewport=BoardInteractionManager.ChooseBoardViewport(new Rect(0,0,width,height),new Rect(0,height-134,width,134),
                    new Rect(portrait?0:534,0,portrait?width:648,expanded?height*.52f:236),boardMax-boardMin,margin);
                float overview=BoardInteractionManager.FitCameraSize(height,viewport,boardMax-boardMin);
                foreach(int active in new[]{0,20}) foreach(var target in anchors)
                {
                    var points=TilePoints(target.transform,false).Concat(TilePoints(anchors[active].transform,true));
                    PlaneBounds(camera,points,out Vector2 min,out Vector2 max);
                    BoardInteractionManager.FitConsultation(height,viewport,boardMin,boardMax,min,max,12,out float size,out Vector2 center);
                    Require(size<=overview+.001f && size>=overview/1.25f-.001f,"Bounded focus "+target.TileIndex);
                    Vector2 half=viewport.size*size/height;
                    Require(min.x>=center.x-half.x-.001f && max.x<=center.x+half.x+.001f &&
                        min.y>=center.y-half.y-.001f && max.y<=center.y+half.y+.001f,"Selected tile and active pawn remain visible "+target.TileIndex);
                }
            }
        }
        static System.Collections.Generic.IEnumerable<Vector3> TilePoints(Transform anchor,bool pawn)
        {
            var col=anchor.GetComponent<BoxCollider>();
            for(int n=0;n<8;n++)
            {
                var sign=new Vector3((n&1)==0?-1:1,(n&2)==0?-1:1,(n&4)==0?-1:1);
                yield return col.transform.TransformPoint(col.center+Vector3.Scale(col.size*.5f,sign));
            }
            if(pawn) yield return anchor.position+Vector3.up*1.45f;
        }
        static void PlaneBounds(Camera camera,System.Collections.Generic.IEnumerable<Vector3> points,out Vector2 min,out Vector2 max)
        {
            min=Vector2.positiveInfinity;max=Vector2.negativeInfinity;
            foreach(var p in points)
            {
                var v=new Vector2(Vector3.Dot(p,camera.transform.right),Vector3.Dot(p,camera.transform.up));
                min=Vector2.Min(min,v);max=Vector2.Max(max,v);
            }
        }
        static void CheckMovementCallbacks(TileIdentifier[] anchors)
        {
            bool skipped=GameBootstrapper.SkipMovementAnimation;
            var host=new GameObject("[QA] Movement contract");
            try
            {
                var motion=host.AddComponent<PawnMovement>();int callbacks=0;
                motion.MoveAlongPath(null,()=>callbacks++);
                Require(callbacks==1,"Empty movement callback fires exactly once");
                GameBootstrapper.SkipMovementAnimation=true;
                var destination=anchors[20].transform.position;
                motion.MoveAlongPath(new System.Collections.Generic.List<Vector3>{anchors[1].transform.position,destination},()=>callbacks++);
                Require(callbacks==2 && host.transform.position==destination,"Teleport-style movement preserves destination and single callback");
            }
            finally { GameBootstrapper.SkipMovementAnimation=skipped;UnityEngine.Object.DestroyImmediate(host); }
        }
        static void Require(bool condition, string message)
        {
            checks++;
            if (!condition) throw new Exception("MOBILE BOARD REGRESSION FAILED: " + message);
        }
    }
}
