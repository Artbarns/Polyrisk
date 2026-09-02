using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using PolyRisk.Core;
using PolyRisk.Systems;

namespace PolyRisk.UI
{
    /// <summary>
    /// Phase E.3 — Affiche l'écran de victoire (Hyper-Mogul $10M ou tour-50 sur VN).
    /// Self-spawn via [RuntimeInitializeOnLoadMethod] et écoute GameEvents.OnVictory.
    /// </summary>
    public class VictoryScreenController : MonoBehaviour
    {
        public UIDocument uiDocument;
        public VisualTreeAsset victoryTemplate;

        private VisualElement root;
        private bool isShown = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoSpawn()
        {
            if (FindObjectOfType<VictoryScreenController>() != null) return;
            var go = new GameObject("[Auto] VictoryScreen");
            var c  = go.AddComponent<VictoryScreenController>();
            var doc = go.AddComponent<UIDocument>();
            doc.sortingOrder = 100; // au-dessus de tout
            c.uiDocument = doc;
            // Fix #9 Phase F — Resources.Load pour marcher en build device (pas juste éditeur)
            c.victoryTemplate = Resources.Load<VisualTreeAsset>("VictoryScreen");

            // PanelSettings du projet (réutilise n'importe lequel trouvé dans la scène)
            var anyDoc = FindObjectOfType<UIDocument>();
            if (anyDoc != null && anyDoc != doc) doc.panelSettings = anyDoc.panelSettings;

            DontDestroyOnLoad(go);
        }

        private void OnEnable()  => GameEvents.OnGameVictory += ShowVictory;
        private void OnDisable() => GameEvents.OnGameVictory -= ShowVictory;

        // ─────────────────────────────────────────────────────────────────────

        private void ShowVictory(PlayerModel winner, string condition)
        {
            if (isShown) return;
            isShown = true;

            if (uiDocument == null || victoryTemplate == null)
            {
                Debug.LogError("[VictoryScreen] UIDocument ou template manquant.");
                return;
            }

            root = uiDocument.rootVisualElement;
            root.Clear();
            victoryTemplate.CloneTree(root);

            // Récup éléments
            var lblWinner    = root.Q<Label>("Lbl_VictoryWinner");
            var lblCondition = root.Q<Label>("Lbl_VictoryCondition");
            var lblTitle     = root.Q<Label>("Lbl_VictoryTitle");
            var statsBox     = root.Q<VisualElement>("Container_VictoryStats");
            var btnNewGame   = root.Q<Button>("Btn_NewGame");

            // Remplissage
            if (lblWinner != null)    lblWinner.text = $"PLAYER {winner.PlayerId}{(winner.IsAI ? $" ({winner.Profile})" : "")}";
            if (lblCondition != null) lblCondition.text = condition;
            if (lblTitle != null)     lblTitle.text = condition.Contains("$10") ? "HYPER-MOGUL" : "WINNER BY NET WORTH";

            // Stats finales révélées (Dark Board OFF en fin de partie)
            if (statsBox != null)
            {
                statsBox.Clear();
                var tm = FindObjectOfType<TurnManager>();
                if (tm != null)
                {
                    foreach (var p in tm.Players.OrderByDescending(x => CalcNetWorth(x)))
                    {
                        statsBox.Add(BuildStatRow(p, p.PlayerId == winner.PlayerId));
                    }
                }
            }

            // CTA
            if (btnNewGame != null)
                btnNewGame.clicked += () => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

            // Animation fade-in
            var overlay = root.Q<VisualElement>("VictoryOverlay");
            if (overlay != null) UIToolkitTransitions.FadeInOverlay(this, overlay);
        }

        private VisualElement BuildStatRow(PlayerModel player, bool isWinner)
        {
            var row = new VisualElement();
            row.AddToClassList("victory-stats-row");

            var nameLbl = new Label($"Player {player.PlayerId}{(player.IsAI ? " (AI)" : "")}");
            nameLbl.AddToClassList("victory-stats-label");

            var valueLbl = new Label(CurrencyFormatter.Format(CalcNetWorth(player)));
            valueLbl.AddToClassList("victory-stats-value");
            if (isWinner) valueLbl.AddToClassList("victory-stats-value--winner");

            row.Add(nameLbl);
            row.Add(valueLbl);
            return row;
        }

        private double CalcNetWorth(PlayerModel p)
        {
            return p.CurrentBalance + p.OwnedProperties.Sum(prop => prop.BasePrice);
        }
    }
}
