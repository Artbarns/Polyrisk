using UnityEngine;

namespace PolyRisk.Core
{
    /// <summary>
    /// Represents the immutable data for a property in the game.
    /// Updated to support 'double' for currency and integration with FamilyData and TileType.
    /// </summary>
    [CreateAssetMenu(fileName = "New Property", menuName = "PolyRisk/PropertyData")]
    public class PropertyData : BoardTileData
    {
        [Header("Property Information")]

        [Tooltip("The base cost to purchase this property.")]
        [SerializeField] private double basePrice;

        [Tooltip("The base rent charged when another player lands on this property.")]
        [SerializeField] private double baseRent;

        [Tooltip("The color group this property belongs to.")]
        [SerializeField] private ColorGroup colorGroup;

        [Header("Family & Progression")]
        [Tooltip("The family this property belongs to, influencing rent growth.")]
        [SerializeField] private FamilyData familyData;

        [Tooltip("The current level of the property (0-5).")]
        [SerializeField] [Range(0, 5)] private int currentLevel = 0;

        // Hijacking mechanics
        public PlayerModel HijackedBy { get; set; }
        public int HijackTurnsRemaining { get; set; } = 0;

        // Embargo mechanics
        public bool IsEmbargoed { get; set; } = false;
        public int EmbargoTurnsRemaining { get; set; } = 0;

        // Burn Rate tracking : true si un adversaire a atterri depuis le dernier GO du propriétaire
        public bool WasVisitedSinceLastGO { get; set; } = false;

        // Phase E.4 — Faillite : hypothèque (50% du prix d'achat, plus de loyer ni Burn Rate)
        public bool IsMortgaged { get; set; } = false;

        // Fix #6 Phase F — Anti-exploit hypothèque : exonération uniquement au GO SUIVANT
        // l'hypothèque. Le flag est posé par LiquidationHelper.MortgageProperty et consommé
        // par FinanceSystem.ProcessGoIncome.
        public bool MortgageExemptFromNextGo { get; set; } = false;

        // Phase E.4 — Cumul des sommes investies en upgrades (utilisé pour la valeur de revente 50%)
        public double TotalUpgradeCostInvested { get; set; } = 0;

        // Public read-only accessors
        public string PropertyName => TileName;
        public double BasePrice => basePrice;
        public double BaseRent => baseRent;
        public ColorGroup ColorGroup => colorGroup;
        public FamilyData FamilyData => familyData;
        public int CurrentLevel => currentLevel;

        /// <summary>
        /// Calculates the current rent based on the property's level and the family's growth curve.
        /// </summary>
        /// <returns>The calculated rent amount.</returns>
        public double GetCurrentRent()
        {
            if (familyData == null || familyData.RentGrowthCurve == null)
            {
                // Fallback if no family data is assigned
                return baseRent * (1 + currentLevel * 0.5f);
            }

            // Evaluate the curve at the normalized level (0 to 1 based on max level 5)
            float t = (float)currentLevel / 5f;
            float multiplier = familyData.RentGrowthCurve.Evaluate(t);
            
            // Rent calculation: Base Rent * Multiplier form curve
            // We ensure multiplier is at least 1.0
            return baseRent * Mathf.Max(1.0f, multiplier);
        }

        /// <summary>
        /// Sets the current level of the property.
        /// </summary>
        /// <param name="level">New level (0-5).</param>
        public void SetLevel(int level)
        {
            currentLevel = Mathf.Clamp(level, 0, 5);
        }
    }
}
