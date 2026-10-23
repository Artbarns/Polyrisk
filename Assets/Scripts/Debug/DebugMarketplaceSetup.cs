using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PolyRisk.Core;
using PolyRisk.Systems;
using PolyRisk.UI;

namespace PolyRisk.DebugTools
{
    /// <summary>
    /// DEBUG ONLY — Raccourcis clavier pour tester la Marketplace (case 20)
    /// sans lancer une vraie partie depuis le début.
    ///
    /// F1 : Player 0 (humain) atterrit sur la Marketplace.
    /// F2 : Player 1 (IA adverse) atterrit sur la Marketplace.
    ///
    /// Au premier appui, les propriétés sont distribuées entre tous les joueurs
    /// et chaque joueur reçoit assez de cash pour enchérir.
    ///
    /// ⚠️ Supprimer ce GameObject avant le build de prod.
    /// </summary>
    [AddComponentMenu("PolyRisk/Debug/Marketplace Setup")]
    public class DebugMarketplaceSetup : MonoBehaviour
    {
        [Header("Références")]
        public TurnManager turnManager;
        public BoardManager boardManager;

        [Header("Config")]
        [Tooltip("Nombre de propriétés standard attribuées à chaque joueur au setup.")]
        [SerializeField] private int propertiesPerPlayer = 3;

        [Tooltip("Cash minimal garanti à chaque joueur pour pouvoir enchérir.")]
        [SerializeField] private double minimumCash = 50000.0;

        private bool propertiesDistributed = false;

        private void Awake()
        {
            if (turnManager == null) turnManager = FindObjectOfType<TurnManager>();
            if (boardManager == null) boardManager = BoardManager.Instance;
        }

        private void Update()
        {
            if (!Debug.isDebugBuild) return;
            if (Input.GetKeyDown(KeyCode.F1)) TriggerMarketplace(humanLands: true);
            if (Input.GetKeyDown(KeyCode.F2)) TriggerMarketplace(humanLands: false);
        }

        // ─────────────────────────────────────────────────────────────────────

        private void TriggerMarketplace(bool humanLands)
        {
            if (!ValidateReferences()) return;

            var players = turnManager.Players;
            if (players == null || players.Count < 2)
            {
                Debug.LogError("[DEBUG] Il faut au moins 2 joueurs dans la scène.");
                return;
            }

            if (!propertiesDistributed)
            {
                DistributeProperties(players.ToList());
                propertiesDistributed = true;
            }

            int targetIndex = humanLands ? 0 : 1;
            string label = humanLands ? "F1 — HUMAIN (Player 0)" : "F2 — ADVERSAIRE (Player 1)";
            Debug.Log($"[DEBUG Marketplace] {label} atterrit sur la case 20.");

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            turnManager.DEBUG_ForcePlayerOnTile(targetIndex, 20);
#else
            Debug.LogWarning("[DEBUG] DebugMarketplaceSetup ne fonctionne qu'en Editor ou Development Build.");
#endif
        }

        // ─────────────────────────────────────────────────────────────────────

        private void DistributeProperties(List<PlayerModel> players)
        {
            if (boardManager == null || boardManager.tileDataList == null)
            {
                Debug.LogWarning("[DEBUG] BoardManager.tileDataList est null — distribution impossible.");
                return;
            }

            // Propriétés standard (hors Logistics Hubs), triées par prix croissant
            var standardProps = boardManager.tileDataList
                .OfType<PropertyData>()
                .Where(p => p.FamilyData != null
                         && !p.FamilyData.FamilyName.Contains("Logistics"))
                .OrderBy(p => p.BasePrice)
                .ToList();

            // Hubs Logistics (un par joueur)
            var hubs = boardManager.tileDataList
                .OfType<PropertyData>()
                .Where(p => p.FamilyData != null
                         && p.FamilyData.FamilyName.Contains("Logistics"))
                .ToList();

            // Réinitialise les propriétés (slate clean)
            foreach (var p in players)
                p.ClearProperties();

            // Distribution round-robin
            int propIndex = 0;
            for (int slot = 0; slot < propertiesPerPlayer; slot++)
            {
                foreach (var player in players)
                {
                    if (propIndex >= standardProps.Count) break;
                    player.AddProperty(standardProps[propIndex]);
                    propIndex++;
                }
            }

            // Un Hub par joueur
            for (int i = 0; i < players.Count && i < hubs.Count; i++)
                players[i].AddProperty(hubs[i]);

            // Garantir un solde minimum
            var tm = TransactionManager.Instance;
            if (tm != null)
            {
                foreach (var p in players)
                {
                    if (p.CurrentBalance < minimumCash)
                        tm.AddMoney(p, minimumCash - p.CurrentBalance);
                }
            }

            // Log récapitulatif
            Debug.Log("[DEBUG] ──── Distribution des propriétés ────");
            foreach (var p in players)
            {
                string propList = p.OwnedProperties.Count > 0
                    ? string.Join(", ", p.OwnedProperties.Select(pr => pr.PropertyName))
                    : "(aucune)";
                Debug.Log($"[DEBUG] Player {p.PlayerId} ({(p.IsAI ? "IA" : "Humain")}) | Cash: {CurrencyFormatter.Format(p.CurrentBalance)} | Props: {propList}");
            }
            Debug.Log("[DEBUG] ────────────────────────────────────");
        }

        // ─────────────────────────────────────────────────────────────────────

        private bool ValidateReferences()
        {
            if (turnManager == null)
            {
                Debug.LogError("[DEBUG] DebugMarketplaceSetup : TurnManager manquant.");
                return false;
            }
            if (boardManager == null)
            {
                Debug.LogError("[DEBUG] DebugMarketplaceSetup : BoardManager manquant.");
                return false;
            }
            return true;
        }

        private void OnGUI()
        {
            if (!Application.isEditor) return;
            if (!Debug.isDebugBuild) return;
            GUI.color = new Color(1f, 0.8f, 0f, 0.9f);
            GUI.Label(new Rect(10, 10, 460, 20),
                "[DEBUG Marketplace] F1 = Humain atterrit sur case 20  |  F2 = Adversaire atterrit");
            GUI.color = Color.white;
        }
    }
}
