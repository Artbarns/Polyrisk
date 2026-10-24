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
        const string Report = "Docs/Validation/StrategicCamera_2026-10-20";
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
            CheckComfortZones();
            CheckMovementCallbacks(anchors);
            camera.transform.rotation = authored;
            var hudTree = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>("Assets/UI/Layouts/TopHUD.uxml").CloneTree();
            Require(UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Button>(hudTree,"Btn_GameMenu") != null, "HUD menu preserved");
            Require(UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Button>(hudTree,"Btn_BoardFocus") == null, "Manual camera controls removed");
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
            File.WriteAllText(Report + "/geometry-and-framing.txt", "PASS: " + checks + " checks; 40 indices/raycasts, 4 meshes, portrait/desktop compact/expanded; official 40-degree projection, comfort-zone continuity, unchanged movement callbacks; no analysis UI, safe insets and short landscape.\n");
            Debug.Log("MOBILE BOARD REGRESSION PASS: " + checks + " checks");
            EconomyBatchValidation.BuildWebGL();
        }
        static void CheckComfortZones()
        {
            var center=Vector2.zero;var half=new Vector2(3,5);
            Require(BoardInteractionManager.ComfortCorrection(center,new Vector2(-1,-1),new Vector2(1,1),half)==Vector2.zero,"Pawn moves freely inside comfort zone");
            var right=BoardInteractionManager.ComfortCorrection(center,new Vector2(3,-1),new Vector2(4,1),half);
            Require(right==Vector2.right,"Horizontal-only correction near lateral boundary");
            var top=BoardInteractionManager.ComfortCorrection(center,new Vector2(-1,4),new Vector2(1,6),half);
            Require(top==Vector2.up,"Vertical correction only at visibility boundary");
            for(int i=0;i<400;i++)
            {
                float a=i*Mathf.PI/200;Vector2 p=new Vector2(Mathf.Cos(a)*8,Mathf.Sin(a)*4);
                Vector2 min=p-Vector2.one*.4f,max=p+Vector2.one*.4f;
                Vector2 corrected=center+BoardInteractionManager.ComfortCorrection(center,min,max,half);
                Require(min.x>=corrected.x-half.x-.001f && max.x<=corrected.x+half.x+.001f &&
                    min.y>=corrected.y-half.y-.001f && max.y<=corrected.y+half.y+.001f,"Continuous corner guard "+i);
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
