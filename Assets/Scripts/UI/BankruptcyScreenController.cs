using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using PolyRisk.Core;
using PolyRisk.Multiplayer;
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
        private bool giveUpArmed = false;
        private System.Action<bool> onResolved;
        private bool isOnlinePresentation;
        private long onlineToken;
        private System.Action<string, int, long> submitOnlineCommand;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void EnsureExists()
        {
            if (FindObjectOfType<BankruptcyScreenController>() != null) return;
            var go = new GameObject("[Auto] BankruptcyScreen");
            var c  = go.AddComponent<BankruptcyScreenController>();
            var doc = go.AddComponent<UIDocument>();
            doc.sortingOrder = 90;
            c.uiDocument = doc;
            // Fix #9 Phase F — Resources.Load pour marcher en build device (pas juste éditeur)
            c.bankTemplate = Resources.Load<VisualTreeAsset>("BankruptcyScreen");

            var anyDoc = FindObjectsOfType<UIDocument>().FirstOrDefault(d => d != doc && d.panelSettings != null);
            if (anyDoc != null && anyDoc != doc) doc.panelSettings = anyDoc.panelSettings;

            DontDestroyOnLoad(go);
        }

        private void OnEnable()  => GameEvents.OnInsolvency += Show;
        private void OnDisable() => GameEvents.OnInsolvency -= Show;

        // ─────────────────────────────────────────────────────────────────────

        public void Show(PlayerModel player, double debt, PlayerModel creditor, System.Action<bool> completed,
            DebtSettlementContext context = null)
        {
            if (player == null || isShown) return;

            // In an online game, insolvency is represented in the authoritative snapshot.
            // Only the debtor's client may present and command the liquidation screen.
            var online = OnlineGameController.Instance;
            if (online != null && online.IsOnlineGame) return;

            // Fix #9 Phase F — Le check IA passe AVANT le guard template :
            // même sans UI, la liquidation IA doit tourner (sinon IA insta-bankrupt en build device).
            if (player.IsAI) { TransactionManager.Instance.PayRequired(player, debt, creditor, completed); return; }

            if (uiDocument == null || bankTemplate == null)
            {
                Debug.LogError("[BankruptcyScreen] Template UXML introuvable pour joueur humain — faillite silencieuse.");
                return;
            }

            onResolved = completed;
            isOnlinePresentation = false;
            isShown = true;
            currentPlayer = player;
            currentCreditor = creditor;
            currentDebt = debt;

            root = uiDocument.rootVisualElement;
            root.Clear();
            bankTemplate.CloneTree(root);
            UIUXRuntime.PrepareRoot(root);
            UIUXRuntime.PrepareView(root);
            ApplyResponsiveLayout();
            giveUpArmed = false;

            var overlay = root.Q<VisualElement>("BankruptcyOverlay");
            if (overlay != null) UIToolkitTransitions.FadeInOverlay(this, overlay);

            RefreshUI();

            var btnGiveUp = root.Q<Button>("Btn_BankGiveUp");
            if (btnGiveUp != null) btnGiveUp.clicked += HandleGiveUp;
        }

        public void ShowOnline(
            PlayerModel player,
            double debt,
            long token,
            System.Action<string, int, long> commandSender)
        {
            if (player == null || token <= 0 || commandSender == null) return;

            bool refreshExisting = isShown && isOnlinePresentation && onlineToken == token && root != null;
            currentPlayer = player;
            currentCreditor = null;
            currentDebt = debt;
            onlineToken = token;
            submitOnlineCommand = commandSender;
            isOnlinePresentation = true;
            giveUpArmed = false;

            if (refreshExisting)
            {
                RefreshUI();
                return;
            }

            if (uiDocument == null || bankTemplate == null)
            {
                Debug.LogError("[BankruptcyScreen] Template UXML introuvable pour la liquidation en ligne.");
                return;
            }

            isShown = true;
            root = uiDocument.rootVisualElement;
            root.Clear();
            bankTemplate.CloneTree(root);
            UIUXRuntime.PrepareRoot(root);
            UIUXRuntime.PrepareView(root);
            ApplyResponsiveLayout();

            var overlay = root.Q<VisualElement>("BankruptcyOverlay");
            if (overlay != null) UIToolkitTransitions.FadeInOverlay(this, overlay);

            RefreshUI();
            var btnGiveUp = root.Q<Button>("Btn_BankGiveUp");
            if (btnGiveUp != null) btnGiveUp.clicked += HandleGiveUp;
        }

        private void ApplyResponsiveLayout()
        {
            var footer = root?.Q<VisualElement>(className: "bankruptcy-footer");
            footer?.EnableInClassList("bankruptcy-footer--portrait", Screen.height > Screen.width);
        }

        public void HideOnline()
        {
            if (!isOnlinePresentation) return;
            ResetAndClear();
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
            list.verticalScrollerVisibility = ScrollerVisibility.Auto;

            // 1) Upgrades (priorité GDD §6)
            foreach (var prop in currentPlayer.OwnedProperties)
            {
                if (prop.CurrentLevel > 0)
                    list.Add(BuildRow(prop, $"{prop.PropertyName} — Lvl{prop.CurrentLevel} upgrades",
                        prop.TotalUpgradeCostInvested * 0.5, "SELL UPGRADES", () =>
                        {
                            if (isOnlinePresentation)
                            {
                                SubmitOnline("bankruptcy.upgrades", prop.TileIndex);
                                return;
                            }
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
                            if (isOnlinePresentation)
                            {
                                SubmitOnline("bankruptcy.mortgage", prop.TileIndex);
                                return;
                            }
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
                            if (isOnlinePresentation)
                            {
                                SubmitOnline("bankruptcy.sell", prop.TileIndex);
                                return;
                            }
                            LiquidationHelper.SellPropertyToBank(currentPlayer, prop);
                            CheckAndRefresh();
                        }));
            }

            if (list.contentContainer.childCount == 0)
            {
                list.verticalScrollerVisibility = ScrollerVisibility.Hidden;
                var empty = new Label("No assets remain to liquidate.");
                empty.AddToClassList("empty-state-label");
                list.Add(empty);
            }

            var btnGiveUp = root.Q<Button>("Btn_BankGiveUp");
            if (btnGiveUp != null && !giveUpArmed) btnGiveUp.text = "GIVE UP";
            var debtLine = root.Q<Label>("Lbl_BankDebtLine");
            if (debtLine != null && !giveUpArmed) debtLine.text = "Outstanding debt to settle";
            UIUXRuntime.PrepareView(root);
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
            giveUpArmed = false;
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

        private void HandleGiveUp()
        {
            if (!giveUpArmed)
            {
                giveUpArmed = true;
                var btn = root?.Q<Button>("Btn_BankGiveUp");
                if (btn != null) btn.text = "CONFIRM";
                var line = root?.Q<Label>("Lbl_BankDebtLine");
                if (line != null) line.text = "This ends your game. Tap again to confirm.";
                return;
            }
            if (isOnlinePresentation)
            {
                SubmitOnline("bankruptcy.give_up", -1);
                return;
            }
            FinalizeBankruptcy();
        }

        private void SubmitOnline(string command, int tileIndex)
        {
            var sender = submitOnlineCommand;
            if (sender == null || onlineToken <= 0) return;
            sender(command, tileIndex, onlineToken);
        }

        /// <summary>
        /// Fix #3 Phase F — Solde le créancier avec le cash disponible puis ferme.
        /// Appelé quand la dette est effectivement comblée.
        /// </summary>
        private void SettleDebtAndClose()
        {
            var tm = TransactionManager.Instance;
            bool paid = tm != null && (currentCreditor == null
                ? tm.DeductMoney(currentPlayer, currentDebt)
                : tm.TransferMoney(currentPlayer, currentCreditor, currentDebt));
            if (paid) Close(true);
        }

        private void FinalizeBankruptcy()
        {
            TransactionManager.Instance.ProcessBankruptcyCash(currentPlayer, currentCreditor);
            Close(false);
        }

        private void Close(bool paid)
        {
            var callback = onResolved;
            ResetAndClear();
            callback?.Invoke(paid);
        }

        private void ResetAndClear()
        {
            isShown = false;
            giveUpArmed = false;
            if (root != null) root.Clear();
            onResolved = null;
            currentCreditor = null;
            currentPlayer = null;
            currentDebt = 0;
            isOnlinePresentation = false;
            onlineToken = 0;
            submitOnlineCommand = null;
        }

    }
}
