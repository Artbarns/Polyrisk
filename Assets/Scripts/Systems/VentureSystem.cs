using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using PolyRisk.Core;

namespace PolyRisk.Systems
{
    /// <summary>
    /// Manages the 'Cruel World 2026' Venture Deck.
    /// Handles deck state (draw/discard piles) and card effect execution.
    /// </summary>
    public class VentureSystem : MonoBehaviour
    {
        [Header("System References")]
        [Tooltip("Required for monetary calculations.")]
        public FinanceSystem financeSystem;
        
        [Tooltip("Required for applying transactions.")]
        public TransactionManager transactionManager;

        [Tooltip("Required for player movement and state.")]
        public TurnManager turnManager;

        [Tooltip("Required for executing tile actions.")]
        public RulesEngine rulesEngine;

        [Tooltip("Required for board data.")]
        public BoardManager boardManager;

        private List<int> drawPile = new List<int>();
        private List<int> discardPile = new List<int>();
        
        private const int MAX_CARDS = 20;

        private void Start()
        {
            InitializeDeck();
        }

        private void Update()
        {
            if (turnManager == null || turnManager.CurrentPlayer == null) return;

            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                int cardId = shift ? 11 : 1;
                Debug.LogWarning($"[DEBUG CHEAT] Forced Venture Card execution: #{cardId}");
                ApplyCardEffect(turnManager.CurrentPlayer, cardId);
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                int cardId = shift ? 12 : 2;
                Debug.LogWarning($"[DEBUG CHEAT] Forced Venture Card execution: #{cardId}");
                ApplyCardEffect(turnManager.CurrentPlayer, cardId);
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                int cardId = shift ? 13 : 3;
                Debug.LogWarning($"[DEBUG CHEAT] Forced Venture Card execution: #{cardId}");
                ApplyCardEffect(turnManager.CurrentPlayer, cardId);
            }
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                int cardId = shift ? 14 : 4;
                Debug.LogWarning($"[DEBUG CHEAT] Forced Venture Card execution: #{cardId}");
                ApplyCardEffect(turnManager.CurrentPlayer, cardId);
            }
            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                int cardId = shift ? 15 : 5;
                Debug.LogWarning($"[DEBUG CHEAT] Forced Venture Card execution: #{cardId}");
                ApplyCardEffect(turnManager.CurrentPlayer, cardId);
            }
            if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                int cardId = shift ? 16 : 6;
                Debug.LogWarning($"[DEBUG CHEAT] Forced Venture Card execution: #{cardId}");
                ApplyCardEffect(turnManager.CurrentPlayer, cardId);
            }
            if (Input.GetKeyDown(KeyCode.Alpha7))
            {
                int cardId = shift ? 17 : 7;
                Debug.LogWarning($"[DEBUG CHEAT] Forced Venture Card execution: #{cardId}");
                ApplyCardEffect(turnManager.CurrentPlayer, cardId);
            }
            if (Input.GetKeyDown(KeyCode.Alpha8))
            {
                int cardId = shift ? 18 : 8;
                Debug.LogWarning($"[DEBUG CHEAT] Forced Venture Card execution: #{cardId}");
                ApplyCardEffect(turnManager.CurrentPlayer, cardId);
            }
            if (Input.GetKeyDown(KeyCode.Alpha9))
            {
                int cardId = shift ? 19 : 9;
                Debug.LogWarning($"[DEBUG CHEAT] Forced Venture Card execution: #{cardId}");
                ApplyCardEffect(turnManager.CurrentPlayer, cardId);
            }
            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                int cardId = shift ? 20 : 10;
                Debug.LogWarning($"[DEBUG CHEAT] Forced Venture Card execution: #{cardId}");
                ApplyCardEffect(turnManager.CurrentPlayer, cardId);
            }
        }

        /// <summary>
        /// Creates the deck with IDs 1 to 20 and shuffles it.
        /// </summary>
        public void InitializeDeck()
        {
            drawPile.Clear();
            discardPile.Clear();

            for (int i = 1; i <= MAX_CARDS; i++)
            {
                drawPile.Add(i);
            }

            ShuffleDeck();
            Debug.Log("Venture Deck initialized and shuffled.");
        }

        private void ShuffleDeck()
        {
            System.Random rng = new System.Random();
            int n = drawPile.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                int value = drawPile[k];
                drawPile[k] = drawPile[n];
                drawPile[n] = value;
            }
        }

        /// <summary>
        /// Draws a card for the player and executes its effect.
        /// Handles reshuffling if the draw pile is empty.
        /// </summary>
        public void DrawCard(PlayerModel player)
        {
            if (player == null) return;

            if (drawPile.Count == 0)
            {
                if (discardPile.Count == 0)
                {
                    Debug.LogWarning("Venture Deck is empty and discard is empty. Re-initializing.");
                    InitializeDeck();
                }
                else
                {
                    Debug.Log("Draw pile empty. Reshuffling discard pile.");
                    drawPile.AddRange(discardPile);
                    discardPile.Clear();
                    ShuffleDeck();
                }
            }

            if (drawPile.Count > 0)
            {
                int cardId = drawPile[0];
                drawPile.RemoveAt(0);
                discardPile.Add(cardId);

                Debug.Log($"Player {player.PlayerId} drew Venture Card #{cardId}");

                // Phase E.2 — Pop-up modale avant l'effet.
                // Humain : on attend confirm. IA : on applique direct (pas d'UI nécessaire).
                var modal = PolyRisk.UI.VentureCardModalController.Instance;
                if (!player.IsAI && modal != null)
                    modal.ShowCard(cardId, () => ApplyCardEffect(player, cardId));
                else
                    ApplyCardEffect(player, cardId);
            }
        }

        /// <summary>
        /// Applies the logic for a specific card ID.
        /// </summary>
        private void ApplyCardEffect(PlayerModel player, int cardId)
        {
            if (financeSystem == null || transactionManager == null || turnManager == null || rulesEngine == null || boardManager == null)
            {
                Debug.LogError("VentureSystem: Missing dependencies (FinanceSystem, TransactionManager, TurnManager, RulesEngine, or BoardManager).");
                return;
            }

            double sgo = financeSystem.CurrentSgo;

            // Liquidity Shield Rule: If cash > 10 * Sgo, penalties are DOUBLED.
            bool hasLiquidityShield = player.CurrentBalance > (10 * sgo);

            // ── Phase B : Immunité (Pharma Protocole Sanitaire / Golden Ticket) ─────
            // Bloque uniquement les cartes négatives (11-18). Les positives, Chaos et OPA passent.
            bool isNegativeCard = cardId >= 11 && cardId <= 18;
            if (isNegativeCard && MarketplaceSystem.IsImmune(player))
            {
                Debug.Log($"[Immunité Venture] Player {player.PlayerId} bloque la carte négative #{cardId}.");
                MarketplaceSystem.ConsumeGoldenTicketIfUsed(player);
                turnManager.CompleteTileResolution();
                return;
            }

            // Passif Pharma — Optimisation Fiscale : amendes Venture négatives réduites de 25%
            double pharmaCutFactor = (isNegativeCard && PharmaPassiveCheck(player)) ? 0.75 : 1.0;
            if (pharmaCutFactor < 1.0)
                Debug.Log($"[Passif Pharma] Optimisation Fiscale : amende Venture #{cardId} -25%");

            switch (cardId)
            {
                case 1: // Héritage Offshore
                    double gain1 = 5 * sgo;
                    transactionManager.AddMoney(player, gain1);
                    Debug.Log($"[Venture Card #1] Héritage Offshore: Player {player.PlayerId} received {gain1} ({5} x Sgo).");
                    turnManager.CompleteTileResolution();
                    break;

                case 2: // Kidnapping Express
                    int randomIndex = Random.Range(0, 40);
                    Debug.Log($"Kidnapping Express! Player {player.PlayerId} is teleported to tile {randomIndex}.");
                    turnManager.TeleportPlayer(player, randomIndex);
                    // IMPORTANT: Execute the tile's action.
                    rulesEngine.ExecutePrimaryAction(player, boardManager.GetTileAt(randomIndex));
                    break;

                case 3: // Arrestation Arbitraire
                    Debug.Log($"Arrestation Arbitraire! Player {player.PlayerId} goes straight to Audit (Tile 30).");
                    turnManager.TeleportPlayer(player, 30);
                    // Trigger the Audit logic (Tile 30 sends to Tile 10 and sets Audit state)
                    rulesEngine.ExecutePrimaryAction(player, boardManager.GetTileAt(30));
                    break;

                case 4: // Cyberattack
                    var upgradedProperties = player.OwnedProperties.Where(p => p.CurrentLevel > 0).ToList();
                    if (upgradedProperties.Count == 0)
                    {
                        Debug.Log("Cyberattack failed: No upgraded properties.");
                    }
                    else
                    {
                        var property = upgradedProperties[Random.Range(0, upgradedProperties.Count)];
                        property.SetLevel(property.CurrentLevel - 1);
                        Debug.Log($"Cyberattack! {property.PropertyName} was downgraded to level {property.CurrentLevel}.");
                    }
                    turnManager.CompleteTileResolution();
                    break;

                case 5: // Bull Market
                    financeSystem.CurrentSgo *= 1.20;
                    financeSystem.CurrentMarketState = "BULL";
                    Debug.Log($"Bull Market! Global Sgo permanently increased to {financeSystem.CurrentSgo}.");
                    turnManager.CompleteTileResolution();
                    break;

                case 6: // Government Bailout
                    // If poor, get help
                    if (player.CurrentBalance < (1 * sgo))
                    {
                        double gain6 = 5 * sgo;
                        transactionManager.AddMoney(player, gain6);
                        Debug.Log($"[Venture Card #6] Government Bailout: Player {player.PlayerId} is in crisis! Received {gain6}.");
                    }
                    else
                    {
                        Debug.Log($"[Venture Card #6] Government Bailout: Player {player.PlayerId} is too rich for bailout. No effect.");
                    }
                    turnManager.CompleteTileResolution();
                    break;

                case 7: // Moon Mining IPO
                    double gain7 = 0;
                    foreach (var prop in player.OwnedProperties)
                    {
                        if (prop.FamilyData != null && (prop.FamilyData.FamilyName.Contains("Aerospace") || prop.FamilyData.FamilyName.Contains("Tech")))
                        {
                            gain7 += 0.2 * sgo;
                        }
                    }
                    if (gain7 > 0)
                    {
                        transactionManager.AddMoney(player, gain7);
                        Debug.Log($"[Venture Card #7] Moon Mining IPO: Player {player.PlayerId} received {gain7}.");
                    }
                    else
                    {
                        Debug.Log($"[Venture Card #7] Moon Mining IPO: Player {player.PlayerId} has no Aerospace/Tech properties. No gain.");
                    }
                    turnManager.CompleteTileResolution();
                    break;

                case 8: // Don de Sang de Stagiaire
                    player.HasGetOutOfAuditCard = true;
                    Debug.Log($"[Venture Card #8] Don de Sang de Stagiaire: Player {player.PlayerId} received a Get Out Of Audit Free card.");
                    turnManager.CompleteTileResolution();
                    break;

                case 9: // Quantitative Easing
                    financeSystem.CurrentSgo *= 1.05;
                    Debug.Log($"[Venture Card #9] Quantitative Easing: Global Sgo increased by 5% to {financeSystem.CurrentSgo}.");
                    turnManager.CompleteTileResolution();
                    break;

                case 10: // Golden Ticket
                    // Fix #5 Phase F — retirer HasImmunityCard : IsImmune() teste déjà HasGoldenTicket,
                    // et sans ImmunityTurnsRemaining, ConsumeGoldenTicketIfUsed ne consommait jamais le ticket.
                    // Ticket à usage unique consommé par MarketplaceSystem.ConsumeGoldenTicketIfUsed.
                    player.HasGoldenTicket = true;
                    Debug.Log($"[Venture Card #10] Golden Ticket: Player {player.PlayerId} received a Golden Ticket (Cancel next Hostile Sanction).");
                    turnManager.CompleteTileResolution();
                    break;

                case 11: // Epstein's Flight Logs (+ déclencheur Cycle WAR, GDD_Economy §7)
                    if (CycleManager.Instance != null)
                    {
                        CycleManager.Instance.StartCycle(CycleType.WAR, 5);
                        Debug.Log("[Venture #11] Cycle WAR déclenché (5 tours) : Armement ×2, Luxe ×0.5, Hubs loyer 0.");
                    }
                    double penalty11 = 3 * sgo;
                    if (hasLiquidityShield)
                    {
                        penalty11 *= 2;
                        Debug.Log("<color=red>LIQUIDITY SHIELD TRIGGERED!</color> Penalty doubled for being too rich.");
                    }
                    penalty11 *= pharmaCutFactor;

                    bool paid11 = transactionManager.DeductMoney(player, penalty11);
                    Debug.Log($"[Venture Card #11] Epstein's Flight Logs: Player {player.PlayerId} fined {penalty11}. Paid: {paid11}");
                    turnManager.CompleteTileResolution();
                    break;

                case 12: // The Messy Divorce
                    double penalty12 = player.CurrentBalance * 0.20 * pharmaCutFactor;
                    // Liquidity shield usually applies to fixed fines, but GDD says "monetary penalties".
                    // However, prompt specified the logic for Case 12: "Deduct player.CurrentBalance * 0.20".
                    // It did NOT explicitly ask to double this one in the Case 12 description.
                    
                    bool paid12 = transactionManager.DeductMoney(player, penalty12);
                    Debug.Log($"[Venture Card #12] The Messy Divorce: Player {player.PlayerId} lost 20% of assets ({penalty12}). Paid: {paid12}");
                    turnManager.CompleteTileResolution();
                    break;

                case 13: // Data Breach
                    double penalty13 = 0.5 * sgo;
                    if (hasLiquidityShield)
                    {
                        penalty13 *= 2;
                        Debug.Log("<color=red>LIQUIDITY SHIELD TRIGGERED!</color> Penalty doubled for being too rich.");
                    }
                    penalty13 *= pharmaCutFactor;

                    foreach (var otherPlayer in turnManager.Players)
                    {
                        if (otherPlayer.PlayerId != player.PlayerId && !otherPlayer.IsBankrupt)
                        {
                            transactionManager.TransferMoney(player, otherPlayer, penalty13);
                            Debug.Log($"[Venture Card #13] Data Breach: Player {player.PlayerId} paid {penalty13} to Player {otherPlayer.PlayerId}.");
                        }
                    }
                    turnManager.CompleteTileResolution();
                    break;

                case 14: // Ransomware Attack
                    // "Pay 3x Sgo OR disable Tech revenues". Implementing payment for now.
                    double penalty14 = 3 * sgo;
                    if (hasLiquidityShield)
                    {
                        penalty14 *= 2;
                        Debug.Log("<color=red>LIQUIDITY SHIELD TRIGGERED!</color> Penalty doubled for being too rich.");
                    }
                    penalty14 *= pharmaCutFactor;
                    transactionManager.DeductMoney(player, penalty14);
                    Debug.Log($"[Venture Card #14] Ransomware Attack: Player {player.PlayerId} paid ransom of {penalty14}.");
                    turnManager.CompleteTileResolution();
                    break;

                case 15: // Water Scarcity Act
                    player.DoubleBurnRateNextGo = true;
                    Debug.Log($"[Venture Card #15] Water Scarcity Act: Player {player.PlayerId}'s Burn Rate will be DOUBLED next GO.");
                    turnManager.CompleteTileResolution();
                    break;

                case 16: // Scandale de la Petite Île
                    Debug.Log($"[Venture Card #16] Scandale de la Petite Île: Player {player.PlayerId} goes straight to Audit Fiscal (Tile 10).");
                    turnManager.TeleportPlayer(player, 10);
                    player.IsInAudit = true;
                    turnManager.CompleteTileResolution();
                    break;

                case 17: // Abonnement Oxygène Premium
                    int totalLevels = 0;
                    foreach (var prop in player.OwnedProperties)
                    {
                        totalLevels += prop.CurrentLevel;
                    }
                    double penalty17 = totalLevels * 0.1 * sgo;
                    if (hasLiquidityShield)
                    {
                        penalty17 *= 2;
                        Debug.Log("<color=red>LIQUIDITY SHIELD TRIGGERED!</color> Penalty doubled for being too rich.");
                    }
                    penalty17 *= pharmaCutFactor;
                    transactionManager.DeductMoney(player, penalty17);
                    Debug.Log($"[Venture Card #17] Abonnement Oxygène Premium: Player {player.PlayerId} paid {penalty17} ({totalLevels} levels * 0.1 * Sgo).");
                    turnManager.CompleteTileResolution();
                    break;

                case 18: // Marée Noire Instagrammable
                    player.OilGasPenaltyTurns = 3;
                    Debug.Log($"[Venture Card #18] Marée Noire Instagrammable: Player {player.PlayerId}'s Oil & Gas revenues halved for 3 turns.");
                    turnManager.CompleteTileResolution();
                    break;

                case 19: // OPA Hostile
                    List<PropertyData> potentialTargets19 = new List<PropertyData>();
                    foreach (var otherPlayer in turnManager.Players)
                    {
                        if (otherPlayer.PlayerId != player.PlayerId && !otherPlayer.IsBankrupt)
                        {
                            foreach (var prop in otherPlayer.OwnedProperties)
                            {
                                potentialTargets19.Add(prop);
                            }
                        }
                    }

                    if (potentialTargets19.Count > 0)
                    {
                        PropertyData targetProp = potentialTargets19[Random.Range(0, potentialTargets19.Count)];
                        
                        targetProp.HijackedBy = player;
                        targetProp.HijackTurnsRemaining = 1; 
                        
                        Debug.Log($"[Venture Card #19] OPA Hostile: Player {player.PlayerId} HIJACKED {targetProp.PropertyName} for 1 turn!");
                    }
                    else
                    {
                        Debug.Log("[Venture Card #19] OPA Hostile: No valid targets found.");
                    }
                    turnManager.CompleteTileResolution();
                    break;

                case 20: // Effondrement Global (BOOM ou CRASH, GDD_Economy §7)
                    bool isCrash = Random.value < 0.5f;
                    CycleType triggered = isCrash ? CycleType.CRASH : CycleType.BOOM;

                    if (CycleManager.Instance != null)
                    {
                        CycleManager.Instance.StartCycle(triggered, 5);
                    }

                    if (isCrash)
                        Debug.Log("[Venture #20] Trump's Golden OD → CRASH CYCLE (5 tours) : revenus ×0.6, Stock Market volatil.");
                    else
                        Debug.Log("[Venture #20] AGI Event → BOOM CYCLE (5 tours) : revenus ×1.3, Burn Rate ×1.2.");

                    turnManager.CompleteTileResolution();
                    break;

                default:
                    Debug.Log($"[Venture Card #{cardId}] Effect not implemented yet. (Placeholder)");
                    turnManager.CompleteTileResolution();
                    break;
            }
        }

        /// <summary>Helper local : true si le joueur a le monopole Pharma complet.</summary>
        private bool PharmaPassiveCheck(PlayerModel player)
        {
            if (player == null || boardManager?.tileDataList == null) return false;
            int total = 0, owned = 0;
            foreach (var td in boardManager.tileDataList)
            {
                if (td is PropertyData p && p.FamilyData != null && p.FamilyData.FamilyName.Contains("Pharma"))
                {
                    total++;
                    if (player.OwnedProperties.Contains(p)) owned++;
                }
            }
            return total > 0 && owned == total;
        }
    }
}
