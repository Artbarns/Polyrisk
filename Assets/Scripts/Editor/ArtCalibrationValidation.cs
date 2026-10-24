using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using PolyRisk.Visuals;

namespace PolyRisk.Editor
{
    /// <summary>
    /// Calibration gates for the currently approved close camera, not historical
    /// fit-all-40 or extra-action-zoom requirements. No artistic asset is saved here.
    /// </summary>
    public static class ArtCalibrationValidation
    {
        static int checks;
        static readonly List<string> results = new List<string>();
        static void Check(bool value, string label)
        {
            checks++;
            results.Add((value ? "PASS " : "FAIL ") + label);
            if (!value) throw new Exception("ART CALIBRATION FAIL " + label);
        }
        static string ReportDirectory
        {
            get
            {
                string configured = Environment.GetEnvironmentVariable("POLYRISK_ART_REPORT");
                return string.IsNullOrWhiteSpace(configured)
                    ? "Docs/Validation/PremiumMasterpiece_2026-10-24" : configured;
            }
        }

        [MenuItem("PolyRisk/Validation/Art Calibration Gates")]
        public static void Validate()
        {
            // Editor scene execution can refresh shared material values. Preserve
            // the user's exact files even when Validate is called without a build.
            var preserved = CaptureMaterialAndPipelineFiles();
            try { ValidateInternal(); }
            finally { RestoreFiles(preserved); }
        }

        static void ValidateInternal()
        {
            checks = 0; results.Clear();
            EconomyBatchValidation.RunAllEditorRegressions();
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            CloseStrategicCameraValidation.Run();
            var board = UnityEngine.Object.FindObjectOfType<PolyRiskBoard>();
            Check(board != null && board.visualTheme != null, "real game scene and reusable theme");
            Check((int)board.visualTheme.surfaceTreatment == 2, "ArchitecturalGraphite is final scene treatment");
            var anchors = board.GetComponentsInChildren<TileIdentifier>().OrderBy(a => a.TileIndex).ToArray();
            Check(anchors.Length == 40 && anchors.Select(a => a.TileIndex).SequenceEqual(Enumerable.Range(0, 40)), "40 exact indexed anchors");
            var filters = board.GetComponentsInChildren<MeshFilter>();
            Check(filters.Length == 4 && filters.All(f => f.sharedMesh != null), "four existing fused meshes");
            var geometry = filters.Select(f => EditorJsonUtility.ToJson(f.sharedMesh)).ToArray();
            var matrices = anchors.Select(a => a.transform.localToWorldMatrix).ToArray();
            var boxes = anchors.Select(a => a.GetComponent<BoxCollider>()).ToArray();
            Check(boxes.All(b => b != null && b.enabled), "40 enabled original selection colliders");
            var centers = boxes.Select(b => b.center).ToArray(); var sizes = boxes.Select(b => b.size).ToArray();
            var camera = Camera.main;
            Check(camera != null && camera.orthographic, "existing orthographic camera");
            Vector3 pose = camera.transform.position; Quaternion rotation = camera.transform.rotation; float cameraSize = camera.orthographicSize;
            var presenter = board.GetComponent<BoardSurfaceLighting>();
            bool temporary = presenter == null;
            if (temporary) presenter = board.gameObject.AddComponent<BoardSurfaceLighting>();
            var variants = new List<ShaderReadback>();
            try
            {
                foreach (bool baseline in new[] { true, false })
                for (int stage = 0; stage < 5; stage++)
                {
                    presenter.ApplyArtCalibration(baseline, stage);
                    string label = (baseline ? "A baseline" : "B proposal") + " stage " + stage;
                    Check(camera.transform.position == pose && camera.transform.rotation == rotation && camera.orthographicSize == cameraSize,
                        label + " camera invariant");
                    Check(geometry.SequenceEqual(filters.Select(f => EditorJsonUtility.ToJson(f.sharedMesh))), label + " fused geometry invariant");
                    Check(matrices.SequenceEqual(anchors.Select(a => a.transform.localToWorldMatrix)), label + " anchors invariant");
                    Check(centers.SequenceEqual(boxes.Select(b => b.center)) && sizes.SequenceEqual(boxes.Select(b => b.size)), label + " collider dimensions invariant");
                    Check(presenter.Finish != null, label + " resolved material finish");
                    Check((int)presenter.Treatment == (baseline ? 1 : 2), label + " exact legacy/proposal treatment");
                    Check(board.metalMaterial != null && board.tileMaterial != null && board.neonMaterial != null && board.darkMaterial != null,
                        label + " original shared board materials");
                    foreach (var material in new[] { board.metalMaterial, board.tileMaterial, board.darkMaterial, board.neonMaterial })
                        Check(material.shader != null && material.shader.isSupported, label + " supported shader " + material.name);
                }
                presenter.ApplyArtCalibration(false, 4);
                foreach (var shader in new[] { board.metalMaterial.shader, board.tileMaterial.shader, board.darkMaterial.shader, board.neonMaterial.shader }.Distinct())
                {
                    var messages = ShaderUtil.GetShaderMessages(shader);
                    Check(messages.All(m => m.severity.ToString() != "Error"), "shader import diagnostics " + shader.name);
                    var countMethod = typeof(ShaderUtil).GetMethod("GetVariantCount", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                        null, new[] { typeof(Shader), typeof(bool) }, null);
                    variants.Add(new ShaderReadback {
                        name = shader.name, variantsAvailable = countMethod != null,
                        variants = countMethod != null ? Convert.ToUInt64(countMethod.Invoke(null, new object[] { shader, true })) : 0,
                        messages = messages.Select(m => m.severity + ": " + m.message).ToArray()
                    });
                }
                Physics.SyncTransforms();
                foreach (var anchor in anchors)
                {
                    RaycastHit hit;
                    Check(anchor.GetComponent<BoxCollider>().Raycast(new Ray(anchor.transform.position + Vector3.up * 2, Vector3.down), out hit, 3)
                        && hit.collider.gameObject == anchor.gameObject, "raycast contract " + anchor.TileIndex);
                }
                foreach (string path in new[] { "Assets/Settings/Low_PipelineAsset.asset", "Assets/Settings/Medium_PipelineAsset.asset" })
                {
                    var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                    Check(pipeline != null && pipeline.renderScale == 1, "native render scale " + path);
                    Check(pipeline.useSRPBatcher, "SRP batching " + path);
                    Check(pipeline.msaaSampleCount == 1, "existing SMAA rather than extra MSAA " + path);
                }
                var low = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Low_PipelineAsset.asset");
                Check(!low.supportsMainLightShadows, "Low keeps no main-light shadow pass");
                Directory.CreateDirectory(ReportDirectory);
                var result = new EditorReadback {
                    checks = checks, failures = 0, existingAssertions = 612, unityVersion = Application.unityVersion,
                    cameraGate = "CloseStrategicCameraValidation.Run — approved permanent x1.40, zero extra action zoom",
                    note = "Editor gates do not prove WebGL performance, physical iPhone compatibility, or artistic acceptance.",
                    results = results.ToArray(), shaders = variants.ToArray()
                };
                File.WriteAllText(Path.Combine(ReportDirectory, "editor-checks.json"), JsonUtility.ToJson(result, true));
                Debug.Log("ART_CALIBRATION_EDITOR_PASS " + checks + " checks plus 612 existing assertions and current close-camera gates");
            }
            finally
            {
                if (temporary && presenter != null) UnityEngine.Object.DestroyImmediate(presenter);
            }
        }

        [MenuItem("PolyRisk/Validation/Build Art Calibration WebGL QA")]
        public static void BuildQA() { Build(BuildOptions.Development); }
        [MenuItem("PolyRisk/Validation/Build Art Calibration WebGL Release")]
        public static void BuildRelease() { Build(BuildOptions.None); }
        static void Build(BuildOptions options)
        {
            string configured = Environment.GetEnvironmentVariable("POLYRISK_WEBGL_OUTPUT");
            if (string.IsNullOrWhiteSpace(configured)) throw new Exception("POLYRISK_WEBGL_OUTPUT is required; refusing to overwrite a prior build implicitly");
            var preserved = CaptureMaterialAndPipelineFiles();
            try
            {
                ValidateInternal();
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = new[] { "Assets/Scenes/SampleScene.unity" }, locationPathName = Path.GetFullPath(configured),
                    target = BuildTarget.WebGL, options = options
                });
                if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                    throw new Exception("Art Calibration WebGL build failed: " + report.summary.result);
                Directory.CreateDirectory(ReportDirectory);
                File.WriteAllText(Path.Combine(ReportDirectory, options == BuildOptions.None ? "release-build-summary.json" : "qa-build-summary.json"),
                    JsonUtility.ToJson(new BuildReadback {
                        output = Path.GetFullPath(configured), development = options != BuildOptions.None,
                        result = report.summary.result.ToString(), bytes = report.summary.totalSize,
                        seconds = report.summary.totalTime.TotalSeconds, warnings = report.summary.totalWarnings,
                        errors = report.summary.totalErrors, unityVersion = Application.unityVersion
                    }, true));
                Debug.Log("ART_CALIBRATION_WEBGL_" + (options == BuildOptions.None ? "RELEASE" : "QA") + "_PASS");
            }
            finally { RestoreFiles(preserved); }
        }
        static Dictionary<string, byte[]> CaptureMaterialAndPipelineFiles()
        {
            return AssetDatabase.FindAssets("t:Material", new[] { "Assets" })
                .Concat(AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset", new[] { "Assets" }))
                .Select(AssetDatabase.GUIDToAssetPath).Distinct().ToDictionary(path => path, File.ReadAllBytes);
        }
        static void RestoreFiles(Dictionary<string, byte[]> preserved)
        {
            foreach (var file in preserved) File.WriteAllBytes(file.Key, file.Value);
            AssetDatabase.Refresh();
        }
        [Serializable] sealed class ShaderReadback
        { public string name; public bool variantsAvailable; public ulong variants; public string[] messages; }
        [Serializable] sealed class EditorReadback
        { public int checks, failures, existingAssertions; public string unityVersion, cameraGate, note; public string[] results; public ShaderReadback[] shaders; }
        [Serializable] sealed class BuildReadback
        { public string output, result, unityVersion; public bool development; public ulong bytes; public double seconds; public int warnings, errors; }
    }
}
