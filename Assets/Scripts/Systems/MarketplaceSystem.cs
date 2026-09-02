using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using PolyRisk.Core;
using PolyRisk.UI;
using System;
using System.Collections;

namespace PolyRisk.Systems
{
    public enum SanctionType
    {
        Hack,
        Sabotage,
        ForceAudit
    }

    /// <summary>
    /// Manages the Marketplace (Tile 20) interactions, specifically PvP sanctions.
    /// </summary>
    public class MarketplaceSystem : MonoBehaviour
    {
        public enum AuctionState
        {
            None,
            SelectingAsset,
            InvitingPlayers,
            Bidding,
            Reviewing,
            Closing
        }

        [System.Serializable]
        public class TenderBid
        {
            public PlayerModel bidder;
            public double cashOffer;
            public List<PropertyData> propertyOffers = new List<PropertyData>();
        }

        [Header("System References")]
        [Tooltip("Reference to the FinanceSystem for calculating costs.")]
        public FinanceSystem financeSystem;

        [Tooltip("Reference to the TurnManager for player targeting.")]
        public TurnManager turnManager;

        [Tooltip("Reference to the TransactionManager for payments.")]
        public TransactionManager transactionManager;

        private readonly Dictionary<int, TenderBid> activeBids = new Dictionary<int, TenderBid>();
        public IReadOnlyDictionary<int, TenderBid> ActiveBids => activeBids;
        
        public PlayerModel CurrentTenderSeller { get; private set; }
        public PropertyData CurrentTenderProperty { get; private set; }

        public AuctionState CurrentAuctionState { get; private set; } = AuctionState.None;
        public event Action<AuctionState> OnAuctionStateChanged;

        public void SetAuctionState(AuctionState newState)
        {
            if (CurrentAuctionState == newState) return;
            CurrentAuctionState = newState;
            OnAuctionStateChanged?.Invoke(CurrentAuctionState);
        }

        /// <summary>
        /// Executes a sanction (attack) from one player to another.
        /// </summary>
        public void ExecuteSanction(PlayerModel attacker, PlayerModel target, SanctionType type)
        {
            if (attacker == null || target == null)
            {
                Debug.LogError("MarketplaceSystem: Invalid attacker or target for sanction.");
                return;
            }

            // ── Pharma Ultime "Protocole Sanitaire" : immunité totale ────────────
            if (IsImmune(target))
            {
                Debug.Log($"[Immunité] Player {target.PlayerId} bloque la sanction {type} (Protocole Sanitaire ou Golden Ticket).");
                ConsumeGoldenTicketIfUsed(target);
                return;
            }

            // Simple null check
            if (financeSystem == null || transactionManager == null || turnManager == null)
            {
                Debug.LogError("MarketplaceSystem: Missing system references.");
                return;
            }

            double sgo = financeSystem.CurrentSgo;
            double cost = 0;
            string sanctionName = "";

            // Determine cost based on type
            switch (type)
            {
                case SanctionType.Hack:
                    cost = 2 * sgo;
                    sanctionName = "Hack";
                    break;
                case SanctionType.Sabotage:
                    cost = 3 * sgo;
                    sanctionName = "Sabotage";
                    break;
                case SanctionType.ForceAudit:
                    cost = 4 * sgo;
                    sanctionName = "Force Audit";
                    break;
            }

            // Check affordability
            if (attacker.CurrentBalance < cost)
            {
                Debug.Log($"MarketplaceSystem: {attacker.PlayerId} cannot afford {sanctionName} (Cost: {cost}).");
                return;
            }

            // Pay the cost
            bool paid = transactionManager.DeductMoney(attacker, cost);
            if (paid)
            {
                Debug.Log($"MarketplaceSystem: {attacker.PlayerId} initiated {sanctionName} against {target.PlayerId} for {cost}.");
                ApplySanctionEffect(attacker, target, type, sgo);
            }
        }

        private void ApplySanctionEffect(PlayerModel attacker, PlayerModel target, SanctionType type, double sgo)
        {
            switch (type)
            {
                case SanctionType.Hack:
                    // Steal 1 * Sgo
                    double stolenAmount = 1 * sgo;
                    // Ensure target has enough money, or steal what's left
                    if (target.CurrentBalance < stolenAmount)
                    {
                        stolenAmount = target.CurrentBalance;
                    }
                    
                    transactionManager.DeductMoney(target, stolenAmount);
                    transactionManager.AddMoney(attacker, stolenAmount);
                    Debug.Log($"[PvP] HACK SUCCESS: {attacker.PlayerId} stole {stolenAmount} from {target.PlayerId}.");
                    break;

                case SanctionType.Sabotage:
                    // Downgrade a property
                    // Find target's upgraded properties
                    var upgradedProps = target.OwnedProperties.Where(p => p.CurrentLevel > 0).ToList();
                    
                    if (upgradedProps.Count > 0)
                    {
                        // Pick random or first? Let's pick random for chaos.
                        var propToSabotage = upgradedProps[UnityEngine.Random.Range(0, upgradedProps.Count)];
                        propToSabotage.SetLevel(propToSabotage.CurrentLevel - 1);
                        Debug.Log($"[PvP] SABOTAGE SUCCESS: {target.PlayerId}'s property {propToSabotage.PropertyName} downgraded to Level {propToSabotage.CurrentLevel}.");
                    }
                    else
                    {
                        Debug.Log($"[PvP] SABOTAGE FAILED: {target.PlayerId} has no upgraded properties.");
                    }
                    break;

                case SanctionType.ForceAudit:
                    // Send to jail
                    Debug.Log($"[PvP] FORCE AUDIT: {target.PlayerId} is being sent to Audit Fiscal!");
                    turnManager.TeleportPlayer(target, 10); // 10 is Audit Fiscal
                    target.IsInAudit = true;
                    target.AuditTurnsRemaining = 3;
                    // We do not reset target's doubles state or anything because it's not their turn.
                    break;
            }
        }

        public void ExecuteEmbargo(PlayerModel attacker, PropertyData target)
        {
            if (attacker == null || target == null || financeSystem == null || transactionManager == null) return;
            double cost = 1.5 * financeSystem.CurrentSgo;

            if (attacker.CurrentBalance < cost)
            {
                Debug.LogWarning($"[Marketplace] EMBARGO failed. {attacker.PlayerId} lacks funds ({cost}).");
                return;
            }

            // ── Immunité du propriétaire ──
            PlayerModel propOwner = turnManager?.GetOwnerOfProperty(target);
            if (propOwner != null && IsImmune(propOwner))
            {
                Debug.Log($"[Immunité] EMBARGO bloqué — Player {propOwner.PlayerId} protégé.");
                ConsumeGoldenTicketIfUsed(propOwner);
                return;
            }

            transactionManager.DeductMoney(attacker, cost);
            target.IsEmbargoed = true;
            target.EmbargoTurnsRemaining = 3;
            
            Debug.Log($"[Marketplace] EMBARGO executed by Player {attacker.PlayerId} on {target.PropertyName} for {cost}. Rent is 0 for 3 turns.");
        }

        public void ExecuteAudit(PlayerModel attacker, PlayerModel target)
        {
            if (attacker == null || target == null || financeSystem == null || transactionManager == null) return;
            double cost = 1.0 * financeSystem.CurrentSgo;

            if (attacker.CurrentBalance < cost)
            {
                Debug.LogWarning($"[Marketplace] TARGETED AUDIT failed. {attacker.PlayerId} lacks funds ({cost}).");
                return;
            }

            // ── Immunité de la cible ──
            if (IsImmune(target))
            {
                Debug.Log($"[Immunité] TARGETED AUDIT bloqué — Player {target.PlayerId} protégé.");
                ConsumeGoldenTicketIfUsed(target);
                return;
            }

            transactionManager.DeductMoney(attacker, cost);
            
            // Target pays 5% of cash to Bank
            double tax = target.CurrentBalance * 0.05;
            transactionManager.DeductMoney(target, tax);
            
            Debug.Log($"[Marketplace] TARGETED AUDIT executed by Player {attacker.PlayerId} on Player {target.PlayerId} for {cost}. Target lost {tax} to the bank.");
        }

        public void ExecuteStrike(PlayerModel attacker, PropertyData targetFamily)
        {
            if (attacker == null || targetFamily == null) return;
            if (financeSystem == null) financeSystem = FindObjectOfType<FinanceSystem>();
            if (transactionManager == null) transactionManager = TransactionManager.Instance;
            if (turnManager == null) turnManager = FindObjectOfType<TurnManager>();
            if (financeSystem == null || transactionManager == null || turnManager == null) return;
            double cost = 2.0 * financeSystem.CurrentSgo;
            
            if (attacker.CurrentBalance < cost)
            {
                Debug.LogWarning($"[Marketplace] PRODUCTION STRIKE failed. {attacker.PlayerId} lacks funds ({cost}).");
                return;
            }
            
            PlayerModel owner = turnManager.GetOwnerOfProperty(targetFamily);
            if (owner != null)
            {
                // ── Immunité du propriétaire ──
                if (IsImmune(owner))
                {
                    Debug.Log($"[Immunité] PRODUCTION STRIKE bloqué — Player {owner.PlayerId} protégé.");
                    ConsumeGoldenTicketIfUsed(owner);
                    return;
                }

                transactionManager.DeductMoney(attacker, cost);
                owner.DoubleBurnRateNextGo = true;
                Debug.Log($"[Marketplace] PRODUCTION STRIKE executed by Player {attacker.PlayerId} targeting family owner Player {owner.PlayerId} for {cost}. Burn rate doubled next GO.");
            }
            else
            {
                Debug.LogWarning($"[Marketplace] PRODUCTION STRIKE failed. Property {targetFamily.PropertyName} is unowned.");
            }
        }

        public void ExecuteSabotage(PlayerModel attacker, PropertyData target)
        {
            if (attacker == null || target == null || financeSystem == null || transactionManager == null) return;
            double cost = 5.0 * financeSystem.CurrentSgo;
            
            if (attacker.CurrentBalance < cost)
            {
                Debug.LogWarning($"[Marketplace] SABOTAGE failed. {attacker.PlayerId} lacks funds ({cost}).");
                return;
            }
            
            // Destroy 1 upgrade level on target (except Aerospace — passif Blindage High-Tech)
            if (target.FamilyData != null && target.FamilyData.FamilyName.Contains("Aerospace"))
            {
                Debug.LogWarning($"[Marketplace] SABOTAGE failed. Aerospace properties are immune (Blindage High-Tech).");
                return;
            }

            // ── Immunité Pharma du propriétaire ──
            PlayerModel propOwner = turnManager?.GetOwnerOfProperty(target);
            if (propOwner != null && IsImmune(propOwner))
            {
                Debug.Log($"[Immunité] SABOTAGE bloqué — Player {propOwner.PlayerId} protégé (Protocole Sanitaire/Golden Ticket).");
                ConsumeGoldenTicketIfUsed(propOwner);
                return;
            }

            if (target.CurrentLevel > 0)
            {
                transactionManager.DeductMoney(attacker, cost);
                target.SetLevel(target.CurrentLevel - 1);
                Debug.Log($"[Marketplace] SABOTAGE executed by Player {attacker.PlayerId} on {target.PropertyName} for {cost}. Level reduced to {target.CurrentLevel}.");
            }
            else
            {
                Debug.LogWarning($"[Marketplace] SABOTAGE failed. Property {target.PropertyName} is already level 0.");
            }
        }

        public bool ExecuteTrade(PlayerModel buyer, PlayerModel seller, PropertyData property, double price)
        {
            // 1. Check if buyer has enough funds
            if (buyer.CurrentBalance < price) {
                UnityEngine.Debug.LogWarning($"[Marketplace] Trade failed. Player {buyer.PlayerId} lacks funds.");
                return false;
            }

            // 2. Execute transaction (deduct from buyer, add to seller)
            // Use your financeSystem.transactionManager here
            if (transactionManager != null)
            {
                transactionManager.TransferMoney(buyer, seller, price);
            }
            else if (financeSystem != null && financeSystem.transactionManager != null)
            {
                financeSystem.transactionManager.TransferMoney(buyer, seller, price);
            }

            // 3. Transfer ownership
            seller.RemoveProperty(property);
            buyer.AddProperty(property);

            UnityEngine.Debug.Log($"[Marketplace] SUCCESS: Player {buyer.PlayerId} bought {property.PropertyName} from Player {seller.PlayerId} for {price}.");
            return true;
        }

        public void BeginTender(PlayerModel seller, PropertyData propertyForSale)
        {
            if (seller == null || propertyForSale == null) return;
            
            if (turnManager == null) turnManager = FindObjectOfType<TurnManager>();
            if (financeSystem == null) financeSystem = FindObjectOfType<FinanceSystem>();
            if (transactionManager == null) transactionManager = TransactionManager.Instance;

            activeBids.Clear();
            CurrentTenderSeller = seller;
            CurrentTenderProperty = propertyForSale;

            SetAuctionState(AuctionState.InvitingPlayers);
            
            // Start AI logic independently 
            StartCoroutine(SimulateAIBidders(propertyForSale));
        }

        public void SkipBid(int playerId)
        {
            if (CurrentAuctionState != AuctionState.InvitingPlayers && CurrentAuctionState != AuctionState.Bidding) return;
            activeBids[playerId] = null; // null signifies a skipped bid
            CheckAuctionProgress();
        }

        public void SubmitBid(int playerId, TenderBid bid)
        {
            if (CurrentAuctionState != AuctionState.InvitingPlayers && CurrentAuctionState != AuctionState.Bidding) return;
            activeBids[playerId] = bid;
            CheckAuctionProgress();
        }

        private void CheckAuctionProgress()
        {
            if (turnManager == null) return;

            int totalExpectedBids = turnManager.Players.Count(p => p != null && !p.IsBankrupt && p.PlayerId != CurrentTenderSeller.PlayerId);

            if (activeBids.Count < totalExpectedBids) return;

            bool anyRealBid = activeBids.Values.Any(b => b != null);

            if (!anyRealBid)
            {
                // Personne n'a enchéri
                SetAuctionState(AuctionState.Reviewing);
                CancelTender();
                return;
            }

            SetAuctionState(AuctionState.Reviewing);

            // Si le vendeur est une IA, elle choisit automatiquement la meilleure offre
            if (CurrentTenderSeller != null && CurrentTenderSeller.IsAI)
            {
                StartCoroutine(AutoSealAfterDelay(1.5f));
            }
            // Si vendeur humain → c'est ShowTenderReview() qui prend le relais (déclenché par l'event)
        }

        private IEnumerator AutoSealAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            AutoSealBestBid();
        }

        private void AutoSealBestBid()
        {
            var bestBid = activeBids.Values
                .Where(b => b != null)
                .OrderByDescending(b => b.cashOffer + (b.propertyOffers != null ? b.propertyOffers.Sum(p => p.BasePrice) : 0))
                .FirstOrDefault();

            if (bestBid != null)
            {
                Debug.Log($"[Marketplace AI] Auto-sealing best bid from Player {bestBid.bidder?.PlayerId} — {CurrencyFormatter.Format(bestBid.cashOffer)} cash + {bestBid.propertyOffers?.Count ?? 0} props.");
                SealTenderDeal(bestBid);
            }
            else
            {
                CancelTender();
            }

            // Terminer le tour — CompleteTileResolution a un guard donc les appels multiples sont sûrs
            if (turnManager != null)
                turnManager.CompleteTileResolution();
        }

        /// <summary>
        /// Phase D — Évaluation rationnelle de la valeur d'une propriété pour une IA.
        /// Retourne la "vraie" valeur stratégique : ROI projeté sur 5 tours,
        /// bonus monopole imminent, modificateurs de cycle.
        /// </summary>
        public double EvaluateBidValue(PlayerModel evaluator, PropertyData property)
        {
            if (property == null) return 0;

            // 1) Loyer estimé reçu sur 5 tours (proxy pour ROI court terme)
            double baseRent = property.BaseRent;
            int[] mults = { 1, 2, 5, 12, 50, 200 };
            int lvl = Mathf.Clamp(property.CurrentLevel, 0, mults.Length - 1);
            double currentRent = baseRent * mults[lvl];

            // Si l'achat complèterait un monopole, on applique le ×2 immédiatement
            bool wouldCompleteMonopoly = CheckMonopolyCompletion(evaluator, property);
            if (wouldCompleteMonopoly) currentRent *= 2;

            double projectedIncome = currentRent * 5; // proxy 5 tours

            // 2) Modificateurs de cycle
            var cm = CycleManager.Instance;
            if (cm != null && cm.IsActive)
            {
                projectedIncome *= cm.GetRentMultiplier();
                projectedIncome *= cm.GetWarFamilyMultiplier(property);
            }

            // 3) Bonus structurel
            double structuralBonus = 0;
            if (wouldCompleteMonopoly) structuralBonus += property.BasePrice * 1.5; // gros bonus monopole
            if (property.FamilyData != null && property.FamilyData.FamilyName.Contains("Logistics"))
                structuralBonus += property.BasePrice * 0.3; // Hubs valorisés (Sgo-indexed)

            // 4) Valeur totale = revenu projeté + bonus − prix d'achat
            return projectedIncome + structuralBonus - property.BasePrice;
        }

        private bool CheckMonopolyCompletion(PlayerModel player, PropertyData property)
        {
            if (property?.FamilyData == null || turnManager == null) return false;
            var board = BoardManager.Instance;
            if (board?.tileDataList == null) return false;

            int total = 0, owned = 0;
            foreach (var td in board.tileDataList)
            {
                if (td is PropertyData p && p.FamilyData == property.FamilyData)
                {
                    total++;
                    if (player.OwnedProperties.Contains(p) || p == property) owned++;
                }
            }
            return total > 0 && owned == total;
        }

        private IEnumerator SimulateAIBidders(PropertyData propertyForSale)
        {
            if (turnManager == null || financeSystem == null) yield break;

            List<PlayerModel> aiCandidates = turnManager.Players
                .Where(p => p != null && !p.IsBankrupt && p.IsAI && p.PlayerId != CurrentTenderSeller.PlayerId)
                .ToList();

            double sgo = financeSystem.CurrentSgo;

            foreach (var ai in aiCandidates)
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(1.0f, 2.5f));

                // ── Phase D : décision basée sur EvaluateBidValue + profil psycho ──
                double bidValue   = EvaluateBidValue(ai, propertyForSale);
                double maxOffer   = System.Math.Min(ai.CurrentBalance, bidValue);
                double skipChance = ProfileSkipChance(ai.Profile, bidValue);

                if (bidValue <= 0 || UnityEngine.Random.value < skipChance)
                {
                    Debug.Log($"[AI Bid] Player {ai.PlayerId} ({ai.Profile}) skip — bidValue={bidValue:N0}, skipChance={skipChance:P0}");
                    SkipBid(ai.PlayerId);
                    continue;
                }

                // Le Negotiator surenchérit (+15%), le Rentier sous-enchérit (-15%)
                double offerRatio = ai.Profile switch
                {
                    PsychoProfile.Negotiator => UnityEngine.Random.Range(0.85f, 1.10f),
                    PsychoProfile.Rentier    => UnityEngine.Random.Range(0.40f, 0.65f),
                    PsychoProfile.Predator   => UnityEngine.Random.Range(0.55f, 0.80f),
                    _                        => UnityEngine.Random.Range(0.50f, 0.85f)
                };
                double cash = System.Math.Min(maxOffer * offerRatio, ai.CurrentBalance);
                // Floor minimum pour rester compétitif
                cash = System.Math.Max(cash, 0.3 * sgo);

                TenderBid bid = new TenderBid { bidder = ai, cashOffer = (int)cash };

                // Le Negotiator inclut plus volontiers des propriétés
                float propChance = ai.Profile == PsychoProfile.Negotiator ? 0.75f : 0.40f;
                if (ai.OwnedProperties != null && ai.OwnedProperties.Count > 0 && UnityEngine.Random.value < propChance)
                {
                    var owned = ai.OwnedProperties.Where(p => p != null && p != propertyForSale).ToList();

                    int propsToOffer = UnityEngine.Random.Range(1, Math.Min(3, owned.Count + 1));
                    for (int i = 0; i < propsToOffer; i++)
                    {
                        var prop = owned[UnityEngine.Random.Range(0, owned.Count)];
                        if (!bid.propertyOffers.Contains(prop))
                        {
                            bid.propertyOffers.Add(prop);
                        }
                    }
                }

                SubmitBid(ai.PlayerId, bid);
            }
        }

        public bool CancelTender()
        {
            if (CurrentAuctionState != AuctionState.Reviewing) return false;

            activeBids.Clear();
            CurrentTenderSeller = null;
            CurrentTenderProperty = null;

            SetAuctionState(AuctionState.Closing);
            Debug.Log("[Marketplace] Tender Cancelled (No participants or rejected manually).");
            return true;
        }

        public bool SealTenderDeal(TenderBid bid)
        {
            if (bid == null || bid.bidder == null) return false;
            if (CurrentTenderSeller == null || CurrentTenderProperty == null) return false;

            if (transactionManager == null) transactionManager = TransactionManager.Instance;
            if (transactionManager == null) return false;

            if (!transactionManager.TransferMoney(bid.bidder, CurrentTenderSeller, bid.cashOffer))
            {
                return false;
            }

            // Transfer property ownership
            CurrentTenderSeller.RemoveProperty(CurrentTenderProperty);
            bid.bidder.AddProperty(CurrentTenderProperty);

            // Refreshed logging for multiple properties
            string propLogs = "";
            foreach (var prop in bid.propertyOffers)
            {
                bid.bidder.RemoveProperty(prop);
                CurrentTenderSeller.AddProperty(prop);
                RefreshTileVisualAfterOwnerChange(prop);
                propLogs += prop.PropertyName + ", ";
            }

            if (bid.propertyOffers.Count > 0)
            {
                Debug.Log($"[Marketplace] Bi-directional swap confirmed: Player {CurrentTenderSeller.PlayerId} received Properties: {propLogs}");
            }

            // Cache for log before nulling
            string sellerLog = CurrentTenderSeller.PlayerId.ToString();
            string tenderLog = CurrentTenderProperty.PropertyName;

            // Refresh 3D board visuals
            RefreshTileVisualAfterOwnerChange(CurrentTenderProperty);

            CurrentTenderSeller = null;
            CurrentTenderProperty = null;
            activeBids.Clear();

            SetAuctionState(AuctionState.Closing);

            Debug.Log($"[Marketplace] DEAL SEALED: {bid.bidder.PlayerId} acquired {tenderLog} from {sellerLog} for {CurrencyFormatter.Format(bid.cashOffer)}.");
            return true;
        }

        private void RefreshTileVisualAfterOwnerChange(PropertyData property)
        {
            if (property == null) return;
            var board = BoardManager.Instance;
            if (board == null || board.visualTiles == null) return;

            foreach (var tile in board.Tiles)
            {
                if (tile == null || tile.Data != property) continue;

                int idx = tile.TileIndex;
                if (idx < 0 || idx >= board.visualTiles.Length) break;

                var go = board.visualTiles[idx];
                if (go == null) break;

                var tv = go.GetComponentInChildren<PolyRisk.Visuals.TileVisuals>();
                if (tv != null)
                {
                    // SetupVisuals refreshes color based on current family/type
                    var boardTile = board.GetTileAt(idx);
                    if (boardTile != null) tv.SetupVisuals(boardTile);
                }
                break;
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // PHASE B — Immunités (Pharma Ultime / Golden Ticket)
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>True si la cible est immunisée contre les sanctions/attaques.</summary>
        public static bool IsImmune(PlayerModel player)
        {
            if (player == null) return false;
            return player.ImmunityTurnsRemaining > 0    // Pharma Ultime — Protocole Sanitaire
                || player.HasImmunityCard                 // Carte Venture Immunité
                || player.HasGoldenTicket;                // Carte Venture #10 Golden Ticket (usage unique)
        }

        /// <summary>Probabilité qu'une IA passe une enchère selon son profil.</summary>
        private static float ProfileSkipChance(PsychoProfile profile, double bidValue)
        {
            // bidValue négatif = mauvaise affaire, on skip à coup sûr (déjà filtré, mais sécurité)
            if (bidValue <= 0) return 1f;
            return profile switch
            {
                PsychoProfile.Rentier    => 0.55f, // refuse souvent les deals risqués
                PsychoProfile.Predator   => 0.40f, // préfère ses sanctions, mais saisit les bonnes affaires
                PsychoProfile.Negotiator => 0.10f, // adore enchérir
                _                        => 0.30f
            };
        }

        /// <summary>Consomme le Golden Ticket s'il a été utilisé pour bloquer une sanction.</summary>
        public static void ConsumeGoldenTicketIfUsed(PlayerModel player)
        {
            if (player == null) return;
            // Le Protocole Sanitaire et HasImmunityCard ont leur propre logique de décrément (TurnManager).
            // Le Golden Ticket est à usage unique : il se consomme à chaque blocage.
            if (player.HasGoldenTicket
                && player.ImmunityTurnsRemaining <= 0
                && !player.HasImmunityCard)
            {
                player.HasGoldenTicket = false;
                Debug.Log($"[Golden Ticket] Player {player.PlayerId} a consommé son Golden Ticket pour bloquer une attaque.");
            }
        }
    }
}
