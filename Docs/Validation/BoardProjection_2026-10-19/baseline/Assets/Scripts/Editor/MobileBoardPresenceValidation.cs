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
        const string Report = "Docs/Validation/MobileBoardPresence_2026-10-19";
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
            var extent = new Vector2(21,11);
            foreach (bool analysis in new[] { false, true })
            foreach (bool expanded in new[] { false, true })
            foreach (bool portrait in new[] { false, true })
            {
                float width = portrait ? 430 : 1200, height = portrait ? 830 : 900;
                var safe = new Rect(0,0,width,height);
                var hud = new Rect(0, height - 134 - (analysis ? 24 : 0), width, 134 + (analysis ? 24 : 0));
                var cockpit = new Rect(portrait ? 0 : width * 0.46f, 0,
                    portrait ? width : width * 0.54f, expanded ? height * 0.52f : 236);
                var viewport = BoardInteractionManager.ChooseBoardViewport(safe, hud, cockpit, extent, 12);
                Require(viewport.width > 0 && viewport.height > 0 && !viewport.Overlaps(cockpit), "Viewport excludes cockpit");
                float size = BoardInteractionManager.FitCameraSize(height, viewport, extent);
                Require(extent.x * height / (2 * size) <= viewport.width + 0.01f &&
                    extent.y * height / (2 * size) <= viewport.height + 0.01f, "Entire board fits both axes");
                Require(viewport.xMin >= 12 && viewport.xMax <= width - 12 && viewport.yMax <= hud.yMin - 12, "HUD and lateral margins");
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
            File.WriteAllText(Report + "/geometry-and-framing.txt", "PASS: " + checks + " checks; 40 indices/raycasts, 4 meshes, portrait/desktop compact/expanded/analysis, safe insets and short landscape.\n");
            Debug.Log("MOBILE BOARD REGRESSION PASS: " + checks + " checks");
            EconomyBatchValidation.BuildWebGL();
        }
        static void Require(bool condition, string message)
        {
            checks++;
            if (!condition) throw new Exception("MOBILE BOARD REGRESSION FAILED: " + message);
        }
    }
}
