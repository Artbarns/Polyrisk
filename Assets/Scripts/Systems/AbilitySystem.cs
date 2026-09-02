using System.Linq;
using UnityEngine;
using PolyRisk.Core;

namespace PolyRisk.Systems
{
    /// <summary>
    /// Gère l'énergie des familles et l'exécution des 8 Ultimes — Phase B.
    /// Charge par passage GO : T1 +100% | T2 +50% | T3 +33%.
    /// Coût d'activation : 100% pour tous.
    /// </summary>
    public class AbilitySystem : MonoBehaviour
    {
        public static AbilitySystem Instance { get; private set; }

        [Header("References")]
        public TransactionManager transactionManager;
        public TurnManager turnManager;
        public BoardManager boardManager;
        public FinanceSystem financeSystem;
        public UpgradeSystem upgradeSystem;

        // Énergie par joueur (0–100%)
        private readonly System.Collections.Generic.Dictionary<int, float> playerEnergy
            = new System.Collections.Generic.Dictionary<int, float>();

        private void Awake()
        {
            if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
            else { Destroy(gameObject); return; }

            if (transactionManager == null) transactionManager = TransactionManager.Instance;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // ÉNERGIE
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Recharge l'énergie selon le Tier de la famille dominante du joueur.
        /// Appelé à chaque passage GO depuis TurnManager.
        /// </summary>
        public void ChargeEnergyOnGo(PlayerModel player)
        {
            if (player == null || boardManager == null) return;

            // Trouver la famille avec monopole la plus puissante (Tier le plus élevé)
            int highestTier = 0;
            foreach (var prop in player.OwnedProperties)
            {
                if (prop.FamilyData == null) continue;
                if (!HasFullFamilyByName(player, prop.FamilyData.FamilyName)) continue;

                int tier = GetTierForRank(prop.FamilyData.PowerRank);
                if (tier > highestTier) highestTier = tier;
            }

            float charge = highestTier switch
            {
                1 => 100f,   // T1 : ultime disponible chaque GO
                2 => 50f,    // T2 : tous les 2 GO
                3 => 33.3f,  // T3 : tous les 3 GO
                _ => 0f
            };

            if (charge > 0) AddEnergy(player, charge);
        }

        public bool HasEnoughEnergy(PlayerModel player, float required = 100f)
            => playerEnergy.TryGetValue(player.PlayerId, out float e) && e >= required;

        public float GetEnergy(PlayerModel player)
            => playerEnergy.TryGetValue(player.PlayerId, out float e) ? e : 0f;

        public void AddEnergy(PlayerModel player, float amount)
        {
            if (!playerEnergy.ContainsKey(player.PlayerId))
                playerEnergy[player.PlayerId] = 0f;
            playerEnergy[player.PlayerId] = Mathf.Min(100f, playerEnergy[player.PlayerId] + amount);
        }

        public void ConsumeEnergy(PlayerModel player, float amount = 100f)
        {
            if (playerEnergy.ContainsKey(player.PlayerId))
                playerEnergy[player.PlayerId] = Mathf.Max(0f, playerEnergy[player.PlayerId] - amount);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // GUARD COMMUN
        // ─────────────────────────────────────────────────────────────────────────

        private bool CanActivate(PlayerModel player, string familyName)
        {
            if (player == null) return false;
            if (!HasEnoughEnergy(player))
            {
                Debug.LogWarning($"[Ultimate] Player {player.PlayerId} — not enough energy ({GetEnergy(player):F0}%).");
                return false;
            }
            if (!HasFullFamilyByName(player, familyName))
            {
                Debug.LogWarning($"[Ultimate] Player {player.PlayerId} — no {familyName} monopoly.");
                return false;
            }
            return true;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // TIER 1 — FONDATIONS
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Real Estate Ultime — Rappel de Dividendes.
        /// Collecte immédiate de 25% des loyers théoriques.
        /// Monopole actif → base × mult × 2 × 25% ; sans → base × mult × 25%.
        /// </summary>
        public bool RealEstate_RappelDividendes(PlayerModel player)
        {
            if (!CanActivate(player, "Real Estate")) return false;

            int[] mults = { 1, 2, 5, 12, 50, 200 };
            double total = 0;

            foreach (var prop in player.OwnedProperties)
            {
                if (prop.FamilyData == null || prop.FamilyData.FamilyName.Contains("Logistics")) continue;
                int lvl = Mathf.Clamp(prop.CurrentLevel, 0, mults.Length - 1);
                bool hasMonopoly = HasFullFamilyByName(player, prop.FamilyData.FamilyName);
                double theoreticalRent = prop.BaseRent * mults[lvl] * (hasMonopoly ? 2.0 : 1.0);
                total += theoreticalRent * 0.25;
            }

            ConsumeEnergy(player);
            transactionManager.AddMoney(player, total);
            Debug.Log($"[Ultimate] Real Estate Rappel Dividendes → +${total:N0} pour Player {player.PlayerId}");
            return true;
        }

        /// <summary>
        /// Pharma Ultime — Protocole Sanitaire.
        /// Immunité totale aux événements négatifs et sanctions pendant 1 tour.
        /// </summary>
        public bool Pharma_ProtocoleSanitaire(PlayerModel player)
        {
            if (!CanActivate(player, "Pharma")) return false;

            ConsumeEnergy(player);
            player.HasImmunityCard = true;
            player.ImmunityTurnsRemaining = 1;
            Debug.Log($"[Ultimate] Pharma Protocole Sanitaire — Player {player.PlayerId} immunisé 1 tour.");
            return true;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // TIER 2 — FLUX ET PRESSION
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Automobile Ultime — Moteur Turbo.
        /// Relancer les dés ce tour et choisir le meilleur résultat.
        /// Le flag AutoTurboReady est consommé dans TurnManager au prochain lancer.
        /// </summary>
        public bool Automobile_MoteurTurbo(PlayerModel player)
        {
            if (!CanActivate(player, "Automobile")) return false;

            ConsumeEnergy(player);
            player.AutoTurboReady = true;
            Debug.Log($"[Ultimate] Automobile Moteur Turbo activé — Player {player.PlayerId} peut reroller.");
            return true;
        }

        /// <summary>
        /// Oil & Gas Ultime — Boom Énergétique.
        /// Revenus de la famille Oil & Gas boostés de 80% pendant 1 tour.
        /// </summary>
        public bool OilGas_BoomEnergetique(PlayerModel player)
        {
            if (!CanActivate(player, "Oil")) return false;

            ConsumeEnergy(player);
            player.OilGasBoostTurnsRemaining = 1;
            Debug.Log($"[Ultimate] Oil&Gas Boom Énergétique — Player {player.PlayerId} : +80% loyers Oil&Gas ce tour.");
            return true;
        }

        /// <summary>
        /// Luxe Ultime — Taxe Somptuaire.
        /// Chaque adversaire verse 1% de son cash. Total plafonné à 8×Sgo.
        /// </summary>
        public bool Luxe_TaxeSomptuaire(PlayerModel player)
        {
            if (!CanActivate(player, "Luxe")) return false;
            if (turnManager == null || financeSystem == null) return false;

            double cap = 8.0 * financeSystem.CurrentSgo;
            double totalCollected = 0;

            foreach (var opponent in turnManager.Players)
            {
                if (opponent == null || opponent.IsBankrupt || opponent.PlayerId == player.PlayerId) continue;
                if (totalCollected >= cap) break;

                double levy = opponent.CurrentBalance * 0.01;
                double remaining = cap - totalCollected;
                levy = System.Math.Min(levy, remaining);

                if (transactionManager.TransferMoney(opponent, player, levy))
                    totalCollected += levy;
            }

            ConsumeEnergy(player);
            Debug.Log($"[Ultimate] Luxe Taxe Somptuaire — Player {player.PlayerId} perçoit ${totalCollected:N0} (cap ${cap:N0}).");
            return true;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // TIER 3 — DOMINATION
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Tech Ultime — Mise à Jour Système.
        /// Upgrade gratuit (1 niveau) sur les 2 propriétés Tech les moins upgradées.
        /// Pour les humains, même logique — un picker UI sera ajouté en Phase E.
        /// </summary>
        public bool Tech_MaJSysteme(PlayerModel player)
        {
            if (!CanActivate(player, "Tech")) return false;

            var techProps = player.OwnedProperties
                .Where(p => p.FamilyData != null && p.FamilyData.FamilyName.Contains("Tech") && p.CurrentLevel < 5)
                .OrderBy(p => p.CurrentLevel)
                .Take(2)
                .ToList();

            if (techProps.Count == 0)
            {
                Debug.LogWarning($"[Ultimate] Tech MàJ Système — aucune propriété Tech upgradable pour Player {player.PlayerId}.");
                return false;
            }

            ConsumeEnergy(player);
            foreach (var prop in techProps)
            {
                int before = prop.CurrentLevel;
                prop.SetLevel(before + 1);
                Debug.Log($"[Ultimate] Tech MàJ : {prop.PropertyName} Lvl{before} → Lvl{prop.CurrentLevel} (gratuit)");
            }
            return true;
        }

        /// <summary>
        /// Aerospace Ultime — Saut Quantique.
        /// Téléporte le pion vers la tile Aero/Tech/Hub/Marketplace la plus stratégique.
        /// Pour l'humain : auto-sélectionne le Hub ou la Marketplace la plus proche.
        /// TODO Phase E : UI picker pour choisir la destination librement.
        /// </summary>
        public bool Aerospace_SautQuantique(PlayerModel player)
        {
            if (!CanActivate(player, "Aerospace")) return false;
            if (turnManager == null || boardManager == null) return false;

            // Trouver les tiles éligibles : Aero, Tech, Hub (Logistics), Marketplace (case 20)
            int bestIndex = -1;
            double bestValue = -1;

            foreach (var tile in boardManager.Tiles)
            {
                if (tile == null) continue;

                bool eligible = false;
                double value = 0;

                if (tile.Type == TileType.Property && tile.Property?.FamilyData != null)
                {
                    string fn = tile.Property.FamilyData.FamilyName;
                    if (fn.Contains("Aerospace") || fn.Contains("Tech") || fn.Contains("Logistics"))
                    {
                        eligible = true;
                        value = tile.Property.BasePrice;
                    }
                }
                else if (tile.Type == TileType.GlobalAuction)
                {
                    eligible = true;
                    value = 999999; // Marketplace en priorité absolue si aucune propriété valide
                }

                if (eligible && value > bestValue)
                {
                    bestValue = value;
                    bestIndex = tile.TileIndex;
                }
            }

            if (bestIndex < 0) return false;

            ConsumeEnergy(player);
            // Saut Quantique ne collecte PAS le Sgo si case 0 traversée
            turnManager.TeleportPlayer(player, bestIndex);
            Debug.Log($"[Ultimate] Aerospace Saut Quantique — Player {player.PlayerId} → Tile {bestIndex}");
            return true;
        }

        /// <summary>
        /// Armement Ultime — Cyber-Sabotage.
        /// Vole 25% des loyers de la propriété adverse la plus rentable pendant 3 tours.
        /// (L'interception est gérée dans RulesEngine.ExecutePrimaryAction via HijackedBy.)
        /// Immune contre Aerospace (Blindage High-Tech).
        /// </summary>
        public bool Armement_CyberSabotage(PlayerModel player, PropertyData targetProperty = null)
        {
            if (!CanActivate(player, "Armement")) return false;
            if (turnManager == null) return false;

            // Auto-ciblage si aucune propriété fournie (IA ou fallback humain)
            if (targetProperty == null)
            {
                double bestRent = -1;
                foreach (var opp in turnManager.Players)
                {
                    if (opp == null || opp.IsBankrupt || opp.PlayerId == player.PlayerId) continue;
                    foreach (var prop in opp.OwnedProperties)
                    {
                        if (prop.FamilyData != null && prop.FamilyData.FamilyName.Contains("Aerospace")) continue;
                        if (prop.BaseRent > bestRent) { bestRent = prop.BaseRent; targetProperty = prop; }
                    }
                }
            }

            if (targetProperty == null) return false;

            // Immunité Aerospace
            if (targetProperty.FamilyData != null && targetProperty.FamilyData.FamilyName.Contains("Aerospace"))
            {
                Debug.LogWarning($"[Ultimate] Cyber-Sabotage bloqué — Aerospace immunité sur {targetProperty.PropertyName}.");
                return false;
            }

            ConsumeEnergy(player);
            targetProperty.HijackedBy = player;
            targetProperty.HijackTurnsRemaining = 3;
            Debug.Log($"[Ultimate] Armement Cyber-Sabotage — Player {player.PlayerId} hijack {targetProperty.PropertyName} (25% revenus × 3 tours).");
            return true;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // ANCIEN Targeted Strike — conservé pour rétro-compatibilité
        // ─────────────────────────────────────────────────────────────────────────

        public bool ExecuteTargetedStrike(PlayerModel attacker, PropertyData targetProperty)
            => Armement_CyberSabotage(attacker, targetProperty);

        // ─────────────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────────────

        private static int GetTierForRank(int powerRank)
        {
            if (powerRank <= 2) return 1;
            if (powerRank <= 5) return 2;
            return 3;
        }

        private bool HasFullFamilyByName(PlayerModel player, string familyNameContains)
        {
            if (boardManager?.tileDataList == null) return false;

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
