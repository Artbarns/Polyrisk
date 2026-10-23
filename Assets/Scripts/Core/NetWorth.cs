using System.Collections.Generic;
using System.Linq;

namespace PolyRisk.Core
{
    public static class NetWorth
    {
        // GDD Economy §1: purchase prices + half of paid upgrades + full families.
        public static double Calculate(PlayerModel player, IEnumerable<PropertyData> boardProperties)
        {
            if (player == null) return 0;
            double value = player.CurrentBalance + player.OwnedProperties.Sum(
                p => p.BasePrice + p.TotalUpgradeCostInvested * 0.5);
            if (boardProperties == null) return value;
            foreach (var family in boardProperties.Where(p => p != null && p.FamilyData != null)
                         .Distinct().GroupBy(p => p.FamilyData))
                if (family.All(p => player.OwnedProperties.Contains(p))) value += 50000;
            return value;
        }
    }
}
