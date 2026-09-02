using UnityEngine;
using PolyRisk.Core;
using PolyRisk.UI;
using System.Collections.Generic;
using System.Linq;

namespace PolyRisk.Systems
{
    public enum CasinoRiskLevel
    {
        Safe,
        Standard,
        HighRisk
    }

    [System.Serializable]
    public class StockInvestment
    {
        public double Amount;
        public int TurnsRemaining;
        public int TotalDuration; // 1, 3, or 5
    }

    public class FinanceSystem : MonoBehaviour
    {
        [Tooltip("Required reference to the TransactionManager.")]
        public TransactionManager transactionManager;

        [Tooltip("Required for Burn Rate and Dividends calculations (Monopoly check).")]
        public BoardManager boardManager;

        public double CurrentSgo = 2000;
        public string CurrentMarketState = "STABLE";

        // Burn Rate par niveau (Mode Actif 100% / Mode Veille 25%) — GDD_Economy §3
        private static readonly double[] BurnRateActive = { 0, 300, 900, 3000, 10000, 35000 };
        private static readonly double[] BurnRateVeille = { 0,  75, 225,  750,  2500,  8750 };

        // Multiplicateurs loyer pour calcul dividendes — GDD_Board v7
        private static readonly int[] RentMultipliers = { 1, 2, 5, 12, 50, 200 };

        private Dictionary<int, List<StockInvestment>> activeInvestments = new Dictionary<int, List<StockInvestment>>();

        public void InvestInStockMarket(PlayerModel player, double amount, int duration)
        {
            if (transactionManager == null)
            {
                Debug.LogError("FinanceSystem: TransactionManager is not assigned.");
                return;
            }

            // Plafonds GDD_Economy §5 : 15×Sgo (1T & 3T) / 20×Sgo (5T)
            double limit = duration == 5 ? 20 * CurrentSgo : 15 * CurrentSgo;
            if (amount > limit)
            {
                amount = limit;
                Debug.Log($"Investment capped at {limit} ({(duration == 5 ? 20 : 15)}×Sgo).");
            }

            if (player.CurrentBalance < amount)
            {
                Debug.LogWarning($"Player {player.PlayerId} cannot afford investment of {amount}.");
                return;
            }

            if (transactionManager.DeductMoney(player, amount))
            {
                if (!activeInvestments.ContainsKey(player.PlayerId))
                {
                    activeInvestments[player.PlayerId] = new List<StockInvestment>();
                }

                activeInvestments[player.PlayerId].Add(new StockInvestment { Amount = amount, TurnsRemaining = duration, TotalDuration = duration });
                Debug.Log($"Player {player.PlayerId} invested {amount} in Stock Market for {duration} turns.");
            }
        }

        public void ProcessActiveInvestments(PlayerModel player)
        {
            if (!activeInvestments.ContainsKey(player.PlayerId)) return;

            var investments = activeInvestments[player.PlayerId];
            for (int i = investments.Count - 1; i >= 0; i--)
            {
                var inv = investments[i];
                inv.TurnsRemaining--;

                if (inv.TurnsRemaining <= 0)
                {
                    double roi = 0;
                    bool totalLoss = false;

                    // Phase C : shifts macroéconomiques (BOOM/CRASH)
                    var cycleMgr = CycleManager.Instance;
                    float shiftMin = cycleMgr != null ? cycleMgr.GetStockShiftMin() : 0f;
                    float shiftMax = cycleMgr != null ? cycleMgr.GetStockShiftMax() : 0f;
                    float lossMultiplier = cycleMgr != null ? cycleMgr.GetStockTotalLossMultiplier() : 1f;

                    switch (inv.TotalDuration)
                    {
                        case 1:
                            roi = Random.Range(-0.30f + shiftMin, 0.40f + shiftMax);
                            break;
                        case 3:
                            roi = Random.Range(-0.30f + shiftMin, 0.70f + shiftMax);
                            break;
                        case 5:
                            int lossThreshold = Mathf.RoundToInt(20f * lossMultiplier);
                            if (Random.Range(0, 100) < lossThreshold)
                            {
                                totalLoss = true;
                                roi = -1.0f;
                            }
                            else
                            {
                                roi = Random.Range(-0.20f + shiftMin, 1.20f + shiftMax);
                            }
                            break;
                        default:
                            roi = 0;
                            break;
                    }

                    double payout = inv.Amount * (1.0 + roi);
                    if (payout < 0) payout = 0;

                    transactionManager.AddMoney(player, payout);
                    
                    string resultColor = payout > inv.Amount ? "green" : "red";
                    
                    if (totalLoss)
                    {
                        Debug.Log($"<color=red>STOCK MARKET CRASH</color> Player {player.PlayerId} ({inv.TotalDuration} turns): TOTAL LOSS! Invested {inv.Amount}, Payout 0.");
                    }
                    else
                    {
                        Debug.Log($"<color={resultColor}>STOCK MARKET RESULT</color> Player {player.PlayerId} ({inv.TotalDuration} turns): Invested {inv.Amount}, ROI {roi:P0}, Payout {payout}.");
                    }
                    
                    investments.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Calcul complet du passage DÉPART — GDD_Economy §4.
        /// Revenu = Sgo + Σ(Dividendes Tiérisés) − Σ(Burn Rate) − Infrastructure_Tax(4 Hubs)
        /// </summary>
        public void ProcessGoIncome(PlayerModel player)
        {
            if (transactionManager == null)
            {
                Debug.LogError("FinanceSystem: TransactionManager is not assigned.");
                return;
            }

            double totalCredit = 0;
            double totalDebit  = 0;

            // ── 1. Salaire GO ──────────────────────────────────────────────────────
            totalCredit += CurrentSgo;

            // ── 2. Dividendes Tiérisés ─────────────────────────────────────────────
            // Versés par propriété si le joueur détient le monopole complet de la famille.
            // Formule : (BaseRent × Mult_Niveau × 2) × Taux_Tier
            var familiesChecked = new System.Collections.Generic.HashSet<FamilyData>();
            foreach (var prop in player.OwnedProperties)
            {
                if (prop.FamilyData == null || prop.FamilyData.FamilyName.Contains("Logistics")) continue;
                if (familiesChecked.Contains(prop.FamilyData)) continue;
                if (!HasFullFamilyForPlayer(player, prop.FamilyData)) continue;

                familiesChecked.Add(prop.FamilyData);

                double tierRate = GetDividendRate(prop.FamilyData);
                foreach (var familyProp in player.OwnedProperties)
                {
                    if (familyProp.FamilyData != prop.FamilyData) continue;
                    int lvl = Mathf.Clamp(familyProp.CurrentLevel, 0, RentMultipliers.Length - 1);
                    double monopolyRent = familyProp.BaseRent * RentMultipliers[lvl] * 2.0;
                    totalCredit += monopolyRent * tierRate;
                }
            }

            // ── 3. Burn Rate ───────────────────────────────────────────────────────
            // Prélevé par propriété. Multiplicateur ×2 si DoubleBurnRateNextGo actif.
            double burnMultiplier = player.DoubleBurnRateNextGo ? 2.0 : 1.0;
            if (player.DoubleBurnRateNextGo) player.DoubleBurnRateNextGo = false;

            // Phase C : modificateur cycle (BOOM × 1.20 sur Burn Rate)
            var cm = CycleManager.Instance;
            if (cm != null && cm.IsActive) burnMultiplier *= cm.GetBurnRateMultiplier();

            foreach (var prop in player.OwnedProperties)
            {
                if (prop.FamilyData != null && prop.FamilyData.FamilyName.Contains("Logistics")) continue;

                // Fix #6 Phase F — Hypothèque : pas de Burn Rate. Règle anti-exploit GDD :
                // exonération uniquement au GO *suivant* l'hypothèque. On stocke MortgageExemptFromNextGo
                // au moment de l'hypothèque (LiquidationHelper). Ici : si exempt, on skip ET on retire l'exemption.
                if (prop.IsMortgaged)
                {
                    if (prop.MortgageExemptFromNextGo) prop.MortgageExemptFromNextGo = false;
                    // Fix #17 remonté : décrémenter embargo/hijack même sur props hypothéquées
                    DecrementDurations(prop);
                    prop.WasVisitedSinceLastGO = false;
                    continue;
                }

                // Fix #17 (partial) — décrémenter embargo/hijack sur toutes les props, pas seulement Lvl>0
                if (prop.CurrentLevel == 0)
                {
                    DecrementDurations(prop);
                    prop.WasVisitedSinceLastGO = false;
                    continue; // Lvl0 : Burn Rate = $0 mais on a bien décrémenté
                }

                int lvl = Mathf.Clamp(prop.CurrentLevel, 0, BurnRateActive.Length - 1);
                double br = prop.WasVisitedSinceLastGO ? BurnRateActive[lvl] : BurnRateVeille[lvl];
                totalDebit += br * burnMultiplier;

                // Reset pour le prochain cycle
                prop.WasVisitedSinceLastGO = false;

                DecrementDurations(prop);
            }

            // ── 4. Infrastructure Tax (4 Hubs) ─────────────────────────────────────
            // $20,000/GO si le joueur possède les 4 Hubs simultanément (anti-snowball)
            int hubCount = player.OwnedProperties.Count(
                p => p.FamilyData != null && p.FamilyData.FamilyName.Contains("Logistics"));
            if (hubCount == 4)
            {
                totalDebit += 20000;
                Debug.Log($"[GO] Player {player.PlayerId} — Infrastructure Tax 4 Hubs : -$20,000");
            }

            // ── 5. Décrémenter OilGasPenaltyTurns ─────────────────────────────────
            if (player.OilGasPenaltyTurns > 0)
                player.OilGasPenaltyTurns--;

            // ── 6. Application nette ───────────────────────────────────────────────
            double netRevenu = totalCredit - totalDebit;
            if (netRevenu >= 0)
            {
                transactionManager.AddMoney(player, netRevenu);
            }
            else
            {
                // Revenu net négatif : déduire le delta (Burn Rate > Sgo + Dividendes)
                transactionManager.AddMoney(player, totalCredit);
                transactionManager.DeductMoney(player, totalDebit);
            }

            Debug.Log($"[GO] Player {player.PlayerId} — Sgo+Divs: +{CurrencyFormatter.Format(totalCredit)} | Burn: -{CurrencyFormatter.Format(totalDebit)} | Net: {CurrencyFormatter.Format(netRevenu)}");

            // Phase E.3 — Check Hyper-Mogul ($10M cash net, GDD §6.1)
            if (player.CurrentBalance >= 10_000_000)
            {
                Debug.Log($"<color=yellow>[VICTORY] Player {player.PlayerId} reached $10M — HYPER-MOGUL!</color>");
                GameEvents.OnGameVictory?.Invoke(player, $"REACHED $10,000,000 IN NET CASH");
            }
        }

        // ── Helpers Dividendes ──────────────────────────────────────────────────────

        /// <summary>Fix #6/#17 Phase F — Décrément embargo/hijack indépendant du Burn Rate.</summary>
        private void DecrementDurations(PropertyData prop)
        {
            if (prop.EmbargoTurnsRemaining > 0)
            {
                prop.EmbargoTurnsRemaining--;
                if (prop.EmbargoTurnsRemaining <= 0) prop.IsEmbargoed = false;
            }
            if (prop.HijackTurnsRemaining > 0)
            {
                prop.HijackTurnsRemaining--;
                if (prop.HijackTurnsRemaining <= 0) prop.HijackedBy = null;
            }
        }

        private double GetDividendRate(FamilyData family)
        {
            int rank = family.PowerRank;
            if (rank <= 2) return 0.10; // Tier 1 : Real Estate, Pharma
            if (rank <= 5) return 0.14; // Tier 2 : Auto, Oil&Gas, Luxe
            return 0.25;                // Tier 3 : Tech, Aerospace, Armement
        }

        private bool HasFullFamilyForPlayer(PlayerModel player, FamilyData family)
        {
            if (family == null || boardManager == null || player == null) return false;

            int total = 0;
            if (boardManager.tileDataList != null)
            {
                foreach (var td in boardManager.tileDataList)
                    if (td is PropertyData p && p.FamilyData == family) total++;
            }
            int owned = player.OwnedProperties.Count(p => p.FamilyData == family);
            return total > 0 && owned == total;
        }

        public void ProcessTax(PlayerModel player, string taxName, double multiplier)
        {
            if (transactionManager == null)
            {
                Debug.LogError("FinanceSystem: TransactionManager is not assigned.");
                return;
            }

            double taxAmount = CurrentSgo * multiplier;
            if (transactionManager.DeductMoney(player, taxAmount))
            {
                Debug.Log($"Player {player.PlayerId} paid {taxName}: {taxAmount} (Multiplier: {multiplier})");
            }
            else
            {
                Debug.LogWarning($"Player {player.PlayerId} could not pay full tax {taxAmount}. Bankruptcy logic needed.");
                // Bankruptcy logic handled by TransactionManager/RulesEngine usually
            }
        }

        public void PlayCasino(PlayerModel player, CasinoRiskLevel risk, double betAmount)
        {
            if (transactionManager == null)
            {
                Debug.LogError("FinanceSystem: TransactionManager is not assigned.");
                return;
            }

            if (betAmount <= 0)
            {
                Debug.LogWarning("Casino: Bet amount must be positive.");
                return;
            }

            // Ensure player has enough cash before betting
            if (player.CurrentBalance < betAmount)
            {
                Debug.LogWarning($"Casino: Player {player.PlayerId} does not have enough cash to bet {betAmount}. Current Cash: {player.CurrentBalance}");
                return;
            }

            // Deduct the bet amount immediately
            if (!transactionManager.DeductMoney(player, betAmount))
            {
                Debug.LogError("Casino: Failed to deduct bet amount despite checking balance.");
                return;
            }

            // Generate a random roll between 1 and 100
            int roll = Random.Range(1, 101);
            bool win = false;
            double winMultiplier = 0.0;

            // Determine win condition based on risk level
            switch (risk)
            {
                case CasinoRiskLevel.Safe:
                    // 80% win chance / +25% gain — GDD_Economy §5
                    winMultiplier = 1.25;
                    if (roll <= 80) win = true;
                    break;

                case CasinoRiskLevel.Standard:
                    // 50% win chance
                    winMultiplier = 1.80;
                    if (roll <= 50) win = true;
                    break;

                case CasinoRiskLevel.HighRisk:
                    // 25% win chance
                    winMultiplier = 3.00;
                    if (roll <= 25) win = true;
                    break;
            }

            if (win)
            {
                double winnings = betAmount * winMultiplier;
                transactionManager.AddMoney(player, winnings);
                Debug.Log($"<color=green>CASINO WIN!</color> Player {player.PlayerId} bet {betAmount} on {risk} (Roll: {roll}). Won {winnings}!");
            }
            else
            {
                Debug.Log($"<color=red>CASINO LOSS.</color> Player {player.PlayerId} bet {betAmount} on {risk} (Roll: {roll}). Lost everything.");
            }
        }
    }
}
