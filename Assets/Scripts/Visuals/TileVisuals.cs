using System.Collections;
using UnityEngine;
using PolyRisk.Core;
using PolyRisk.UI;

namespace PolyRisk.Visuals
{
    /// <summary>
    /// Handles the visual representation of a board tile.
    /// Sets material colors and emission based on tile type and family data.
    ///
    /// Board_Procedural : si un PolyRisk.PolyRiskBoard est présent dans la scène,
    /// Highlight / ResetHighlight / BeginPulse / EndPulse forwardent vers son
    /// shader néon (SetTile / ResetTile / SetTileIntensity). Sinon fallback
    /// vers l'ancien pipeline material.color (Board_Visual).
    /// </summary>
    public class TileVisuals : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The MeshRenderer of the tile to color.")]
        [SerializeField] private MeshRenderer tileRenderer;

        // ── Board_Procedural forwarding (Phase 3D board) ────────────────────
        // Cache d'instance — résolu dans Awake, re-résolu en fallback si détruit
        // (évite le piège du cache static qui survit au reload de scène).
        private PolyRisk.PolyRiskBoard cachedBoard;
        private PolyRisk.Visuals.TileIdentifier cachedIdentifier;

        private PolyRisk.PolyRiskBoard Board
        {
            get
            {
                if (cachedBoard == null) cachedBoard = FindObjectOfType<PolyRisk.PolyRiskBoard>();
                return cachedBoard;
            }
        }

        private int TileIndex => cachedIdentifier != null ? cachedIdentifier.TileIndex : -1;
        private bool CanForwardToBoard => Board != null && cachedIdentifier != null;

        [Header("Color Settings")]
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private Color goColor = Color.green;
        [SerializeField] private Color prisonColor = Color.gray;
        [SerializeField] private Color auctionColor = Color.yellow;
        [SerializeField] private Color safeZoneColor = Color.blue;

        // Family colors could be mapped here or fetched from FamilyData directly if it had color.
        // Since FamilyData only has PowerRank (int), we map rank to color.
        [SerializeField] private Color[] familyColors;

        private void Awake()
        {
            if (tileRenderer == null)
            {
                tileRenderer = GetComponent<MeshRenderer>();
            }
            // Résolution du TileIdentifier (peut être sur ce GO ou un parent selon le générateur)
            cachedIdentifier = GetComponent<PolyRisk.Visuals.TileIdentifier>();
            if (cachedIdentifier == null)
                cachedIdentifier = GetComponentInParent<PolyRisk.Visuals.TileIdentifier>();
            // Cache board (peut être null en Edit Mode avant Generate)
            cachedBoard = FindObjectOfType<PolyRisk.PolyRiskBoard>();
        }

        private Color originalColor = Color.white;
        private Color originalEmissionColor = Color.black;
        private Coroutine pulseCoroutine;

        /// <summary>
        /// Sets up the visuals for the tile based on its data.
        /// </summary>
        /// <param name="tileData">The data representing this tile.</param>
        public void SetupVisuals(BoardTile tileData)
        {
            if (tileRenderer == null || tileData == null) return;

            Color targetColor = defaultColor;

            // Determine color based on TileType
            switch (tileData.Type)
            {
                case TileType.Go_Start:
                    targetColor = goColor;
                    break;
                case TileType.Prison:
                    targetColor = prisonColor;
                    break;
                case TileType.GlobalAuction:
                    targetColor = auctionColor;
                    break;
                case TileType.SafeZone:
                    targetColor = safeZoneColor;
                    break;
                case TileType.Property:
                    if (tileData.Property != null && tileData.Property.FamilyData != null)
                    {
                        // Guard: familyColors must be populated in Inspector
                        if (familyColors == null || familyColors.Length == 0)
                        {
                            targetColor = defaultColor;
                        }
                        else
                        {
                            // Map Family PowerRank (1-8) to color index (0-7)
                            // PowerRank of 0 or negative is treated as index 0 (first color)
                            int rank = tileData.Property.FamilyData.PowerRank;
                            int rankIndex = Mathf.Clamp(rank - 1, 0, familyColors.Length - 1);
                            targetColor = familyColors[rankIndex];
                        }
                    }
                    else if (tileData.Property != null)
                    {
                        // Fallback: property exists but has no FamilyData
                        targetColor = defaultColor;
                    }
                    break;
            }

            // Apply Color
            // Using MaterialPropertyBlock is better for performance if many tiles, 
            // but for simplicity and "material.color" requirement, direct assignment:
            originalColor = targetColor;
            tileRenderer.material.color = targetColor;
            
            // Enable Emission if using Standard shader or similar
            tileRenderer.material.EnableKeyword("_EMISSION");
            originalEmissionColor = targetColor * 0.5f;
            tileRenderer.material.SetColor("_EmissionColor", originalEmissionColor);
        }

        /// <summary>
        /// Visually highlights the tile during selection modes.
        /// </summary>
        public void Highlight(Color highlightColor)
        {
            // Board_Procedural : forward vers le shader néon
            if (CanForwardToBoard)
            {
                Board.SetTile(TileIndex, highlightColor, Board.highlightIntensity);
                return;
            }
            // Fallback legacy Board_Visual
            if (tileRenderer != null && tileRenderer.material != null)
            {
                ApplyBaseColor(highlightColor);
                ApplyEmissionColor(highlightColor * 1.25f);
            }
        }

        public void Highlight()
        {
            if (ColorUtility.TryParseHtmlString("#05DF72", out Color neonGreen))
            {
                Highlight(neonGreen);
            }
        }

        public void ResetHighlight()
        {
            // Board_Procedural : restaure couleur du groupe + intensité de repos
            if (CanForwardToBoard)
            {
                Board.ResetTile(TileIndex);
                return;
            }
            // Fallback legacy
            if (tileRenderer != null && tileRenderer.material != null)
            {
                ApplyBaseColor(originalColor);
                ApplyEmissionColor(originalEmissionColor);
            }
        }

        public void BeginPulse(Color targetColor, float speed = 4f)
        {
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;
            }

            // Keep the selection clear without a repeating brightness animation when
            // the accessibility preference asks for reduced motion.
            if (UIUXRuntime.ReducedMotion)
            {
                Highlight(targetColor);
                return;
            }

            // Board_Procedural : pulse via intensité shader
            if (CanForwardToBoard)
            {
                // Fixer la couleur cible et laisser la coroutine faire respirer l'intensité
                Board.SetTile(TileIndex, targetColor, Board.settings.emissiveIntensity);
                pulseCoroutine = StartCoroutine(PulseRoutineNeon(speed));
                return;
            }
            // Fallback legacy
            if (tileRenderer == null || tileRenderer.material == null) return;
            pulseCoroutine = StartCoroutine(PulseRoutine(targetColor, speed));
        }

        public void EndPulse(bool resetToOriginal = true)
        {
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;
            }

            if (resetToOriginal)
            {
                ResetHighlight();
            }
        }

        /// <summary>Pulse pour Board_Procedural : interpole entre repos et highlight.</summary>
        private IEnumerator PulseRoutineNeon(float speed)
        {
            while (true)
            {
                if (Board == null || cachedIdentifier == null) yield break;
                float t = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;
                float restIntensity = Board.settings.emissiveIntensity;
                float peakIntensity = Board.highlightIntensity;
                float intensity = Mathf.Lerp(restIntensity, peakIntensity, t);
                Board.SetTileIntensity(TileIndex, intensity);
                yield return null;
            }
        }

        private IEnumerator PulseRoutine(Color targetColor, float speed)
        {
            while (true)
            {
                float t = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;
                Color c = Color.Lerp(originalColor, targetColor, t);
                ApplyBaseColor(c);
                ApplyEmissionColor(c * 1.5f);
                yield return null;
            }
        }

        private void ApplyBaseColor(Color color)
        {
            var mat = tileRenderer.material;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            mat.color = color;
        }

        private void ApplyEmissionColor(Color color)
        {
            var mat = tileRenderer.material;
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color);
            }
        }
    }
}
