using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PolyRisk.DebugTools;

namespace PolyRisk.Editor
{
    [InitializeOnLoad]
    public static class EconomyBatchValidation
    {
        private const string Key = "PolyRisk.EconomyBatchValidation";
        static EconomyBatchValidation() { EditorApplication.update += Tick; }
        public static void BuildPlayer()
        {
            // URP writes shader-stripping cache fields into its assets during a build.
            // Preserve the working copy, including any user's uncommitted asset settings.
            var assets = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath).ToDictionary(path => path, File.ReadAllBytes);
            try
            {
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = new[] { "Assets/Scenes/SampleScene.unity" },
                    locationPathName = "/tmp/PolyRiskEconomyValidation.app",
                    target = BuildTarget.StandaloneOSX,
                    options = BuildOptions.Development
                });
                if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                    throw new Exception("Validation player build failed: " + report.summary.result);
                Debug.Log("ECONOMY PLAYER BUILD PASS");
            }
            finally
            {
                foreach (var asset in assets) File.WriteAllBytes(asset.Key, asset.Value);
                AssetDatabase.Refresh();
            }
        }

        [MenuItem("PolyRisk/Validation/Build WebGL Development")]
        public static void BuildWebGL() => BuildWebGLInternal(BuildOptions.Development);

        private static void BuildWebGLInternal(BuildOptions buildOptions)
        {
            var assets = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath).ToDictionary(path => path, File.ReadAllBytes);
            try
            {
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = new[] { "Assets/Scenes/SampleScene.unity" },
                    locationPathName = WebGLValidationOutput(),
                    target = BuildTarget.WebGL,
                    options = buildOptions
                });
                if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                    throw new Exception("Validation WebGL build failed: " + report.summary.result);
                Debug.Log("WEBGL PLAYER BUILD PASS");
            }
            finally
            {
                foreach (var asset in assets) File.WriteAllBytes(asset.Key, asset.Value);
                AssetDatabase.Refresh();
            }
        }

        [MenuItem("PolyRisk/Validation/Test and Build WebGL Development")]
        public static void BuildVerifiedWebGL()
        {
            EconomyRegression.Run();
            BuildWebGL();
        }

        public static void BuildVerifiedWebGLPreview()
        {
            RunAllEditorRegressions();
            BuildWebGLInternal(BuildOptions.None);
        }

        public static void RunAllEditorRegressions()
        {
            EconomyRegression.Run();
            MechanicsRegression.Run();
            GameEventRegression.Run();
            UIAssetRegression.Run();
        }

        private static string WebGLValidationOutput()
        {
            string configured = Environment.GetEnvironmentVariable("POLYRISK_WEBGL_OUTPUT");
            return string.IsNullOrWhiteSpace(configured)
                ? "/tmp/PolyRiskWebGLValidationFixed"
                : Path.GetFullPath(configured);
        }

        [MenuItem("PolyRisk/Validation/Build WebGL Release")]
        public static void BuildWebGLRelease()
        {
            BuildRelease(BuildTarget.WebGL, "/tmp/PolyRiskWebGLRelease", "WEBGL RELEASE BUILD PASS");
        }

        [MenuItem("PolyRisk/Validation/Build iOS Xcode Project")]
        public static void BuildIOSProject()
        {
            string configured = Environment.GetEnvironmentVariable("POLYRISK_IOS_OUTPUT");
            string outputPath = string.IsNullOrWhiteSpace(configured)
                ? "/tmp/PolyRisk-iOS"
                : Path.GetFullPath(configured);
            BuildRelease(BuildTarget.iOS, outputPath, "IOS XCODE PROJECT BUILD PASS");
        }

        private static void BuildRelease(BuildTarget target, string outputPath, string successMessage)
        {
            var assets = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath).ToDictionary(path => path, File.ReadAllBytes);
            try
            {
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = new[] { "Assets/Scenes/SampleScene.unity" },
                    locationPathName = outputPath,
                    target = target,
                    options = BuildOptions.None
                });
                if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                    throw new Exception($"Release build failed for {target}: {report.summary.result}");
                Debug.Log(successMessage);
            }
            finally
            {
                foreach (var asset in assets) File.WriteAllBytes(asset.Key, asset.Value);
                AssetDatabase.Refresh();
            }
        }

        public static void Start()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            SessionState.SetInt(Key, 1);
            SessionState.SetFloat(Key + ".start", (float)EditorApplication.timeSinceStartup);
            EditorApplication.EnterPlaymode();
        }
        private static int Arg(string key, int fallback)
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, key);
            return index >= 0 && index + 1 < args.Length && int.TryParse(args[index + 1], out int value) ? value : fallback;
        }
        private static void Tick()
        {
            int phase = SessionState.GetInt(Key, 0);
            if (phase == 0) return;
            if (EditorApplication.timeSinceStartup - SessionState.GetFloat(Key + ".start", 0) > 1800)
            {
                SessionState.SetInt(Key, 0);
                Debug.LogError("BATCH VALIDATION exceeded 30-minute wall-clock limit");
                EditorApplication.Exit(2);
                return;
            }
            if (!EditorApplication.isPlaying) return;
            // Headless Editor has no visible GameView to request continuous player updates.
            EditorApplication.QueuePlayerLoopUpdate();
            var runner = UnityEngine.Object.FindObjectOfType<StressTestRunner>();
            if (runner == null) return;
            if (phase == 1)
            {
                SessionState.SetInt(Key, 2);
                runner.StartBatch(Arg("-polyGames", 5), Arg("-polyPlayers", 4), Arg("-polySeed", 260926));
            }
            else if (!runner.BatchRunning)
            {
                SessionState.SetInt(Key, 0);
                Debug.Log($"BATCH VALIDATION completed={runner.CompletedGames} anomalies={runner.Anomalies} path={runner.LastResultsPath}");
                EditorApplication.Exit(runner.Anomalies == 0 && runner.CompletedGames == Arg("-polyGames", 5) ? 0 : 1);
            }
        }
    }
}
