using UnityEngine;

namespace PolyRisk
{
    /// <summary>Static surface art direction only. Contains no gameplay state.</summary>
    [CreateAssetMenu(menuName = "PolyRisk/Board Visual Theme")]
    public sealed class BoardVisualTheme : ScriptableObject
    {
        [Header("Graphite palette (sRGB)")]
        public Color board = new Color32(23, 29, 37, 255);
        public Color tile = new Color32(37, 45, 55, 255);
        public Color outline = new Color32(58, 69, 81, 255);
        public Color center = new Color32(20, 26, 34, 255);
        public Color engraving = new Color32(15, 20, 27, 255);
        public Color background = new Color32(12, 16, 22, 255);

        [Header("Geometry, relative to the short tile dimension")]
        [Range(0.03f, 0.06f)] public float thicknessRatio = 0.045f;
        [Range(0.01f, 0.06f)] public float cornerRadiusRatio = 0.025f;
        [Range(0.01f, 0.02f)] public float outlineWidthRatio = 0.014f;
        [Range(0.01f, 0.03f)] public float categoryBandRatio = 0.018f;
        [Range(1, 6)] public int cornerSegments = 3;
        [Range(0.02f, 0.2f)] public float frameCornerRadius = 0.08f;
        [Tooltip("Keeps the existing tile surface and all collider/pawn anchors unchanged.")]
        public float tileSurfaceY = 0.096f;

        [Header("Satin finish")]
        [Range(0f, 1f)] public float frameMetallic = 0.25f;
        [Range(0f, 1f)] public float frameSmoothness = 0.32f;
        [Range(0f, 1f)] public float tileMetallic = 0.12f;
        [Range(0f, 1f)] public float tileSmoothness = 0.23f;

        [Header("Localized light")]
        [Range(0f, 2f)] public float restingEmission = 0.65f;
        [Range(0f, 1f)] public float trimEmission = 0.12f;
        [Range(0f, 1f)] public float categorySaturation = 0.5f;
        [Range(0f, 0.25f)] public float simulatedReflection = 0.035f;
        [Range(0.02f, 0.5f)] public float reflectionFalloff = 0.12f;

        [Header("Surface refinement (no geometry changes)")]
        [Range(0f, 0.4f)] public float frameSheen = 0.18f;
        [Range(0f, 0.4f)] public float tileSheen = 0.12f;
        [Range(0f, 0.25f)] public float edgeNormalStrength = 0.13f;
        [Range(0f, 1.5f)] public float edgeShadingPixels = 0.7f;
        [Tooltip("Coverage assistance inside the tile footprint; does not widen the mesh or override occlusion.")]
        [Range(0f, 2f)] public float minimumBandPixels = 0.95f;
        [Range(0f, 2f)] public float neonBoost = 1.15f;
        [Tooltip("Maximum world-space reach of the simulated colored reflection.")]
        [Range(0.02f, 0.2f)] public float reflectionReach = 0.10f;
        public enum SurfaceTreatment { GraphiteSatin, GraphiteReflectiveNeon }

        [System.Serializable]
        public sealed class SurfaceFinish
        {
            public Color boardTint = new Color(.80f,.83f,.87f);
            public Color tileTint = new Color(.88f,.91f,.95f);
            [Range(0,1)] public float boardMetallic=.36f, tileMetallic=.27f;
            [Range(0,1)] public float boardSmoothness=.47f, tileSmoothness=.41f;
            [Range(0,1)] public float studioReflection=.46f, edgeSpecular=.035f;
            [Range(0,2)] public float neonEmission=.82f, neonReflection=.06f;
            public float reflectionFalloff=.025f, reflectionReach=.09f;
            public float contactReflection=.035f, arrivalReflection=.075f, contactOutline=.90f;
            public float contactReflectionFalloff=.025f, contactReflectionReach=.09f, activeSurfaceContrast=0;
            public float backgroundSeparation=.012f;
            public Color pawnBody = new Color(.23f,.28f,.33f);
            public float pawnMetallic=.42f, pawnSmoothness=.40f;
            public float pawnEmission=.88f, pawnRing=.9f, pawnTop=.9f, pawnBaseReflection=.008f;

            public static SurfaceFinish ReflectiveNeon() => new SurfaceFinish
            {
                boardMetallic=.36f,tileMetallic=.27f,boardSmoothness=.47f,tileSmoothness=.41f,
                studioReflection=.46f,edgeSpecular=.040f,neonEmission=.88f,neonReflection=.08f,
                reflectionFalloff=.030f,reflectionReach=.10f,contactReflection=.78f,arrivalReflection=1.10f,
                contactReflectionFalloff=.065f,contactReflectionReach=.22f,activeSurfaceContrast=.14f,
                contactOutline=1.12f,backgroundSeparation=.014f,pawnMetallic=.48f,pawnSmoothness=.46f,
                pawnEmission=1.10f,pawnRing=1.12f,pawnTop=1.02f,pawnBaseReflection=.065f
            };
        }
        [Header("PASS 2F — shared finish system, no geometry or gameplay")]
        public SurfaceTreatment surfaceTreatment=SurfaceTreatment.GraphiteReflectiveNeon;
        public SurfaceFinish graphiteSatin=new SurfaceFinish();
        public SurfaceFinish reflectiveNeon=SurfaceFinish.ReflectiveNeon();
        [Header("PASS 2F.1 — continuous contact envelope")]
        public PolyRisk.Visuals.BoardContactFeedback.Tuning contactLight=new PolyRisk.Visuals.BoardContactFeedback.Tuning();
        [Header("Shared architectural lighting — same exposure for A / B")]
        public Color fillColor=new Color(.32f,.35f,.39f);
        public Color keyColor=new Color(.94f,.96f,.98f);
        [Range(0,2)] public float keyIntensity=.55f;
        public SurfaceFinish Finish(SurfaceTreatment treatment) =>
            treatment==SurfaceTreatment.GraphiteSatin?graphiteSatin:reflectiveNeon;
    }
}
