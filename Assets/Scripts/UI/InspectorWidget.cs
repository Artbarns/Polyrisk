using UnityEngine;
using TMPro;
using PolyRisk.Core;

namespace PolyRisk.UI
{
    /// <summary>
    /// UI Widget that displays details about the currently selected tile.
    /// Updates dynamically when a tile is selected via GameEvents.
    /// </summary>
    public class InspectorWidget : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Text element for the property/tile name.")]
        [SerializeField] private TextMeshProUGUI tileNameText;
        
        [Tooltip("Text element for the rent or description.")]
        [SerializeField] private TextMeshProUGUI rentText;
        
        [Tooltip("Text element for the property level.")]
        [SerializeField] private TextMeshProUGUI levelText;

        private void OnEnable()
        {
            GameEvents.OnTileSelected += UpdateInspector;
        }

        private void OnDisable()
        {
            GameEvents.OnTileSelected -= UpdateInspector;
        }

        /// <summary>
        /// Updates the UI with details from the selected tile.
        /// </summary>
        /// <param name="tile">The selected board tile.</param>
        private void UpdateInspector(BoardTile tile)
        {
            if (tile == null) return;

            // 1. Update Name
            if (tile.Type == TileType.Property && tile.Property != null)
            {
                if (tileNameText != null) tileNameText.text = tile.Property.PropertyName;
                
                // 2. Update Rent using CurrencyFormatter
                if (rentText != null)
                {
                    double currentRent = tile.Property.GetCurrentRent();
                    rentText.text = $"Rent: {CurrencyFormatter.Format(currentRent)}";
                }
                
                // 3. Update Level
                if (levelText != null)
                {
                    levelText.text = $"Level: {tile.Property.CurrentLevel}";
                    levelText.gameObject.SetActive(true);
                }
            }
            else
            {
                // Non-property tiles (Go, Prison, etc.)
                if (tileNameText != null) tileNameText.text = tile.Type.ToString();
                if (rentText != null) rentText.text = "Special Tile";
                if (levelText != null) levelText.gameObject.SetActive(false);
            }
        }
    }
}
