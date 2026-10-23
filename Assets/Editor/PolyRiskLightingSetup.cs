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
//   7. Crée / met à jour une Reflection Probe Realtime au-dessus du plateau, pour
//      que les matériaux très lisses (Background_Plate, Board_Metal) reflètent
//      vraiment les néons au lieu du skybox plat.
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
            EnsureReflections();

            AssetDatabase.SaveAssets();
            EditorSceneManager_MarkActiveDirty();
            Debug.Log("[PolyRisk] Cyber-Luxury lighting appliqué. Sauvegarde la scène (Ctrl+S) pour persister.");
        }

        public static void SetupAndSaveSampleScene()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            SetupAll();
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            PolyRisk.Editor.EconomyBatchValidation.BuildVerifiedWebGLPreview();
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

            // Bloom contenu pour préserver les contours et les valeurs sur petit écran.
            var bloom = GetOrAdd<Bloom>(profile);
            bloom.threshold.overrideState = true;    bloom.threshold.value = 0.8f;
            bloom.intensity.overrideState = true;    bloom.intensity.value = 0.55f;
            bloom.scatter.overrideState = true;      bloom.scatter.value = 0.35f;
            bloom.tint.overrideState = true;         bloom.tint.value = Color.white;
            bloom.highQualityFiltering.overrideState = true;
            bloom.highQualityFiltering.value = false;

            // Tonemapping Neutral pour ne pas tuer les couleurs saturées des néons.
            var tone = GetOrAdd<Tonemapping>(profile);
            tone.mode.overrideState = true;          tone.mode.value = TonemappingMode.Neutral;

            // Contraste contenu : conserver le caractère sombre sans teinte néon dominante.
            var color = GetOrAdd<ColorAdjustments>(profile);
            color.postExposure.overrideState = true; color.postExposure.value = 0.05f;
            color.contrast.overrideState = true;     color.contrast.value = 12f;
            color.saturation.overrideState = true;   color.saturation.value = 4f;
            color.colorFilter.overrideState = true;
            ColorUtility.TryParseHtmlString("#F5F0FF", out var filter);
            color.colorFilter.value = filter;        // très léger teint violet

            // Vignette discrète pour ne pas assombrir les cases du pourtour.
            var vig = GetOrAdd<Vignette>(profile);
            vig.intensity.overrideState = true;      vig.intensity.value = 0.18f;
            vig.smoothness.overrideState = true;     vig.smoothness.value = 0.5f;
            vig.color.overrideState = true;          vig.color.value = new Color(0.02f, 0.02f, 0.05f, 1f);

            // Depth of Field (Bokeh) : plateau net, bords flous — aspect "maquette photo".
            // focusDistance calé sur EnsureCamera() (caméra isométrique au-dessus du plateau) ;
            // à ajuster dans l'inspecteur si l'angle de vue change.
            var dof = GetOrAdd<DepthOfField>(profile);
            dof.active = false;
            dof.mode.overrideState = true;             dof.mode.value = DepthOfFieldMode.Bokeh;
            dof.focusDistance.overrideState = true;    dof.focusDistance.value = 12f;
            dof.aperture.overrideState = true;         dof.aperture.value = 5.6f;
            dof.focalLength.overrideState = true;      dof.focalLength.value = 50f;
            dof.bladeCount.overrideState = true;       dof.bladeCount.value = 6;

            EditorUtility.SetDirty(profile);
            return profile;
        }

        static T GetOrAdd<T>(VolumeProfile p) where T : VolumeComponent
        {
            if (p.TryGet<T>(out var comp)) return comp;
            comp = p.Add<T>(true);
            // Sans ça, le composant existe en mémoire (visible/actif tant que l'Editor
            // ne recharge pas) mais n'est jamais écrit comme sous-asset du .asset —
            // il disparaît au prochain domain reload. C'est ce qui manquait pour DoF.
            AssetDatabase.AddObjectToAsset(comp, p);
            return comp;
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
            board.settings.emissiveIntensity = 0.8f;
            board.highlightIntensity = 3.2f;    // réservé aux cases actives/sélectionnées
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
            // Baissé de 0.9 : à cette intensité l'ambient bleu lavait tout le plateau
            // d'une teinte pâle uniforme au lieu de laisser l'anthracite du métal dominer.
            RenderSettings.ambientIntensity = 0.35f;

            // Skybox procédural sombre — remplace le Default-Skybox bleu délavé.
            var skyPath = "Assets/Settings/PolyRisk_Skybox.mat";
            var sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            bool isNewSky = sky == null;
            if (isNewSky)
            {
                var proc = Shader.Find("Skybox/Procedural");
                if (proc != null)
                {
                    sky = new Material(proc);
                    AssetDatabase.CreateAsset(sky, skyPath);
                }
            }
            if (sky != null)
            {
                sky.SetFloat("_SunSize", 0.02f);
                sky.SetFloat("_AtmosphereThickness", 0.5f);
                sky.SetColor("_SkyTint", new Color(0.04f, 0.05f, 0.10f, 1f));
                sky.SetColor("_GroundColor", new Color(0.02f, 0.02f, 0.04f, 1f));
                // Baissé de 0.4 : avec la Reflection Probe, ce skybox est maintenant
                // reflété par le métal du plateau — trop d'exposure = teinte bleue pâle.
                // Réappliqué à chaque run (plus seulement à la création) pour rester idempotent.
                sky.SetFloat("_Exposure", 0.22f);
                EditorUtility.SetDirty(sky);
                RenderSettings.skybox = sky;
            }

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
                moon.intensity = 0.6f;
                moon.transform.rotation = Quaternion.Euler(60f, -30f, 0f);
                moon.shadows = LightShadows.Soft;
            }

            // Key violet doux : dessine les volumes sans laver le plateau.
            var keyGo = GameObject.Find("Key néon violet") ?? new GameObject("Key néon violet");
            var key = keyGo.GetComponent<Light>() ?? keyGo.AddComponent<Light>();
            key.type = LightType.Point;
            ColorUtility.TryParseHtmlString("#C27AFF", out var keyColor);
            key.color = keyColor;
            key.intensity = 1.4f;
            key.range = 12f;
            key.shadows = LightShadows.None;
            keyGo.transform.position = new Vector3(0f, 5f, 0f);

            // Rim vert discret pour séparer le plateau du fond.
            var rimGo = GameObject.Find("Rim néon vert") ?? new GameObject("Rim néon vert");
            var rim = rimGo.GetComponent<Light>() ?? rimGo.AddComponent<Light>();
            rim.type = LightType.Point;
            ColorUtility.TryParseHtmlString("#05DF72", out var rimColor);
            rim.color = rimColor;
            rim.intensity = 0.85f;
            rim.range = 10f;
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
        // 6) Reflection Probe : une capture au chargement suffit à donner une réponse
        //    de matériau cohérente sans recalculer six faces de cubemap chaque frame.
        // ─────────────────────────────────────────────────────────────

        static void EnsureReflections()
        {
            var probeGo = GameObject.Find("Board_ReflectionProbe") ?? new GameObject("Board_ReflectionProbe");
            var probe = probeGo.GetComponent<ReflectionProbe>();
            if (probe == null) probe = probeGo.AddComponent<ReflectionProbe>();

            probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
            probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            probe.boxProjection = true;
            probe.size = new Vector3(24f, 8f, 24f);
            probe.center = Vector3.zero;
            probe.resolution = 256;
            probe.hdr = true;
            probe.intensity = 1f;
            probe.nearClipPlane = 0.3f;
            probe.farClipPlane = 40f;

            probeGo.transform.position = new Vector3(0f, 3f, 0f);
            EditorUtility.SetDirty(probeGo);
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
