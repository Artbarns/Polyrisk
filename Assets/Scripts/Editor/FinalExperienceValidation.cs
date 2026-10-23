using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using PolyRisk.Visuals;

namespace PolyRisk.Editor
{
    /// <summary>Release gate for the existing game and its close strategic presentation.</summary>
    public static class FinalExperienceValidation
    {
        private const string Report = "Docs/Validation/FinalExperience_2026-10-23";
        private static int checks;
        private static void Check(bool value, string name)
        {
            checks++;
            if (!value) throw new Exception("FINAL EXPERIENCE FAIL " + name);
        }
        public static void Validate()
        {
            EconomyBatchValidation.RunAllEditorRegressions();
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            CloseStrategicCameraValidation.Run();
            var board = UnityEngine.Object.FindObjectOfType<PolyRiskBoard>();
            var anchors = board.GetComponentsInChildren<TileIdentifier>().OrderBy(a => a.TileIndex).ToArray();
            Check(anchors.Length == 40 && anchors.Select(a => a.TileIndex).SequenceEqual(Enumerable.Range(0,40)), "40 exact indices");
            Check(board.GetComponentsInChildren<MeshFilter>().Length == 4, "four fused meshes");
            Physics.SyncTransforms();
            foreach (var a in anchors)
            {
                RaycastHit hit;
                Check(Physics.Raycast(a.transform.position + Vector3.up * 2, Vector3.down, out hit, 3) && hit.collider.gameObject == a.gameObject, "unchanged collider " + a.TileIndex);
            }
            Check(board.visualTheme.surfaceTreatment == BoardVisualTheme.SurfaceTreatment.GraphiteReflectiveNeon, "Reflective Neon retained");
            var low = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Low_PipelineAsset.asset");
            var medium = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Medium_PipelineAsset.asset");
            Check(low.renderScale == 1 && medium.renderScale == 1, "native pipeline scale retained");
            Check(low.useSRPBatcher && medium.useSRPBatcher, "SRP batching retained");
            Check(!low.supportsMainLightShadows, "Low no shadow passes");
            foreach (var name in new[] { "PolyRisk/GraphiteSurface", "PolyRisk/NeonState" })
            {
                var shader = Shader.Find(name);
                Check(shader != null && ShaderUtil.GetShaderMessages(shader).All(m => m.severity.ToString() != "Error"), "shader import " + name);
            }
            Directory.CreateDirectory(Report);
            string result = "FINAL EXPERIENCE EDITOR PASS " + checks + " checks, plus camera gate and 612 existing assertions.\n";
            File.WriteAllText(Report + "/editor-checks.txt", result);
            Debug.Log(result);
        }
        public static void BuildQA()
        {
            Validate();
            EconomyBatchValidation.BuildWebGL();
        }
        public static void BuildRelease()
        {
            Validate();
            var assets = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath).ToDictionary(path => path, File.ReadAllBytes);
            try
            {
                string output = Environment.GetEnvironmentVariable("POLYRISK_WEBGL_OUTPUT");
                if (string.IsNullOrWhiteSpace(output)) throw new Exception("POLYRISK_WEBGL_OUTPUT is required");
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = new[] { "Assets/Scenes/SampleScene.unity" }, locationPathName = Path.GetFullPath(output),
                    target = BuildTarget.WebGL, options = BuildOptions.None
                });
                if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                    throw new Exception("Final Release build failed: " + report.summary.result);
                Debug.Log("FINAL EXPERIENCE WEBGL RELEASE PASS");
            }
            finally
            {
                foreach (var asset in assets) File.WriteAllBytes(asset.Key, asset.Value);
                AssetDatabase.Refresh();
            }
        }
    }
}
