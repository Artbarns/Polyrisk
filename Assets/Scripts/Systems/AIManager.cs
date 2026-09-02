using System.Collections;
using System.Linq;
using UnityEngine;
using PolyRisk.Core;

namespace PolyRisk.Systems
{
    /// <summary>
    /// Phase D — Cerveau des IA. Gère :
    ///   • La Phase Planning pré-roll (Ultimes + Auto Precision)
    ///   • Les décisions sur Case 20 selon le profil psycho (Tender vs Sanction)
    ///   • Le fallback de fin de tour
    /// </summary>
    public class AIManager : MonoBehaviour
    {
        [Header("System References")]
        public RulesEngine rulesEngine;
        public TurnManager turnManager;
        public BoardManager boardManager;
        public MarketplaceSystem marketplaceSystem;
        public AbilitySystem abilitySystem;

        private void Start()
        {
            if (rulesEngine == null)       rulesEngine       = FindObjectOfType<RulesEngine>();
            if (turnManager == null)       turnManager       = FindObjectOfType<TurnManager>();
            if (boardManager == null)      boardManager      = BoardManager.Instance ?? FindObjectOfType<BoardManager>();
            if (marketplaceSystem == null) marketplaceSystem = FindObjectOfType<MarketplaceSystem>();
            if (abilitySystem == null)     abilitySystem     = AbilitySystem.Instance ?? FindObjectOfType<AbilitySystem>();
        }

        // ─────────────────────────────────────────────────────────────────────────
        // PHASE PLANNING (pré-roll)
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Décide quoi activer en phase Planning. Appelé depuis TurnManager.HandleTurnState.
        /// Active Ultime si pertinent + pose le delta Auto Precision si pertinent.
        /// </summary>
        public IEnumerator HandlePlanningPhase(PlayerModel ai)
        {
            if (ai == null || !ai.IsAI) yield break;

            Debug.Log($"[AI Planning] Player {ai.PlayerId} ({ai.Profile}) — energy={(abilitySystem != null ? abilitySystem.GetEnergy(ai) : 0):F0}%");

            // 1) Activer un Ultime si l'IA a 100% d'énergie (déclencheur trivial Phase D)
            if (abilitySystem != null && abilitySystem.HasEnoughEnergy(ai))
            {
                TryActivateBestUltimate(ai);
                yield return new WaitForSeconds(0.5f);
            }

            // 2) Auto Precision : si la prochaine case fortement loyée se trouve à ±1 case
            //    du jet moyen attendu (7), on tente d'ajuster. Heuristique simple.
            if (ai.AutoPrecisionAvailable && HasMonopoly(ai, "Automobile"))
            {
                int delta = PickAutoPrecisionDelta(ai);
                if (delta != 0 && turnManager != null)
                    turnManager.UseAutoPrecision(ai, delta);
            }

            yield return new WaitForSeconds(0.5f);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // RÉSOLUTION DE TUILE (ancien ProcessAITurn — case 20 enrichie)
        // ─────────────────────────────────────────────────────────────────────────

        public void RequestAIDecision(PlayerModel aiPlayer, BoardTile currentTile)
        {
            StartCoroutine(ProcessAITurn(aiPlayer, currentTile));
        }

        private IEnumerator ProcessAITurn(PlayerModel aiPlayer, BoardTile currentTile)
        {
            Debug.Log($"[AI] Player {aiPlayer.PlayerId} ({aiPlayer.Profile}) is thinking...");
            yield return new WaitForSeconds(1.5f);

            // Case 20 Marketplace : décision spécifique selon profil psychologique
            if (currentTile != null && currentTile.Type == TileType.GlobalAuction)
            {
                yield return StartCoroutine(HandleMarketplaceDecision(aiPlayer));
                yield break;
            }

            // Cas standard : passe par le rules engine (achat / loyer / etc.)
            if (rulesEngine != null)
                rulesEngine.ExecutePrimaryAction(aiPlayer, currentTile);

            // FALLBACK SAFETY — ne pas terminer si un Tender est en cours
            bool tenderActive = marketplaceSystem != null
                && marketplaceSystem.CurrentAuctionState != MarketplaceSystem.AuctionState.None
                && marketplaceSystem.CurrentAuctionState != MarketplaceSystem.AuctionState.Closing;

            if (!tenderActive && turnManager != null && turnManager.CurrentPlayer == aiPlayer)
            {
                Debug.Log($"[AI] Player {aiPlayer.PlayerId} action done. Completing tile resolution.");
                turnManager.CompleteTileResolution();
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // DÉCISION CASE 20 — Tender vs Sanction selon profil
        // ─────────────────────────────────────────────────────────────────────────

        private IEnumerator HandleMarketplaceDecision(PlayerModel ai)
        {
            if (marketplaceSystem == null || turnManager == null)
            {
                turnManager?.CompleteTileResolution();
                yield break;
            }

            // RÈGLE GDD : les Sanctions sont une action exclusive de la case 20 — pour tous les joueurs.
            // L'IA est physiquement sur la case 20, donc elle peut en lancer.
            bool prefersSanction = ShouldPreferSanction(ai);

            if (prefersSanction && ai.CurrentBalance >= 1.0 * GetSgo())
            {
                LaunchSanction(ai);
                yield return new WaitForSeconds(0.3f);
                turnManager.CompleteTileResolution();
                yield break;
            }

            // Sinon : Tender (IA met une propriété en vente)
            if (ai.OwnedProperties.Count > 0)
            {
                var propToSell = PickPropertyToSell(ai);
                if (propToSell != null)
                {
                    Debug.Log($"[AI Marketplace] Player {ai.PlayerId} ({ai.Profile}) starts Tender for {propToSell.PropertyName}.");
                    marketplaceSystem.BeginTender(ai, propToSell);
                    // Ne PAS appeler CompleteTileResolution — AutoSealBestBid le fera
                    yield break;
                }
            }

            // Fallback : pas de propriété à vendre et pas envie de sanctions → pass
            turnManager.CompleteTileResolution();
        }

        /// <summary>
        /// Predator : 100% Sanction (règle GDD précisée par chef de projet).
        /// Negotiator : 0% Sanction (négocie, n'attaque pas).
        /// Rentier : 30% Sanction (défensif mais peut frapper si menacé).
        /// None : 50/50.
        /// </summary>
        private bool ShouldPreferSanction(PlayerModel ai)
        {
            return ai.Profile switch
            {
                PsychoProfile.Predator   => true,                          // toujours
                PsychoProfile.Negotiator => false,                         // jamais
                PsychoProfile.Rentier    => UnityEngine.Random.value < 0.30f,
                _                        => UnityEngine.Random.value < 0.50f
            };
        }

        private void LaunchSanction(PlayerModel attacker)
        {
            // Cible : le joueur le plus riche (en cash)
            var richestOpponent = turnManager.Players
                .Where(p => p != null && !p.IsBankrupt && p.PlayerId != attacker.PlayerId)
                .OrderByDescending(p => p.CurrentBalance)
                .FirstOrDefault();

            if (richestOpponent == null) return;

            double sgo = GetSgo();
            // Choix sanction selon budget : Audit (cheap) → Embargo → Strike → Sabotage
            if (attacker.CurrentBalance >= 5.0 * sgo)
            {
                var target = richestOpponent.OwnedProperties
                    .Where(p => p.CurrentLevel > 0 && (p.FamilyData == null || !p.FamilyData.FamilyName.Contains("Aerospace")))
                    .OrderByDescending(p => p.CurrentLevel).FirstOrDefault();
                if (target != null) { marketplaceSystem.ExecuteSabotage(attacker, target); return; }
            }
            if (attacker.CurrentBalance >= 2.0 * sgo)
            {
                var target = richestOpponent.OwnedProperties.OrderByDescending(p => p.BaseRent).FirstOrDefault();
                if (target != null) { marketplaceSystem.ExecuteStrike(attacker, target); return; }
            }
            if (attacker.CurrentBalance >= 1.5 * sgo)
            {
                var target = richestOpponent.OwnedProperties.OrderByDescending(p => p.BaseRent).FirstOrDefault();
                if (target != null) { marketplaceSystem.ExecuteEmbargo(attacker, target); return; }
            }
            // Fallback : Targeted Audit (1×Sgo, cible le joueur directement)
            marketplaceSystem.ExecuteAudit(attacker, richestOpponent);
        }

        /// <summary>
        /// Rentier : vend ses Tier 1 (faibles) pour racheter du Tier 3.
        /// Negotiator : vend n'importe quoi avec prix de réserve.
        /// Predator : vend rarement (préfère Sanctions). Si Tender → la moins rentable.
        /// </summary>
        private PropertyData PickPropertyToSell(PlayerModel ai)
        {
            var sellable = ai.OwnedProperties.Where(p => p != null).ToList();
            if (sellable.Count == 0) return null;

            return ai.Profile switch
            {
                PsychoProfile.Rentier    => sellable.OrderBy(p => p.BasePrice).FirstOrDefault(),
                PsychoProfile.Negotiator => sellable[UnityEngine.Random.Range(0, sellable.Count)],
                PsychoProfile.Predator   => sellable.OrderBy(p => p.BaseRent).FirstOrDefault(),
                _                        => sellable[UnityEngine.Random.Range(0, sellable.Count)]
            };
        }

        // ─────────────────────────────────────────────────────────────────────────
        // ULTIMES — sélection selon famille avec monopole
        // ─────────────────────────────────────────────────────────────────────────

        private void TryActivateBestUltimate(PlayerModel ai)
        {
            if (abilitySystem == null) return;

            // Priorité IA : Tech > Armement > Real Estate > Pharma > Luxe > Oil&Gas > Aerospace > Auto
            string[] priority = { "Tech", "Armement", "Real Estate", "Pharma", "Luxe", "Oil", "Aerospace", "Automobile" };
            foreach (var fam in priority)
            {
                if (!HasMonopoly(ai, fam)) continue;

                bool activated = fam switch
                {
                    "Tech"        => abilitySystem.Tech_MaJSysteme(ai),
                    "Armement"    => abilitySystem.Armement_CyberSabotage(ai),
                    "Real Estate" => abilitySystem.RealEstate_RappelDividendes(ai),
                    "Pharma"      => abilitySystem.Pharma_ProtocoleSanitaire(ai),
                    "Luxe"        => abilitySystem.Luxe_TaxeSomptuaire(ai),
                    "Oil"         => abilitySystem.OilGas_BoomEnergetique(ai),
                    "Aerospace"   => abilitySystem.Aerospace_SautQuantique(ai),
                    "Automobile"  => abilitySystem.Automobile_MoteurTurbo(ai),
                    _             => false
                };

                if (activated)
                {
                    Debug.Log($"[AI Ultimate] Player {ai.PlayerId} activated {fam} ultimate.");
                    return;
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // HEURISTIQUE AUTO PRECISION (placeholder simple)
        // ─────────────────────────────────────────────────────────────────────────

        private int PickAutoPrecisionDelta(PlayerModel ai)
        {
            // Stratégie minimale : ajuster pour ÉVITER une case dangereuse (Audit/Direction Audit/Tax)
            // ou ATTEINDRE une case Venture/GO si elle est à +7 ou +8 cases.
            // Phase D : heuristique simple. À enrichir Phase F.
            // Pour l'instant : delta nul (ne pas perturber). On posera des bons cas en QA.
            return 0;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────────────

        private bool HasMonopoly(PlayerModel player, string familyNameContains)
        {
            if (boardManager?.tileDataList == null) return false;
            int total = 0, owned = 0;
            foreach (var td in boardManager.tileDataList)
            {
                if (td is PropertyData p && p.FamilyData != null && p.FamilyData.FamilyName.Contains(familyNameContains))
                {
                    total++;
                    if (player.OwnedProperties.Contains(p)) owned++;
                }
            }
            return total > 0 && owned == total;
        }

        private double GetSgo()
        {
            var fs = FindObjectOfType<FinanceSystem>();
            return fs != null ? fs.CurrentSgo : 2000;
        }
    }
}
