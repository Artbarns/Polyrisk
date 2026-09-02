using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using TMPro;
using PolyRisk.Core;
using PolyRisk.Systems;

namespace PolyRisk.UI
{
    /// <summary>
    /// Phase E.1 — Top HUD Cyber-Luxury.
    /// Affiche pour Player 0 (humain) : CAPITAL exact, NET WORTH, TURN x/y, MARKET.
    /// Le cash des autres joueurs est masqué (Dark Board) — visible uniquement comme
    /// pastille WealthStatus dans les autres écrans (Marketplace Select Player notamment).
    ///
    /// Compatibilité legacy : si UIDocument n'est pas assigné, le widget reste compatible
    /// avec les anciens champs TextMeshProUGUI (capitalText, netWorthText, marketStatusText).
    /// </summary>
    public class TopHUDWidget : MonoBehaviour
    {
        [Header("Dependencies")]
        public TurnManager turnManager;

        [Header("UI Toolkit (préféré)")]
        [Tooltip("UIDocument hôte de TopHUD.uxml. Si null, fallback sur les champs TMP ci-dessous.")]
        public UIDocument uiDocument;
        public VisualTreeAsset topHudTemplate;

        [Header("Legacy TMP fallback")]
        [SerializeField] private TextMeshProUGUI capitalText;
        [SerializeField] private TextMeshProUGUI netWorthText;
        [SerializeField] private TextMeshProUGUI marketStatusText;

        [Header("Game Config")]
        [Tooltip("Nombre de tours max (mode tournoi). Affiché dans TURN: x/MAX")]
        [SerializeField] private int maxTurns = 50;

        // Refs UI Toolkit résolues une fois au Start
        private Label lblCapital, lblNetWorth, lblTurn, lblMarket;

        // ─────────────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            GameEvents.OnTurnStarted    += OnTurnStarted;
            GameEvents.OnBalanceChanged += OnBalanceChanged;
            UpdateHumanPlayerUI();
        }

        private void OnDisable()
        {
            GameEvents.OnTurnStarted    -= OnTurnStarted;
            GameEvents.OnBalanceChanged -= OnBalanceChanged;
        }

        private void Start()
        {
            TryBindUIToolkit();
            UpdateHumanPlayerUI();
        }

        private void TryBindUIToolkit()
        {
            if (uiDocument == null) return;
            var root = uiDocument.rootVisualElement;
            if (root == null) return;

            // Si le template n'est pas encore monté, on l'instancie
            if (topHudTemplate != null && root.Q<VisualElement>("TopHUD_Bar") == null)
                topHudTemplate.CloneTree(root);

            lblCapital  = root.Q<Label>("Lbl_Capital");
            lblNetWorth = root.Q<Label>("Lbl_NetWorth");
            lblTurn     = root.Q<Label>("Lbl_Turn");
            lblMarket   = root.Q<Label>("Lbl_Market");
        }

        private void OnTurnStarted(int playerId) => UpdateHumanPlayerUI();

        private void OnBalanceChanged(int playerId, double newAmount)
        {
            // Dark Board : on rafraîchit que pour Player 0 (humain)
            if (playerId == 0) UpdateHumanPlayerUI();
        }

        // ─────────────────────────────────────────────────────────────────────

        private void UpdateHumanPlayerUI()
        {
            if (turnManager == null) return;
            var humanPlayer = turnManager.GetPlayerById(0);
            if (humanPlayer == null) return;

            // Capital
            string capStr = CurrencyFormatter.Format(humanPlayer.CurrentBalance);
            if (lblCapital != null) lblCapital.text = capStr;
            if (capitalText != null) capitalText.text = $"Capital: {capStr}";

            // Net Worth = cash + valeur d'achat propriétés
            double propertyValue = humanPlayer.OwnedProperties.Sum(p => p.BasePrice);
            double netWorth = humanPlayer.CurrentBalance + propertyValue;
            string nwStr = CurrencyFormatter.Format(netWorth);
            if (lblNetWorth != null) lblNetWorth.text = nwStr;
            if (netWorthText != null) netWorthText.text = $"Net Worth: {nwStr}";

            // Turn x/MAX (tour de plateau global pour le joueur humain)
            int currentTurn = humanPlayer.LapsCompleted + 1;
            string turnStr = $"{currentTurn}/{maxTurns}";
            if (lblTurn != null) lblTurn.text = turnStr;

            // Market state (cycle macro)
            string marketState = (turnManager.financeSystem != null)
                ? turnManager.financeSystem.CurrentMarketState
                : "STABLE";
            if (string.IsNullOrEmpty(marketState)) marketState = "STABLE";
            ApplyMarketStyling(marketState);

            if (marketStatusText != null) marketStatusText.text = $"Market: {marketState}";
        }

        /// <summary>Applique la couleur néon adaptée selon BOOM/CRASH/WAR/NORMAL/STABLE.</summary>
        private void ApplyMarketStyling(string marketState)
        {
            if (lblMarket == null) return;
            lblMarket.text = marketState;

            // Nettoyer
            lblMarket.RemoveFromClassList("top-hud-value--green");
            lblMarket.RemoveFromClassList("top-hud-value--red");
            lblMarket.RemoveFromClassList("top-hud-value--orange");
            lblMarket.RemoveFromClassList("top-hud-value--purple");

            string s = marketState.ToUpperInvariant();
            if      (s.Contains("BOOM"))                       lblMarket.AddToClassList("top-hud-value--green");
            else if (s.Contains("CRASH"))                      lblMarket.AddToClassList("top-hud-value--red");
            else if (s.Contains("WAR"))                        lblMarket.AddToClassList("top-hud-value--orange");
            else                                               lblMarket.AddToClassList("top-hud-value--green");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Notifications transitoires (Marketplace etc.)
        // ─────────────────────────────────────────────────────────────────────

        public void ShowNotification(string message, float duration = 3f)
        {
            if (lblMarket != null) StartCoroutine(NotificationRoutineUIToolkit(message, duration));
            else if (marketStatusText != null) StartCoroutine(NotificationRoutineTMP(message, duration));
        }

        private IEnumerator NotificationRoutineUIToolkit(string message, float duration)
        {
            string original = lblMarket.text;
            lblMarket.text = message;
            lblMarket.AddToClassList("top-hud-value--green");
            yield return new WaitForSeconds(duration);
            UpdateHumanPlayerUI();
        }

        private IEnumerator NotificationRoutineTMP(string message, float duration)
        {
            marketStatusText.text = message;
            marketStatusText.color = Color.green;
            yield return new WaitForSeconds(duration);
            marketStatusText.color = Color.white;
            UpdateHumanPlayerUI();
        }
    }
}
