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
            var propertyTarget = richestOpponent.OwnedProperties.OrderByDescending(p => p.BaseRent).FirstOrDefault();
            var sabotageTarget = richestOpponent.OwnedProperties
                .Where(p => p.CurrentLevel > 0 && (p.FamilyData == null || !p.FamilyData.FamilyName.Contains("Aerospace")))
                .OrderByDescending(p => p.CurrentLevel).FirstOrDefault();

            // Construire les sanctions réellement abordables et applicables, puis en tirer une.
            // Cela évite que Strike masque systématiquement Embargo dans une chaîne de priorités.
            var choices = new System.Collections.Generic.List<int> { 0 }; // 0 = Audit, toujours le fallback à 1×Sgo
            if (attacker.CurrentBalance >= 2.0 * sgo && propertyTarget != null) choices.Add(1); // Strike
            if (attacker.CurrentBalance >= 5.0 * sgo && propertyTarget != null) choices.Add(2); // Embargo
            if (attacker.CurrentBalance >= 5.0 * sgo && sabotageTarget != null) choices.Add(3); // Sabotage

            int choice = choices[UnityEngine.Random.Range(0, choices.Count)];
            switch (choice)
            {
                case 1: marketplaceSystem.ExecuteStrike(attacker, propertyTarget); break;
                case 2: marketplaceSystem.ExecuteEmbargo(attacker, propertyTarget); break;
                case 3: marketplaceSystem.ExecuteSabotage(attacker, sabotageTarget); break;
                default: marketplaceSystem.ExecuteAudit(attacker, richestOpponent); break;
            }
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

        public int ChooseAutoPrecisionDelta(PlayerModel ai, int rolledSteps)
        {
            if (ai == null || turnManager == null || boardManager == null) return 0;
            int position = turnManager.GetPlayerPosition(ai);
            if (position < 0) return 0;

            double Score(int delta)
            {
                var tile = boardManager.GetTileAt((position + rolledSteps + delta + 40) % 40);
                if (tile == null) return 0;
                switch (tile.Type)
                {
                    case TileType.GoToPrison: return -10;
                    case TileType.Tax: return -4;
                    case TileType.Venture: return 2;
                    case TileType.GlobalAuction: return 1.5;
                    case TileType.StockMarket:
                    case TileType.Casino: return 1;
                    case TileType.Property:
                        if (tile.Property == null) return 0;
                        var owner = turnManager.GetOwnerOfProperty(tile.Property);
                        if (owner == null) return ai.CurrentBalance >= tile.Property.BasePrice ? 3 : -1;
                        return owner == ai ? 1 : -2;
                    default: return 0;
                }
            }

            double baseline = Score(0);
            double lower = Score(-1);
            double higher = Score(1);
            if (lower > baseline && lower >= higher) return -1;
            if (higher > baseline) return 1;
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
