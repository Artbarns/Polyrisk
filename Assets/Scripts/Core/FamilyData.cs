using UnityEngine;

namespace PolyRisk.Core
{
    /// <summary>
    /// Defines a Family or Faction within the game.
    /// Families provide specific bonuses, scaling curves, and ultimate abilities.
    /// </summary>
    [CreateAssetMenu(fileName = "New Family", menuName = "PolyRisk/FamilyData")]
    public class FamilyData : ScriptableObject
    {
        [Header("Family Identity")]
        [Tooltip("The name of the family (e.g., 'The Syndicates', 'Tech Moguls').")]
        [SerializeField] private string familyName;

        [Tooltip("The power rank of the family (1 to 8). Higher rank might indicate stronger late-game scaling.")]
        [Range(1, 8)]
        [SerializeField] private int powerRank;

        [Tooltip("The name of the family's ultimate ability.")]
        [SerializeField] private string ultimateAbilityName;

        [Header("Economic Scaling")]
        [Tooltip("Curve determining how rent increases based on property level or other factors.")]
        [SerializeField] private AnimationCurve rentGrowthCurve;

        public string FamilyName => familyName;
        public int PowerRank => powerRank;
        public string UltimateAbilityName => ultimateAbilityName;
        public AnimationCurve RentGrowthCurve => rentGrowthCurve;
    }
}
