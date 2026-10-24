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
    }
}
