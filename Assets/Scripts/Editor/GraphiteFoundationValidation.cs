using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using PolyRisk.Visuals;

namespace PolyRisk.Editor
{
    /// <summary>Explicit, repeatable PASS 2A setup. Never runs automatically.</summary>
    public static class GraphiteFoundationValidation
    {
        const string Root = "Assets/Settings/GraphiteFoundation";
        const string Report = "Docs/Validation/GraphiteFoundation_2026-10-18";

        [MenuItem("PolyRisk/Graphite Foundation/Apply and verify static surfaces")]
        public static void Apply()
        {
            Directory.CreateDirectory(Root);
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var board = UnityEngine.Object.FindObjectOfType<PolyRiskBoard>();
            if (board == null) throw new Exception("Procedural board missing");
            var anchors = board.GetComponentsInChildren<TileIdentifier>();
            Require(anchors.Length == 40, "40 original anchors");
            string[] signatures = anchors.OrderBy(a => a.TileIndex).Select(AnchorSignature).ToArray();
            string cameraTransform = EditorJsonUtility.ToJson(Camera.main.transform);
            float cameraSize = Camera.main.orthographicSize;
            string beforeMeshes = MeshReport(board);

            var theme = AssetDatabase.LoadAssetAtPath<BoardVisualTheme>(Root + "/GraphiteTheme.asset");
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<BoardVisualTheme>();
                AssetDatabase.CreateAsset(theme, Root + "/GraphiteTheme.asset");
            }
            board.visualTheme = theme;
            Shader shader = Shader.Find("PolyRisk/GraphiteSurface");
            Require(shader != null, "Graphite shader imported");
            board.metalMaterial = Material("Frame", board.metalMaterial, shader);
            board.tileMaterial = Material("Tiles", board.tileMaterial, shader);
            board.darkMaterial = Material("Engraving", board.darkMaterial, shader);
            board.neonMaterial = Material("Accents", board.neonMaterial, board.neonMaterial.shader);
            board.RebuildSurfacesPreservingAnchors();

            // Environment only; camera transform, projection, UI and input remain intact.
            Camera.main.backgroundColor = theme.background;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color32(145, 151, 159, 255);
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.skybox = null;
            foreach (var light in UnityEngine.Object.FindObjectsOfType<Light>())
            {
                if (light.type == LightType.Directional)
                {
                    light.color = new Color32(237, 240, 244, 255);
                    light.intensity = 1.05f;
                }
                else light.enabled = false;
            }
            foreach (var probe in UnityEngine.Object.FindObjectsOfType<ReflectionProbe>())
                probe.enabled = false;
            var background = GameObject.Find("Background_Plate")?.GetComponent<Renderer>();
            if (background != null)
            {
                var material = Material("Background", background.sharedMaterial, Shader.Find("Universal Render Pipeline/Unlit"));
                material.SetColor("_BaseColor", theme.background);
                background.sharedMaterial = material;
                background.shadowCastingMode = ShadowCastingMode.Off;
            }
            ConfigurePost();

            Physics.SyncTransforms();
            var after = board.GetComponentsInChildren<TileIdentifier>().OrderBy(a => a.TileIndex).ToArray();
            Require(after.Select(a => a.TileIndex).SequenceEqual(Enumerable.Range(0, 40)), "Unique indices 0..39");
            Require(signatures.SequenceEqual(after.Select(AnchorSignature)), "Original anchor IDs, transforms and collider contracts unchanged");
            Require(board.GetComponentsInChildren<MeshRenderer>().Length == 4, "Four fused render meshes");
            Require(cameraTransform == EditorJsonUtility.ToJson(Camera.main.transform) && cameraSize == Camera.main.orthographicSize,
                "Camera transform and framing unchanged");
            foreach (var anchor in after)
            {
                RaycastHit hit;
                Require(Physics.Raycast(anchor.transform.position + Vector3.up * 2f, Vector3.down, out hit, 3f)
                    && hit.collider.gameObject == anchor.gameObject, "Raycast tile " + anchor.TileIndex);
            }
            foreach (var slot in board.Slots.Where(s => !s.isCorner))
                Require(Mathf.Abs(slot.surfaceY - theme.tileSurfaceY) < 0.00001f, "Existing surface height " + slot.index);
            var shaderMessages = ShaderUtil.GetShaderMessages(shader);
            foreach (var message in shaderMessages)
                Debug.Log("GRAPHITE SHADER: " + message.severity + " " + message.file + ":" + message.line + " " + message.message);
            Require(shaderMessages.All(m => m.severity.ToString() != "Error"), "No shader compiler error message");
            Debug.Log("GRAPHITE SHADER CHECK: device=" + SystemInfo.graphicsDeviceType +
                "; hasError=" + ShaderUtil.ShaderHasError(shader) + "; messages=" + shaderMessages.Length +
                "; URP/Lit hasError=" + ShaderUtil.ShaderHasError(Shader.Find("Universal Render Pipeline/Lit")));
            // The Null graphics device cannot establish runtime shader support.
            // Actual target compilation and a rendered browser capture are required below.
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                Require(!ShaderUtil.ShaderHasError(shader), "Graphite shader support on active graphics device");
            EditorUtility.SetDirty(board);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory(Report);
            File.WriteAllText(Report + "/surface-validation.txt", "PASS: 40 anchors; indices 0..39; original anchor/collider signatures; 40 raycasts; 36 surface heights; four meshes; unchanged framing.\n\nBEFORE\n" + beforeMeshes + "\nAFTER\n" + MeshReport(board));
            File.WriteAllLines(Report + "/anchor-signatures.txt", signatures);
            Debug.Log("GRAPHITE FOUNDATION SURFACE VALIDATION PASS");
        }

        public static void ApplyAndBuild()
        {
            Apply();
            VerifyAndBuild();
        }

        public static void VerifyAndBuild()
        {
            EconomyBatchValidation.RunAllEditorRegressions();
            EconomyBatchValidation.BuildWebGL();
        }

        static void ConfigurePost()
        {
            var volume = UnityEngine.Object.FindObjectOfType<Volume>();
            if (volume == null || volume.sharedProfile == null) return;
            string path = Root + "/GraphitePost.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(volume.sharedProfile), path);
                profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            }
            volume.sharedProfile = profile;
            Bloom bloom;
            if (profile.TryGet(out bloom))
            {
                bloom.intensity.Override(0.08f);
                bloom.threshold.Override(1.15f);
                bloom.scatter.Override(0.2f);
                EditorUtility.SetDirty(bloom);
            }
            ColorAdjustments color;
            if (profile.TryGet(out color))
            {
                color.postExposure.Override(0.15f);
                color.contrast.Override(6f);
                color.saturation.Override(-8f);
                color.colorFilter.Override(Color.white);
                EditorUtility.SetDirty(color);
            }
            Vignette vignette;
            if (profile.TryGet(out vignette))
            {
                vignette.intensity.Override(0.1f);
                vignette.color.Override(new Color32(8, 11, 15, 255));
                EditorUtility.SetDirty(vignette);
            }
            EditorUtility.SetDirty(profile);
        }

        static Material Material(string name, Material source, Shader shader)
        {
            string path = Root + "/Graphite" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(source) { name = "Graphite" + name, shader = shader };
                material.shaderKeywords = Array.Empty<string>();
                AssetDatabase.CreateAsset(material, path);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        static string AnchorSignature(TileIdentifier anchor)
        {
            return anchor.GetInstanceID() + ":" + anchor.TileIndex + ":" +
                EditorJsonUtility.ToJson(anchor.transform) + ":" + EditorJsonUtility.ToJson(anchor.GetComponent<BoxCollider>());
        }

        static string MeshReport(PolyRiskBoard board) => string.Join("\n", board.GetComponentsInChildren<MeshFilter>().Select(f =>
            f.name + ": " + f.sharedMesh.vertexCount + " vertices, " + f.sharedMesh.triangles.Length / 3 + " triangles"));

        static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("Graphite validation failed: " + message);
        }
    }
}
