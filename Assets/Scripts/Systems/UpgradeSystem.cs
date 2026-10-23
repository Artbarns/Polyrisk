using System.Linq;
using UnityEngine;
using PolyRisk.Core;

namespace PolyRisk.Systems
{
    public class UpgradeSystem : MonoBehaviour
    {
        [Header("System References")]
        public TransactionManager transactionManager;
        public BoardManager boardManager;

        // Coûts fixes par niveau cible — GDD_Board v7
        // Index 0 = Lvl0→1 ($500), Index 4 = Lvl4→5 ($100,000)
        private static readonly double[] UpgradeCosts = { 500, 2000, 7500, 25000, 100000 };

        private void Awake()
        {
            if (transactionManager == null || boardManager == null)
                Debug.LogError("UpgradeSystem: references missing.");
        }

        public bool TryUpgradeProperty(PlayerModel player, PropertyData property)
        {
            if (player == null || property == null) return false;

            if (!player.OwnedProperties.Contains(property))
            {
                Debug.LogWarning($"Upgrade failed: Player {player.PlayerId} doesn't own {property.PropertyName}.");
                return false;
            }

            if (!IsFamilyComplete(player, property))
            {
                Debug.LogWarning($"Upgrade failed: incomplete family for {property.PropertyName}.");
                return false;
            }

            if (property.CurrentLevel >= 5)
            {
                Debug.LogWarning($"Upgrade failed: {property.PropertyName} already at max level.");
                return false;
            }

            double cost = CalculateUpgradeCost(property, player);
            int oldLevel = property.CurrentLevel;
            double cashBefore = player.CurrentBalance;
            bool success = transactionManager.DeductMoney(player, cost);

            if (success)
            {
                property.SetLevel(property.CurrentLevel + 1);
                property.TotalUpgradeCostInvested += cost; // Phase E.4 — tracker pour la revente faillite
                GameEvents.PublishGameEvent(GameEventPhase.Resolution,
                    GameEventKind.UpgradeApplied, GameEventPriority.Major,
                    player.PlayerId, tileId: property.TileIndex, amount: -cost,
                    reason: GameEventReason.Upgrade, valueA: oldLevel, valueB: property.CurrentLevel);
                GameEvents.PublishActivity("upgrade", player.PlayerId,
                    $"améliore {property.PropertyName} · niveau {property.CurrentLevel}",
                    property.CurrentLevel >= 4);
                Debug.Log($"[Upgrade] {property.PropertyName} → Lvl{property.CurrentLevel} | Cost: ${cost:N0}");
                // [UPGRADE] diagnostic temporaire — investigation rythme d'upgrade IA (tasks/todo.md).
                // LogWarning (pas Log) pour survivre au filtre Warning du stress test.
                Debug.LogWarning($"[UPGRADE] Player {player.PlayerId} ({player.Profile}) | {property.PropertyName} lvl{oldLevel}→lvl{oldLevel + 1} | cost={cost:N0} | cash_before={cashBefore:N0} | cash_after={player.CurrentBalance:N0} | turn={player.LapsCompleted}");
                return true;
            }

            Debug.LogWarning($"Upgrade failed: insufficient funds (need ${cost:N0}).");
            return false;
        }

        /// <summary>
        /// Coût GDD_Board v7 avec passifs familiaux (Real Estate -15%, Tech -10%).
        /// </summary>
        public double CalculateUpgradeCost(PropertyData property, PlayerModel player = null)
        {
            int targetLevel = property.CurrentLevel + 1;
            if (targetLevel < 1 || targetLevel > 5) return double.MaxValue;

            double cost = UpgradeCosts[targetLevel - 1];

            if (player != null)
            {
                // Passif Real Estate — Optimisation de Chantier : -15% sur TOUS upgrades si monopole
                if (HasFullFamilyByName(player, "Real Estate"))
                    cost *= 0.85;

                // Passif Tech — Algorithme Scalable : -10% uniquement sur upgrades Tech
                if (property.FamilyData != null
                    && property.FamilyData.FamilyName.Contains("Tech")
                    && HasFullFamilyByName(player, "Tech"))
                    cost *= 0.90;
            }

            return System.Math.Round(cost);
        }

        /// <summary>
        /// Phase E.4 — Vend tous les upgrades d'une propriété et rembourse 50% du cumul investi.
        /// </summary>
        public double SellAllUpgrades(PlayerModel player, PropertyData property)
        {
            if (player == null || property == null || property.CurrentLevel == 0) return 0;
            double refund = property.TotalUpgradeCostInvested * 0.5;
            property.SetLevel(0);
            property.TotalUpgradeCostInvested = 0;
            if (transactionManager != null && refund > 0) transactionManager.AddMoney(player, refund);
            Debug.Log($"[Bankruptcy] Sold all upgrades of {property.PropertyName} for ${refund:N0}.");
            return refund;
        }

        public bool IsFamilyComplete(PlayerModel player, PropertyData property)
        {
            if (boardManager == null || property == null || player == null) return false;

            System.Collections.Generic.List<PropertyData> familyProps;

            if (property.FamilyData != null)
                familyProps = boardManager.Tiles
                    .Where(t => t.Type == TileType.Property && t.Property?.FamilyData == property.FamilyData)
                    .Select(t => t.Property).ToList();
            else
                familyProps = boardManager.Tiles
                    .Where(t => t.Type == TileType.Property && t.Property?.ColorGroup == property.ColorGroup)
                    .Select(t => t.Property).ToList();

            if (familyProps.Count == 0) return false;
            return familyProps.All(p => player.OwnedProperties.Contains(p));
        }

        /// <summary>
        /// Vérifie si le joueur possède le monopole complet d'une famille par son nom.
        /// </summary>
        public bool HasFullFamilyByName(PlayerModel player, string familyNameContains)
        {
            if (boardManager?.tileDataList == null || player == null) return false;

            int total = 0, owned = 0;
            FamilyData target = null;

            foreach (var td in boardManager.tileDataList)
            {
                if (td is PropertyData p && p.FamilyData != null
                    && p.FamilyData.FamilyName.Contains(familyNameContains))
                {
                    target = p.FamilyData;
                    total++;
                    if (player.OwnedProperties.Contains(p)) owned++;
                }
            }

            return target != null && total > 0 && owned == total;
        }
    }
}
