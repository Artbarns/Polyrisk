using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PolyRisk.Visuals;

namespace PolyRisk.Editor
{
    /// <summary>PASS 2A.1 changes material assets only; the scene and meshes stay byte-identical.</summary>
    public static class GraphiteRefinementValidation
    {
        const string ScenePath = "Assets/Scenes/SampleScene.unity";
        const string Report = "Docs/Validation/GraphiteRefinement_2026-10-19";

        public static void ApplyAndBuild()
        {
            Apply();
            EconomyBatchValidation.RunAllEditorRegressions();
            EconomyBatchValidation.BuildWebGL();
            var shader = Shader.Find("PolyRisk/GraphiteSurface");
            foreach (var message in ShaderUtil.GetShaderMessages(shader))
                if (message.severity.ToString() == "Error") throw new Exception(message.message);
        }

        [MenuItem("PolyRisk/Graphite Foundation/Apply PASS 2A.1 materials only")]
        public static void Apply()
        {
            string sceneHash = Hash(File.ReadAllBytes(ScenePath));
            EditorSceneManager.OpenScene(ScenePath);
            var board = UnityEngine.Object.FindObjectOfType<PolyRiskBoard>();
            if (board == null || board.visualTheme == null) throw new Exception("PASS 2A theme is required");
            var theme = board.visualTheme;
            string geometry = Geometry(theme);
            var meshes = board.GetComponentsInChildren<MeshFilter>();
            var meshSignatures = meshes.Select(m => Hash(System.Text.Encoding.UTF8.GetBytes(EditorJsonUtility.ToJson(m.sharedMesh)))).ToArray();
            var anchors = board.GetComponentsInChildren<TileIdentifier>().OrderBy(a => a.TileIndex).ToArray();
            var anchorSignatures = anchors.Select(a => EditorJsonUtility.ToJson(a.transform) + EditorJsonUtility.ToJson(a.GetComponent<BoxCollider>())).ToArray();
            string camera = EditorJsonUtility.ToJson(Camera.main) + EditorJsonUtility.ToJson(Camera.main.transform);

            theme.frameMetallic = 0.40f;
            theme.frameSmoothness = 0.52f;
            theme.tileMetallic = 0.28f;
            theme.tileSmoothness = 0.44f;
            theme.restingEmission = 1.20f;
            theme.trimEmission = 0.18f;
            theme.categorySaturation = 0.62f;
            theme.simulatedReflection = 0.18f;
            theme.reflectionFalloff = 0.025f;
            theme.frameSheen = 0.30f;
            theme.tileSheen = 0.24f;
            theme.edgeNormalStrength = 0.20f;
            theme.edgeShadingPixels = 0.85f;
            theme.minimumBandPixels = 1.20f;
            theme.neonBoost = 1.50f;
            theme.reflectionReach = 0.10f;
            board.RefreshSurfaceMaterials();
            EditorUtility.SetDirty(theme);
            foreach (var material in new[] { board.metalMaterial, board.tileMaterial, board.darkMaterial, board.neonMaterial })
                EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();

            Require(sceneHash == Hash(File.ReadAllBytes(ScenePath)), "Scene byte-identical");
            Require(geometry == Geometry(theme), "PASS 2A geometry parameters unchanged");
            Require(meshes.Length == 4 && meshSignatures.SequenceEqual(meshes.Select(m => Hash(System.Text.Encoding.UTF8.GetBytes(EditorJsonUtility.ToJson(m.sharedMesh))))), "Four meshes unchanged");
            Require(anchors.Length == 40 && anchors.Select(a => a.TileIndex).SequenceEqual(Enumerable.Range(0,40)), "40 unique indices");
            Require(anchorSignatures.SequenceEqual(anchors.Select(a => EditorJsonUtility.ToJson(a.transform) + EditorJsonUtility.ToJson(a.GetComponent<BoxCollider>()))), "Anchors and colliders unchanged");
            Require(camera == EditorJsonUtility.ToJson(Camera.main) + EditorJsonUtility.ToJson(Camera.main.transform), "Camera unchanged");
            Physics.SyncTransforms();
            foreach (var anchor in anchors)
            {
                RaycastHit hit;
                Require(Physics.Raycast(anchor.transform.position + Vector3.up * 2f, Vector3.down, out hit, 3f)
                    && hit.collider.gameObject == anchor.gameObject, "Raycast " + anchor.TileIndex);
            }
            Require(ShaderUtil.GetShaderMessages(Shader.Find("PolyRisk/GraphiteSurface")).All(m => m.severity.ToString() != "Error"), "Shader import diagnostics");
            Directory.CreateDirectory(Report);
            File.WriteAllText(Report + "/surface-validation.txt", "PASS: scene byte-identical; four meshes unchanged; geometry parameters unchanged; 40 indices, anchors, colliders and raycasts unchanged; camera unchanged.\n" +
                "Vertices: " + meshes.Sum(m => m.sharedMesh.vertexCount) + "\nTriangles: " + meshes.Sum(m => m.sharedMesh.triangles.Length / 3) + "\n");
            File.WriteAllText(Report + "/theme-after.json", EditorJsonUtility.ToJson(theme, true));
            Debug.Log("GRAPHITE REFINEMENT VALIDATION PASS");
        }

        static string Geometry(BoardVisualTheme t) => t.thicknessRatio + "/" + t.cornerRadiusRatio + "/" + t.outlineWidthRatio + "/" +
            t.categoryBandRatio + "/" + t.cornerSegments + "/" + t.frameCornerRadius + "/" + t.tileSurfaceY;
        static string Hash(byte[] bytes)
        {
            using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(bytes));
        }
        static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("Graphite refinement: " + message);
        }
    }
}
