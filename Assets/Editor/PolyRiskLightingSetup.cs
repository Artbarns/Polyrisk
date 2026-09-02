// PolyRiskLightingSetup.cs — Setup Cyber-Luxury en un clic.
//
// Menu : PolyRisk > Setup Cyber-Luxury Lighting
//
// Ce que ça fait :
//   1. Crée / met à jour Assets/Settings/PolyRisk_PostProfile.asset
//      avec Bloom + Tonemapping + ColorAdjustments + Vignette calibrés.
//   2. Crée / met à jour un GameObject "Post_Volume" dans la scène active,
//      isGlobal, avec le profil assigné.
//   3. Configure la Main Camera : Clear Flags Solid Color, background sombre,
//      Post Processing activé, MSAA 4x.
//   4. Règle l'ambient light et les paramètres d'environnement.
//   5. Baisse la Directional Light existante à une "moon light" bleue rasante.
//   6. Crée / met à jour 2 Point Lights néon (violet + vert) pour l'ambiance.
//
// Idempotent : re-exécutable sans casser ce qui existe déjà.

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;

namespace PolyRisk.EditorTools
{
    public static class PolyRiskLightingSetup
    {
        const string ProfileAssetPath = "Assets/Settings/PolyRisk_PostProfile.asset";

        [MenuItem("PolyRisk/Setup Cyber-Luxury Lighting")]
        public static void SetupAll()
        {
            var profile = EnsureProfile();
            EnsureVolumeInScene(profile);
            EnsureCamera();
            BoostBoardEmissive();   // patch : force le bloom à réagir sur tous les groupes
            EnsureEnvironment();
            EnsureLights();

            AssetDatabase.SaveAssets();
            EditorSceneManager_MarkActiveDirty();
            Debug.Log("[PolyRisk] Cyber-Luxury lighting appliqué. Sauvegarde la scène (Ctrl+S) pour persister.");
        }

        // ─────────────────────────────────────────────────────────────
        // 1) Volume Profile — Bloom / Tone / Color / Vignette
        // ─────────────────────────────────────────────────────────────

        static VolumeProfile EnsureProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfileAssetPath);
            if (profile == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ProfileAssetPath));
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfileAssetPath);
            }

            // Bloom : c'est LA transformation.
            // Threshold BAS (0.4) pour attraper aussi les couleurs sombres (bleu #282BCC, rouge #F72626)
            // que la palette PolyRisk contient. À 0.75 seul le vert déclenchait vraiment.
            var bloom = GetOrAdd<Bloom>(profile);
            bloom.threshold.overrideState = true;    bloom.threshold.value = 0.4f;
            bloom.intensity.overrideState = true;    bloom.intensity.value = 1.8f;
            bloom.scatter.overrideState = true;      bloom.scatter.value = 0.6f;
            bloom.tint.overrideState = true;         bloom.tint.value = Color.white;
            bloom.highQualityFiltering.overrideState = true;
            bloom.highQualityFiltering.value = true;

            // Tonemapping Neutral pour ne pas tuer les couleurs saturées des néons.
            var tone = GetOrAdd<Tonemapping>(profile);
            tone.mode.overrideState = true;          tone.mode.value = TonemappingMode.Neutral;

            // Color Adjustments : plus de contraste, un peu plus de saturation.
            var color = GetOrAdd<ColorAdjustments>(profile);
            color.postExposure.overrideState = true; color.postExposure.value = 0.35f;
            color.contrast.overrideState = true;     color.contrast.value = 18f;
            color.saturation.overrideState = true;   color.saturation.value = 12f;
            color.colorFilter.overrideState = true;
            ColorUtility.TryParseHtmlString("#F5F0FF", out var filter);
            color.colorFilter.value = filter;        // très léger teint violet

            // Vignette pour focaliser sur le plateau.
            var vig = GetOrAdd<Vignette>(profile);
            vig.intensity.overrideState = true;      vig.intensity.value = 0.32f;
            vig.smoothness.overrideState = true;     vig.smoothness.value = 0.5f;
            vig.color.overrideState = true;          vig.color.value = new Color(0.02f, 0.02f, 0.05f, 1f);

            EditorUtility.SetDirty(profile);
            return profile;
        }

        static T GetOrAdd<T>(VolumeProfile p) where T : VolumeComponent
        {
            if (p.TryGet<T>(out var comp)) return comp;
            return p.Add<T>(true);
        }

        // ─────────────────────────────────────────────────────────────
        // 2) Post_Volume dans la scène active
        // ─────────────────────────────────────────────────────────────

        static void EnsureVolumeInScene(VolumeProfile profile)
        {
            var existing = GameObject.Find("Post_Volume");
            var go = existing != null ? existing : new GameObject("Post_Volume");
            var vol = go.GetComponent<Volume>();
            if (vol == null) vol = go.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 10;
            vol.weight = 1f;
            vol.sharedProfile = profile;
        }

        // ─────────────────────────────────────────────────────────────
        // 3) Main Camera : Clear Flags Solid Color, PP on, ortho tilt
        // ─────────────────────────────────────────────────────────────

        static void EnsureCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[PolyRisk] Main Camera introuvable — passe cette étape.");
                return;
            }

            cam.clearFlags = CameraClearFlags.SolidColor;
            ColorUtility.TryParseHtmlString("#050710", out var bg);
            cam.backgroundColor = bg;

            // HDR sur la caméra elle-même (le URP Asset l'active globalement,
            // mais chaque caméra a son propre allowHDR — obligatoire pour que Bloom voit
            // les couleurs émissives > 1.0 des rainures néon).
            cam.allowHDR = true;
            cam.allowMSAA = true;

            // Post-processing sur URP (via UniversalAdditionalCameraData)
            var addCam = cam.GetComponent<UniversalAdditionalCameraData>();
            if (addCam == null) addCam = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            addCam.renderPostProcessing = true;
            addCam.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            addCam.antialiasingQuality = AntialiasingQuality.High;
        }

        // ─────────────────────────────────────────────────────────────
        // 4bis) Booster l'émissive du plateau procédural pour que le Bloom réagisse
        // ─────────────────────────────────────────────────────────────

        static void BoostBoardEmissive()
        {
            var board = Object.FindObjectOfType<PolyRisk.PolyRiskBoard>();
            if (board == null) return;

            // 2.0 dépasse largement le Bloom threshold 0.4, quelle que soit la couleur du groupe.
            // Le shader NeonState multiplie state.rgb * state.a — donc avec un vert #1BDE1F
            // (luminance ~0.5) × 2.0 = ~1.0, ce qui produit un halo visible.
            board.settings.emissiveIntensity = 2.0f;
            board.highlightIntensity = 5.0f;    // même logique côté highlight (F1/F2/pulse)
            board.ResetAllTiles();
            EditorUtility.SetDirty(board);
        }

        // ─────────────────────────────────────────────────────────────
        // 4) Environment : ambient + skybox procédural sombre
        // ─────────────────────────────────────────────────────────────

        static void EnsureEnvironment()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            ColorUtility.TryParseHtmlString("#0A0D18", out var ambient);
            RenderSettings.ambientLight = ambient;
            RenderSettings.ambientIntensity = 0.9f;

            // Skybox procédural sombre — remplace le Default-Skybox bleu délavé.
            var skyPath = "Assets/Settings/PolyRisk_Skybox.mat";
            var sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if (sky == null)
            {
                var proc = Shader.Find("Skybox/Procedural");
                if (proc != null)
                {
                    sky = new Material(proc);
                    sky.SetFloat("_SunSize", 0.02f);
                    sky.SetFloat("_AtmosphereThickness", 0.5f);
                    sky.SetColor("_SkyTint", new Color(0.04f, 0.05f, 0.10f, 1f));
                    sky.SetColor("_GroundColor", new Color(0.02f, 0.02f, 0.04f, 1f));
                    sky.SetFloat("_Exposure", 0.4f);
                    AssetDatabase.CreateAsset(sky, skyPath);
                }
            }
            if (sky != null) RenderSettings.skybox = sky;

            // Fog subtil pour adoucir les fonds
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.02f;
            ColorUtility.TryParseHtmlString("#080A14", out var fogColor);
            RenderSettings.fogColor = fogColor;
        }

        // ─────────────────────────────────────────────────────────────
        // 5) Lights : Moon Light + 2 Point Lights néon
        // ─────────────────────────────────────────────────────────────

        static void EnsureLights()
        {
            // Directional Light existante → devient Moon Light bleue rasante.
            var moon = FindDirectionalLight();
            if (moon != null)
            {
                ColorUtility.TryParseHtmlString("#B7C4FF", out var moonColor);
                moon.color = moonColor;
                moon.intensity = 0.4f;
                moon.transform.rotation = Quaternion.Euler(60f, -30f, 0f);
                moon.shadows = LightShadows.Soft;
            }

            // Point Light "Key néon violet" au-dessus du plateau
            var keyGo = GameObject.Find("Key néon violet") ?? new GameObject("Key néon violet");
            var key = keyGo.GetComponent<Light>() ?? keyGo.AddComponent<Light>();
            key.type = LightType.Point;
            ColorUtility.TryParseHtmlString("#C27AFF", out var keyColor);
            key.color = keyColor;
            key.intensity = 5f;
            key.range = 15f;
            key.shadows = LightShadows.None;
            keyGo.transform.position = new Vector3(0f, 5f, 0f);

            // Point Light "Rim néon vert" en coin
            var rimGo = GameObject.Find("Rim néon vert") ?? new GameObject("Rim néon vert");
            var rim = rimGo.GetComponent<Light>() ?? rimGo.AddComponent<Light>();
            rim.type = LightType.Point;
            ColorUtility.TryParseHtmlString("#05DF72", out var rimColor);
            rim.color = rimColor;
            rim.intensity = 3.5f;
            rim.range = 12f;
            rim.shadows = LightShadows.None;
            rimGo.transform.position = new Vector3(-6f, 3f, -6f);
        }

        static Light FindDirectionalLight()
        {
            foreach (var l in Object.FindObjectsOfType<Light>())
                if (l.type == LightType.Directional) return l;
            return null;
        }

        // ─────────────────────────────────────────────────────────────

        static void EditorSceneManager_MarkActiveDirty()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
#endif

