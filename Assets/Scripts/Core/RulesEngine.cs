using System;
using UnityEngine;
using System.Linq;
using PolyRisk.Systems;
using PolyRisk.UI;

namespace PolyRisk.Core
{
    /// <summary>
    /// The central authority for game rules and logic validation.
    /// Updated to support 'double' for currency.
    /// </summary>
    public class RulesEngine : MonoBehaviour
    {
        // Removed Singleton Instance to enforce Inspector Injection
        // public static RulesEngine Instance { get; private set; }

        [Header("System References")]
        [Tooltip("Required reference to the TransactionManager.")]
        public TransactionManager transactionManager;
        
        [Tooltip("Required reference to the TurnManager.")]
        public TurnManager turnManager;
        
        [Tooltip("Required reference to the UpgradeSystem.")]
        public UpgradeSystem upgradeSystem;

        [Tooltip("Required reference to the FinanceSystem.")]
        public FinanceSystem financeSystem;

        [Tooltip("Required reference to the VentureSystem.")]
        public VentureSystem ventureSystem;

        [Tooltip("Required reference to the MarketplaceSystem.")]
        public MarketplaceSystem marketplaceSystem;

        [Tooltip("Required reference to the BoardManager.")]
        public BoardManager boardManager;

        private void Awake()
        {
            // No more Singleton initialization
            // Validation check
            if (transactionManager == null || turnManager == null || upgradeSystem == null)
            {
                Debug.LogError("RulesEngine: Critical system references are missing! Please assign them in the Inspector.");
            }
            if (ventureSystem == null)
            {
                Debug.LogWarning("RulesEngine: VentureSystem reference is missing! Venture cards will not work.");
            }
        }

        // Removed Start() and InitializeDependencies() as we rely on Inspector injection

        public void SkipOptionalAction()
        {
            Debug.Log("Player chose to SKIP the action.");
            turnManager.CompleteTileResolution();
        }

        /// <summary>
        /// Executes the primary action for the player on the given tile.
        /// Called by UI buttons or AI logic.
        /// </summary>
        public void ExecutePrimaryAction(PlayerModel player, BoardTile tile)
        {
            if (player == null || tile == null) return;

            // Strict check - no auto-resolve
            if (turnManager == null || upgradeSystem == null || transactionManager == null || financeSystem == null || boardManager == null)
            {
                Debug.LogError("RulesEngine: Cannot execute action. Dependencies are missing.");
                return;
            }

            switch (tile.Type)
            {
                case TileType.Property:
                case TileType.Hub:
                    // Safely cast to PropertyData
                    if (tile.Data is PropertyData propData)
                    {
                        // Check ownership
                        PlayerModel owner = turnManager.GetOwnerOfProperty(propData);

                        if (owner == null)
                        {
                            // Unowned -> Buy
                            if (TryBuyProperty(player, propData))
                            {
                                Debug.Log($"Primary action executed: Buy Property {propData.PropertyName}");
                                turnManager.CompleteTileResolution();
                                return;
                            }

                            // TryBuyProperty a échoué (cash insuffisant, le cas quasi-exclusif ici) —
                            // avant ce fix, l'absence de else faisait tomber jusqu'au garde-fou générique
                            // en bas de la méthode ("Unhandled tile type Property"), un message trompeur
                            // qui laissait croire à un case non géré alors que c'est un skip normal faute
                            // de moyens. Comportement inchangé (on ne fait toujours rien d'autre que
                            // passer), juste un log qui dit ce qui se passe réellement.
                            Debug.Log($"Player {player.PlayerId} ne peut pas acheter {propData.PropertyName} (${propData.BasePrice}) — case laissée libre.");
                            turnManager.CompleteTileResolution();
                            return;
                        }
                        else if (owner.PlayerId == player.PlayerId)
                        {
                            // Hubs cannot be upgraded
                            if (tile.Type == TileType.Hub)
                            {
                                Debug.Log($"Primary action executed: {propData.PropertyName} is a Hub and cannot be upgraded.");
                                turnManager.CompleteTileResolution();
                                return;
                            }

                            // Check Monopoly Rule before upgrading
                            if (!HasFullFamily(player, propData.FamilyData))
                            {
                                Debug.LogWarning($"Primary action executed: Cannot upgrade {propData.PropertyName} without the full {propData.FamilyData.FamilyName} family.");
                                turnManager.CompleteTileResolution();
                                return;
                            }

                            // Owned by self -> Upgrade
                            if (upgradeSystem.TryUpgradeProperty(player, propData))
                            {
                                Debug.Log($"Primary action executed: Upgrade Property {propData.PropertyName}");
                                turnManager.CompleteTileResolution();
                                return;
                            }
                        }
                        else
                        {
                            // Owned by other -> Pay Rent
                            PlayerModel receiver = owner;
                            double rent = CalculateRent(propData);

                            // Fix #4 Phase F — Rent = 0 (embargo actif, Hub en cycle WAR, propriété hypothéquée) :
                            // court-circuit — pas de TransferMoney (renverrait false → insolvabilité fantôme).
                            if (rent <= 0)
                            {
                                Debug.Log($"[Rent] {propData.PropertyName} : loyer 0 (embargo/WAR/mortgage) — pas de paiement.");
                                propData.WasVisitedSinceLastGO = true;
                                turnManager.CompleteTileResolution();
                                return;
                            }

                            // Hijacking Logic
                            if (propData.HijackedBy != null && propData.HijackTurnsRemaining > 0)
                            {
                                receiver = propData.HijackedBy;
                                Debug.Log($"<color=orange>HIJACKED!</color> Rent for {propData.PropertyName} diverted to Player {receiver.PlayerId}.");
                            }

                            bool paid;

                            // Cyber-Sabotage (Armement Ultime) : 25% au hijacker, 75% au propriétaire
                            if (propData.HijackedBy != null && propData.HijackTurnsRemaining > 0)
                            {
                                double stolenPortion = rent * 0.25;
                                double ownerPortion  = rent - stolenPortion;
                                bool s1 = transactionManager.TransferMoney(player, propData.HijackedBy, stolenPortion);
                                bool s2 = transactionManager.TransferMoney(player, owner, ownerPortion);
                                paid = s1 || s2;
                                Debug.Log($"[Hijack] {propData.PropertyName} : {CurrencyFormatter.Format(stolenPortion)} → Player {propData.HijackedBy.PlayerId} | {CurrencyFormatter.Format(ownerPortion)} → Player {owner.PlayerId}");
                            }
                            else
                            {
                                paid = transactionManager.TransferMoney(player, receiver, rent);
                            }

                            if (paid)
                            {
                                propData.WasVisitedSinceLastGO = true;
                                Debug.Log($"Primary action executed: Paid {rent} Rent to {receiver.PlayerId} for {propData.PropertyName}");
                                
                                // Decrement Hijack turns ONLY if payment occurred? Or regardless?
                                // Prompt: "Ensure ExecutePrimaryAction decrements HijackTurnsRemaining after a hijacked rent is paid"
                                if (propData.HijackedBy != null)
                                {
                                    propData.HijackTurnsRemaining--;
                                    if (propData.HijackTurnsRemaining <= 0)
                                    {
                                        propData.HijackedBy = null;
                                        Debug.Log($"Hijack on {propData.PropertyName} has expired.");
                                    }
                                }
                            }
                            else
                            {
                                Debug.LogWarning($"Player {player.PlayerId} cannot afford ${rent} rent. Triggering Bankruptcy flow.");
                                // Fix #3 Phase F — Insolvabilité asynchrone.
                                // Le controller prend la main : IA synchrone (liquide et paie immédiatement),
                                // humain via UI (paie au close du BankruptcyScreen).
                                GameEvents.OnInsolvency?.Invoke(player, rent, receiver);

                                if (player.IsAI)
                                {
                                    // IA : ProcessAILiquidation est synchrone. À sa sortie on solde le créancier.
                                    if (player.CurrentBalance >= rent)
                                        transactionManager.TransferMoney(player, receiver, rent);
                                    else
                                        transactionManager.ProcessBankruptcyCash(player, receiver);
                                    turnManager.CompleteTileResolution();
                                }
                                // Humain : le BankruptcyScreenController appellera CompleteTileResolution
                                // à la fermeture du panel. On ne complète PAS ici.
                                return;
                            }

                            turnManager.CompleteTileResolution();
                            return;
                        }
                    }
                    break;

                case TileType.GlobalAuction:
                    Debug.Log("Primary action executed: Marketplace (case 20)");
                    // Phase D : la décision IA case 20 (Tender vs Sanction selon profil)
                    // est gérée par AIManager.HandleMarketplaceDecision dans ProcessAITurn.
                    // Pour le joueur humain, l'ActionButtonsWidget ouvre l'UI Marketplace.
                    // Dans les deux cas, ne pas appeler CompleteTileResolution ici.
                    return;

                case TileType.Casino:
                    Debug.Log("Primary action executed: Casino");
                    // Simulate a standard bet of $1500 for testing
                    financeSystem.PlayCasino(player, CasinoRiskLevel.Standard, 1500);
                    turnManager.CompleteTileResolution();
                    return;
                
                case TileType.Go_Start:
                    Debug.Log($"Primary action executed: {tile.Type}");
                    // Do NOT process income here to avoid double-dipping if TurnManager handled it
                    // Or, check if TurnManager logic handles "Landed on GO" as "Passed GO".
                    // TurnManager logic: "newPos < currentPos".
                    // If start at 38, roll 2 -> newPos 0. 0 < 38 is TRUE. So TurnManager awards income.
                    // So we should NOT award it again here.
                    turnManager.CompleteTileResolution();
                    return;

                case TileType.Prison:
                    Debug.Log("Primary action executed: Visiting Audit Fiscal.");
                    turnManager.CompleteTileResolution();
                    return;

                case TileType.GoToPrison:
                    Debug.Log("Primary action executed: Caught in Tax Fraud! Going to Audit Fiscal.");
                    turnManager.TeleportPlayer(player, 10); // 10 is Audit Fiscal index
                    player.IsInAudit = true;
                    player.AuditTurnsRemaining = 3;
                    // Enforce rule: Lose extra turn if doubles were rolled
                    turnManager.ResetDoublesState();
                    // Transition to Audit Decision Mode instead of completing resolution
                    turnManager.EnterAuditDecisionMode();
                    return;

                case TileType.SafeZone:
                    Debug.Log($"Primary action executed: {tile.Type}");
                    turnManager.CompleteTileResolution();
                    return;

                case TileType.Venture:
                    Debug.Log($"Primary action executed: Venture Card");
                    if (ventureSystem != null)
                    {
                        ventureSystem.DrawCard(player);
                    }
                    else
                    {
                        Debug.LogError("RulesEngine: VentureSystem reference is missing!");
                    }
                    turnManager.CompleteTileResolution();
                    return;

                case TileType.StockMarket:
                    Debug.Log($"Primary action executed: Stock Market Investment");
                    // Simulate an investment since we have no UI yet
                    double investment = 1000;
                    int duration = 3; // Defaulting to medium term
                    financeSystem.InvestInStockMarket(player, investment, duration);
                    turnManager.CompleteTileResolution();
                    return;

                case TileType.Tax:
                    Debug.Log($"Primary action executed: Tax");

                    double multiplier = tile.TileIndex switch
                    {
                        4  => 0.3,  // Taxe Douanière : 0.3×Sgo
                        22 => 1.0,  // Fix #12 Phase F — Tax Plus-Values 1.0×Sgo (autorité GDD_Board)
                        _  => 0.3
                    };
                    string taxName = tile.TileIndex == 4 ? "Taxe Douanière" : "Taxe Plus-Values";

                    // Passif Pharma — Optimisation Fiscale : taxes réduites de 25%
                    if (HasPharmaPassive(player))
                    {
                        multiplier *= 0.75;
                        Debug.Log($"[Passif Pharma] Optimisation Fiscale appliquée : -{25}% sur {taxName}");
                    }

                    financeSystem.ProcessTax(player, taxName, multiplier);
                    turnManager.CompleteTileResolution();
                    return;

                // Marketplace is GlobalAuction, handled above, but just in case of enum confusion:
                // case TileType.Marketplace: // If this enum existed separately
                //    break;
            }

            // If we reached here, no action was successfully taken (or it was a type that requires no action?)
            // For safety, force complete resolution to prevent softlock.
            Debug.LogWarning($"Unhandled tile type {tile.Type}. Force completing resolution to prevent softlock.");
            turnManager.CompleteTileResolution();
        }

        /// <summary>
        /// Attempts to purchase a property for a player.
        /// </summary>
        public bool TryBuyProperty(PlayerModel player, PropertyData property)
        {
            if (player == null || property == null)
            {
                Debug.LogError("Purchase failed: Player or Property is null.");
                return false;
            }

            if (player.OwnedProperties.Contains(property))
            {
                Debug.LogWarning($"Player {player.PlayerId} already owns {property.PropertyName}.");
                return false;
            }

            // Using double for price
            bool transactionSuccess = transactionManager.DeductMoney(player, property.BasePrice);

            if (transactionSuccess)
            {
                player.AddProperty(property);
                GameEvents.OnPropertyPurchased?.Invoke(player.PlayerId, property.PropertyName);
                
                Debug.Log($"Player {player.PlayerId} successfully purchased {property.PropertyName} for {property.BasePrice}.");
                return true;
            }
            else
            {
                Debug.Log($"Purchase failed: Player {player.PlayerId} has insufficient funds for {property.PropertyName}.");
                return false;
            }
        }

        /// <summary>
        /// Calculates the rent for a specific property.
        /// </summary>
        public double CalculateRent(PropertyData property)
        {
            // Fix #6 Phase F — Propriété hypothéquée = zéro loyer (règle GDD)
            if (property.IsMortgaged)
            {
                Debug.Log($"[Rent] {property.PropertyName} hypothéquée : loyer 0.");
                return 0;
            }

            if (property.IsEmbargoed && property.EmbargoTurnsRemaining > 0)
            {
                Debug.Log($"<color=red>EMBARGO!</color> Rent for {property.PropertyName} is 0 this turn.");
                return 0;
            }

            PlayerModel owner = turnManager.GetOwnerOfProperty(property);
            if (owner == null) return 0;

            // ── Phase C : Cycle WAR — les Hubs ne génèrent plus de loyer ──
            var cycleMgr = CycleManager.Instance;
            if (cycleMgr != null && cycleMgr.ShouldZeroHubRent()
                && property.FamilyData != null && property.FamilyData.FamilyName.Contains("Logistics"))
            {
                Debug.Log($"[Cycle WAR] Hub {property.PropertyName} : loyer = 0.");
                return 0;
            }

            // Check if it's a Logistics Hub (Assuming the family name contains "Logistics" or "Hub")
            if (property.FamilyData != null && property.FamilyData.FamilyName.Contains("Logistics"))
            {
                // Count how many Logistics Hubs the owner has
                int hubCount = 0;
                foreach (var p in owner.OwnedProperties)
                {
                    if (p.FamilyData != null && p.FamilyData.FamilyName.Contains("Logistics"))
                    {
                        hubCount++;
                    }
                }

                // Calculate rent based on Hub Count and Sgo
                double sgo = financeSystem.CurrentSgo;
                double hubRent = hubCount switch
                {
                    1 => 0.5 * sgo,
                    2 => 1.5 * sgo,
                    3 => 4.0 * sgo,
                    4 => 6.0 * sgo,   // GDD_Board v7 : 6×Sgo (anti-snowball)
                    _ => 0
                };
                // Phase C : cycle BOOM/CRASH module aussi le loyer Hub
                if (cycleMgr != null && cycleMgr.IsActive) hubRent *= cycleMgr.GetRentMultiplier();
                return hubRent;
            }

            // Multiplicateurs GDD_Board v7 : ×1 / ×2 / ×5 / ×12 / ×50 / ×200
            double standardRent = property.BaseRent;
            int[] rentMultipliers = { 1, 2, 5, 12, 50, 200 };

            if (property.CurrentLevel >= 0 && property.CurrentLevel < rentMultipliers.Length)
                standardRent *= rentMultipliers[property.CurrentLevel];

            // Bonus monopole ×2 — s'applique à TOUS les niveaux dès que le monopole est détenu (GDD_Board v7)
            if (HasFullFamily(owner, property.FamilyData))
                standardRent *= 2;

            // ── Passif Oil & Gas — Forage Intensif : +20% loyers si monopole complet ──
            if (property.FamilyData != null
                && (property.FamilyData.FamilyName.Contains("Oil") || property.FamilyData.FamilyName.Contains("Gas"))
                && HasFullFamily(owner, property.FamilyData))
                standardRent *= 1.20;

            // ── Passif Luxe — Prestige Social : adversaires paient +25% si monopole Luxe ──
            if (property.FamilyData != null
                && property.FamilyData.FamilyName.Contains("Luxe")
                && HasFullFamily(owner, property.FamilyData))
                standardRent *= 1.25;

            // ── Ultime Oil & Gas — Boom Énergétique : +80% pendant 1 tour ──
            if (owner.OilGasBoostTurnsRemaining > 0
                && property.FamilyData != null
                && (property.FamilyData.FamilyName.Contains("Oil") || property.FamilyData.FamilyName.Contains("Gas")))
                standardRent *= 1.80;

            // ── Venture Card penalty — Marée Noire : Oil&Gas ÷2 pendant N tours ──
            if (owner.OilGasPenaltyTurns > 0
                && property.FamilyData != null
                && (property.FamilyData.FamilyName.Contains("Oil") || property.FamilyData.FamilyName.Contains("Gas")))
                standardRent *= 0.5;

            // ── Phase C : modificateurs de cycle ──
            if (cycleMgr != null && cycleMgr.IsActive)
            {
                // BOOM/CRASH : modificateur global sur tous les loyers
                standardRent *= cycleMgr.GetRentMultiplier();

                // WAR : Armement ×2, Luxe ×0.5
                standardRent *= cycleMgr.GetWarFamilyMultiplier(property);

                // Passif Armement — Logistique de Conflit : +15% revenus pendant WAR
                if (cycleMgr.IsWar
                    && property.FamilyData != null
                    && property.FamilyData.FamilyName.Contains("Armement")
                    && HasFullFamily(owner, property.FamilyData))
                {
                    standardRent *= 1.15;
                }
            }

            return standardRent;
        }

        /// <summary>Vérifie si le joueur a le passif Pharma actif (monopole Pharma complet).</summary>
        private bool HasPharmaPassive(PlayerModel player)
        {
            if (boardManager?.tileDataList == null) return false;
            int total = 0, owned = 0;
            FamilyData pharma = null;
            foreach (var td in boardManager.tileDataList)
            {
                if (td is PropertyData p && p.FamilyData != null && p.FamilyData.FamilyName.Contains("Pharma"))
                {
                    pharma = p.FamilyData; total++;
                    if (player.OwnedProperties.Contains(p)) owned++;
                }
            }
            return pharma != null && total > 0 && owned == total;
        }

        public bool HasFullFamily(PlayerModel player, FamilyData family)
        {
            if (family == null || boardManager == null || player == null) return false;

            // 1. Count total properties in this family on the board
            // We iterate through boardManager.tileDataList (which contains BoardTileData/PropertyData)
            int totalInFamily = 0;
            if (boardManager.tileDataList != null)
            {
                foreach (var tileData in boardManager.tileDataList)
                {
                    if (tileData is PropertyData prop && prop.FamilyData == family)
                    {
                        totalInFamily++;
                    }
                }
            }

            // 2. Count properties owned by player in this family
            int ownedInFamily = 0;
            foreach (var prop in player.OwnedProperties)
            {
                if (prop.FamilyData == family)
                {
                    ownedInFamily++;
                }
            }

            // 3. Compare
            return totalInFamily > 0 && ownedInFamily == totalInFamily;
        }

        /// <summary>
        /// Handles a player landing on a property.
        /// </summary>
        public void HandlePropertyLanding(PlayerModel landingPlayer, PropertyData property, PlayerModel owner)
        {
            if (owner == null)
            {
                Debug.Log($"Player {landingPlayer.PlayerId} landed on unowned property {property.PropertyName}.");
            }
            else if (owner.PlayerId != landingPlayer.PlayerId)
            {
                double rent = CalculateRent(property);
                bool paid = transactionManager.TransferMoney(landingPlayer, owner, rent);
                
                if (!paid)
                {
                    Debug.Log($"Player {landingPlayer.PlayerId} cannot afford rent!");
                    // Bankruptcy logic to be implemented
                }
                else
                {
                     Debug.Log($"Player {landingPlayer.PlayerId} paid {rent} rent to Player {owner.PlayerId} for {property.PropertyName}.");
                }
            }
        }
    }
}
