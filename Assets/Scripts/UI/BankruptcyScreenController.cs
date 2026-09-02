using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using PolyRisk.Core;
using PolyRisk.Systems;

namespace PolyRisk.UI
{
    /// <summary>
    /// Phase E.4 — Écran de liquidation forcée (faillite).
    /// Affiché quand un joueur ne peut pas payer une dette. Liste les actifs liquidables
    /// (upgrades 50%, hypothèques 50%, vente titres 50%) avec boutons individuels.
    /// Si dette comblée → fermeture. Si liquidation totale insuffisante → faillite finale,
    /// propriétés retournent à la banque (OwnerID = -1).
    /// </summary>
    public class BankruptcyScreenController : MonoBehaviour
    {
        public UIDocument uiDocument;
        public VisualTreeAsset bankTemplate;

        private VisualElement root;
        private PlayerModel currentPlayer;
        private PlayerModel currentCreditor; // Fix #3 Phase F — créancier à payer au close (null = banque)
        private double currentDebt;
        private bool isShown = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoSpawn()
        {
            if (FindObjectOfType<BankruptcyScreenController>() != null) return;
            var go = new GameObject("[Auto] BankruptcyScreen");
            var c  = go.AddComponent<BankruptcyScreenController>();
            var doc = go.AddComponent<UIDocument>();
            doc.sortingOrder = 90;
            c.uiDocument = doc;
            // Fix #9 Phase F — Resources.Load pour marcher en build device (pas juste éditeur)
            c.bankTemplate = Resources.Load<VisualTreeAsset>("BankruptcyScreen");

            var anyDoc = FindObjectOfType<UIDocument>();
            if (anyDoc != null && anyDoc != doc) doc.panelSettings = anyDoc.panelSettings;

            DontDestroyOnLoad(go);
        }

        private void OnEnable()  => GameEvents.OnInsolvency += Show;
        private void OnDisable() => GameEvents.OnInsolvency -= Show;

        // ─────────────────────────────────────────────────────────────────────

        public void Show(PlayerModel player, double debt, PlayerModel creditor)
        {
            if (player == null || isShown) return;

            // Fix #9 Phase F — Le check IA passe AVANT le guard template :
            // même sans UI, la liquidation IA doit tourner (sinon IA insta-bankrupt en build device).
            if (player.IsAI) { ProcessAILiquidation(player, debt, creditor); return; }

            if (uiDocument == null || bankTemplate == null)
            {
                Debug.LogError("[BankruptcyScreen] Template UXML introuvable pour joueur humain — faillite silencieuse.");
                return;
            }

            isShown = true;
            currentPlayer = player;
            currentCreditor = creditor;
            currentDebt = debt;

            root = uiDocument.rootVisualElement;
            root.Clear();
            bankTemplate.CloneTree(root);

            var overlay = root.Q<VisualElement>("BankruptcyOverlay");
            if (overlay != null) UIToolkitTransitions.FadeInOverlay(this, overlay);

            RefreshUI();

            var btnGiveUp = root.Q<Button>("Btn_BankGiveUp");
            if (btnGiveUp != null) btnGiveUp.clicked += FinalizeBankruptcy;
        }

        private void RefreshUI()
        {
            if (root == null) return;

            var lblDebt = root.Q<Label>("Lbl_BankDebtValue");
            if (lblDebt != null) lblDebt.text = CurrencyFormatter.Format(currentDebt);

            var lblCash = root.Q<Label>("Lbl_BankCurrentCash");
            if (lblCash != null) lblCash.text = $"Cash: {CurrencyFormatter.Format(currentPlayer.CurrentBalance)}";

            var list = root.Q<ScrollView>("Container_BankAssets");
            if (list == null) return;
            list.Clear();

            // 1) Upgrades (priorité GDD §6)
            foreach (var prop in currentPlayer.OwnedProperties)
            {
                if (prop.CurrentLevel > 0)
                    list.Add(BuildRow(prop, $"{prop.PropertyName} — Lvl{prop.CurrentLevel} upgrades",
                        prop.TotalUpgradeCostInvested * 0.5, "SELL UPGRADES", () =>
                        {
                            var us = FindObjectOfType<UpgradeSystem>();
                            us?.SellAllUpgrades(currentPlayer, prop);
                            CheckAndRefresh();
                        }));
            }

            // 2) Hypothèques
            foreach (var prop in currentPlayer.OwnedProperties)
            {
                if (!prop.IsMortgaged && prop.CurrentLevel == 0)
                    list.Add(BuildRow(prop, $"{prop.PropertyName} — Mortgage",
                        prop.BasePrice * 0.5, "MORTGAGE", () =>
                        {
                            LiquidationHelper.MortgageProperty(currentPlayer, prop);
                            CheckAndRefresh();
                        }));
            }

            // 3) Vente à la banque
            foreach (var prop in currentPlayer.OwnedProperties)
            {
                if (prop.IsMortgaged || prop.CurrentLevel == 0)
                    list.Add(BuildRow(prop, $"{prop.PropertyName} — Sell to bank",
                        prop.BasePrice * 0.5, "SELL", () =>
                        {
                            LiquidationHelper.SellPropertyToBank(currentPlayer, prop);
                            CheckAndRefresh();
                        }));
            }
        }

        private VisualElement BuildRow(PropertyData prop, string label, double refundAmount, string btnText, System.Action onClick)
        {
            var row = new VisualElement();
            row.AddToClassList("bankruptcy-asset-row");

            var textStack = new VisualElement();
            textStack.style.flexGrow = 1;
            textStack.style.flexDirection = FlexDirection.Column;

            var nameLbl = new Label(label);
            nameLbl.AddToClassList("bankruptcy-asset-name");

            var metaLbl = new Label($"+{CurrencyFormatter.Format(refundAmount)}");
            metaLbl.AddToClassList("bankruptcy-asset-meta");

            textStack.Add(nameLbl);
            textStack.Add(metaLbl);

            var btn = new Button(onClick) { text = btnText };
            btn.AddToClassList("bankruptcy-asset-btn");

            row.Add(textStack);
            row.Add(btn);
            return row;
        }

        private void CheckAndRefresh()
        {
            // Dette comblée ?
            if (currentPlayer.CurrentBalance >= currentDebt)
            {
                Debug.Log($"[Bankruptcy] Player {currentPlayer.PlayerId} a comblé la dette ${currentDebt:N0}.");
                SettleDebtAndClose();
                return;
            }

            // Plus rien à liquider ?
            double liquidable = LiquidationHelper.GetTotalLiquidableValue(currentPlayer);
            if (liquidable <= 0)
            {
                FinalizeBankruptcy();
                return;
            }

            RefreshUI();
        }

        /// <summary>
        /// Fix #3 Phase F — Solde le créancier avec le cash disponible puis ferme.
        /// Appelé quand la dette est effectivement comblée.
        /// </summary>
        private void SettleDebtAndClose()
        {
            var tm = TransactionManager.Instance;
            if (tm != null && currentCreditor != null && currentDebt > 0)
                tm.TransferMoney(currentPlayer, currentCreditor, currentDebt);
            Close();
        }

        private void FinalizeBankruptcy()
        {
            Debug.LogWarning($"[Bankruptcy] Player {currentPlayer.PlayerId} déclaré en faillite. Propriétés retour banque.");

            // Fix #3 Phase F — Créancier touche le cash résiduel avant que les propriétés partent en banque.
            var tm = TransactionManager.Instance;
            if (tm != null && currentCreditor != null)
                tm.ProcessBankruptcyCash(currentPlayer, currentCreditor);

            // Toutes les propriétés restantes → banque (OwnerID = -1)
            foreach (var prop in currentPlayer.OwnedProperties.ToList())
                LiquidationHelper.SellPropertyToBank(currentPlayer, prop);

            currentPlayer.IsBankrupt = true;
            GameEvents.OnPlayerBankruptcy?.Invoke(currentPlayer.PlayerId, 0);
            Close();
        }

        private void Close()
        {
            isShown = false;
            if (root != null) root.Clear();
            // Fix #3 Phase F — Rendre la main au GameLoop qui attendait en ResolveTile
            var turnMgr = FindObjectOfType<TurnManager>();
            if (turnMgr != null) turnMgr.CompleteTileResolution();

            currentCreditor = null;
            currentPlayer = null;
            currentDebt = 0;
        }

        // ─────────────────────────────────────────────────────────────────────
        // IA : auto-liquidation respectant l'ordre strict
        // ─────────────────────────────────────────────────────────────────────

        private void ProcessAILiquidation(PlayerModel ai, double debt, PlayerModel creditor)
        {
            Debug.Log($"[Bankruptcy AI] Player {ai.PlayerId} doit ${debt:N0}. Auto-liquidation.");
            var us = FindObjectOfType<UpgradeSystem>();

            // 1) Vendre upgrades du plus cher au moins cher
            foreach (var prop in ai.OwnedProperties.Where(p => p.CurrentLevel > 0).OrderByDescending(p => p.TotalUpgradeCostInvested).ToList())
            {
                if (ai.CurrentBalance >= debt) return;
                us?.SellAllUpgrades(ai, prop);
            }

            // 2) Hypothéquer
            foreach (var prop in ai.OwnedProperties.Where(p => !p.IsMortgaged).OrderBy(p => p.BasePrice).ToList())
            {
                if (ai.CurrentBalance >= debt) return;
                LiquidationHelper.MortgageProperty(ai, prop);
            }

            // 3) Vendre à la banque
            foreach (var prop in ai.OwnedProperties.ToList())
            {
                if (ai.CurrentBalance >= debt) return;
                LiquidationHelper.SellPropertyToBank(ai, prop);
            }

            // Encore insolvable → faillite
            if (ai.CurrentBalance < debt)
            {
                ai.IsBankrupt = true;
                GameEvents.OnPlayerBankruptcy?.Invoke(ai.PlayerId, 0);
                Debug.LogWarning($"[Bankruptcy AI] Player {ai.PlayerId} BANKRUPT.");
            }
        }
    }
}
