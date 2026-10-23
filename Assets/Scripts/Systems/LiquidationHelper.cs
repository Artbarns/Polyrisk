using UnityEngine;
using System.Linq;
using PolyRisk.Core;

namespace PolyRisk.Systems
{
    /// <summary>
    /// Phase E.4 — Helpers de liquidation forcée (faillite).
    /// Ordre strict GDD_Economy §6 : (1) vendre upgrades 50% → (2) hypothéquer 50% prix achat
    /// → (3) vendre titres à la banque 50% prix achat.
    /// </summary>
    public static class LiquidationHelper
    {
        public static void LiquidateToCover(PlayerModel player, double debt)
        {
            var tm = TransactionManager.Instance;
            foreach (var prop in player.OwnedProperties.Where(p => p.CurrentLevel > 0)
                         .OrderByDescending(p => p.TotalUpgradeCostInvested).ToList())
            {
                if (player.CurrentBalance >= debt) return;
                double refund = prop.TotalUpgradeCostInvested * 0.5;
                prop.SetLevel(0);
                prop.TotalUpgradeCostInvested = 0;
                if (refund > 0) tm.AddMoney(player, refund);
            }
            foreach (var prop in player.OwnedProperties.Where(p => !p.IsMortgaged).OrderBy(p => p.BasePrice).ToList())
            {
                if (player.CurrentBalance >= debt) return;
                MortgageProperty(player, prop);
            }
            // Mortgaged titles have no remaining cash value; bankruptcy returns them to the bank.
        }

        /// <summary>Hypothèque la propriété : 50% du prix, plus de loyer ni Burn Rate.</summary>
        public static double MortgageProperty(PlayerModel player, PropertyData prop)
        {
            if (player == null || prop == null || prop.IsMortgaged) return 0;
            double refund = prop.BasePrice * 0.5;
            prop.IsMortgaged = true;
            // Fix #6 Phase F — Anti-exploit : exonération de Burn Rate au GO SUIVANT uniquement.
            prop.MortgageExemptFromNextGo = true;
            var tm = TransactionManager.Instance;
            if (tm != null) tm.AddMoney(player, refund);
            Debug.Log($"[Bankruptcy] {prop.PropertyName} hypothéquée pour ${refund:N0}.");
            return refund;
        }

        /// <summary>Vend le titre à la banque : 50% du prix (0 si déjà hypothéquée), propriété neutre.</summary>
        public static double SellPropertyToBank(PlayerModel player, PropertyData prop)
        {
            if (player == null || prop == null) return 0;
            // Fix #6 Phase F — Si déjà hypothéquée, le joueur a déjà touché les 50%.
            // Vendre à la banque après hypothèque = 0 (sinon = 100% cumulé = exploit).
            double refund = prop.IsMortgaged ? 0 : prop.BasePrice * 0.5;
            player.RemoveProperty(prop);
            // Reset état banque
            prop.IsMortgaged = false;
            prop.MortgageExemptFromNextGo = false;
            prop.SetLevel(0);
            prop.TotalUpgradeCostInvested = 0;
            prop.HijackedBy = null;
            prop.HijackTurnsRemaining = 0;
            prop.IsEmbargoed = false;
            prop.EmbargoTurnsRemaining = 0;
            var tm = TransactionManager.Instance;
            if (tm != null && refund > 0) tm.AddMoney(player, refund);
            Debug.Log($"[Bankruptcy] {prop.PropertyName} vendue à la banque pour ${refund:N0}.");
            return refund;
        }

        /// <summary>Valeur totale liquidable d'un joueur (upgrades + hypothèques + titres restants).</summary>
        public static double GetTotalLiquidableValue(PlayerModel player)
        {
            if (player == null) return 0;
            double total = 0;
            foreach (var prop in player.OwnedProperties)
            {
                total += prop.TotalUpgradeCostInvested * 0.5; // upgrades (option toujours dispo si CurrentLevel > 0)
                // Fix #6 Phase F — Une propriété donne 50% une seule fois : soit hypothèque, soit vente.
                if (!prop.IsMortgaged) total += prop.BasePrice * 0.5;
            }
            return total;
        }
    }
}
