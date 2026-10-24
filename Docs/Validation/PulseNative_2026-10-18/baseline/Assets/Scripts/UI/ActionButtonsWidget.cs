using UnityEngine;
using UnityEngine.UIElements;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;
using PolyRisk.Core;
using PolyRisk.Systems;
using PolyRisk.Multiplayer;

namespace PolyRisk.UI
{
    /// <summary>
    /// Manages the primary/secondary action buttons (UI Toolkit) ET l'inspection de
    /// tuile (fusion avec l'ancien InspectorWidget, retiré — cf. InspectorWidget.cs).
    /// Le nom de la classe est conservé tel quel : TurnManager.cs et
    /// Marketplace_UIManager.cs y font référence par type (ClearButtons(),
    /// FindObjectOfType + SetActive) — un renommage casserait ces 6 call-sites pour
    /// zéro bénéfice fonctionnel.
    ///
    /// Les anciens boutons/textes uGUI ont été supprimés de la scène (remplacés par
    /// TileDetail_BottomSheet.uxml) — plus de chemin legacy à maintenir ici.
    /// </summary>
    public class ActionButtonsWidget : MonoBehaviour
    {
        private static bool transientDicePresentationActive;
        private static readonly Dictionary<VisualElement, DisplayStyle> transientDicePanelDisplayStates =
            new Dictionary<VisualElement, DisplayStyle>();

        [Header("System References")]
        [Tooltip("Reference to the RulesEngine for action execution.")]
        public RulesEngine rulesEngine;
        [Tooltip("Reference to the TurnManager for player context.")]
        public TurnManager turnManager;
        [Tooltip("Reference to the AbilitySystem for Ultimate activation.")]
        public AbilitySystem abilitySystem;
        [Tooltip("Reference to the BoardManager for monopoly checks.")]
        public BoardManager boardManager;

        [Header("UI References")]
        [Tooltip("Reference to the new UI Toolkit Marketplace UIManager.")]
        public Marketplace_UIManager marketplaceUIManager;

        [Header("UI Toolkit")]
        [Tooltip("UIDocument hôte de TileDetail_BottomSheet.uxml.")]
        public UIDocument uiDocument;
        public VisualTreeAsset bottomSheetTemplate;

        // Refs UI Toolkit résolues une fois au Start
        private Button btnPrimary, btnSecondary, btnTertiary;
        private VisualElement buySlideTrack, buySlideHandle;
        private Label buySlideLabel;
        private bool buySlideDragging;
        private int buySlidePointerId = -1;
        private float buySlideStartX;
        private float buySlideProgress;
        private bool lastReducedMotion;
        private Label lblTileName, lblTileFamily, lblTileEffect, lblTileRent, lblTileRentTrend, lblTileRisk, lblTileRoi, lblTileRoiTrend;
        private VisualElement tileInspectorHeading, tileStats, containerTileRisk, iconTileFamily, tileDetailPanel, abilityPickerOverlay, investmentPickerOverlay;
        private VisualElement pulsePanel, pulseDetails, pulseTabContent;
        private Button pulseToggle, pulseInspectorTab, pulsePortfolioTab, pulseGoTab, pulseGoSummary;
        private Button pulseFamilyProgress;
        private Label pulseToggleState, pulseOwnership, pulseGoBase, pulseGoCosts, pulseGoNet;
        private Label pulseFamilyOwned, pulseFamilyTotal;
        private VisualElement pulseFamilySlots;
        private Label primaryActionTitle, primaryActionValue;
        private string pulseActiveTab = "inspector";
        private bool pulseExpanded;
        private VisualElement gameStateDock, gameStateHistory, gameStateHistoryList;
        private Label lblGameStateTitle, lblGameStateMessage;
        private Button btnGameStateHistory, btnSkipGameEvent;
        private readonly List<GameStateHistoryItem> gameStateTimeline = new List<GameStateHistoryItem>();
        private readonly HashSet<string> gameStateActivityKeys = new HashSet<string>();
        private readonly HashSet<long> gameStateEventIds = new HashSet<long>();
        private GameEventPresenter gameEventPresenter;
        private GameEvent activeDockEvent;
        private int gameEventPresentationGeneration;
        private string pendingTargetedUltimate;
        private int pendingInvestmentDuration = 3;
        private int pendingInvestmentPreset = 5;
        private readonly List<PolyRisk.Visuals.TileVisuals> abilityTargetPulses = new List<PolyRisk.Visuals.TileVisuals>();

        public bool IsAbilityTargetSelectionActive => !string.IsNullOrEmpty(pendingTargetedUltimate);

        private BoardTile currentSelectedTile;
        private float lastClickTime = 0f;
        private const float CLICK_COOLDOWN = 0.5f;

        private sealed class GameStateHistoryItem
        {
            public GameActivityEntry Activity;
            public GameEvent Event;
        }

        private void OnEnable()
        {
            GameEvents.OnTileSelected += OnTileSelected;
            GameEvents.OnTurnStarted += OnTurnStarted;
            GameEvents.OnActivityPublished += OnGameActivityPublished;
            gameEventPresenter = new GameEventPresenter
            {
                LocalPlayerId = LocalSeatIndex(),
                ReducedMotion = UIUXRuntime.ReducedMotion,
                ShouldQueue = GameEventTextFormatter.IsDockEvent
            };
            gameEventPresenter.TraceRecorded += OnGameEventTraceRecorded;
        }

        private void OnDisable()
        {
            ResetBuySlide();
            GameEvents.OnTileSelected -= OnTileSelected;
            GameEvents.OnTurnStarted -= OnTurnStarted;
            GameEvents.OnActivityPublished -= OnGameActivityPublished;
            gameEventPresentationGeneration++;
            activeDockEvent = null;
            if (gameEventPresenter != null)
            {
                gameEventPresenter.TraceRecorded -= OnGameEventTraceRecorded;
                gameEventPresenter.Dispose();
                gameEventPresenter = null;
            }
            gameStateDock?.RemoveFromClassList("game-state-dock--presenting");
            CloseUltimatePicker();
            CloseInvestmentPicker();
        }

        private void OnTurnStarted(int playerId)
        {
            if (turnManager == null) return;
            UpdateGameStateDock();
            
            // Reset selection for the new turn
            currentSelectedTile = null;
            pulseActiveTab = "inspector";
            SetPulseExpanded(false);
            UpdateInspectorSection(null);
            CloseInvestmentPicker();

            PlayerModel activePlayer = turnManager.GetPlayerById(playerId);
            if (activePlayer != null)
            {
                if (!IsLocalPlayer(activePlayer))
                {
                    SetPrimaryButtonState("PLAYER TURN", false);
                    SetSecondaryButtonState(string.Empty, false);
                    return;
                }
                if (activePlayer.IsAI)
                {
                    // Force the UI to show AI TURN immediately, before the AI even rolls the dice
                    SetPrimaryButtonState("AI TURN", false);
                    SetSecondaryButtonState(string.Empty, false);
                }
                else
                {
                    // Check for Audit state
                    if (activePlayer.IsInAudit)
                    {
                         double sgo = turnManager.financeSystem != null ? turnManager.financeSystem.CurrentSgo : 500;
                         SetPrimaryButtonState($"PAY FINE ({CurrencyFormatter.Format(sgo)})", true);
                         
                         if (turnManager.IsJustLandedInAudit)
                         {
                             SetSecondaryButtonState("STAY IN AUDIT", true);
                         }
                         else
                         {
                             SetSecondaryButtonState("ROLL DOUBLES", true);
                         }
                    }
                    else
                    {
                        // Replicated turns can move directly from Planning to Planning.
                        // Refresh now; Update's state-change cache will not detect that turn.
                        UpdateButtonsState(null, activePlayer);
                    }
                }
            }
        }
        private void Start()
        {
             if (marketplaceUIManager == null)
                 marketplaceUIManager = FindObjectOfType<Marketplace_UIManager>();
             if (abilitySystem == null)
                 abilitySystem = AbilitySystem.Instance ?? FindObjectOfType<AbilitySystem>();
             if (boardManager == null)
                 boardManager = BoardManager.Instance ?? FindObjectOfType<BoardManager>();

             TryBindUIToolkit();

             // Initial check
             if (currentSelectedTile == null)
                 UpdateButtonsState(null, null);
             UpdateInspectorSection(null);
        }

        public static void SetTransientDicePresentation(bool active)
        {
            if (transientDicePresentationActive == active) return;
            transientDicePresentationActive = active;

            if (!active)
            {
                foreach (var entry in transientDicePanelDisplayStates)
                {
                    if (entry.Key != null)
                        entry.Key.style.display = entry.Value;
                }
                transientDicePanelDisplayStates.Clear();
                return;
            }

            foreach (var document in FindObjectsOfType<UIDocument>(true))
            {
                var panel = document.rootVisualElement?.Q<VisualElement>("Panel_TileDetail");
                SetPanelTransientVisibility(panel, true);
            }
        }

        private static void SetPanelTransientVisibility(VisualElement panel, bool hidden)
        {
            if (panel == null) return;
            if (hidden)
            {
                if (!transientDicePanelDisplayStates.ContainsKey(panel))
                    transientDicePanelDisplayStates.Add(panel, panel.resolvedStyle.display);
                panel.style.display = DisplayStyle.None;
                return;
            }

            if (transientDicePanelDisplayStates.TryGetValue(panel, out var previousDisplay))
            {
                panel.style.display = previousDisplay;
                transientDicePanelDisplayStates.Remove(panel);
            }
        }

        private void TryBindUIToolkit()
        {
            if (uiDocument == null) return;
            var root = uiDocument.rootVisualElement;
            if (root == null) return;

            if (bottomSheetTemplate != null && root.Q<VisualElement>("Panel_TileDetail") == null)
                bottomSheetTemplate.CloneTree(root);

            UIUXRuntime.PrepareRoot(root);
            UIUXRuntime.PrepareView(root.Q<VisualElement>("Panel_TileDetail"));

            btnPrimary = root.Q<Button>("Btn_Primary");
            btnSecondary = root.Q<Button>("Btn_Secondary");
            btnTertiary = root.Q<Button>("Btn_Tertiary");
            primaryActionTitle = root.Q<Label>("Lbl_PrimaryTitle");
            primaryActionValue = root.Q<Label>("Lbl_PrimaryValue");
            buySlideTrack = root.Q<VisualElement>("Slider_Buy");
            buySlideHandle = root.Q<VisualElement>("Handle_BuySlide");
            buySlideLabel = root.Q<Label>("Lbl_BuySlide");
            if (buySlideTrack != null)
            {
                buySlideTrack.focusable = true;
                buySlideTrack.tooltip = "Slide right to buy. Press Enter or Space with a keyboard.";
                buySlideTrack.RegisterCallback<KeyDownEvent>(OnBuySlideKeyDown);
            }
            if (buySlideHandle != null)
            {
                buySlideHandle.RegisterCallback<PointerDownEvent>(OnBuySlidePointerDown);
                buySlideHandle.RegisterCallback<PointerMoveEvent>(OnBuySlidePointerMove);
                buySlideHandle.RegisterCallback<PointerUpEvent>(OnBuySlidePointerUp);
                buySlideHandle.RegisterCallback<PointerCaptureOutEvent>(_ => ResetBuySlide());
            }
            lastReducedMotion = UIUXRuntime.ReducedMotion;
            tileDetailPanel = root.Q<VisualElement>("Panel_TileDetail");
            pulsePanel = tileDetailPanel;
            pulseDetails = root.Q<VisualElement>("PulseDetails");
            pulseTabContent = root.Q<VisualElement>("PulseTabContent");
            pulseToggle = root.Q<Button>("Btn_PulseToggle");
            pulseToggleState = root.Q<Label>("Lbl_PulseToggleState");
            pulseInspectorTab = root.Q<Button>("Btn_PulseInspector");
            pulsePortfolioTab = root.Q<Button>("Btn_PulsePortfolio");
            pulseGoTab = root.Q<Button>("Btn_PulseGo");
            pulseGoSummary = root.Q<Button>("Btn_PulseGoSummary");
            pulseFamilyProgress = root.Q<Button>("Btn_PulseFamilyProgress");
            pulseFamilyOwned = root.Q<Label>("Lbl_PulseFamilyOwned");
            pulseFamilyTotal = root.Q<Label>("Lbl_PulseFamilyTotal");
            pulseFamilySlots = root.Q<VisualElement>("PulseFamilySlots");
            pulseOwnership = root.Q<Label>("Lbl_PulseOwnership");
            pulseGoBase = root.Q<Label>("Lbl_PulseGoBase");
            pulseGoCosts = root.Q<Label>("Lbl_PulseGoCosts");
            pulseGoNet = root.Q<Label>("Lbl_PulseGoNet");
            if (pulseToggle != null) pulseToggle.clicked += TogglePulsePanel;
            if (pulseInspectorTab != null) pulseInspectorTab.clicked += () => SetPulseTab("inspector");
            if (pulsePortfolioTab != null) pulsePortfolioTab.clicked += () => SetPulseTab("portfolio");
            if (pulseGoTab != null) pulseGoTab.clicked += () => SetPulseTab("go");
            if (pulseGoSummary != null) pulseGoSummary.clicked += OpenPulseGoFlow;
            if (pulseFamilyProgress != null) pulseFamilyProgress.clicked += OpenPulseInspector;
            UpdatePulseToggleLabel();
            if (transientDicePresentationActive)
                SetPanelTransientVisibility(tileDetailPanel, true);
            gameStateDock = root.Q<VisualElement>("GameStateDock");
            lblGameStateTitle = root.Q<Label>("Lbl_GameStateTitle");
            lblGameStateMessage = root.Q<Label>("Lbl_GameStateMessage");
            gameStateHistory = root.Q<VisualElement>("GameStateHistory");
            gameStateHistoryList = root.Q<VisualElement>("GameStateHistoryList");
            btnGameStateHistory = root.Q<Button>("Btn_GameStateHistory");
            btnSkipGameEvent = root.Q<Button>("Btn_SkipGameEvent");
            if (btnGameStateHistory != null) btnGameStateHistory.clicked += ToggleGameStateHistory;
            if (btnSkipGameEvent != null) btnSkipGameEvent.clicked += SkipCurrentGameEventPresentation;
            RenderGameStateHistory();
            UpdateGameStateDock();
            BeginNextGameEventPresentation();
            tileInspectorHeading = root.Q<VisualElement>(className: "tile-inspector-heading");
            lblTileName = root.Q<Label>("Lbl_TileName");
            lblTileFamily = root.Q<Label>("Lbl_TileFamily");
            lblTileEffect = root.Q<Label>("Lbl_TileEffect");
            lblTileRent = root.Q<Label>("Lbl_TileRent");
            lblTileRentTrend = root.Q<Label>("Lbl_TileRentTrend");
            lblTileRisk = root.Q<Label>("Lbl_TileRisk");
            lblTileRoi = root.Q<Label>("Lbl_TileRoi");
            lblTileRoiTrend = root.Q<Label>("Lbl_TileRoiTrend");
            containerTileRisk = root.Q<VisualElement>("Container_TileRisk");
            tileStats = root.Q<VisualElement>(className: "tile-stat-row");
            iconTileFamily = root.Q<VisualElement>("Icon_TileFamily");

            // Start() ne s'exécute qu'une fois par durée de vie du composant — pas de
            // risque de double-abonnement.
            if (btnPrimary != null) btnPrimary.clicked += OnPrimaryClicked;
            if (btnSecondary != null) btnSecondary.clicked += OnSecondaryClicked;
            if (btnTertiary != null) btnTertiary.clicked += OnTertiaryClicked;
            SetPulseExpanded(pulseExpanded);
        }

        public void SetPresentationVisible(bool visible)
        {
            // Keep UIDocument and event listeners alive while a modal covers the HUD.
            // Disabling the GameObject recreates the document and invalidates button refs.
            if (uiDocument != null && uiDocument.rootVisualElement != null)
                uiDocument.rootVisualElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible || turnManager == null) return;
            var current = turnManager.CurrentPlayer;
            if (turnManager.CurrentState == TurnManager.TurnState.ResolveTile && current != null)
                currentSelectedTile = boardManager != null
                    ? boardManager.GetTileAt(turnManager.GetPlayerPosition(current)) : currentSelectedTile;
            UpdateInspectorSection(currentSelectedTile);
            RenderPulseDetails();
            UpdateButtonsState(currentSelectedTile, current);
        }

        public void OnPrimaryClicked()
        {
            if (Time.time - lastClickTime < CLICK_COOLDOWN) return;
            lastClickTime = Time.time;

            if (rulesEngine == null || turnManager == null)
            {
                string missing = "";
                if (rulesEngine == null) missing += "RulesEngine ";
                if (turnManager == null) missing += "TurnManager ";
                Debug.LogError($"[UI] Missing references in ActionButtonsWidget: {missing}");
                return;
            }

            PlayerModel currentPlayer = turnManager.CurrentPlayer;
            if (!IsLocalPlayer(currentPlayer)) return;
            if (currentPlayer != null && !currentPlayer.IsAI)
            {
                if (turnManager.CurrentState == TurnManager.TurnState.ChooseTurbo)
                {
                    if (OnlineGameController.Instance != null
                        && OnlineGameController.Instance.TrySubmitLocalCommand("dice.turbo", "{\"choice\":0}")) return;
                    turnManager.ConfirmTurboChoice(0);
                    return;
                }
                if (turnManager.CurrentState == TurnManager.TurnState.AdjustDice)
                {
                    if (OnlineGameController.Instance != null
                        && OnlineGameController.Instance.TrySubmitLocalCommand("dice.adjust", "{\"delta\":-1}"))
                        return;
                    turnManager.ConfirmAutoPrecision(-1);
                    return;
                }

                if (turnManager.CurrentState == TurnManager.TurnState.WaitingForTransitChoice)
                {
                    if (OnlineGameController.Instance != null
                        && OnlineGameController.Instance.TrySubmitLocalCommand(
                            "transit.choose", "{\"takeTransit\":true}")) return;
                    turnManager.ResolveTransitExpressChoice(true);
                    return;
                }

                // Phase D — PLANNING PHASE : Primary = ROLL DICE
                if (turnManager.CurrentState == TurnManager.TurnState.Planning)
                {
                    if (OnlineGameController.Instance != null
                        && OnlineGameController.Instance.TrySubmitLocalCommand("turn.roll"))
                        return;
                    turnManager.EndPlanningPhase();
                    return;
                }

                // Handle Audit Actions
                if (currentPlayer.IsInAudit)
                {
                    if (OnlineGameController.Instance != null
                        && OnlineGameController.Instance.TrySubmitLocalCommand("audit.pay"))
                        return;
                    turnManager.AuditPayFine();
                    return;
                }

                if (currentSelectedTile == null)
                {
                    Debug.LogWarning("[UI] No tile selected to interact with.");
                    return;
                }

                if (currentSelectedTile.TileIndex == 20)
                {
                    if (marketplaceUIManager != null)
                    {
                        marketplaceUIManager.OpenMarketplace();
                    }
                    else
                    {
                        Debug.LogError("Marketplace_UIManager reference is missing in ActionButtonsWidget!");
                    }
                    return;
                }

                if (turnManager.CurrentState == TurnManager.TurnState.ResolveTile
                    && currentSelectedTile.Type == TileType.StockMarket)
                {
                    ShowInvestmentPicker(currentPlayer);
                    return;
                }


                if (turnManager.CurrentState == TurnManager.TurnState.ResolveTile
                    && OnlineGameController.Instance != null
                    && OnlineGameController.Instance.TrySubmitLocalCommand("action.primary"))
                    return;

                // Affordability Check Override
                if (currentSelectedTile.Type == TileType.Property && currentSelectedTile.Data is PropertyData prop)
                {
                    if (!currentPlayer.OwnedProperties.Contains(prop) && currentPlayer.CurrentBalance < prop.BasePrice)
                    {
                        // Cannot afford -> Primary acts as SKIP
                        rulesEngine.SkipOptionalAction();
                        UpdateButtonsState(currentSelectedTile, currentPlayer);
                        return;
                    }
                }

                rulesEngine.ExecutePrimaryAction(currentPlayer, currentSelectedTile);
                // Refresh buttons?
                UpdateButtonsState(currentSelectedTile, currentPlayer);
            }
        }

        public void OnSecondaryClicked()
        {
             if (Time.time - lastClickTime < CLICK_COOLDOWN) return;
             lastClickTime = Time.time;

             if (turnManager == null)
             {
                 Debug.LogError("[UI] TurnManager reference missing in ActionButtonsWidget.");
                 return;
             }
             
             PlayerModel currentPlayer = turnManager.CurrentPlayer;
             if (!IsLocalPlayer(currentPlayer)) return;

             if (currentPlayer != null && !currentPlayer.IsAI)
             {
                if (turnManager.CurrentState == TurnManager.TurnState.ChooseTurbo)
                {
                    if (OnlineGameController.Instance != null
                        && OnlineGameController.Instance.TrySubmitLocalCommand("dice.turbo", "{\"choice\":1}")) return;
                    turnManager.ConfirmTurboChoice(1);
                    return;
                }
                 if (turnManager.CurrentState == TurnManager.TurnState.AdjustDice)
                 {
                     if (OnlineGameController.Instance != null
                         && OnlineGameController.Instance.TrySubmitLocalCommand("dice.adjust", "{\"delta\":0}"))
                         return;
                     turnManager.ConfirmAutoPrecision(0);
                     return;
                 }

                 if (turnManager.CurrentState == TurnManager.TurnState.WaitingForTransitChoice)
                 {
                     if (OnlineGameController.Instance != null
                         && OnlineGameController.Instance.TrySubmitLocalCommand(
                             "transit.choose", "{\"takeTransit\":false}")) return;
                     turnManager.ResolveTransitExpressChoice(false);
                     return;
                 }

                 // Phase D — PLANNING PHASE : Secondary = Use Ultimate (si dispo) sinon PASS
                 if (turnManager.CurrentState == TurnManager.TurnState.Planning)
                 {
                     if (GetAvailableUltimates(currentPlayer).Count > 0)
                     {
                         ShowUltimatePicker(currentPlayer);
                     }
                    else
                    {
                        // PASS = roll dice directement
                        if (OnlineGameController.Instance != null
                            && OnlineGameController.Instance.TrySubmitLocalCommand("turn.roll"))
                            return;
                        turnManager.EndPlanningPhase();
                    }
                     return;
                 }

                 // Handle Audit Actions
                 if (currentPlayer.IsInAudit)
                {
                    if (currentPlayer.HasGetOutOfAuditCard)
                    {
                         if (OnlineGameController.Instance != null
                             && OnlineGameController.Instance.TrySubmitLocalCommand("audit.card"))
                             return;
                         // Button logic for Escape Card
                         turnManager.UseEscapeCard();
                         return;
                    }

                    if (turnManager.IsJustLandedInAudit)
                    {
                        if (OnlineGameController.Instance != null
                            && OnlineGameController.Instance.TrySubmitLocalCommand("audit.stay"))
                            return;
                        turnManager.StayInAudit();
                    }
                    else
                    {
                        if (OnlineGameController.Instance != null
                            && OnlineGameController.Instance.TrySubmitLocalCommand("audit.roll"))
                            return;
                        turnManager.AuditRollDice();
                    }
                    return;
                }

                 if (turnManager.CurrentState == TurnManager.TurnState.ResolveTile
                     && OnlineGameController.Instance != null
                     && OnlineGameController.Instance.TrySubmitLocalCommand("action.secondary"))
                     return;

                 if (currentSelectedTile != null)
                 {
                     // Logic for secondary action (e.g. Auction, Mortgage, Pass)
                     
                     if (currentSelectedTile.Type == TileType.Property && currentSelectedTile.Data is PropertyData property)
                     {
                          bool ownsProperty = currentPlayer.OwnedProperties.Contains(property);
                          if (!ownsProperty)
                          {
                              // Pass -> Start Auction or just Skip
                              // If AuctionManager exists, start auction. Else skip.
                              if (AuctionManager.Instance != null)
                              {
                                  AuctionManager.Instance.StartAuction(property);
                                  turnManager.CompleteTileResolution();
                              }
                              else
                              {
                                  // Just skip buying
                                  rulesEngine.SkipOptionalAction();
                              }
                          }
                          else
                          {
                              // Owns Property -> End Turn (Skip Upgrade)
                              rulesEngine.SkipOptionalAction();
                          }
                     }
                     else if (currentSelectedTile.Type == TileType.Casino || currentSelectedTile.Type == TileType.StockMarket)
                     {
                         // Pass on optional tiles
                         rulesEngine.SkipOptionalAction();
                     }
                 }
             }
        }

        public void OnTertiaryClicked()
        {
            if (Time.time - lastClickTime < CLICK_COOLDOWN) return;
            lastClickTime = Time.time;
            if (turnManager != null && turnManager.CurrentState == TurnManager.TurnState.AdjustDice)
            {
                if (!IsLocalPlayer(turnManager.CurrentPlayer)) return;
                if (OnlineGameController.Instance != null
                    && OnlineGameController.Instance.TrySubmitLocalCommand("dice.adjust", "{\"delta\":1}"))
                    return;
                turnManager.ConfirmAutoPrecision(1);
            }
        }

        private void OnTileSelected(BoardTile tile)
        {
            HandleTileSelected(tile);
            if (tile != null)
            {
                pulseActiveTab = "inspector";
                SetPulseExpanded(false);
            }
            UpdateGameStateDock();
        }

        private void OnGameActivityPublished(GameActivityEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.text)) return;
            string key = $"{entry.id}|{entry.kind}|{entry.playerId}|{entry.text}";
            if (!gameStateActivityKeys.Add(key)) return;

            var matchingTypedItem = gameStateTimeline.FirstOrDefault(item => item.Activity == null
                && item.Event != null && MatchesActivity(item.Event, entry));
            if (matchingTypedItem != null)
                matchingTypedItem.Activity = entry;
            else
                gameStateTimeline.Add(new GameStateHistoryItem { Activity = entry });

            TrimGameStateTimeline();
            UpdateGameStateDock();
            RenderGameStateHistory();
        }

        private void OnGameEventTraceRecorded(GameEvent gameEvent)
        {
            if (gameEvent == null) return;
            if (gameEventPresenter != null)
            {
                gameEventPresenter.LocalPlayerId = LocalSeatIndex();
                gameEventPresenter.ReducedMotion = UIUXRuntime.ReducedMotion;
                if (activeDockEvent != null && gameEvent.Coalescible
                    && activeDockEvent.CanCoalesceWith(gameEvent)
                    && gameEventPresenter.Current != null)
                    activeDockEvent = gameEventPresenter.Current;
            }

            if (GameEventTextFormatter.IsDockEvent(gameEvent) && gameStateEventIds.Add(gameEvent.Id))
            {
                var matchingActivityItem = gameStateTimeline.FirstOrDefault(item => item.Event == null
                    && item.Activity != null && MatchesActivity(gameEvent, item.Activity));
                if (matchingActivityItem != null)
                    matchingActivityItem.Event = gameEvent;
                else
                    gameStateTimeline.Add(new GameStateHistoryItem { Event = gameEvent });
                TrimGameStateTimeline();
                UpdateGameStateDock();
                RenderGameStateHistory();
            }

            BeginNextGameEventPresentation();
        }

        private bool MatchesActivity(GameEvent gameEvent, GameActivityEntry activity)
        {
            if (gameEvent == null || activity == null || gameEvent.ActorId != activity.playerId) return false;
            if (gameEvent.Kind == GameEventKind.PropertyPurchased
                && string.Equals(activity.kind, "purchase", System.StringComparison.OrdinalIgnoreCase))
                return ActivityMentionsTile(gameEvent, activity.text);
            if (gameEvent.Kind == GameEventKind.PropertyPassed
                && string.Equals(activity.kind, "pass", System.StringComparison.OrdinalIgnoreCase))
                return ActivityMentionsTile(gameEvent, activity.text);
            if (gameEvent.Kind == GameEventKind.UpgradeApplied
                && string.Equals(activity.kind, "upgrade", System.StringComparison.OrdinalIgnoreCase))
                return ActivityMentionsTile(gameEvent, activity.text);
            if (gameEvent.Kind == GameEventKind.TaxPaid
                && string.Equals(activity.kind, "tax", System.StringComparison.OrdinalIgnoreCase))
                return true;
            if (gameEvent.Kind == GameEventKind.TransferConfirmed
                && gameEvent.Reason == GameEventReason.Rent
                && string.Equals(activity.kind, "rent", System.StringComparison.OrdinalIgnoreCase))
                return ActivityMentionsTile(gameEvent, activity.text);
            return false;
        }

        private bool ActivityMentionsTile(GameEvent gameEvent, string activityText)
        {
            string tileName = ResolveGameEventTileName(gameEvent.TileId);
            return !string.IsNullOrWhiteSpace(tileName)
                && !tileName.StartsWith("Tile ", System.StringComparison.Ordinal)
                && activityText.IndexOf(tileName, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void TrimGameStateTimeline()
        {
            while (gameStateTimeline.Count > 12)
            {
                var removed = gameStateTimeline[0];
                if (removed.Activity != null)
                    gameStateActivityKeys.Remove($"{removed.Activity.id}|{removed.Activity.kind}|{removed.Activity.playerId}|{removed.Activity.text}");
                if (removed.Event != null)
                    gameStateEventIds.Remove(removed.Event.Id);
                gameStateTimeline.RemoveAt(0);
            }
        }

        private void BeginNextGameEventPresentation()
        {
            if (gameEventPresenter == null || gameStateDock == null || activeDockEvent != null) return;
            if (!gameEventPresenter.TryBeginNext(out var nextEvent)) return;
            activeDockEvent = nextEvent;
            gameEventPresenter.LocalPlayerId = LocalSeatIndex();
            gameEventPresenter.ReducedMotion = UIUXRuntime.ReducedMotion;
            gameStateDock.EnableInClassList("game-state-dock--presenting", true);
            btnSkipGameEvent?.AddToClassList("game-state-dock__skip-button--visible");
            UpdateGameStateDock();
            ScheduleGameEventBeat(++gameEventPresentationGeneration);
        }

        private void ScheduleGameEventBeat(int generation)
        {
            if (gameEventPresenter == null || activeDockEvent == null || gameStateDock == null) return;
            var beat = gameEventPresenter.CurrentBeat;
            gameStateDock.EnableInClassList("game-state-dock--beat-announcement", beat == GamePresentationBeat.Announcement);
            gameStateDock.EnableInClassList("game-state-dock--beat-anticipation", beat == GamePresentationBeat.Anticipation);
            gameStateDock.EnableInClassList("game-state-dock--beat-impact", beat == GamePresentationBeat.Impact);
            gameStateDock.EnableInClassList("game-state-dock--beat-transfer", beat == GamePresentationBeat.Transfer);
            gameStateDock.EnableInClassList("game-state-dock--beat-trace", beat == GamePresentationBeat.Trace);
            long delayMs = Mathf.Max(0, Mathf.RoundToInt(gameEventPresenter.GetBeatDurationSeconds(activeDockEvent, beat) * 1000f));
            gameStateDock.schedule.Execute(() =>
            {
                if (generation != gameEventPresentationGeneration || gameEventPresenter == null) return;
                if (gameEventPresenter.AdvanceBeat())
                {
                    ScheduleGameEventBeat(generation);
                    return;
                }

                activeDockEvent = null;
                gameStateDock.RemoveFromClassList("game-state-dock--presenting");
                gameStateDock.RemoveFromClassList("game-state-dock--beat-announcement");
                gameStateDock.RemoveFromClassList("game-state-dock--beat-anticipation");
                gameStateDock.RemoveFromClassList("game-state-dock--beat-impact");
                gameStateDock.RemoveFromClassList("game-state-dock--beat-transfer");
                gameStateDock.RemoveFromClassList("game-state-dock--beat-trace");
                btnSkipGameEvent?.RemoveFromClassList("game-state-dock__skip-button--visible");
                UpdateGameStateDock();
                BeginNextGameEventPresentation();
            }).StartingIn(delayMs);
        }

        private void SkipCurrentGameEventPresentation()
        {
            if (gameEventPresenter == null || activeDockEvent == null) return;
            gameEventPresenter.SkipCurrent();
            gameEventPresentationGeneration++;
            activeDockEvent = null;
            gameStateDock?.RemoveFromClassList("game-state-dock--presenting");
            btnSkipGameEvent?.RemoveFromClassList("game-state-dock__skip-button--visible");
            UpdateGameStateDock();
            BeginNextGameEventPresentation();
        }

        private void UpdateGameStateDock()
        {
            if (gameStateDock == null || lblGameStateTitle == null || lblGameStateMessage == null
                || turnManager == null) return;

            var activePlayer = turnManager.CurrentPlayer;
            if (activePlayer == null)
            {
                gameStateDock.RemoveFromClassList("game-state-dock--quiet");
                lblGameStateTitle.text = "WAITING FOR GAME";
                lblGameStateMessage.text = "The next game state will appear here.";
                return;
            }

            bool localTurn = IsLocalPlayer(activePlayer) && !activePlayer.IsAI;
            bool isAI = activePlayer.IsAI;
            bool inspectingTileAction = turnManager.CurrentState == TurnManager.TurnState.ResolveTile;
            string playerName = ResolveGameStatePlayerName(activePlayer.PlayerId);
            string phase = ResolveGameStatePhase(activePlayer);

            gameStateDock.EnableInClassList("game-state-dock--tile-action", inspectingTileAction);
            gameStateDock.RemoveFromClassList("game-state-dock--local");
            gameStateDock.RemoveFromClassList("game-state-dock--waiting");
            gameStateDock.RemoveFromClassList("game-state-dock--attention");
            bool decisionState = turnManager.CurrentState == TurnManager.TurnState.WaitingForAuditDecision
                || turnManager.CurrentState == TurnManager.TurnState.WaitingForTransitChoice
                || turnManager.CurrentState == TurnManager.TurnState.AdjustDice
                || turnManager.CurrentState == TurnManager.TurnState.ChooseTurbo;
            gameStateDock.AddToClassList(decisionState ? "game-state-dock--attention"
                : localTurn ? "game-state-dock--local" : "game-state-dock--waiting");

            if (localTurn)
                lblGameStateTitle.text = "YOUR TURN · " + phase;
            else if (isAI)
                lblGameStateTitle.text = playerName.ToUpperInvariant() + " · PLAYING";
            else
                lblGameStateTitle.text = "WAITING · " + playerName.ToUpperInvariant() + " · " + phase;

            var latest = gameStateTimeline.Count > 0 ? gameStateTimeline[gameStateTimeline.Count - 1] : null;
            var eventToShow = activeDockEvent != null ? activeDockEvent : latest?.Event;
            if (eventToShow != null)
            {
                bool settledEventUnderInspector = activeDockEvent == null && !IsGameStateHistoryOpen()
                    && (pulseExpanded || currentSelectedTile != null);
                gameStateDock.EnableInClassList("game-state-dock--quiet", settledEventUnderInspector);
                if (settledEventUnderInspector) return;
                lblGameStateMessage.text = FormatGameEvent(eventToShow);
                gameStateDock.EnableInClassList("game-state-dock--event-purchase",
                    eventToShow.Kind == GameEventKind.PropertyPurchased);
                gameStateDock.EnableInClassList("game-state-dock--event-pass",
                    eventToShow.Kind == GameEventKind.PropertyPassed);
                gameStateDock.EnableInClassList("game-state-dock--event-transfer",
                    eventToShow.Kind == GameEventKind.TransferConfirmed);
                gameStateDock.EnableInClassList("game-state-dock--event-tax",
                    eventToShow.Kind == GameEventKind.TaxPaid);
                gameStateDock.EnableInClassList("game-state-dock--event-upgrade",
                    eventToShow.Kind == GameEventKind.UpgradeApplied);
                return;
            }

            gameStateDock.RemoveFromClassList("game-state-dock--event-purchase");
            gameStateDock.RemoveFromClassList("game-state-dock--event-pass");
            gameStateDock.RemoveFromClassList("game-state-dock--event-transfer");
            gameStateDock.RemoveFromClassList("game-state-dock--event-tax");
            gameStateDock.RemoveFromClassList("game-state-dock--event-upgrade");

            // A turn-start notice duplicates the turn counter, active-player rail,
            // and the primary action. Keep the activity in history without covering
            // the market/player row in the HUD.
            bool quietNotice = !IsGameStateHistoryOpen()
                && (pulseExpanded || currentSelectedTile != null || IsLatestActivityTurnStart());
            gameStateDock.EnableInClassList("game-state-dock--quiet", quietNotice);
            if (quietNotice) return;

            if (latest == null || latest.Activity == null)
            {
                gameStateDock.RemoveFromClassList("game-state-dock--quiet");
                lblGameStateMessage.text = localTurn
                    ? "Choose an action below."
                    : "Watch the board; the latest action will stay here.";
                return;
            }

            var activity = latest.Activity;
            bool actorIsLocal = activity.playerId == LocalSeatIndex();
            string actorName = actorIsLocal ? "You" : ResolveGameStatePlayerName(activity.playerId);
            string detail = TopHUDWidget.FormatActivityText(activity.text);
            if (string.Equals(activity.kind, "rent", System.StringComparison.OrdinalIgnoreCase))
                detail = ReplaceActivityVerb(detail, "pays ", actorIsLocal ? "You paid " : actorName + " paid ");
            else if (string.Equals(activity.kind, "purchase", System.StringComparison.OrdinalIgnoreCase))
                detail = ReplaceActivityVerb(detail, "buys ", actorIsLocal ? "You bought " : actorName + " bought ");
            else if (detail.StartsWith("lands on ", System.StringComparison.Ordinal))
                detail = (actorIsLocal ? "You" : actorName) + " landed on " + detail.Substring(9);
            else if (detail.StartsWith("rolls ", System.StringComparison.Ordinal))
                detail = (actorIsLocal ? "You" : actorName) + " rolled " + detail.Substring(6);
            else if (string.Equals(detail, "starts their turn", System.StringComparison.Ordinal))
                detail = actorIsLocal ? "Your turn has started" : actorName + "'s turn has started";
            else
                detail = (actorIsLocal ? "You · " : actorName + " · ") + detail;
            lblGameStateMessage.text = detail;
        }

        private string FormatGameEvent(GameEvent gameEvent)
        {
            return GameEventTextFormatter.Format(gameEvent, LocalSeatIndex(),
                ResolveGameStatePlayerName, ResolveGameEventTileName);
        }

        private string ResolveGameEventTileName(int tileId)
        {
            var tile = BoardManager.Instance != null ? BoardManager.Instance.GetTileAt(tileId) : null;
            return tile != null && tile.Data != null ? tile.Data.TileName : null;
        }

        private string ResolveGameStatePhase(PlayerModel activePlayer)
        {
            if (activePlayer != null && activePlayer.IsInAudit) return "AUDIT DECISION";
            switch (turnManager.CurrentState)
            {
                case TurnManager.TurnState.TurnStart: return "TURN STARTING";
                case TurnManager.TurnState.WaitingForAuditDecision: return "CHOOSE AN AUDIT ACTION";
                case TurnManager.TurnState.Planning: return "ROLL OR USE A POWER";
                case TurnManager.TurnState.RollDice: return "ROLLING DICE";
                case TurnManager.TurnState.AdjustDice: return "CHOOSE YOUR RESULT";
                case TurnManager.TurnState.ChooseTurbo: return "CHOOSE A DICE PAIR";
                case TurnManager.TurnState.MovingPawn: return "MOVING ON THE BOARD";
                case TurnManager.TurnState.WaitingForTransitChoice: return "EXPRESS TRANSIT DECISION";
                case TurnManager.TurnState.ResolveTile:
                    string tileName = currentSelectedTile != null && currentSelectedTile.Data != null
                        ? currentSelectedTile.Data.TileName : string.Empty;
                    return string.IsNullOrWhiteSpace(tileName)
                        ? "CHOOSE A TILE ACTION" : "AT " + tileName.ToUpperInvariant();
                case TurnManager.TurnState.EndTurn: return "TURN COMPLETE";
                case TurnManager.TurnState.ResolveDice: return "DICE RESULT";
                default: return "TURN IN PROGRESS";
            }
        }

        private static string ReplaceActivityVerb(string message, string verb, string replacement)
        {
            return message != null && message.StartsWith(verb, System.StringComparison.Ordinal)
                ? replacement + message.Substring(verb.Length)
                : message;
        }

        private int LocalSeatIndex()
        {
            var online = OnlineGameController.Instance;
            return online != null && online.IsOnlineGame ? online.LocalSeatIndex : 0;
        }

        private static string ResolveGameStatePlayerName(int playerId)
        {
            if (playerId < 0) return "Market";
            var players = LobbySession.Instance?.CurrentLobby?.players;
            if (players != null && playerId < players.Length
                && !string.IsNullOrWhiteSpace(players[playerId].displayName))
                return players[playerId].displayName;
            return "Player " + (playerId + 1);
        }

        private void ToggleGameStateHistory()
        {
            if (gameStateHistory == null) return;
            bool isOpen = gameStateHistory.ClassListContains("is-open");
            gameStateHistory.EnableInClassList("is-open", !isOpen);
            if (btnGameStateHistory != null) btnGameStateHistory.text = isOpen ? "LOG" : "FERMER";
            if (isOpen) UpdateGameStateDock();
            else gameStateDock?.RemoveFromClassList("game-state-dock--quiet");
        }

        private void RenderGameStateHistory()
        {
            if (gameStateHistoryList == null) return;
            gameStateHistoryList.Clear();
            for (int i = gameStateTimeline.Count - 1; i >= 0; i--)
            {
                var item = gameStateTimeline[i];
                int actorId = item.Event != null ? item.Event.ActorId : item.Activity.playerId;
                bool actorIsLocal = actorId == LocalSeatIndex();
                string text = item.Event != null
                    ? FormatGameEvent(item.Event)
                    : (actorIsLocal ? "YOU" : ResolveGameStatePlayerName(actorId).ToUpperInvariant())
                        + " · " + TopHUDWidget.FormatActivityText(item.Activity.text);
                var row = new Label(text);
                row.AddToClassList("game-state-history__entry");
                if (actorIsLocal) row.AddToClassList("game-state-history__entry--local");
                if (item.Event != null)
                {
                    row.AddToClassList("game-state-history__entry--typed");
                    switch (item.Event.Kind)
                    {
                        case GameEventKind.PropertyPurchased:
                            row.AddToClassList("game-state-history__entry--purchase");
                            break;
                        case GameEventKind.PropertyPassed:
                            row.AddToClassList("game-state-history__entry--pass");
                            break;
                        case GameEventKind.TransferConfirmed:
                            row.AddToClassList("game-state-history__entry--transfer");
                            break;
                        case GameEventKind.TaxPaid:
                            row.AddToClassList("game-state-history__entry--tax");
                            break;
                        case GameEventKind.UpgradeApplied:
                            row.AddToClassList("game-state-history__entry--upgrade");
                            break;
                    }
                }
                gameStateHistoryList.Add(row);
            }
        }

        private void HandleTileSelected(BoardTile tile)
        {
            currentSelectedTile = tile;
            UpdateInspectorSection(tile);

            if (turnManager == null)
            {
                 // Do not fallback to Instance
                 return;
            }
            
            PlayerModel currentPlayer = turnManager.CurrentPlayer;

            // Check if player is in Audit first (Before rolling dice phase)
            if (currentPlayer != null && currentPlayer.IsInAudit && turnManager.CurrentState == TurnManager.TurnState.WaitingForAuditDecision)
            {
                double sgo = turnManager.financeSystem != null ? turnManager.financeSystem.CurrentSgo : 500;
                SetPrimaryButtonState($"PAY FINE ({CurrencyFormatter.Format(sgo)})", true);
                
                if (turnManager.IsJustLandedInAudit)
                {
                    if (currentPlayer.HasGetOutOfAuditCard)
                    {
                        SetSecondaryButtonState("USE ESCAPE CARD", true);
                        // We need to re-bind secondary listener to UseEscapeCard instead of StayInAudit?
                        // But ActionButtonsWidget relies on OnSecondaryClicked which calls methods based on state.
                        // We will update OnSecondaryClicked.
                    }
                    else
                    {
                        SetSecondaryButtonState("STAY IN AUDIT", true);
                    }
                }
                else
                {
                    if (currentPlayer.HasGetOutOfAuditCard)
                    {
                        SetSecondaryButtonState("USE ESCAPE CARD", true);
                    }
                    else
                    {
                        SetSecondaryButtonState("ROLL DOUBLES", true);
                    }
                }
                
                // Re-bind listeners for Audit Actions if needed, but since we use OnPrimary/SecondaryClicked, we can branch there.
                return;
            }

            UpdateButtonsState(currentSelectedTile, currentPlayer);
        }

        // GetCurrentPlayer() helper is no longer needed but kept if referenced elsewhere or can be removed.
        // It was used in OnPrimaryClicked/OnSecondaryClicked but now those use turnManager directly.
        // I will remove it to clean up.

        public void ClearButtons()
        {
            SetPrimaryButtonState("ACTION IN PROGRESS", false);
            SetSecondaryButtonState(string.Empty, false);
            SetTertiaryButtonState(string.Empty, false, false);
        }

        /// <summary>
        /// Updates the visual state and interactivity of the buttons.
        /// </summary>
        // Phase D — Cache pour refresh du bottom sheet quand TurnState change
        private TurnManager.TurnState lastObservedState = TurnManager.TurnState.EndTurn;

        private void Update()
        {
            if (turnManager == null) return;
            if (lastReducedMotion != UIUXRuntime.ReducedMotion)
            {
                lastReducedMotion = UIUXRuntime.ReducedMotion;
                UpdateButtonsState(currentSelectedTile, turnManager.CurrentPlayer);
            }
            if (turnManager.CurrentState != lastObservedState)
            {
                lastObservedState = turnManager.CurrentState;
                UpdateButtonsState(currentSelectedTile, turnManager.CurrentPlayer);
                UpdateGameStateDock();
            }
        }

        public void UpdateButtonsState(BoardTile tile, PlayerModel currentPlayer)
        {
            UpdateInspectorSection(tile);
            if (currentPlayer != null && !IsLocalPlayer(currentPlayer))
            {
                SetPrimaryButtonState("PLAYER TURN", false);
                SetSecondaryButtonState(string.Empty, false);
                SetTertiaryButtonState(string.Empty, false, false);
                return;
            }
            if (turnManager != null
                && turnManager.CurrentState == TurnManager.TurnState.WaitingForTransitChoice
                && currentPlayer != null && !currentPlayer.IsAI)
            {
                SetPrimaryButtonState("USE TRANSIT EXPRESS", true);
                SetSecondaryButtonState("STAY ON HUB", true);
                SetTertiaryButtonState("OPTIONAL HUB TRANSIT", false, false);
                return;
            }
            if (turnManager != null && turnManager.CurrentState == TurnManager.TurnState.ChooseTurbo)
            {
                string label(int[] roll) => roll == null ? "WAIT" : $"{roll[0]} + {roll[1]} → {(turnManager.GetPlayerPosition(currentPlayer) + roll[0] + roll[1]) % 40}";
                SetPrimaryButtonState("JET 1 · " + label(turnManager.TurboFirst), true);
                SetSecondaryButtonState("JET 2 · " + label(turnManager.TurboSecond), true);
                SetTertiaryButtonState(string.Empty, false, false);
                return;
            }
            if (turnManager != null
                && turnManager.CurrentState == TurnManager.TurnState.AdjustDice
                && currentPlayer != null && !currentPlayer.IsAI)
            {
                int roll = turnManager.PendingDiceTotal;
                SetPrimaryButtonState($"−1  ({Mathf.Max(1, roll - 1)})", true);
                SetSecondaryButtonState($"KEEP  ({roll})", true);
                SetTertiaryButtonState($"+1  ({roll + 1})", true, true);
                return;
            }

            SetTertiaryButtonState(string.Empty, false, false);

            // ── Phase D : PLANNING PHASE ─────────────────────────────────────────
            if (turnManager != null
                && turnManager.CurrentState == TurnManager.TurnState.Planning
                && currentPlayer != null && !currentPlayer.IsAI)
            {
                // Primary = ROLL DICE (vert, toujours actif)
                SetPrimaryButtonState("ROLL DICE", true);

                // Secondary = Use Ultimate (si dispo) sinon PASS
                int ultimateCount = GetAvailableUltimates(currentPlayer).Count;
                if (ultimateCount > 0)
                    SetSecondaryButtonState(ultimateCount == 1 ? "USE ULTIMATE" : $"CHOOSE ULTIMATE ({ultimateCount})", true);
                else
                    SetSecondaryButtonState("PASS", true);
                return;
            }

            if (turnManager != null && (turnManager.CurrentState == TurnManager.TurnState.MovingPawn || turnManager.CurrentState == TurnManager.TurnState.RollDice))
            {
                ClearButtons();
                return;
            }

            if (currentPlayer == null)
            {
                SetPrimaryButtonState("WAITING FOR PLAYER", false);
                SetSecondaryButtonState(string.Empty, false);
                return;
            }

            if (currentPlayer.IsAI)
            {
                SetPrimaryButtonState("AI TURN", false);
                SetSecondaryButtonState("AUTOMATIC TURN", false);
                return;
            }

            // Check if player is in Audit first (Before rolling dice phase)
            if (currentPlayer.IsInAudit)
            {
                 double sgo = turnManager.financeSystem != null ? turnManager.financeSystem.CurrentSgo : 500;
                 SetPrimaryButtonState($"PAY FINE ({CurrencyFormatter.Format(sgo)})", true);
                 
                 if (turnManager.IsJustLandedInAudit)
                 {
                     SetSecondaryButtonState("STAY IN AUDIT", true);
                 }
                 else
                 {
                     SetSecondaryButtonState("ROLL DOUBLES", true);
                 }
                
                // Re-bind listeners for Audit Actions if needed, but since we use OnPrimary/SecondaryClicked, we can branch there.
                return;
            }

            if (tile == null)
            {
                // Probably rolling dice phase
                SetPrimaryButtonState(turnManager.CurrentState == TurnManager.TurnState.MovingPawn ? "MOVING PAWN" : "ROLLING DICE", false);
                SetSecondaryButtonState(string.Empty, false);
                return;
            }

            // Safely try to cast the data as a Property
            PropertyData propData = tile.Data as PropertyData;
            
            if (propData != null)
            {
                bool ownsProperty = currentPlayer.OwnedProperties.Contains(propData);
                PlayerModel owner = turnManager.GetOwnerOfProperty(propData);
                
                // If currentPlayer owns it -> UPGRADE or END TURN
                if (ownsProperty)
                {
                    if (propData.CurrentLevel >= 5) // Assuming MaxLevel is 5
                    {
                        SetPrimaryButtonState("MAX LEVEL", false);
                    }
                    else
                    {
                        // Only allow upgrade if they have full family? 
                        // Prompt said: "Show UPGRADE/PASS (if they have the full family, otherwise just PASS)"
                        if (rulesEngine != null && rulesEngine.HasFullFamily(currentPlayer, propData.FamilyData))
                        {
                            SetPrimaryButtonState("UPGRADE", true);
                        }
                        else
                        {
                            SetPrimaryButtonState("UPGRADE (Need Monopoly)", false);
                        }
                    }
                    SetSecondaryButtonState("END TURN", true);
                }
                else
                {
                    // Check ownership
                    if (owner == null)
                    {
                        // Unowned -> BUY
                        if (currentPlayer.CurrentBalance >= propData.BasePrice)
                        {
                            SetPrimaryButtonState($"BUY ({CurrencyFormatter.FormatExact(propData.BasePrice)})", true);
                            SetSecondaryButtonState("PASS", true);
                        }
                        else
                        {
                            SetPrimaryButtonState("CAN'T AFFORD", false);
                            SetSecondaryButtonState("PASS", true);
                        }
                    }
                    else
                    {
                        // Owned by someone else -> Should have been auto-paid?
                        // If we are here, maybe auto-pay failed or we are viewing it?
                        // Prompt Bug 1: "Human players see a "BUY" button when landing on properties owned by opponents"
                        // Since we added Auto-Pay in TurnManager, this UI state might be transient or for "Just Visiting".
                        // We should show "RENT PAID" or similar.
                        SetPrimaryButtonState($"RENT PAID TO {owner.PlayerId}", false);
                        SetSecondaryButtonState("END TURN", true);
                    }
                }
            }
            else
            {
                // It's a special tile (GO, Casino, etc.)
                string actionText = "INFO";
                bool canInteract = false;
                bool canPass = false;

                if (tile.Type == TileType.Casino)
                {
                    actionText = "PLAY CASINO";
                    canInteract = true;
                    canPass = true;
                }
                else if (tile.Type == TileType.StockMarket)
                {
                    actionText = "INVEST";
                    canInteract = true;
                    canPass = true;
                }
                else if (tile.Type == TileType.Venture)
                {
                    actionText = "DRAW CARD";
                    canInteract = true; 
                    canPass = false; // Mandatory action
                }
                else if (tile.Type == TileType.GoToPrison)
                {
                    actionText = "GO TO AUDIT";
                    canInteract = true;
                    canPass = false;
                }
                else if (tile.TileIndex == 20)
                {
                    actionText = "OPEN MARKET";
                    canInteract = true;
                    canPass = false;
                }
                
                SetPrimaryButtonState(actionText, canInteract);
                if (canPass)
                {
                    SetSecondaryButtonState("PASS", true);
                }
                else
                {
                    SetSecondaryButtonState(string.Empty, false);
                }
            }
        }

        private static bool IsLocalPlayer(PlayerModel player)
        {
            if (player == null) return false;
            var online = OnlineGameController.Instance;
            return online == null || !online.IsOnlineGame || player.PlayerId == online.LocalSeatIndex;
        }

        private void SetPrimaryButtonState(string label, bool interactable)
        {
            if (btnPrimary == null) return;
            if (buySlideTrack != null)
            {
                ResetBuySlide();
                buySlideTrack.style.display = DisplayStyle.None;
            }
            bool propertyPurchase = label != null
                && label.StartsWith("BUY (", System.StringComparison.Ordinal)
                && label.EndsWith(")", System.StringComparison.Ordinal);
            bool rollAction = string.Equals(label, "ROLL DICE", System.StringComparison.Ordinal);
            string displayLabel = rollAction ? "Lancer les dés" : label;
            btnPrimary.EnableInClassList("pulse-action--purchase", propertyPurchase);
            btnPrimary.EnableInClassList("pulse-action--roll", rollAction);
            if (primaryActionTitle != null && primaryActionValue != null)
            {
                primaryActionTitle.style.display = propertyPurchase ? DisplayStyle.Flex : DisplayStyle.None;
                primaryActionValue.style.display = propertyPurchase ? DisplayStyle.Flex : DisplayStyle.None;
                if (propertyPurchase)
                {
                    primaryActionTitle.text = "Acheter";
                    primaryActionValue.text = label.Substring(5, label.Length - 6);
                    btnPrimary.text = string.Empty;
                }
                else
                {
                    btnPrimary.text = displayLabel;
                }
            }
            else
            {
                btnPrimary.text = displayLabel;
            }
            btnPrimary.style.display = DisplayStyle.Flex;
            btnPrimary.SetEnabled(interactable);
        }

        private void OnBuySlideKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter && evt.keyCode != KeyCode.Space) return;
            if (buySlideTrack == null || buySlideTrack.resolvedStyle.display == DisplayStyle.None) return;
            evt.StopPropagation();
            CompleteBuySlide();
        }

        private void OnBuySlidePointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0 || buySlideTrack == null || buySlideTrack.resolvedStyle.display == DisplayStyle.None) return;
            buySlideDragging = true;
            buySlidePointerId = evt.pointerId;
            buySlideStartX = evt.position.x;
            buySlideProgress = 0f;
            buySlideHandle.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnBuySlidePointerMove(PointerMoveEvent evt)
        {
            if (!buySlideDragging || evt.pointerId != buySlidePointerId) return;
            float distance = Mathf.Max(1f, buySlideTrack.resolvedStyle.width - buySlideHandle.resolvedStyle.width - 6f);
            // A short dead zone gives the thumb slight initial resistance.
            buySlideProgress = Mathf.Clamp01((evt.position.x - buySlideStartX - 8f) / distance);
            buySlideHandle.style.left = 3f + buySlideProgress * distance;
            evt.StopPropagation();
        }

        private void OnBuySlidePointerUp(PointerUpEvent evt)
        {
            if (!buySlideDragging || evt.pointerId != buySlidePointerId) return;
            bool confirmed = buySlideProgress >= 0.9f;
            buySlideHandle.ReleasePointer(evt.pointerId);
            ResetBuySlide();
            evt.StopPropagation();
            if (confirmed) CompleteBuySlide();
        }

        private void CompleteBuySlide()
        {
            if (buySlideTrack == null || buySlideTrack.resolvedStyle.display == DisplayStyle.None) return;
            buySlideTrack.SetEnabled(false);
            if (buySlideLabel != null) buySlideLabel.text = "PURCHASE REQUESTED ✓";
            OnPrimaryClicked();
            // A rejected online command must not leave the control permanently inert.
            buySlideTrack.schedule.Execute(() =>
            {
                if (buySlideTrack != null) buySlideTrack.SetEnabled(true);
            }).StartingIn(700);
        }

        private void ResetBuySlide()
        {
            buySlideDragging = false;
            buySlidePointerId = -1;
            buySlideProgress = 0f;
            if (buySlideHandle != null) buySlideHandle.style.left = 3f;
            buySlideTrack?.SetEnabled(true);
        }

        private void SetSecondaryButtonState(string label, bool interactable)
        {
            if (btnSecondary == null) return;
            bool visible = !string.IsNullOrWhiteSpace(label);
            btnSecondary.EnableInClassList("pulse-action--pass",
                string.Equals(label, "PASS", System.StringComparison.OrdinalIgnoreCase));
            btnSecondary.text = visible
                ? string.Equals(label, "PASS", System.StringComparison.OrdinalIgnoreCase) ? "Passer" : label
                : string.Empty;
            btnSecondary.SetEnabled(visible && interactable);
            btnSecondary.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SetTertiaryButtonState(string label, bool interactable, bool visible)
        {
            if (btnTertiary == null) return;
            btnTertiary.text = label;
            btnTertiary.SetEnabled(interactable);
            btnTertiary.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>
        /// Section "inspection de tuile" — absorbée depuis l'ancien InspectorWidget.
        /// Alimente nom/famille/niveau/rent/rent-yield + l'icône (TileIconResolver).
        /// UI Toolkit uniquement (pas de chemin legacy — InspectorWidget est retiré).
        /// </summary>
        private void UpdateInspectorSection(BoardTile tile)
        {
            double baseGo = turnManager?.financeSystem != null ? turnManager.financeSystem.CurrentSgo : 0;
            if (pulseGoBase != null)
                pulseGoBase.text = baseGo > 0 ? "+" + CurrencyFormatter.FormatExact(baseGo) : "Indisponible";
            if (pulseGoCosts != null) pulseGoCosts.text = "Indisponible";
            if (pulseGoNet != null) pulseGoNet.text = "Indisponible";
            if (pulseGoSummary != null)
            {
                pulseGoSummary.tooltip = "Voir le détail du passage GO";
            }
            bool turboChoice = turnManager != null && turnManager.CurrentState == TurnManager.TurnState.ChooseTurbo;
            bool precisionChoice = turnManager != null && turnManager.CurrentState == TurnManager.TurnState.AdjustDice;
            bool hasInspectorContent = tile != null || turboChoice || precisionChoice;
            bool hasPropertyStats = tile != null && tile.Type == TileType.Property && tile.Property != null;
            if (tileInspectorHeading != null)
                tileInspectorHeading.style.display = hasInspectorContent ? DisplayStyle.Flex : DisplayStyle.None;
            if (pulseFamilyProgress != null)
                pulseFamilyProgress.style.display = hasPropertyStats && !turboChoice && !precisionChoice
                    ? DisplayStyle.Flex : DisplayStyle.None;
            if (tileStats != null)
                tileStats.style.display = hasPropertyStats && !turboChoice && !precisionChoice
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            if (lblTileEffect != null)
            {
                lblTileEffect.text = string.Empty;
                lblTileEffect.style.display = DisplayStyle.None;
                lblTileEffect.RemoveFromClassList("tile-effect-badge--sabotage");
                lblTileEffect.RemoveFromClassList("tile-effect-badge--oil-boost");
            }
            if (turboChoice || precisionChoice)
            {
                if (iconTileFamily != null) TileIconResolver.PopulateIcon(iconTileFamily, null);
                if (pulseOwnership != null) pulseOwnership.text = "ACTION DE DÉPLACEMENT";
                if (lblTileName != null) lblTileName.text = turboChoice ? "Moteur Turbo" : "Assistance de précision";
                if (lblTileFamily != null) lblTileFamily.text = turboChoice
                    ? "Choisissez votre jet · la flèche indique la case d’arrivée"
                    : "Ajustez votre déplacement · les doubles restent inchangés";
                return;
            }
            if (iconTileFamily != null) TileIconResolver.PopulateIcon(iconTileFamily, tile?.Data);

            if (tile != null && tile.Type == TileType.Property && tile.Property != null)
            {
                var prop = tile.Property;
                PlayerModel propertyOwner = turnManager != null ? turnManager.GetOwnerOfProperty(prop) : null;
                PlayerModel viewer = turnManager != null
                    ? turnManager.GetPlayerById(LocalSeatIndex()) ?? turnManager.CurrentPlayer
                    : null;
                double currentRent = propertyOwner != null && rulesEngine != null
                    ? rulesEngine.CalculateRent(prop, emitDiagnostics: false)
                    : prop.GetCurrentRent();
                int owned = 0;
                int total = 0;
                if (prop.FamilyData != null && boardManager?.tileDataList != null)
                {
                    total = boardManager.tileDataList.OfType<PropertyData>()
                        .Count(item => item != null && item.FamilyData == prop.FamilyData);
                    owned = viewer != null
                        ? viewer.OwnedProperties.Count(item => item != null && item.FamilyData == prop.FamilyData)
                        : 0;
                }
                if (pulseOwnership != null)
                    pulseOwnership.text = propertyOwner == null
                        ? "À ACQUÉRIR"
                        : viewer != null && propertyOwner.PlayerId == viewer.PlayerId
                            ? "VOTRE PROPRIÉTÉ"
                            : "PROPRIÉTÉ DÉTENUE";
                if (pulseFamilyProgress != null)
                {
                    pulseFamilyProgress.style.display = total > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                    if (pulseFamilyOwned != null)
                        pulseFamilyOwned.text = total > 0 ? owned.ToString(CultureInfo.InvariantCulture) : "—";
                    if (pulseFamilyTotal != null)
                        pulseFamilyTotal.text = total > 0 ? "/" + total.ToString(CultureInfo.InvariantCulture) : string.Empty;
                    RenderPulseFamilySlots(owned, total);
                    pulseFamilyProgress.tooltip = prop.FamilyData != null
                        ? $"Inspecter la famille {prop.FamilyData.FamilyName} · {owned} propriété(s) sur {total}"
                        : "Famille non renseignée";
                }
                if (lblTileName != null) lblTileName.text = prop.PropertyName;
                if (lblTileFamily != null)
                    lblTileFamily.text = prop.FamilyData != null
                        ? $"{prop.FamilyData.FamilyName} · Niveau {prop.CurrentLevel}"
                        : $"Famille inconnue · Niveau {prop.CurrentLevel}";
                bool oilBoostActive = propertyOwner != null
                    && propertyOwner.OilGasBoostTurnsRemaining > 0
                    && prop.FamilyData != null
                    && (prop.FamilyData.FamilyName.Contains("Oil") || prop.FamilyData.FamilyName.Contains("Gas"));
                if (lblTileEffect != null && prop.HijackTurnsRemaining > 0 && prop.HijackedBy != null)
                {
                    int turns = prop.HijackTurnsRemaining;
                    lblTileEffect.text = $"CYBER-SABOTAGE · {turns} {(turns == 1 ? "TURN" : "TURNS")}";
                    if (oilBoostActive)
                        lblTileEffect.text += $" · OIL +80% ({propertyOwner.OilGasBoostTurnsRemaining}T)";
                    lblTileEffect.AddToClassList("tile-effect-badge--sabotage");
                    lblTileEffect.style.display = DisplayStyle.Flex;
                }
                else if (lblTileEffect != null && oilBoostActive)
                {
                    int turns = propertyOwner.OilGasBoostTurnsRemaining;
                    lblTileEffect.text = $"OIL & GAS · +80% RENT · {turns} {(turns == 1 ? "TURN" : "TURNS")}";
                    lblTileEffect.AddToClassList("tile-effect-badge--oil-boost");
                    lblTileEffect.style.display = DisplayStyle.Flex;
                }
                if (lblTileRent != null) lblTileRent.text = CurrencyFormatter.FormatExactNumber(currentRent);
                SetTrendIndicator(lblTileRentTrend, currentRent, prop.BaseRent);
                if (lblTileRisk != null)
                {
                    lblTileRisk.text = "Risque non renseigné";
                    lblTileRisk.tooltip = "Aucune statistique de risque par propriété n’est modélisée.";
                }
                if (containerTileRisk != null) containerTileRisk.style.display = DisplayStyle.Flex;
                if (lblTileRoi != null)
                {
                    // ROI locatif courant : loyer actuel sur prix de base; aucune donnée
                    // de risque par propriété n'est modélisée.
                    double roiPct = prop.BasePrice > 0 ? currentRent / prop.BasePrice * 100.0 : 0.0;
                    lblTileRoi.text = roiPct.ToString("0.0", CultureInfo.InvariantCulture).Replace('.', ',');
                    lblTileRoi.tooltip = "Rent per visit divided by base property price";
                }
                SetTrendIndicator(lblTileRoiTrend, currentRent, prop.BaseRent);
            }
            else if (tile != null)
            {
                if (pulseOwnership != null) pulseOwnership.text = "CASE SPÉCIALE";
                if (pulseFamilyProgress != null) pulseFamilyProgress.style.display = DisplayStyle.None;
                if (lblTileName != null) lblTileName.text = tile.Data != null ? tile.Data.TileName : tile.Type.ToString();
                if (lblTileFamily != null) lblTileFamily.text = "Événement du plateau";
                if (lblTileRent != null) lblTileRent.text = "—";
                if (lblTileRoi != null) lblTileRoi.text = "—";
                ClearTrendIndicators();
                if (containerTileRisk != null) containerTileRisk.style.display = DisplayStyle.None;
            }
            else
            {
                if (pulseOwnership != null) pulseOwnership.text = "SÉLECTIONNEZ UNE PROPRIÉTÉ";
                if (pulseFamilyProgress != null) pulseFamilyProgress.style.display = DisplayStyle.None;
                if (lblTileName != null) lblTileName.text = "—";
                if (lblTileFamily != null) lblTileFamily.text = "";
                if (lblTileRent != null) lblTileRent.text = "—";
                if (lblTileRoi != null) lblTileRoi.text = "—";
                ClearTrendIndicators();
                if (containerTileRisk != null) containerTileRisk.style.display = DisplayStyle.None;
            }
            RenderPulseDetails();
        }

        private void TogglePulsePanel()
        {
            SetPulseExpanded(!pulseExpanded);
        }

        private void SetPulseExpanded(bool expanded)
        {
            pulseExpanded = expanded;
            pulsePanel?.EnableInClassList("pulse-panel--expanded", expanded);
            pulseDetails?.EnableInClassList("pulse-details--visible", expanded);
            UpdatePulseToggleLabel();
            gameStateDock?.EnableInClassList("game-state-dock--quiet",
                !IsGameStateHistoryOpen()
                && (((expanded || currentSelectedTile != null) && activeDockEvent == null)
                    || IsLatestActivityTurnStart()));
            if (expanded) RenderPulseDetails();
        }

        private bool IsGameStateHistoryOpen() =>
            gameStateHistory != null && gameStateHistory.ClassListContains("is-open");

        private bool IsLatestActivityTurnStart()
        {
            var latest = gameStateTimeline.Count > 0 ? gameStateTimeline[gameStateTimeline.Count - 1] : null;
            return activeDockEvent == null && latest?.Activity != null
                && string.Equals(latest.Activity.kind, "turn", System.StringComparison.OrdinalIgnoreCase);
        }

        private void UpdatePulseToggleLabel()
        {
            if (pulseToggleState != null)
                pulseToggleState.text = pulseExpanded ? "Réduire ↓" : "Déplier ↑";
        }

        private void OpenPulseInspector()
        {
            pulseActiveTab = "inspector";
            SetPulseExpanded(true);
        }

        private void SetPulseTab(string tab)
        {
            pulseActiveTab = tab;
            RenderPulseDetails();
        }

        private void OpenPulseGoFlow()
        {
            pulseActiveTab = "go";
            if (!pulseExpanded) SetPulseExpanded(true);
            RenderPulseDetails();
        }

        private void RenderPulseDetails()
        {
            if (pulseTabContent == null || boardManager == null || turnManager == null) return;
            bool inspector = pulseActiveTab == "inspector";
            bool portfolio = pulseActiveTab == "portfolio";
            pulseInspectorTab?.EnableInClassList("pulse-tab--active", inspector);
            pulsePortfolioTab?.EnableInClassList("pulse-tab--active", portfolio);
            pulseGoTab?.EnableInClassList("pulse-tab--active", !inspector && !portfolio);
            pulseTabContent.Clear();

            var selectedProperty = currentSelectedTile != null ? currentSelectedTile.Property : null;
            var family = selectedProperty != null ? selectedProperty.FamilyData : null;
            var viewer = turnManager.GetPlayerById(LocalSeatIndex()) ?? turnManager.CurrentPlayer;
            if (family == null && selectedProperty != null)
                family = selectedProperty.FamilyData;

            if (inspector)
            {
                if (family == null)
                {
                    AddPulseText("Select a property on the board to inspect its family.", "pulse-empty-state");
                    return;
                }

                var familyProperties = boardManager.tileDataList?.OfType<PropertyData>()
                    .Where(property => property != null && property.FamilyData == family)
                    .OrderBy(property => property.TileIndex).ToList();
                if (familyProperties == null || familyProperties.Count == 0)
                {
                    AddPulseText("No properties are configured for this family.", "pulse-empty-state");
                    return;
                }

                int ownedCount = viewer != null
                    ? familyProperties.Count(property => viewer.OwnedProperties.Contains(property)) : 0;
                AddPulseSectionHeading(family.FamilyName, $"{ownedCount}/{familyProperties.Count} propriétés");
                var cards = new VisualElement();
                cards.AddToClassList("pulse-property-cards");
                foreach (var property in familyProperties)
                {
                    var owner = turnManager.GetOwnerOfProperty(property);
                    var card = new Button(() =>
                    {
                        var boardTile = boardManager.GetTileAt(property.TileIndex);
                        if (boardTile != null) HandleTileSelected(boardTile);
                    });
                    card.AddToClassList("pulse-property-card");
                    card.EnableInClassList("pulse-property-card--selected", property == selectedProperty);
                    var cardTop = new VisualElement();
                    cardTop.AddToClassList("pulse-property-card__top");
                    var icon = new VisualElement();
                    icon.AddToClassList("icon-box");
                    icon.AddToClassList("icon-box--compact");
                    icon.AddToClassList("pulse-property-card__icon");
                    TileIconResolver.PopulateIcon(icon, property);
                    var level = new Label("LV " + property.CurrentLevel);
                    level.AddToClassList("pulse-property-card__level");
                    cardTop.Add(icon);
                    cardTop.Add(level);
                    var name = new Label(property.PropertyName);
                    name.AddToClassList("pulse-property-card__name");
                    var state = new Label(owner != null
                        ? viewer != null && owner.PlayerId == viewer.PlayerId ? "À VOUS" : "DÉTENUE"
                        : CurrencyFormatter.FormatExact(property.BasePrice) + " ↗");
                    state.AddToClassList("pulse-property-card__state");
                    card.Add(cardTop);
                    card.Add(name);
                    card.Add(state);
                    cards.Add(card);
                }
                pulseTabContent.Add(cards);
                AddPulseText("Touchez une carte pour l’inspecter sur le plateau.", "pulse-footnote");
                return;
            }

            var allProperties = boardManager.tileDataList?.OfType<PropertyData>()
                .Where(property => property != null && property.FamilyData != null).ToList();
            if (portfolio)
            {
                if (viewer == null || allProperties == null)
                {
                    AddPulseText("Portfolio unavailable.", "pulse-empty-state");
                    return;
                }
                var ownedFamilies = allProperties.GroupBy(property => property.FamilyData)
                    .OrderBy(group => group.Key.FamilyName, System.StringComparer.Ordinal);
                foreach (var group in ownedFamilies)
                {
                    var total = group.Count();
                    var owned = group.Count(property => viewer.OwnedProperties.Contains(property));
                    var card = new Button(() =>
                    {
                        var property = group.FirstOrDefault(item => viewer.OwnedProperties.Contains(item));
                        var tile = property != null ? boardManager.GetTileAt(property.TileIndex) : null;
                        if (tile != null)
                        {
                            pulseActiveTab = "inspector";
                            HandleTileSelected(tile);
                        }
                    });
                    card.AddToClassList("pulse-family-card");
                    var familyName = new Label(group.Key.FamilyName);
                    familyName.AddToClassList("pulse-family-card__name");
                    var progress = new Label($"{owned} / {total} PROPRIÉTÉS");
                    progress.AddToClassList("pulse-family-card__progress");
                    var slots = new VisualElement();
                    slots.AddToClassList("pulse-family-card__slots");
                    foreach (var property in group.OrderBy(item => item.TileIndex))
                    {
                        bool hasProperty = viewer.OwnedProperties.Contains(property);
                        var slot = new Label(hasProperty ? "●" : "○");
                        slot.AddToClassList("pulse-family-slot");
                        slot.AddToClassList(hasProperty ? "pulse-family-slot--owned" : "pulse-family-slot--missing");
                        slot.tooltip = property.PropertyName;
                        slots.Add(slot);
                    }
                    card.Add(familyName);
                    card.Add(progress);
                    card.Add(slots);
                    pulseTabContent.Add(card);
                }
                if (pulseTabContent.childCount == 0)
                    AddPulseText("Aucune propriété dans votre portefeuille.", "pulse-empty-state");
                return;
            }

            AddPulseSectionHeading("Prochain passage GO", "Données confirmées uniquement");
            double sgo = turnManager.financeSystem != null ? turnManager.financeSystem.CurrentSgo : 0;
            AddPulseMetric("Salaire GO de base", sgo > 0 ? "+" + CurrencyFormatter.FormatExact(sgo) : "Indisponible", "pulse-metric--primary");
            AddPulseMetric("Dividendes de famille", "Calculés au passage GO", "");
            AddPulseMetric("Entretien des propriétés", "Calculé au passage GO", "");
            AddPulseMetric("Impôts et variations de marché", "Réglés au passage GO", "");
            AddPulseMetric("Net du prochain GO", "Indisponible avant règlement", "");
            AddPulseText("Le salaire affiché est la base actuelle. Le total futur dépend de l’état du marché, des propriétés et des effets actifs au moment du passage.", "pulse-footnote");
        }

        private void AddPulseSectionHeading(string title, string status)
        {
            var row = new VisualElement();
            row.AddToClassList("pulse-section-heading");
            var heading = new Label(title.ToUpperInvariant());
            var detail = new Label(status);
            detail.AddToClassList("pulse-section-heading__status");
            row.Add(heading);
            row.Add(detail);
            pulseTabContent.Add(row);
        }

        private void RenderPulseFamilySlots(int owned, int total)
        {
            if (pulseFamilySlots == null) return;
            pulseFamilySlots.Clear();
            for (int index = 0; index < Mathf.Max(0, total); index++)
            {
                var slot = new VisualElement();
                slot.AddToClassList("pulse-family-progress__slot");
                slot.EnableInClassList("pulse-family-progress__slot--owned", index < owned);
                pulseFamilySlots.Add(slot);
            }
        }

        private void AddPulseText(string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList(className);
            pulseTabContent.Add(label);
        }

        private void AddPulseMetric(string name, string value, string modifier)
        {
            var row = new VisualElement();
            row.AddToClassList("pulse-metric-row");
            if (!string.IsNullOrEmpty(modifier)) row.AddToClassList(modifier);
            var label = new Label(name);
            label.AddToClassList("pulse-metric-row__label");
            var amount = new Label(value);
            amount.AddToClassList("pulse-metric-row__value");
            row.Add(label);
            row.Add(amount);
            pulseTabContent.Add(row);
        }

        private static void SetTrendIndicator(Label indicator, double current, double baseline)
        {
            if (indicator == null) return;
            bool rising = current > baseline;
            bool falling = current < baseline;
            indicator.text = rising ? "↑" : falling ? "↓" : "→";
            indicator.EnableInClassList("tile-stat-trend--up", rising);
            indicator.EnableInClassList("tile-stat-trend--down", falling);
            indicator.EnableInClassList("tile-stat-trend--flat", !rising && !falling);
            indicator.tooltip = rising ? "Above base rent" : falling ? "Below base rent" : "At base rent";
        }

        private void ClearTrendIndicators()
        {
            if (lblTileRentTrend != null) lblTileRentTrend.text = "";
            if (lblTileRoiTrend != null) lblTileRoiTrend.text = "";
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Phase D — Helpers Planning Phase
        // ─────────────────────────────────────────────────────────────────────────

        private List<string> GetAvailableUltimates(PlayerModel player)
        {
            var result = new List<string>();
            if (abilitySystem == null || player == null || !abilitySystem.HasEnoughEnergy(player)) return result;

            string[] families = { "Tech", "Armement", "Real Estate", "Pharma", "Luxe", "Oil", "Aerospace", "Automobile" };
            foreach (var fam in families)
                if (HasMonopoly(player, fam)) result.Add(fam);
            return result;
        }

        private void ShowUltimatePicker(PlayerModel player)
        {
            CloseUltimatePicker();
            if (uiDocument == null) return;
            var root = uiDocument.rootVisualElement;
            if (root == null) return;

            abilityPickerOverlay = new VisualElement { name = "Panel_UltimatePicker" };
            abilityPickerOverlay.AddToClassList("bottom-panel-container");
            abilityPickerOverlay.AddToClassList("marketplace-modal");

            var panel = new VisualElement();
            panel.AddToClassList("marketplace-panel");
            panel.AddToClassList("marketplace-panel--compact");
            var content = new VisualElement();
            content.AddToClassList("panel-content");
            var title = new Label("CHOOSE A FAMILY ULTIMATE");
            title.AddToClassList("panel-header-title");
            content.Add(title);

            foreach (string family in GetAvailableUltimates(player))
            {
                string captured = family;
                var button = new Button(() => HandleUltimateChoice(player, captured)) { text = captured.ToUpperInvariant() };
                button.AddToClassList("btn-standard");
                button.style.marginBottom = 8;
                content.Add(button);
            }

            var cancel = new Button(CloseUltimatePicker) { text = "CANCEL" };
            cancel.AddToClassList("btn-secondary");
            content.Add(cancel);
            panel.Add(content);
            abilityPickerOverlay.Add(panel);
            root.Add(abilityPickerOverlay);
            UIUXRuntime.PrepareView(abilityPickerOverlay, CloseUltimatePicker);
            UIToolkitTransitions.SlideUpEnter(this, panel);
            if (tileDetailPanel != null) tileDetailPanel.style.display = DisplayStyle.None;
        }

        private void HandleUltimateChoice(PlayerModel player, string family)
        {
            if (family == "Armement" || family == "Aerospace")
            {
                BeginAbilityTargetSelection(family);
                return;
            }

            var online = OnlineGameController.Instance;
            if (online != null && online.IsOnlineGame)
            {
                string payload = JsonUtility.ToJson(new UltimateCommandDto
                {
                    family = family,
                    targetTileIndex = -1
                });
                online.TrySubmitLocalCommand("ultimate.use", payload);
                CloseUltimatePicker();
                return;
            }

            TriggerUltimate(player, family);
            CloseUltimatePicker();
            UpdateButtonsState(currentSelectedTile, player);
        }

        private bool TriggerUltimate(PlayerModel player, string family)
        {
            if (abilitySystem == null) return false;
            bool ok = family switch
            {
                "Tech"        => abilitySystem.Tech_MaJSysteme(player),
                "Real Estate" => abilitySystem.RealEstate_RappelDividendes(player),
                "Pharma"      => abilitySystem.Pharma_ProtocoleSanitaire(player),
                "Luxe"        => abilitySystem.Luxe_TaxeSomptuaire(player),
                "Oil"         => abilitySystem.OilGas_BoomEnergetique(player),
                "Automobile"  => abilitySystem.Automobile_MoteurTurbo(player),
                _             => false
            };
            Debug.Log(ok
                ? $"[Planning UI] Player {player.PlayerId} activated {family} ultimate."
                : $"[Planning UI] Activation {family} ultimate failed.");
            return ok;
        }

        private void BeginAbilityTargetSelection(string family)
        {
            if (abilityPickerOverlay == null || turnManager == null) return;
            pendingTargetedUltimate = family;
            turnManager.IsInSelectionMode = true;

            abilityPickerOverlay.Clear();
            abilityPickerOverlay.RemoveFromClassList("marketplace-modal");
            abilityPickerOverlay.AddToClassList("marketplace-nonmodal");
            abilityPickerOverlay.pickingMode = PickingMode.Ignore;

            var panel = new VisualElement();
            panel.AddToClassList("marketplace-panel");
            panel.AddToClassList("marketplace-panel--selection");
            panel.pickingMode = PickingMode.Position;
            var content = new VisualElement();
            content.AddToClassList("panel-content");
            var title = new Label(family == "Armement"
                ? "SELECT AN OPPONENT PROPERTY"
                : "SELECT AERO / TECH / HUB / MARKETPLACE");
            title.AddToClassList("panel-header-title");
            content.Add(title);
            var cancel = new Button(CloseUltimatePicker) { text = "CANCEL" };
            cancel.AddToClassList("btn-secondary");
            content.Add(cancel);
            panel.Add(content);
            abilityPickerOverlay.Add(panel);
            UIUXRuntime.PrepareView(abilityPickerOverlay, CloseUltimatePicker);
            HighlightAbilityTargets(family);
        }

        public bool OnAbilityTileSelected(BoardTile tile)
        {
            if (!IsAbilityTargetSelectionActive || tile == null || turnManager == null || abilitySystem == null) return false;
            var player = turnManager.CurrentPlayer;
            if (player == null) return false;

            bool activated = false;
            var online = OnlineGameController.Instance;
            if (online != null && online.IsOnlineGame)
            {
                if (!IsAbilityTileEligible(tile)) return false;
                string payload = JsonUtility.ToJson(new UltimateCommandDto
                {
                    family = pendingTargetedUltimate,
                    targetTileIndex = tile.TileIndex
                });
                online.TrySubmitLocalCommand("ultimate.use", payload);
                CloseUltimatePicker();
                return true;
            }
            if (pendingTargetedUltimate == "Armement")
            {
                var property = tile.Property;
                var owner = property != null ? turnManager.GetOwnerOfProperty(property) : null;
                bool aerospace = property?.FamilyData != null && property.FamilyData.FamilyName.Contains("Aerospace");
                if (property == null || owner == null || owner == player || aerospace) return false;
                activated = abilitySystem.Armement_CyberSabotage(player, property);
            }
            else if (pendingTargetedUltimate == "Aerospace")
            {
                bool eligible = tile.Type == TileType.GlobalAuction;
                if (tile.Property?.FamilyData != null)
                {
                    string name = tile.Property.FamilyData.FamilyName;
                    eligible |= name.Contains("Aerospace") || name.Contains("Tech") || name.Contains("Logistics");
                }
                if (!eligible) return false;
                activated = abilitySystem.Aerospace_SautQuantique(player, tile.TileIndex);
            }

            if (!activated) return false;
            CloseUltimatePicker();
            UpdateButtonsState(currentSelectedTile, player);
            return true;
        }

        public bool IsAbilityTileEligible(BoardTile tile)
        {
            if (!IsAbilityTargetSelectionActive || tile == null || turnManager == null) return false;
            var player = turnManager.CurrentPlayer;
            if (player == null) return false;

            if (pendingTargetedUltimate == "Armement")
            {
                var property = tile.Property;
                var owner = property != null ? turnManager.GetOwnerOfProperty(property) : null;
                bool aerospace = property?.FamilyData != null && property.FamilyData.FamilyName.Contains("Aerospace");
                return property != null && owner != null && owner != player && !aerospace;
            }

            if (pendingTargetedUltimate == "Aerospace")
            {
                if (tile.Type == TileType.GlobalAuction) return true;
                string name = tile.Property?.FamilyData?.FamilyName;
                return name != null && (name.Contains("Aerospace") || name.Contains("Tech") || name.Contains("Logistics"));
            }

            return false;
        }

        private void HighlightAbilityTargets(string family)
        {
            ResetAbilityTargetHighlights();
            if (boardManager == null || turnManager == null) return;
            ColorUtility.TryParseHtmlString(family == "Armement" ? "#FF5B5B" : "#00FFFF", out Color color);
            foreach (var tile in boardManager.Tiles)
            {
                bool eligible;
                if (family == "Armement")
                {
                    var owner = tile.Property != null ? turnManager.GetOwnerOfProperty(tile.Property) : null;
                    eligible = tile.Property != null && owner != null && owner != turnManager.CurrentPlayer
                        && (tile.Property.FamilyData == null || !tile.Property.FamilyData.FamilyName.Contains("Aerospace"));
                }
                else
                {
                    eligible = tile.Type == TileType.GlobalAuction;
                    string name = tile.Property?.FamilyData?.FamilyName;
                    eligible |= name != null && (name.Contains("Aerospace") || name.Contains("Tech") || name.Contains("Logistics"));
                }
                if (!eligible || boardManager.visualTiles == null || tile.TileIndex >= boardManager.visualTiles.Length) continue;
                var visual = boardManager.visualTiles[tile.TileIndex]?.GetComponentInChildren<PolyRisk.Visuals.TileVisuals>();
                if (visual == null) continue;
                visual.BeginPulse(color);
                abilityTargetPulses.Add(visual);
            }
        }

        private void ResetAbilityTargetHighlights()
        {
            foreach (var visual in abilityTargetPulses)
                if (visual != null) visual.EndPulse(true);
            abilityTargetPulses.Clear();
        }

        private void CloseUltimatePicker()
        {
            ResetAbilityTargetHighlights();
            pendingTargetedUltimate = null;
            if (turnManager != null) turnManager.IsInSelectionMode = false;
            abilityPickerOverlay?.RemoveFromHierarchy();
            abilityPickerOverlay = null;
            if (tileDetailPanel != null) tileDetailPanel.style.display = DisplayStyle.Flex;
        }

        private void ShowInvestmentPicker(PlayerModel player)
        {
            if (player == null || turnManager?.financeSystem == null || uiDocument == null) return;
            CloseInvestmentPicker();
            RebuildInvestmentPicker(player, animate: true);
        }

        private void RebuildInvestmentPicker(PlayerModel player, bool animate = false)
        {
            var root = uiDocument?.rootVisualElement;
            if (root == null || player == null) return;

            investmentPickerOverlay?.RemoveFromHierarchy();
            investmentPickerOverlay = new VisualElement { name = "Panel_InvestmentPicker" };
            investmentPickerOverlay.AddToClassList("bottom-panel-container");
            investmentPickerOverlay.AddToClassList("marketplace-modal");

            var panel = new VisualElement();
            panel.AddToClassList("marketplace-panel");
            panel.AddToClassList("marketplace-panel--compact");
            panel.AddToClassList("investment-panel");
            var content = new VisualElement();
            content.AddToClassList("panel-content");

            var title = new Label("STOCK MARKET");
            title.AddToClassList("panel-header-title");
            content.Add(title);

            var market = turnManager.financeSystem.CurrentMarketState;
            var context = new Label($"Market {market} · Sgo {CurrencyFormatter.Format(turnManager.financeSystem.CurrentSgo)}");
            context.AddToClassList("investment-context");
            content.Add(context);

            content.Add(BuildInvestmentLabel("DURATION · longer means more upside and risk"));
            var durationRow = BuildInvestmentRow();
            AddInvestmentOption(durationRow, "1 TURN", pendingInvestmentDuration == 1, () =>
            {
                pendingInvestmentDuration = 1;
                RebuildInvestmentPicker(player);
            });
            AddInvestmentOption(durationRow, "3 TURNS", pendingInvestmentDuration == 3, () =>
            {
                pendingInvestmentDuration = 3;
                RebuildInvestmentPicker(player);
            });
            AddInvestmentOption(durationRow, "5 TURNS", pendingInvestmentDuration == 5, () =>
            {
                pendingInvestmentDuration = 5;
                RebuildInvestmentPicker(player);
            });
            content.Add(durationRow);

            content.Add(BuildInvestmentLabel("AMOUNT"));
            var amountRow = BuildInvestmentRow();
            AddInvestmentOption(amountRow, "1× Sgo", pendingInvestmentPreset == 1, () =>
            {
                pendingInvestmentPreset = 1;
                RebuildInvestmentPicker(player);
            });
            AddInvestmentOption(amountRow, "5× Sgo", pendingInvestmentPreset == 5, () =>
            {
                pendingInvestmentPreset = 5;
                RebuildInvestmentPicker(player);
            });
            AddInvestmentOption(amountRow, "MAX", pendingInvestmentPreset == 0, () =>
            {
                pendingInvestmentPreset = 0;
                RebuildInvestmentPicker(player);
            });
            content.Add(amountRow);

            double amount = ResolveInvestmentAmount(player);
            var risk = new Label(ResolveInvestmentRiskText());
            risk.AddToClassList("investment-risk");
            content.Add(risk);

            var confirm = new Button(() => ConfirmInvestment(player))
            {
                text = $"INVEST {CurrencyFormatter.Format(amount)} · {pendingInvestmentDuration}T"
            };
            confirm.AddToClassList("btn-standard");
            confirm.SetEnabled(amount > 0);
            content.Add(confirm);

            var cancel = new Button(CloseInvestmentPicker) { text = "CANCEL" };
            cancel.AddToClassList("btn-secondary");
            content.Add(cancel);

            panel.Add(content);
            investmentPickerOverlay.Add(panel);
            root.Add(investmentPickerOverlay);
            UIUXRuntime.PrepareView(investmentPickerOverlay, CloseInvestmentPicker);
            if (animate) UIToolkitTransitions.SlideUpEnter(this, panel);
            if (tileDetailPanel != null) tileDetailPanel.style.display = DisplayStyle.None;
        }

        private static Label BuildInvestmentLabel(string text)
        {
            var label = new Label(text);
            label.AddToClassList("investment-label");
            return label;
        }

        private static VisualElement BuildInvestmentRow()
        {
            var row = new VisualElement();
            row.AddToClassList("investment-option-row");
            return row;
        }

        private static void AddInvestmentOption(VisualElement row, string text, bool selected, System.Action action)
        {
            var button = new Button(action) { text = text };
            button.AddToClassList("btn-secondary");
            button.AddToClassList("investment-option-button");
            if (selected) button.AddToClassList("card-selected");
            row.Add(button);
        }

        private double ResolveInvestmentAmount(PlayerModel player)
        {
            double sgo = System.Math.Max(0, turnManager?.financeSystem?.CurrentSgo ?? 0);
            double cap = (pendingInvestmentDuration == 5 ? 20 : 15) * sgo;
            double requested = pendingInvestmentPreset == 0 ? cap : pendingInvestmentPreset * sgo;
            return System.Math.Min(player.CurrentBalance, System.Math.Min(cap, requested));
        }

        private string ResolveInvestmentRiskText()
        {
            switch (pendingInvestmentDuration)
            {
                case 1: return "1T · ROI −30% to +40% before market-cycle modifiers";
                case 3: return "3T · ROI −30% to +70% before market-cycle modifiers";
                default: return "5T · 20% total-loss risk, otherwise ROI −20% to +120%";
            }
        }

        private void ConfirmInvestment(PlayerModel player)
        {
            double amount = ResolveInvestmentAmount(player);
            if (amount <= 0 || turnManager?.financeSystem == null) return;

            var request = new StockInvestmentCommandDto
            {
                amount = amount,
                duration = pendingInvestmentDuration
            };
            var online = OnlineGameController.Instance;
            if (online != null && online.IsOnlineGame)
            {
                online.TrySubmitLocalCommand("stock.invest", JsonUtility.ToJson(request));
                CloseInvestmentPicker();
                return;
            }

            if (turnManager.financeSystem.InvestInStockMarket(player, amount, pendingInvestmentDuration))
                turnManager.CompleteTileResolution();
            CloseInvestmentPicker();
        }

        private void CloseInvestmentPicker()
        {
            investmentPickerOverlay?.RemoveFromHierarchy();
            investmentPickerOverlay = null;
            if (tileDetailPanel != null) tileDetailPanel.style.display = DisplayStyle.Flex;
        }

        private bool HasMonopoly(PlayerModel player, string familyNameContains)
        {
            if (boardManager?.tileDataList == null || player == null) return false;
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
    }
}
