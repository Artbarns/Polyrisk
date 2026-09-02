using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using PolyRisk.Core;
using PolyRisk.Systems;

namespace PolyRisk.UI
{
    public class Marketplace_UIManager : MonoBehaviour
    {
        private enum MarketplaceView
        {
            Hub,
            Sanctions,
            TenderInit,
            WaitingForBids,    // Vendeur attend les offres (transition 2B→3B)
            TenderReview,      // Vendeur choisit une offre
            BidderSpectator,   // Enchérisseur regarde les offres (read-only, Frame 4.B)
            SelectPlayer       // Sélection de cible pour une sanction (Frame 3.A)
        }

        private enum SelectionContext
        {
            None,
            TenderProperty,
            BidProperty,
            SanctionTarget
        }

        private enum SanctionChoice
        {
            None,
            Embargo,
            Audit,
            Strike,
            Sabotage
        }

        [Header("Root Document")]
        public UIDocument uiDocument;

        [Header("UI Templates")]
        public VisualTreeAsset marketplace_Main;
        public VisualTreeAsset sanctions;
        public VisualTreeAsset tender_Init;
        public VisualTreeAsset tender_Review;
        public VisualTreeAsset auctionItem;
        public VisualTreeAsset marketplace_Notification;
        public VisualTreeAsset marketplace_BidderForm;
        public VisualTreeAsset marketplace_Waiting;
        public VisualTreeAsset marketplace_BidderSpectator;
        public VisualTreeAsset marketplace_SelectPlayer;

        [Header("System References")]
        public MarketplaceSystem marketplaceSystem;
        public FinanceSystem financeSystem;
        public TurnManager turnManager;

        private VisualElement root;
        private VisualElement viewRoot;
        private List<PropertyData> selectedProperties = new List<PropertyData>();
        private readonly List<PolyRisk.Visuals.TileVisuals> pulsingTiles = new List<PolyRisk.Visuals.TileVisuals>();

#pragma warning disable 0414
        private MarketplaceView currentView = MarketplaceView.Hub;
#pragma warning restore 0414
        private SelectionContext selectionContext = SelectionContext.None;

        private SanctionChoice pendingSanctionChoice = SanctionChoice.None;
        private PropertyData pendingSanctionPropertyTarget;
        private PlayerModel pendingSanctionPlayerTarget;

        private static Color GetNeonGreen()
        {
            ColorUtility.TryParseHtmlString("#05DF72", out Color c);
            return c;
        }

        private static Color GetCyan()
        {
            ColorUtility.TryParseHtmlString("#00FFFF", out Color c);
            return c;
        }

        private int selectedBidIndex = -1;
        private MarketplaceSystem.TenderBid selectedBid;
        private List<MarketplaceSystem.TenderBid> currentDisplayedBids = new List<MarketplaceSystem.TenderBid>();

        // Rôle de l'humain dans l'enchère en cours
        private bool isHumanTheSeller = false;

        // Cible en cours de sélection pour les sanctions
        private PlayerModel pendingTargetPlayer = null;

        private void Awake()
        {
            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
            if (uiDocument != null)
            {
                uiDocument.visualTreeAsset = null;
            }

            if (turnManager == null) turnManager = FindObjectOfType<TurnManager>();
            if (financeSystem == null) financeSystem = FindObjectOfType<FinanceSystem>();
            if (marketplaceSystem == null) marketplaceSystem = FindObjectOfType<MarketplaceSystem>();

            auctionItem                  = ResolveVisualTreeAsset(auctionItem,                  "AuctionItem_Component");
            marketplace_Waiting          = ResolveVisualTreeAsset(marketplace_Waiting,          "Marketplace_Waiting");
            marketplace_BidderSpectator  = ResolveVisualTreeAsset(marketplace_BidderSpectator,  "Marketplace_Bidder_Spectator");
            marketplace_SelectPlayer     = ResolveVisualTreeAsset(marketplace_SelectPlayer,     "Marketplace_Select_Player");
        }

        private void Start()
        {
            if (marketplaceSystem != null)
            {
                marketplaceSystem.OnAuctionStateChanged += HandleAuctionStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (marketplaceSystem != null)
            {
                marketplaceSystem.OnAuctionStateChanged -= HandleAuctionStateChanged;
            }
        }

        private void HandleAuctionStateChanged(MarketplaceSystem.AuctionState newState)
        {
            switch (newState)
            {
                case MarketplaceSystem.AuctionState.InvitingPlayers:
                    // Le vendeur reste sur son écran d'attente — on ne fait rien pour lui ici.
                    // Pour les autres joueurs (humains non-IA), on affiche la notification.
                    ShowAuctionNotification();
                    // Mise à jour du compteur si l'écran d'attente est affiché
                    UpdateWaitingBidCount();
                    break;

                case MarketplaceSystem.AuctionState.Bidding:
                    ShowBidderForm();
                    break;

                case MarketplaceSystem.AuctionState.Reviewing:
                    if (isHumanTheSeller)
                        ShowTenderReview();
                    else
                        ShowBidderSpectator();
                    break;

                case MarketplaceSystem.AuctionState.Closing:
                    ResetAllHighlights();
                    if (!isHumanTheSeller)
                    {
                        // L'IA était le vendeur — ferme l'UI spectateur/notification et libère le tour.
                        // (Si c'est le vendeur humain, CloseMarketplace est déjà appelé par SealSelectedDeal.)
                        CloseMarketplace();
                    }
                    break;
            }
        }

        private void ResetAllHighlights()
        {
            var board = BoardManager.Instance;
            if (board == null || board.visualTiles == null) return;
            for (int i = 0; i < board.visualTiles.Length; i++)
            {
                var tv = GetTileVisuals(i);
                if (tv != null)
                {
                    tv.ResetHighlight();
                }
            }
        }

        private void ShowAuctionNotification()
        {
            if (marketplaceSystem == null || turnManager == null) return;
            var seller = marketplaceSystem.CurrentTenderSeller;
            // Le "local player" est toujours l'humain (Player 0), pas le joueur du tour en cours
            // (qui peut être une IA quand c'est elle qui a déclenché le Marketplace).
            var localPlayer = turnManager.GetPlayerById(0)
                           ?? turnManager.Players?.FirstOrDefault(p => !p.IsAI);

            if (localPlayer != null && seller != null && localPlayer.PlayerId != seller.PlayerId && !localPlayer.IsAI)
            {
                marketplace_Notification = ResolveVisualTreeAsset(marketplace_Notification, "Marketplace_Notification");
                if (marketplace_Notification == null || root == null) return;

                // Make sure root is visible for the notification overlay
                root.style.display = DisplayStyle.Flex;

                var notifElement = marketplace_Notification.Instantiate();
                root.Add(notifElement);

                // Phase E.5 — Scale-in animation pour la notification
                UIToolkitTransitions.ScaleInEnter(this, notifElement);

                // Populate labels
                var lblTitle = notifElement.Q<Label>("Label_Title");
                var lblAsset = notifElement.Q<Label>("Label_Asset");
                var lblTimer = notifElement.Q<Label>("Label_Timer");

                if (lblTitle != null) lblTitle.text = "AUCTION STARTED";
                if (lblAsset != null)
                {
                    string propName = marketplaceSystem.CurrentTenderProperty != null
                        ? marketplaceSystem.CurrentTenderProperty.PropertyName
                        : "an asset";
                    lblAsset.text = $"Player {seller.PlayerId} is selling {propName}";
                }

                var btnParticipate = notifElement.Q<Button>("Btn_Participate");
                var btnSkip = notifElement.Q<Button>("Btn_Skip");

                btnParticipate?.RegisterCallback<ClickEvent>(ev => {
                    marketplaceSystem.SetAuctionState(MarketplaceSystem.AuctionState.Bidding);
                    notifElement.RemoveFromHierarchy();
                });

                btnSkip?.RegisterCallback<ClickEvent>(ev => {
                    marketplaceSystem.SkipBid(localPlayer.PlayerId);
                    notifElement.RemoveFromHierarchy();
                });

                // 5-second countdown — auto-skip on expiry
                StartCoroutine(NotificationCountdown(notifElement, lblTimer, 5, localPlayer));
            }
        }

        private System.Collections.IEnumerator NotificationCountdown(
            VisualElement notifElement, Label timerLabel, int seconds, PlayerModel localPlayer)
        {
            int remaining = seconds;
            while (remaining > 0)
            {
                if (timerLabel != null) timerLabel.text = $"{remaining}s";
                yield return new UnityEngine.WaitForSeconds(1f);
                remaining--;
                // Abort if the element was already dismissed manually
                if (notifElement.parent == null) yield break;
            }
            // Time's up — auto-skip
            if (notifElement.parent != null)
            {
                marketplaceSystem?.SkipBid(localPlayer.PlayerId);
                notifElement.RemoveFromHierarchy();
            }
        }

        private void ShowBidderForm()
        {
            if (marketplaceSystem == null || turnManager == null) return;
            var seller = marketplaceSystem.CurrentTenderSeller;
            // Toujours l'humain (Player 0)
            var localPlayer = turnManager.GetPlayerById(0)
                           ?? turnManager.Players?.FirstOrDefault(p => !p.IsAI);

            if (localPlayer == null || seller == null || localPlayer.PlayerId == seller.PlayerId || localPlayer.IsAI) return;

            // ── Désactiver le HUD principal pour éviter qu'il reste visible derrière ──
            ActionButtonsWidget actionBtns = FindObjectOfType<ActionButtonsWidget>(true);
            if (actionBtns != null) actionBtns.gameObject.SetActive(false);

            isHumanTheSeller = false;
            selectionContext = SelectionContext.BidProperty;
            selectedProperties.Clear();
            ResetAllHighlights();

            marketplace_BidderForm = ResolveVisualTreeAsset(marketplace_BidderForm, "Marketplace_Bidder_Form");
            SwitchView(marketplace_BidderForm);
            SetModal(true); // Panel UI interactif (slider, boutons)
            if (root != null) root.style.display = DisplayStyle.Flex;

            var btnSkip   = viewRoot.Q<Button>("Btn_Skip");
            var btnSubmit = viewRoot.Q<Button>("Btn_SubmitBid");
            var btnPick   = viewRoot.Q<Button>("Btn_PickPropertyBid");
            var slider    = viewRoot.Q<SliderInt>("Slider_CashOffer");
            var lblCash   = viewRoot.Q<Label>("Label_CashValue");

            if (slider != null)
                slider.highValue = Mathf.FloorToInt((float)localPlayer.CurrentBalance);

            slider?.RegisterValueChangedCallback(evt => {
                if (lblCash != null) lblCash.text = CurrencyFormatter.Format(evt.newValue);
                UpdateBidderTotal(evt.newValue);
            });

            // ── Highlights cyan immédiats (visuels) — sans SetModal(false) pour garder le panel UI réactif ──
            HighlightOwnedProperties(GetCyan());

            // Btn_PickPropertyBid : active le mode click 3D (SetModal false)
            btnPick?.RegisterCallback<ClickEvent>(ev => {
                EnterSelectionMode(SelectionContext.BidProperty);
            });

            btnSkip?.RegisterCallback<ClickEvent>(ev => {
                marketplaceSystem.SkipBid(localPlayer.PlayerId);
                if (turnManager != null) turnManager.IsInSelectionMode = false;
                ResetAllHighlights();
                CloseMarketplace();
            });

            btnSubmit?.RegisterCallback<ClickEvent>(ev => {
                var bid = new MarketplaceSystem.TenderBid {
                    bidder    = localPlayer,
                    cashOffer = slider?.value ?? 0,
                };
                bid.propertyOffers.AddRange(selectedProperties);

                marketplaceSystem.SubmitBid(localPlayer.PlayerId, bid);
                if (turnManager != null) turnManager.IsInSelectionMode = false;
                ResetAllHighlights();
                ShowBidderSpectator();
            });
        }

        private void UpdateBidderTotal(int cash)
        {
            if (viewRoot == null) return;
            var lblTotal = viewRoot.Q<Label>("Label_TotalValue");
            if (lblTotal != null)
            {
                double propTotal = selectedProperties.Sum(p => p.BasePrice);
                lblTotal.text = CurrencyFormatter.Format(cash + propTotal);
            }
        }

        private VisualTreeAsset ResolveVisualTreeAsset(VisualTreeAsset current, string desiredName)
        {
            if (current != null && current.name == desiredName) return current;

            var assets = Resources.FindObjectsOfTypeAll<VisualTreeAsset>();
            if (assets == null) return current;

            for (int i = 0; i < assets.Length; i++)
            {
                var a = assets[i];
                if (a != null && a.name == desiredName) return a;
            }

            return current;
        }

        private void OnEnable()
        {
            if (uiDocument != null)
            {
                root = uiDocument.rootVisualElement;
                if (root != null)
                {
                    root.style.display = DisplayStyle.None;
                    root.Clear();
                }
            }
        }

        public void OpenMarketplace()
        {
            ActionButtonsWidget oldUI = FindObjectOfType<ActionButtonsWidget>(true);
            if (oldUI != null) oldUI.gameObject.SetActive(false);

            if (uiDocument != null) root = uiDocument.rootVisualElement;

            if (root != null)
            {
                root.style.display = DisplayStyle.Flex;
                ShowMainHub();
            }
        }

        /// <summary>
        /// Clears the UIDocument's root and instantiates the new template.
        /// </summary>
        public void SwitchView(VisualTreeAsset template)
        {
            if (template == null || root == null) return;

            root.Clear();
            template.CloneTree(root);
            viewRoot = root.childCount > 0 ? root[0] : root;

            BindCommonCloseButton();

            // Phase E.5 — Slide-up enter animation sur les bottom sheets
            VisualElement marketplacePanel = viewRoot.Q<VisualElement>(className: "marketplace-panel")
                                           ?? viewRoot;
            UIToolkitTransitions.SlideUpEnter(this, marketplacePanel);
        }

        public void ShowMainHub()
        {
            currentView = MarketplaceView.Hub;
            selectionContext = SelectionContext.None;
            StopPulses(true);
            SetSelectionMode(false);

            SwitchView(marketplace_Main);
            SetModal(true);

            // Bind Navigation to Sanctions
            Button btnSanctions = root.Q<Button>("Btn_Sanctions");
            if (btnSanctions == null) btnSanctions = root.Q<Button>("Btn_GoSanctions");
            if (btnSanctions != null)
            {
                btnSanctions.clicked += ShowSanctions;
            }

            // Bind Navigation to Tender Init
            Button btnInitiateTender = root.Q<Button>("Btn_InitiateTender");
            if (btnInitiateTender == null) btnInitiateTender = root.Q<Button>("Btn_GoTenders"); // Fallback
            
            if (btnInitiateTender != null)
            {
                btnInitiateTender.clicked += ShowTenderInit;
            }
            else
            {
                Debug.LogError("[Marketplace] Button 'Call for Tenders' (Btn_GoTenders) NOT FOUND in Main Hub UI!");
            }
            
            // Optional: Bind Navigation to Tender Review
            Button btnTenderReview = root.Q<Button>("Btn_ReviewTenders");
            if (btnTenderReview != null)
            {
                btnTenderReview.clicked += () => SwitchView(tender_Review);
            }
        }

        public void ShowSanctions()
        {
            currentView = MarketplaceView.Sanctions;
            selectionContext = SelectionContext.None;
            StopPulses(true);
            SetSelectionMode(false);

            SwitchView(sanctions);
            SetModal(true);

            Button embargoBtn = root.Q<Button>("Btn_SanctionEmbargo");
            Button auditBtn = root.Q<Button>("Btn_SanctionAudit");
            Button strikeBtn = root.Q<Button>("Btn_SanctionStrike");
            Button sabotageBtn = root.Q<Button>("Btn_SanctionSabotage");

            WireSanctionButton(embargoBtn, SanctionChoice.Embargo);
            WireSanctionButton(auditBtn, SanctionChoice.Audit);
            WireSanctionButton(strikeBtn, SanctionChoice.Strike);
            WireSanctionButton(sabotageBtn, SanctionChoice.Sabotage);

            var confirmContainer = root.Q<VisualElement>("Container_SanctionConfirm");
            var confirmLabel = root.Q<Label>("Lbl_SanctionConfirm");
            var confirmBtn = root.Q<Button>("Btn_ConfirmSanction");
            if (confirmContainer != null) confirmContainer.style.display = DisplayStyle.None;
            if (confirmBtn != null)
            {
                confirmBtn.clicked += ConfirmPendingSanction;
            }
        }

        private void WireSanctionButton(Button btn, SanctionChoice choice)
        {
            if (btn == null) return;
            btn.AddToClassList("clickable");
            // Utilise RegisterCallback<ClickEvent> (pas PointerDown) pour être compatible avec SetModal
            btn.RegisterCallback<ClickEvent>(_ => ShowPlayerSelection(choice));
        }

        public void ShowTenderInit()
        {
            currentView = MarketplaceView.TenderInit;
            selectionContext = SelectionContext.None;
            StopPulses(true);
            SetSelectionMode(false);
            selectedProperties.Clear();

            SwitchView(tender_Init);

            // Hard clear the container — kills any residual UXML content
            ScrollView ownedContainer = root.Q<ScrollView>("Container_OwnedAssets");
            if (ownedContainer != null)
            {
                Debug.Log("[Marketplace] Cleaning UI Container (Container_OwnedAssets)...");
                ownedContainer.Clear();
            }
            else
            {
                Debug.LogWarning("[Marketplace] Container_OwnedAssets NOT FOUND after SwitchView.");
            }

            // Make CTA purely informational — no interaction required, selection starts immediately
            Button btnPickProperty = root.Q<Button>("Btn_PickProperty");
            if (btnPickProperty != null)
            {
                btnPickProperty.pickingMode = PickingMode.Ignore;
            }

            // Wire Confirm — disabled until a valid property is selected
            Button confirmBtn = root.Q<Button>("Btn_ConfirmTender");
            if (confirmBtn != null)
            {
                confirmBtn.SetEnabled(false);
                confirmBtn.clicked += ConfirmTenderSelection;
            }

            // Enter selection mode immediately — panel stays visible but non-blocking for 3D clicks
            // SetModal(false) lets raycasts through while panel stays on screen
            EnterSelectionMode(SelectionContext.TenderProperty);
        }



        private void EnterSelectionMode(SelectionContext context)
        {
            selectionContext = context;
            SetSelectionMode(true);
            SetModal(false);

            StopPulses(false);
            if (context == SelectionContext.TenderProperty)
                HighlightOwnedProperties(GetNeonGreen());
            else if (context == SelectionContext.BidProperty)
                HighlightOwnedProperties(GetCyan());
            else if (context == SelectionContext.SanctionTarget)
                HighlightTargetableProperties();
        }

        private void ExitSelectionMode(bool clearTenderSelection)
        {
            StopPulses(true);
            selectionContext = SelectionContext.None;
            SetSelectionMode(false);
            SetModal(true);
            if (clearTenderSelection) selectedProperties.Clear();
        }

        public void OnTileSelected(PropertyData tile, PolyRisk.Visuals.TileVisuals tileVisuals = null)
        {
            if (tile == null) return;

            // Resolve TileVisuals from index if the parameter came in null
            if (tileVisuals == null)
            {
                int idx = FindTileIndexForProperty(tile);
                if (idx >= 0) tileVisuals = GetTileVisuals(idx);
            }

            if (selectionContext == SelectionContext.TenderProperty)
            {
                PlayerModel currentPlayer = turnManager != null ? turnManager.CurrentPlayer : null;
                if (currentPlayer == null) return;
                if (!currentPlayer.OwnedProperties.Contains(tile)) return;

                // Deselect previous — stop its static highlight and restart pulse (tile stays available)
                if (selectedProperties.Count > 0)
                {
                    int prevIndex = FindTileIndexForProperty(selectedProperties[0]);
                    if (prevIndex >= 0)
                    {
                        var prevTv = GetTileVisuals(prevIndex);
                        if (prevTv != null)
                        {
                            prevTv.EndPulse(false);
                            prevTv.ResetHighlight();
                            prevTv.BeginPulse(GetNeonGreen());
                            if (!pulsingTiles.Contains(prevTv)) pulsingTiles.Add(prevTv);
                        }
                    }
                    selectedProperties.Clear();
                }

                // Stop pulse on the newly selected tile so static highlight is stable
                if (tileVisuals != null)
                {
                    tileVisuals.EndPulse(false);
                    pulsingTiles.Remove(tileVisuals);
                    tileVisuals.Highlight(GetNeonGreen());
                }

                selectedProperties.Add(tile);

                ScrollView container = root.Q<ScrollView>("Container_OwnedAssets");
                if (container != null)
                {
                    container.Clear();
                    AddSelectedPropertyToTenderUI(tile, currentPlayer);
                }

                Button cta = root.Q<Button>("Btn_PickProperty");
                if (cta != null) cta.style.display = DisplayStyle.None;

                Button confirmBtn = root.Q<Button>("Btn_ConfirmTender");
                if (confirmBtn != null) confirmBtn.SetEnabled(true);
            }
            else if (selectionContext == SelectionContext.BidProperty)
            {
                PlayerModel currentPlayer = turnManager != null ? turnManager.CurrentPlayer : null;
                if (currentPlayer == null) return;
                if (!currentPlayer.OwnedProperties.Contains(tile)) return;

                if (selectedProperties.Contains(tile)) return;

                // At max 2 — evict oldest, restart its pulse
                if (selectedProperties.Count >= 2)
                {
                    var removedProp = selectedProperties[0];
                    int prevIndex = FindTileIndexForProperty(removedProp);
                    if (prevIndex >= 0)
                    {
                        var prevTv = GetTileVisuals(prevIndex);
                        if (prevTv != null)
                        {
                            prevTv.EndPulse(false);
                            prevTv.ResetHighlight();
                            prevTv.BeginPulse(GetCyan());
                            if (!pulsingTiles.Contains(prevTv)) pulsingTiles.Add(prevTv);
                        }
                    }
                    selectedProperties.RemoveAt(0);
                }

                // Stop pulse on the newly selected tile so static highlight is stable
                if (tileVisuals != null)
                {
                    tileVisuals.EndPulse(false);
                    pulsingTiles.Remove(tileVisuals);
                    tileVisuals.Highlight(GetCyan());
                }

                selectedProperties.Add(tile);

                // Container_BidAssets est maintenant un VisualElement (pas ScrollView)
                VisualElement bidContainer = root.Q<VisualElement>("Container_BidAssets");
                if (bidContainer != null)
                {
                    bidContainer.Clear();
                    foreach (var prop in selectedProperties)
                        AddBidPropertyToUI(prop, bidContainer);
                }

                // Masquer le bouton pick quand 2 props sont sélectionnées
                Button pickBtn = root.Q<Button>("Btn_PickPropertyBid");
                if (pickBtn != null)
                    pickBtn.style.display = selectedProperties.Count >= 2 ? DisplayStyle.None : DisplayStyle.Flex;

                // Quand 2 props : désactiver le mode 3D sélection, reprendre modal pour le UI
                if (selectedProperties.Count >= 2)
                {
                    SetSelectionMode(false);
                    SetModal(true);
                }

                var slider = viewRoot?.Q<SliderInt>("Slider_CashOffer");
                UpdateBidderTotal(slider?.value ?? 0);
            }



            if (selectionContext == SelectionContext.SanctionTarget)
            {
                PlayerModel attacker = turnManager != null ? turnManager.CurrentPlayer : null;
                if (attacker == null || turnManager == null) return;

                PlayerModel owner = turnManager.GetOwnerOfProperty(tile);
                if (owner == null || owner.PlayerId == attacker.PlayerId) return;

                pendingSanctionPropertyTarget = tile;
                pendingSanctionPlayerTarget = owner;

                if (tileVisuals != null)
                {
                    tileVisuals.Highlight();
                }

                ExitSelectionMode(false);
                ShowSanctionConfirm();
            }
        }

 

        private void BindCommonCloseButton()
        {
            string[] closeNames = { "Btn_Close", "Btn_CloseHub", "Btn_CloseSanctions", "Btn_CloseTenders", "Btn_CloseTenderReview" };
            foreach (var name in closeNames)
            {
                Button btn = root.Q<Button>(name);
                if (btn != null)
                {
                    btn.clicked += CloseMarketplace;
                }
            }
        }

        public void CloseMarketplace()
        {
            StopPulses(true);
            selectionContext = SelectionContext.None;
            SetSelectionMode(false);
            selectedBid = null;
            selectedBidIndex = -1;
            pendingSanctionPropertyTarget = null;
            pendingSanctionPlayerTarget = null;

            if (root != null)
            {
                root.style.display = DisplayStyle.None;
                root.Clear();
            }

            ActionButtonsWidget oldUI = FindObjectOfType<ActionButtonsWidget>(true);
            if (oldUI != null) oldUI.gameObject.SetActive(true);

            if (turnManager != null)
            {
                turnManager.CompleteTileResolution();
            }
        }

        private void ConfirmTenderSelection()
        {
            if (marketplaceSystem == null) marketplaceSystem = FindObjectOfType<MarketplaceSystem>();
            if (turnManager == null) turnManager = FindObjectOfType<TurnManager>();
            if (marketplaceSystem == null || turnManager == null) return;
            if (selectedProperties.Count == 0) return;

            isHumanTheSeller = true;

            // Lance l'enchère asynchrone (les IA vont bidder pendant qu'on attend)
            marketplaceSystem.BeginTender(turnManager.CurrentPlayer, selectedProperties[0]);

            ExitSelectionMode(true);

            // Transition vers l'écran d'attente intermédiaire au lieu de cacher le root
            ShowWaitingForBids();
        }

        // ─────────────────────────────────────────────────────────────────────────
        // WAITING FOR BIDS (vendeur attend — transition 2B→3B)
        // ─────────────────────────────────────────────────────────────────────────

        private void ShowWaitingForBids()
        {
            currentView = MarketplaceView.WaitingForBids;

            marketplace_Waiting = ResolveVisualTreeAsset(marketplace_Waiting, "Marketplace_Waiting");
            SwitchView(marketplace_Waiting);
            SetModal(true);

            if (root != null) root.style.display = DisplayStyle.Flex;

            // Remplir le nom de la propriété mise aux enchères
            var propNameLbl = root.Q<Label>("Lbl_WaitingPropName");
            if (propNameLbl != null && marketplaceSystem?.CurrentTenderProperty != null)
                propNameLbl.text = marketplaceSystem.CurrentTenderProperty.PropertyName;

            var propRentLbl = root.Q<Label>("Lbl_WaitingPropRent");
            if (propRentLbl != null && marketplaceSystem?.CurrentTenderProperty != null)
                propRentLbl.text = $"Base rent: {CurrencyFormatter.Format(marketplaceSystem.CurrentTenderProperty.BaseRent)}";

            UpdateWaitingBidCount();

            // Bouton Cancel
            var btnCancel = root.Q<Button>("Btn_CancelWaiting");
            if (btnCancel != null)
                btnCancel.clicked += CancelTender;
        }

        private void UpdateWaitingBidCount()
        {
            if (currentView != MarketplaceView.WaitingForBids || root == null) return;
            var countLbl = root.Q<Label>("Lbl_WaitingBidCount");
            if (countLbl == null || marketplaceSystem == null) return;

            int count = marketplaceSystem.ActiveBids.Count(kvp => kvp.Value != null);
            countLbl.text = count == 0 ? "No offers yet..." : $"{count} offer{(count > 1 ? "s" : "")} received";
        }

        // ─────────────────────────────────────────────────────────────────────────
        // BIDDER SPECTATOR — Frame 4.B (enchérisseur regarde la deal list, read-only)
        // ─────────────────────────────────────────────────────────────────────────

        private void ShowBidderSpectator()
        {
            if (root != null) root.style.display = DisplayStyle.Flex;

            currentView = MarketplaceView.BidderSpectator;
            selectionContext = SelectionContext.None;
            StopPulses(true);
            SetSelectionMode(false);

            marketplace_BidderSpectator = ResolveVisualTreeAsset(marketplace_BidderSpectator, "Marketplace_Bidder_Spectator");
            SwitchView(marketplace_BidderSpectator);
            SetModal(true);

            // Remplir la liste des offres (identique à TenderReview mais sans boutons d'action)
            var bidList = root.Q<ScrollView>("Container_SpectatorBidList");
            if (bidList != null) bidList.Clear();

            var bids = marketplaceSystem != null
                ? marketplaceSystem.ActiveBids.Values.Where(b => b != null).ToList()
                : new List<MarketplaceSystem.TenderBid>();

            if (bidList != null && auctionItem != null)
            {
                foreach (var bid in bids)
                {
                    VisualElement holder = new VisualElement();
                    auctionItem.CloneTree(holder);
                    VisualElement row = holder.childCount > 0 ? holder[0] : holder;

                    var playerName = row.Q<Label>("Txt_PlayerName");
                    if (playerName != null && bid.bidder != null)
                        playerName.text = $"PLAYER {bid.bidder.PlayerId}";

                    var propTag  = row.Q<VisualElement>("Tag_PropOffered");
                    var cashTag  = row.Q<VisualElement>("Tag_CashOffered");
                    var plusSep  = row.Q<Label>("Txt_PlusSep");

                    if (propTag != null)
                    {
                        bool hasProp = bid.propertyOffers.Count > 0;
                        var nameLbl  = propTag.Q<Label>(className: "tag-label");
                        var priceLbl = propTag.Q<Label>(className: "txt-price");
                        if (nameLbl  != null) nameLbl.text  = hasProp ? string.Join(", ", bid.propertyOffers.Select(p => p.PropertyName)) : "";
                        if (priceLbl != null) priceLbl.text = hasProp ? CurrencyFormatter.Format(bid.propertyOffers.Sum(p => p.BasePrice)) : "";
                        propTag.style.display = hasProp ? DisplayStyle.Flex : DisplayStyle.None;
                    }

                    if (cashTag != null)
                    {
                        var priceLbl = cashTag.Q<Label>(className: "txt-price");
                        if (priceLbl != null) priceLbl.text = CurrencyFormatter.Format(bid.cashOffer);
                        cashTag.style.display = DisplayStyle.Flex;
                    }

                    if (plusSep != null)
                        plusSep.style.display = bid.propertyOffers.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;

                    bidList.Add(row);
                }
            }

            // Label footer : indique si l'humain a enchéri
            var footerLbl = root.Q<Label>("Lbl_SpectatorFooter");
            if (footerLbl != null)
            {
                var humanPlayer = turnManager?.GetPlayerById(0);
                bool humanBid   = bids.Any(b => b.bidder != null && humanPlayer != null && b.bidder.PlayerId == humanPlayer.PlayerId);
                footerLbl.text  = humanBid ? "✓ Your offer is in the running" : "You passed on this auction";
            }
        }

        // ─────────────────────────────────────────────────────────────────────────

        private void ShowTenderReview()
        {
            if (root != null) root.style.display = DisplayStyle.Flex;

            currentView = MarketplaceView.TenderReview;
            selectionContext = SelectionContext.None;
            StopPulses(true);
            SetSelectionMode(false);

            SwitchView(tender_Review);
            SetModal(true);

            selectedBid = null;
            selectedBidIndex = -1;
            currentDisplayedBids = marketplaceSystem != null ? 
                marketplaceSystem.ActiveBids.Values.Where(b => b != null).ToList() : 
                new List<MarketplaceSystem.TenderBid>();

            ScrollView bidList = root.Q<ScrollView>("Container_BidList");
            if (bidList != null) bidList.Clear();

            if (marketplaceSystem != null && bidList != null && auctionItem != null)
            {
                for (int i = 0; i < currentDisplayedBids.Count; i++)
                {
                    int index = i;
                    var bid = currentDisplayedBids[i];

                    VisualElement holder = new VisualElement();
                    auctionItem.CloneTree(holder);
                    VisualElement row = holder.childCount > 0 ? holder[0] : holder;
                    row.AddToClassList("clickable");

                    var glow = row.Q<VisualElement>("GlowLayer_BidSelection");
                    if (glow != null) glow.RemoveFromClassList("is-glowing");

                    var playerName = row.Q<Label>("Txt_PlayerName");
                    if (playerName != null && bid != null && bid.bidder != null)
                    {
                        playerName.text = $"PLAYER {bid.bidder.PlayerId}";
                    }

                    var propTag = row.Q<VisualElement>("Tag_PropOffered");
                    var cashTag = row.Q<VisualElement>("Tag_CashOffered");
                    var plus = row.Q<Label>("Txt_PlusSep");

                    if (propTag != null)
                    {
                        var nameLbl = propTag.Q<Label>(className: "tag-label");
                        var priceLbl = propTag.Q<Label>(className: "txt-price");
                        if (nameLbl != null) nameLbl.text = bid != null && bid.propertyOffers.Count > 0 ? string.Join(", ", bid.propertyOffers.Select(p => p.PropertyName)) : "NO PROPERTY";
                        if (priceLbl != null) priceLbl.text = bid != null && bid.propertyOffers.Count > 0 ? CurrencyFormatter.Format(bid.propertyOffers.Sum(p => p.BasePrice)) : "";
                        propTag.style.display = bid != null && bid.propertyOffers.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                    }

                    if (cashTag != null)
                    {
                        var priceLbl = cashTag.Q<Label>(className: "txt-price");
                        if (priceLbl != null) priceLbl.text = bid != null ? CurrencyFormatter.Format(bid.cashOffer) : "";
                        cashTag.style.display = DisplayStyle.Flex;
                    }

                    if (plus != null)
                    {
                        plus.style.display = bid != null && bid.propertyOffers.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                    }

                    row.RegisterCallback<PointerDownEvent>(_ => SelectBid(index));
                    bidList.Add(row);
                }
            }

            Button rejectAll = root.Q<Button>("Btn_RejectAll");
            if (rejectAll != null) rejectAll.clicked += CancelTender;

            Button sealDeal = root.Q<Button>("Btn_SealDeal");
            if (sealDeal != null) sealDeal.clicked += SealSelectedDeal;
        }

        private void CancelTender()
        {
            if (marketplaceSystem != null) marketplaceSystem.CancelTender();
            CloseMarketplace();
        }

        private void SelectBid(int index)
        {
            if (marketplaceSystem == null) return;
            if (index < 0 || index >= currentDisplayedBids.Count) return;

            selectedBidIndex = index;
            selectedBid = currentDisplayedBids[index];

            ScrollView bidList = root.Q<ScrollView>("Container_BidList");
            if (bidList == null) return;

            for (int i = 0; i < bidList.childCount; i++)
            {
                var child = bidList[i];
                // Toggle neon glow layer
                var glow = child.Q<VisualElement>("GlowLayer_BidSelection");
                if (glow != null) glow.EnableInClassList("is-glowing", i == index);
                // Toggle explicit border class
                child.EnableInClassList("bid-selected", i == index);
            }

            // Enable Seal Deal button now that a bid is selected
            Button sealDeal = root.Q<Button>("Btn_SealDeal");
            if (sealDeal != null) sealDeal.SetEnabled(true);
        }

        private void SealSelectedDeal()
        {
            if (marketplaceSystem == null || selectedBid == null) return;

            // Cache la référence EN LOCAL avant tout appel externe.
            // SealTenderDeal → SetAuctionState(Closing) → event → CloseMarketplace() → selectedBid = null.
            // Sans ce cache, l'accès à selectedBid.propertyOffers après le call crashe (NullRef ligne ~805).
            var dealToSeal = selectedBid;

            // Anti-exploit : désactiver le bouton immédiatement
            Button sealDeal = root.Q<Button>("Btn_SealDeal");
            if (sealDeal != null) sealDeal.SetEnabled(false);

            if (!marketplaceSystem.SealTenderDeal(dealToSeal))
            {
                if (sealDeal != null) sealDeal.SetEnabled(true);
                Debug.LogWarning("[Marketplace] SealTenderDeal failed. Button re-enabled.");
                return;
            }

            // Succès — SealTenderDeal a déjà déclenché SetAuctionState(Closing)
            // → HandleAuctionStateChanged(Closing) → ResetAllHighlights()
            // On affiche juste la notification et on ferme.
            Debug.Log("[Marketplace] Deal sealed successfully. Closing marketplace.");

            TopHUDWidget hud = FindObjectOfType<TopHUDWidget>();
            if (hud != null)
            {
                string acquired = dealToSeal.propertyOffers != null && dealToSeal.propertyOffers.Count > 0
                    ? string.Join(", ", dealToSeal.propertyOffers.Select(p => p.PropertyName))
                    : CurrencyFormatter.Format(dealToSeal.cashOffer);
                hud.ShowNotification($"DEAL SEALED — {acquired}", 4f);
            }

            CloseMarketplace();
        }

        // ─────────────────────────────────────────────────────────────────────────
        // FRAME 3.A — SELECT PLAYER (puis optionnellement SELECT PROPERTY)
        // ─────────────────────────────────────────────────────────────────────────

        private static readonly Color[] PlayerColors = new Color[]
        {
            new Color(1f, 0.24f, 0.27f, 1f),  // rouge   — P0
            new Color(0.25f, 0.55f, 1f, 1f),  // bleu    — P1
            new Color(0.15f, 0.85f, 0.45f, 1f),// vert   — P2
            new Color(1f, 0.65f, 0.1f, 1f),   // orange  — P3
            new Color(0.6f, 0.3f, 1f, 1f),    // violet  — P4
            new Color(1f, 0.85f, 0.1f, 1f),   // jaune   — P5
        };

        private void ShowPlayerSelection(SanctionChoice choice)
        {
            if (turnManager == null) { Debug.LogError("[Marketplace] ShowPlayerSelection: TurnManager null"); return; }
            if (financeSystem == null) financeSystem = FindObjectOfType<FinanceSystem>();
            if (financeSystem == null) { Debug.LogError("[Marketplace] ShowPlayerSelection: FinanceSystem null"); return; }
            if (marketplace_SelectPlayer == null)
            {
                marketplace_SelectPlayer = ResolveVisualTreeAsset(marketplace_SelectPlayer, "Marketplace_Select_Player");
                if (marketplace_SelectPlayer == null) { Debug.LogError("[Marketplace] Marketplace_Select_Player UXML not found — assign it in Inspector."); return; }
            }

            pendingSanctionChoice      = choice;
            pendingTargetPlayer        = null;
            pendingSanctionPropertyTarget = null;
            pendingSanctionPlayerTarget   = null;

            currentView = MarketplaceView.SelectPlayer;

            marketplace_SelectPlayer = ResolveVisualTreeAsset(marketplace_SelectPlayer, "Marketplace_Select_Player");
            SwitchView(marketplace_SelectPlayer);
            SetModal(true);
            if (root != null) root.style.display = DisplayStyle.Flex;

            // Titre + description de la sanction
            var titleLbl = root.Q<Label>("Lbl_SelectTitle");
            var descLbl  = root.Q<Label>("Lbl_SelectDesc");
            double sgo   = financeSystem.CurrentSgo;

            (string title, string desc) = choice switch
            {
                SanctionChoice.Embargo  => ("SELECT PLAYER",   $"EMBARGO — Cost: {CurrencyFormatter.Format(1.5 * sgo)}\nTarget property generates no rent for 3 turns."),
                SanctionChoice.Audit    => ("SELECT PLAYER",   $"TARGETED AUDIT — Cost: {CurrencyFormatter.Format(1.0 * sgo)}\nTarget pays 5% of their cash to the Bank."),
                SanctionChoice.Strike   => ("SELECT PLAYER",   $"PRODUCTION STRIKE — Cost: {CurrencyFormatter.Format(2.0 * sgo)}\nDouble Burn Rate on targeted family next GO."),
                SanctionChoice.Sabotage => ("SELECT PLAYER",   $"INDUSTRIAL SABOTAGE — Cost: {CurrencyFormatter.Format(5.0 * sgo)}\nDestroy 1 upgrade level on their property."),
                _                       => ("SELECT PLAYER",   "")
            };
            if (titleLbl != null) titleLbl.text = title;
            if (descLbl  != null) descLbl.text  = desc;

            // Construire la grille des joueurs adversaires
            var grid = root.Q<ScrollView>("Container_SelectGrid");
            if (grid == null) return;
            grid.Clear();

            // Rangée horizontale pour grille 2 colonnes
            VisualElement row = null;
            var attacker = turnManager.CurrentPlayer;
            var opponents = turnManager.Players
                .Where(p => p != null && !p.IsBankrupt && p.PlayerId != attacker?.PlayerId)
                .ToList();

            for (int i = 0; i < opponents.Count; i++)
            {
                // Nouvelle rangée toutes les 2 cartes
                if (i % 2 == 0)
                {
                    row = new VisualElement();
                    row.style.flexDirection = FlexDirection.Row;
                    row.style.justifyContent = Justify.SpaceBetween;
                    row.style.marginBottom = 4;
                    grid.Add(row);
                }

                var opp   = opponents[i];
                var card  = BuildPlayerCard(opp);
                row?.Add(card);
            }

            // Confirm bar (masquée au départ)
            var confirmBar  = root.Q<VisualElement>("Container_SelectConfirmBar");
            var confirmBtn  = root.Q<Button>("Btn_SelectConfirm");
            var costLbl     = root.Q<Label>("Lbl_SelectConfirmCost");
            if (confirmBar != null) confirmBar.style.display = DisplayStyle.None;

            if (confirmBtn != null)
            {
                confirmBtn.clicked += () => OnSanctionPlayerConfirmed(choice);
            }
        }

        private VisualElement BuildPlayerCard(PlayerModel player)
        {
            var card = new Button();
            card.AddToClassList("player-select-card");

            // Avatar coloré
            var avatar = new VisualElement();
            avatar.AddToClassList("player-avatar-circle");
            Color col = player.PlayerId < PlayerColors.Length ? PlayerColors[player.PlayerId] : Color.gray;
            avatar.style.backgroundColor = new StyleColor(new Color(col.r, col.g, col.b, 0.25f));
            avatar.style.borderTopColor = avatar.style.borderBottomColor =
            avatar.style.borderLeftColor = avatar.style.borderRightColor = new StyleColor(col);
            avatar.style.borderTopWidth = avatar.style.borderBottomWidth =
            avatar.style.borderLeftWidth = avatar.style.borderRightWidth = 1.5f;

            var initLabel = new Label(player.PlayerId.ToString());
            initLabel.style.color = new StyleColor(col);
            initLabel.style.fontSize = 13;
            initLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            initLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            avatar.Add(initLabel);

            // Infos texte
            var textStack = new VisualElement();
            textStack.style.flexDirection = FlexDirection.Column;
            textStack.style.flexGrow = 1;

            var nameLbl = new Label($"Player {player.PlayerId}{(player.IsAI ? " (AI)" : "")}");
            nameLbl.AddToClassList("player-select-name");

            // Phase E.1 — Dark Board : pastille WealthStatus à la place du count properties
            var wealthPill = BuildWealthPill(player);

            // Affiche aussi le nombre de propriétés (info publique, pas cachée par le Dark Board)
            var propsLbl = new Label($"{player.OwnedProperties.Count} owned");
            propsLbl.AddToClassList("player-select-sub");

            textStack.Add(nameLbl);
            textStack.Add(wealthPill);
            textStack.Add(propsLbl);

            card.Add(avatar);
            card.Add(textStack);

            card.clicked += () => SelectSanctionTarget(player, card);

            return card;
        }

        /// <summary>
        /// Phase E.1 — Construit une pastille Dark Board pour un joueur.
        /// La pastille n'expose JAMAIS le cash exact : juste un statut catégoriel
        /// (LOW / MID / HIGH / MOGUL) coloré selon WealthStatusHelper.
        /// </summary>
        private VisualElement BuildWealthPill(PlayerModel player)
        {
            var pill = new VisualElement();
            pill.AddToClassList("wealth-pill");

            double sgo = (financeSystem != null) ? financeSystem.CurrentSgo : 2000;
            WealthStatus status = WealthStatusHelper.GetStatus(player, sgo);
            string suffix = status.ToString().ToLowerInvariant(); // low / mid / high / mogul

            pill.AddToClassList($"wealth-pill--{suffix}");

            var dot = new VisualElement();
            dot.AddToClassList("wealth-pill-dot");
            dot.AddToClassList($"wealth-pill-dot--{suffix}");
            pill.Add(dot);

            var label = new Label(WealthStatusHelper.GetLabel(status));
            label.AddToClassList("wealth-pill-label");
            label.AddToClassList($"wealth-pill-label--{suffix}");
            pill.Add(label);

            // Conteneur en row pour aligner avec le nom
            pill.style.alignSelf = Align.FlexStart;
            pill.style.marginTop = 4;
            pill.style.marginBottom = 2;

            return pill;
        }

        private VisualElement _selectedPlayerCard = null;

        private void SelectSanctionTarget(PlayerModel player, VisualElement card)
        {
            pendingTargetPlayer = player;

            // Déselectionner la carte précédente
            if (_selectedPlayerCard != null)
                _selectedPlayerCard.RemoveFromClassList("card-selected");

            _selectedPlayerCard = card;
            card.AddToClassList("card-selected");

            // Afficher la confirm bar
            var confirmBar = root.Q<VisualElement>("Container_SelectConfirmBar");
            var costLbl    = root.Q<Label>("Lbl_SelectConfirmCost");
            if (confirmBar != null) confirmBar.style.display = DisplayStyle.Flex;
            if (costLbl != null)
            {
                double sgo  = financeSystem != null ? financeSystem.CurrentSgo : 0;
                double cost = pendingSanctionChoice switch
                {
                    SanctionChoice.Embargo  => 1.5 * sgo,
                    SanctionChoice.Audit    => 1.0 * sgo,
                    SanctionChoice.Strike   => 2.0 * sgo,
                    SanctionChoice.Sabotage => 5.0 * sgo,
                    _ => 0
                };
                costLbl.text = $"Target: Player {player.PlayerId} — Cost: {CurrencyFormatter.Format(cost)}";
            }
        }

        private void OnSanctionPlayerConfirmed(SanctionChoice choice)
        {
            if (pendingTargetPlayer == null) return;

            if (choice == SanctionChoice.Audit)
            {
                // Audit ne cible pas une propriété — on exécute directement
                pendingSanctionPlayerTarget = pendingTargetPlayer;
                ConfirmPendingSanction();
                CloseMarketplace();
            }
            else
            {
                // Les autres sanctions ciblent une propriété — passer à la sélection de propriété
                ShowPropertySelectionForSanction(choice, pendingTargetPlayer);
            }
        }

        private void ShowPropertySelectionForSanction(SanctionChoice choice, PlayerModel targetPlayer)
        {
            if (targetPlayer == null || targetPlayer.OwnedProperties.Count == 0)
            {
                Debug.LogWarning("[Marketplace] Target player has no properties to sanction.");
                return;
            }

            // Réutiliser le même panel — changer titre, vider grille, reconstruire avec propriétés
            var titleLbl = root.Q<Label>("Lbl_SelectTitle");
            var descLbl  = root.Q<Label>("Lbl_SelectDesc");
            if (titleLbl != null) titleLbl.text = "SELECT PROPERTY";
            if (descLbl  != null) descLbl.text  = $"Pick a property owned by Player {targetPlayer.PlayerId}";

            var grid = root.Q<ScrollView>("Container_SelectGrid");
            if (grid == null) return;
            grid.Clear();

            // Masquer confirm bar jusqu'à sélection
            var confirmBar = root.Q<VisualElement>("Container_SelectConfirmBar");
            var confirmBtn = root.Q<Button>("Btn_SelectConfirm");
            if (confirmBar != null) confirmBar.style.display = DisplayStyle.None;
            _selectedPlayerCard = null;

            // Recâbler le bouton confirm pour la propriété
            if (confirmBtn != null)
            {
                // Retirer l'ancien listener en remplaçant le bouton (clone)
                confirmBtn.clicked -= () => OnSanctionPlayerConfirmed(choice);
                confirmBtn.clicked += () => {
                    if (pendingSanctionPropertyTarget != null)
                    {
                        pendingSanctionPlayerTarget = targetPlayer;
                        ConfirmPendingSanction();
                        CloseMarketplace();
                    }
                };
            }

            // Construire la liste des propriétés du joueur cible
            foreach (var prop in targetPlayer.OwnedProperties)
            {
                var card = BuildPropertyCard(prop);
                grid.Add(card);
            }
        }

        private VisualElement BuildPropertyCard(PropertyData prop)
        {
            var card = new Button();
            card.AddToClassList("property-select-card");

            var nameLbl = new Label(prop.PropertyName);
            nameLbl.AddToClassList("seller-prop-name");

            var infoLbl = new Label($"Level {prop.CurrentLevel} — Rent: {CurrencyFormatter.Format(prop.BaseRent)}");
            infoLbl.AddToClassList("seller-prop-income");
            infoLbl.style.fontSize = 11;

            var textStack = new VisualElement();
            textStack.style.flexGrow = 1;
            textStack.Add(nameLbl);
            textStack.Add(infoLbl);
            card.Add(textStack);

            card.clicked += () =>
            {
                // Déselectionner la précédente
                if (_selectedPlayerCard != null)
                    _selectedPlayerCard.RemoveFromClassList("card-selected");
                _selectedPlayerCard = card;
                card.AddToClassList("card-selected");

                pendingSanctionPropertyTarget = prop;

                var confirmBar = root.Q<VisualElement>("Container_SelectConfirmBar");
                var costLbl    = root.Q<Label>("Lbl_SelectConfirmCost");
                if (confirmBar != null) confirmBar.style.display = DisplayStyle.Flex;
                if (costLbl   != null) costLbl.text = $"Target: {prop.PropertyName}";
            };

            return card;
        }

        // ─────────────────────────────────────────────────────────────────────────

        private void BeginSanctionTargetSelection(SanctionChoice choice)
        {
            pendingSanctionChoice = choice;
            pendingSanctionPropertyTarget = null;
            pendingSanctionPlayerTarget = null;

            var confirmContainer = root.Q<VisualElement>("Container_SanctionConfirm");
            var confirmLabel = root.Q<Label>("Lbl_SanctionConfirm");
            if (confirmContainer != null) confirmContainer.style.display = DisplayStyle.Flex;
            if (confirmLabel != null) confirmLabel.text = "Select a target on the board";

            EnterSelectionMode(SelectionContext.SanctionTarget);
        }

        private void ShowSanctionConfirm()
        {
            var confirmContainer = root.Q<VisualElement>("Container_SanctionConfirm");
            var confirmLabel = root.Q<Label>("Lbl_SanctionConfirm");
            if (confirmContainer == null || confirmLabel == null) return;

            if (financeSystem == null) financeSystem = FindObjectOfType<FinanceSystem>();
            double sgo = financeSystem != null ? financeSystem.CurrentSgo : 0;

            double cost = pendingSanctionChoice switch
            {
                SanctionChoice.Embargo => 1.5 * sgo,
                SanctionChoice.Audit => 1.0 * sgo,
                SanctionChoice.Strike => 2.0 * sgo,
                SanctionChoice.Sabotage => 5.0 * sgo,
                _ => 0
            };

            confirmContainer.style.display = DisplayStyle.Flex;
            confirmLabel.text = $"Confirm cost: {CurrencyFormatter.Format(cost)}";
        }

        private void ConfirmPendingSanction()
        {
            if (marketplaceSystem == null) marketplaceSystem = FindObjectOfType<MarketplaceSystem>();
            if (turnManager == null) turnManager = FindObjectOfType<TurnManager>();
            if (financeSystem == null) financeSystem = FindObjectOfType<FinanceSystem>();
            if (marketplaceSystem == null || turnManager == null) return;

            PlayerModel attacker = turnManager.CurrentPlayer;
            if (attacker == null) return;

            if (pendingSanctionChoice == SanctionChoice.Embargo)
            {
                marketplaceSystem.ExecuteEmbargo(attacker, pendingSanctionPropertyTarget);
            }
            else if (pendingSanctionChoice == SanctionChoice.Audit)
            {
                marketplaceSystem.ExecuteAudit(attacker, pendingSanctionPlayerTarget);
            }
            else if (pendingSanctionChoice == SanctionChoice.Strike)
            {
                marketplaceSystem.ExecuteStrike(attacker, pendingSanctionPropertyTarget);
            }
            else if (pendingSanctionChoice == SanctionChoice.Sabotage)
            {
                marketplaceSystem.ExecuteSabotage(attacker, pendingSanctionPropertyTarget);
            }

            pendingSanctionPropertyTarget = null;
            pendingSanctionPlayerTarget = null;
            pendingSanctionChoice = SanctionChoice.None;

            var confirmContainer = root.Q<VisualElement>("Container_SanctionConfirm");
            if (confirmContainer != null) confirmContainer.style.display = DisplayStyle.None;
        }

        private void SetModal(bool modal)
        {
            if (viewRoot == null) return;
            viewRoot.EnableInClassList("marketplace-modal", modal);
            viewRoot.EnableInClassList("marketplace-nonmodal", !modal);
            viewRoot.pickingMode = modal ? PickingMode.Position : PickingMode.Ignore;
        }

        private void SetSelectionMode(bool enabled)
        {
            if (turnManager != null) turnManager.IsInSelectionMode = enabled;
        }

        private void StopPulses(bool resetToOriginal)
        {
            foreach (var t in pulsingTiles)
            {
                if (t != null) t.EndPulse(resetToOriginal);
            }
            pulsingTiles.Clear();
        }

        private void HighlightOwnedProperties(Color pulseColor)
        {
            if (turnManager == null) return;
            var player = turnManager.CurrentPlayer;
            if (player == null) return;
            var board = BoardManager.Instance;
            if (board == null) return;

            foreach (var prop in player.OwnedProperties)
            {
                int tileIndex = FindTileIndexForProperty(prop);
                if (tileIndex < 0) continue;
                var tv = GetTileVisuals(tileIndex);
                if (tv == null) continue;
                tv.BeginPulse(pulseColor);
                pulsingTiles.Add(tv);
            }
        }

        private void HighlightTargetableProperties()
        {
            if (turnManager == null) return;
            var attacker = turnManager.CurrentPlayer;
            if (attacker == null) return;
            var board = BoardManager.Instance;
            if (board == null) return;

            if (!ColorUtility.TryParseHtmlString("#c27aff", out Color purple))
            {
                purple = Color.magenta;
            }

            foreach (var tile in board.Tiles)
            {
                var prop = tile.Data as PropertyData;
                if (prop == null) continue;
                var owner = turnManager.GetOwnerOfProperty(prop);
                if (owner == null || owner.PlayerId == attacker.PlayerId) continue;
                var tv = GetTileVisuals(tile.TileIndex);
                if (tv == null) continue;
                tv.BeginPulse(purple);
                pulsingTiles.Add(tv);
            }
        }

        private int FindTileIndexForProperty(PropertyData property)
        {
            if (property == null) return -1;
            var board = BoardManager.Instance;
            if (board == null) return -1;

            foreach (var tile in board.Tiles)
            {
                if (tile != null && tile.Data == property) return tile.TileIndex;
            }
            return -1;
        }

        private PolyRisk.Visuals.TileVisuals GetTileVisuals(int tileIndex)
        {
            var board = BoardManager.Instance;
            if (board == null || board.visualTiles == null) return null;
            if (tileIndex < 0 || tileIndex >= board.visualTiles.Length) return null;
            var t = board.visualTiles[tileIndex];
            if (t == null) return null;
            return t.GetComponentInChildren<PolyRisk.Visuals.TileVisuals>();
        }

        private void AddSelectedPropertyToTenderUI(PropertyData property, PlayerModel currentPlayer)
        {
            ScrollView list = root.Q<ScrollView>("Container_OwnedAssets");
            if (list == null) return;

            // Use simplified seller row (Nom + Income) — not the AuctionItem bid component
            VisualElement row = new VisualElement();
            row.AddToClassList("card-seller-row");

            Label propName = new Label(property.PropertyName);
            propName.AddToClassList("seller-prop-name");
            row.Add(propName);

            // Income indicator: Base rent or formatted
            double rent = property.BaseRent;
            Label incomeLabel = new Label($"Income: {CurrencyFormatter.Format(rent)} / turn");
            incomeLabel.AddToClassList("seller-prop-income");
            row.Add(incomeLabel);

            // Add Deselect button
            Button deselectBtn = new Button { text = "X" };
            deselectBtn.AddToClassList("btn-standard");
            deselectBtn.style.backgroundColor = new StyleColor(new Color(0.8f, 0.2f, 0.2f));
            deselectBtn.style.width = 30;
            deselectBtn.style.height = 30;
            deselectBtn.style.marginLeft = 10;
            deselectBtn.clicked += () => {
                int prevIndex = FindTileIndexForProperty(property);
                if (prevIndex >= 0)
                {
                    var prevTv = GetTileVisuals(prevIndex);
                    if (prevTv != null)
                    {
                        prevTv.EndPulse(false);
                        prevTv.ResetHighlight();
                        // Tile back to available — restart pulse
                        prevTv.BeginPulse(GetNeonGreen());
                        if (!pulsingTiles.Contains(prevTv)) pulsingTiles.Add(prevTv);
                    }
                }
                selectedProperties.Remove(property);
                list.Clear();

                Button cta = root.Q<Button>("Btn_PickProperty");
                if (cta != null) cta.style.display = DisplayStyle.Flex;

                Button confirmBtn = root.Q<Button>("Btn_ConfirmTender");
                if (confirmBtn != null) confirmBtn.SetEnabled(false);
            };
            row.Add(deselectBtn);

            list.Add(row);
            list.MarkDirtyRepaint();
        }

        private void AddBidPropertyToUI(PropertyData property, VisualElement list)
        {
            if (list == null || property == null) return;

            VisualElement row = new VisualElement();
            row.AddToClassList("card-seller-row");

            Label propName = new Label(property.PropertyName);
            propName.AddToClassList("seller-prop-name");
            row.Add(propName);

            double price = property.BasePrice;
            Label incomeLabel = new Label($"Value: {CurrencyFormatter.Format(price)}");
            incomeLabel.AddToClassList("seller-prop-income");
            row.Add(incomeLabel);

            Button deselectBtn = new Button { text = "✕" };
            deselectBtn.AddToClassList("btn-standard");
            deselectBtn.style.backgroundColor = new StyleColor(new Color(0.75f, 0.15f, 0.15f, 1f));
            deselectBtn.style.width = 30;
            deselectBtn.style.height = 30;
            deselectBtn.style.borderTopLeftRadius = deselectBtn.style.borderTopRightRadius =
            deselectBtn.style.borderBottomLeftRadius = deselectBtn.style.borderBottomRightRadius = 8;
            deselectBtn.style.marginLeft = 10;
            deselectBtn.style.paddingTop = deselectBtn.style.paddingBottom =
            deselectBtn.style.paddingLeft = deselectBtn.style.paddingRight = 0;

            deselectBtn.clicked += () => {
                int prevIndex = FindTileIndexForProperty(property);
                if (prevIndex >= 0)
                {
                    var prevTv = GetTileVisuals(prevIndex);
                    if (prevTv != null)
                    {
                        prevTv.EndPulse(false);
                        prevTv.ResetHighlight();
                        prevTv.BeginPulse(GetCyan());
                        if (!pulsingTiles.Contains(prevTv)) pulsingTiles.Add(prevTv);
                    }
                }
                selectedProperties.Remove(property);

                list.Clear();
                foreach (var prop in selectedProperties)
                    AddBidPropertyToUI(prop, list);

                // Réafficher le bouton pick (< 2 props maintenant)
                Button pickBtn = root?.Q<Button>("Btn_PickPropertyBid");
                if (pickBtn != null) pickBtn.style.display = DisplayStyle.Flex;

                // Réactiver le mode sélection 3D si on était à la limite
                if (selectedProperties.Count < 2)
                {
                    SetSelectionMode(true);
                    SetModal(false);
                }

                var slider = viewRoot?.Q<SliderInt>("Slider_CashOffer");
                UpdateBidderTotal(slider?.value ?? 0);
            };
            row.Add(deselectBtn);

            list.Add(row);
        }
    }
}
