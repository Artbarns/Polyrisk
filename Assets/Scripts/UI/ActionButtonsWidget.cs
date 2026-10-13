using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using PolyRisk.Core;
using PolyRisk.Systems;

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
        private Button btnPrimary, btnSecondary;
        private Label lblTileName, lblTileFamily, lblTileRent, lblTileLevel, lblTileYield;
        private VisualElement containerTileLevel, iconTileFamily;

        private BoardTile currentSelectedTile;
        private float lastClickTime = 0f;
        private const float CLICK_COOLDOWN = 0.5f;

        private void OnEnable()
        {
            GameEvents.OnTileSelected += OnTileSelected;
            GameEvents.OnTurnStarted += OnTurnStarted;
        }

        private void OnDisable()
        {
            GameEvents.OnTileSelected -= OnTileSelected;
            GameEvents.OnTurnStarted -= OnTurnStarted;
        }

        private void OnTurnStarted(int playerId)
        {
            if (turnManager == null) return;
            
            // Reset selection for the new turn
            currentSelectedTile = null;

            PlayerModel activePlayer = turnManager.GetPlayerById(playerId);
            if (activePlayer != null)
            {
                if (activePlayer.IsAI)
                {
                    // Force the UI to show AI TURN immediately, before the AI even rolls the dice
                    SetPrimaryButtonState("AI TURN", false);
                    SetSecondaryButtonState("...", false);
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
                             SetSecondaryButtonState("STAY IN JAIL", true);
                         }
                         else
                         {
                             SetSecondaryButtonState("ROLL DOUBLES", true);
                         }
                    }
                    else
                    {
                        // Reset buttons for human player
                        SetPrimaryButtonState("WAIT", false);
                        SetSecondaryButtonState("...", false);
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

        private void TryBindUIToolkit()
        {
            if (uiDocument == null) return;
            var root = uiDocument.rootVisualElement;
            if (root == null) return;

            if (bottomSheetTemplate != null && root.Q<VisualElement>("Panel_TileDetail") == null)
                bottomSheetTemplate.CloneTree(root);

            btnPrimary = root.Q<Button>("Btn_Primary");
            btnSecondary = root.Q<Button>("Btn_Secondary");
            lblTileName = root.Q<Label>("Lbl_TileName");
            lblTileFamily = root.Q<Label>("Lbl_TileFamily");
            lblTileRent = root.Q<Label>("Lbl_TileRent");
            lblTileLevel = root.Q<Label>("Lbl_TileLevel");
            lblTileYield = root.Q<Label>("Lbl_TileYield");
            containerTileLevel = root.Q<VisualElement>("Container_TileLevel");
            iconTileFamily = root.Q<VisualElement>("Icon_TileFamily");

            // Start() ne s'exécute qu'une fois par durée de vie du composant — pas de
            // risque de double-abonnement.
            if (btnPrimary != null) btnPrimary.clicked += OnPrimaryClicked;
            if (btnSecondary != null) btnSecondary.clicked += OnSecondaryClicked;
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
            if (currentPlayer != null && !currentPlayer.IsAI)
            {
                // Phase D — PLANNING PHASE : Primary = ROLL DICE
                if (turnManager.CurrentState == TurnManager.TurnState.Planning)
                {
                    turnManager.EndPlanningPhase();
                    return;
                }

                // Handle Audit Actions
                if (currentPlayer.IsInAudit)
                {
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

             if (currentPlayer != null && !currentPlayer.IsAI)
             {
                 // Phase D — PLANNING PHASE : Secondary = Use Ultimate (si dispo) sinon PASS
                 if (turnManager.CurrentState == TurnManager.TurnState.Planning)
                 {
                     string ultFam = FindUltimateAvailable(currentPlayer);
                     if (ultFam != null)
                     {
                         TriggerUltimate(currentPlayer, ultFam);
                         UpdateButtonsState(currentSelectedTile, currentPlayer); // refresh (energy peut avoir baissé)
                     }
                     else
                     {
                         // PASS = roll dice directement
                         turnManager.EndPlanningPhase();
                     }
                     return;
                 }

                 // Handle Audit Actions
                 if (currentPlayer.IsInAudit)
                {
                    if (currentPlayer.HasGetOutOfAuditCard)
                    {
                         // Button logic for Escape Card
                         turnManager.UseEscapeCard();
                         return;
                    }

                    if (turnManager.IsJustLandedInAudit)
                    {
                        turnManager.StayInAudit();
                    }
                    else
                    {
                        turnManager.AuditRollDice();
                    }
                    return;
                }

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

        private void OnTileSelected(BoardTile tile)
        {
            HandleTileSelected(tile);
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
                        SetSecondaryButtonState("STAY IN JAIL", true);
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
            SetPrimaryButtonState("...", false);
            SetSecondaryButtonState("...", false);
        }

        /// <summary>
        /// Updates the visual state and interactivity of the buttons.
        /// </summary>
        // Phase D — Cache pour refresh du bottom sheet quand TurnState change
        private TurnManager.TurnState lastObservedState = TurnManager.TurnState.EndTurn;

        private void Update()
        {
            if (turnManager == null) return;
            if (turnManager.CurrentState != lastObservedState)
            {
                lastObservedState = turnManager.CurrentState;
                UpdateButtonsState(currentSelectedTile, turnManager.CurrentPlayer);
            }
        }

        public void UpdateButtonsState(BoardTile tile, PlayerModel currentPlayer)
        {
            // ── Phase D : PLANNING PHASE ─────────────────────────────────────────
            if (turnManager != null
                && turnManager.CurrentState == TurnManager.TurnState.Planning
                && currentPlayer != null && !currentPlayer.IsAI)
            {
                // Primary = ROLL DICE (vert, toujours actif)
                SetPrimaryButtonState("ROLL DICE", true);

                // Secondary = Use Ultimate (si dispo) sinon PASS
                string ultFam = FindUltimateAvailable(currentPlayer);
                if (ultFam != null)
                    SetSecondaryButtonState($"USE {ultFam.ToUpper()} ULT", true);
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
                SetPrimaryButtonState("WAIT", false);
                SetSecondaryButtonState("...", false);
                return;
            }

            if (currentPlayer.IsAI)
            {
                SetPrimaryButtonState("AI TURN", false);
                SetSecondaryButtonState("...", false);
                return;
            }

            // Check if player is in Audit first (Before rolling dice phase)
            if (currentPlayer.IsInAudit)
            {
                 double sgo = turnManager.financeSystem != null ? turnManager.financeSystem.CurrentSgo : 500;
                 SetPrimaryButtonState($"PAY FINE ({CurrencyFormatter.Format(sgo)})", true);
                 
                 if (turnManager.IsJustLandedInAudit)
                 {
                     SetSecondaryButtonState("STAY IN JAIL", true);
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
                SetPrimaryButtonState("ROLLING...", false);
                SetSecondaryButtonState("...", false);
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
                            SetPrimaryButtonState($"BUY ({CurrencyFormatter.Format(propData.BasePrice)})", true);
                            SetSecondaryButtonState("PASS", true);
                        }
                        else
                        {
                            SetPrimaryButtonState("PASS (No Funds)", true);
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
                    SetSecondaryButtonState("...", false);
                }
            }
        }

        private void SetPrimaryButtonState(string label, bool interactable)
        {
            if (btnPrimary == null) return;
            btnPrimary.text = label;
            btnPrimary.SetEnabled(interactable);
        }

        private void SetSecondaryButtonState(string label, bool interactable)
        {
            if (btnSecondary == null) return;
            btnSecondary.text = label;
            btnSecondary.SetEnabled(interactable);
        }

        /// <summary>
        /// Section "inspection de tuile" — absorbée depuis l'ancien InspectorWidget.
        /// Alimente nom/famille/niveau/rent/rent-yield + l'icône (TileIconResolver).
        /// UI Toolkit uniquement (pas de chemin legacy — InspectorWidget est retiré).
        /// </summary>
        private void UpdateInspectorSection(BoardTile tile)
        {
            bool turboChoice = turnManager != null && turnManager.CurrentState == TurnManager.TurnState.ChooseTurbo;
            bool precisionChoice = turnManager != null && turnManager.CurrentState == TurnManager.TurnState.AdjustDice;
            lblTileFamily?.EnableInClassList("tile-family-badge",
                !turboChoice && !precisionChoice && tile != null && tile.Type == TileType.Property && tile.Property != null);
            if (iconTileFamily != null) TileIconResolver.PopulateIcon(iconTileFamily, tile?.Data);

            if (tile != null && tile.Type == TileType.Property && tile.Property != null)
            {
                var prop = tile.Property;
                if (lblTileName != null) lblTileName.text = prop.PropertyName;
                if (lblTileFamily != null)
                    lblTileFamily.text = prop.FamilyData != null
                        ? $"{prop.FamilyData.FamilyName} · Level {prop.CurrentLevel}"
                        : $"Level {prop.CurrentLevel}";
                if (lblTileRent != null) lblTileRent.text = CurrencyFormatter.Format(prop.GetCurrentRent());
                if (lblTileLevel != null) lblTileLevel.text = prop.CurrentLevel.ToString();
                if (containerTileLevel != null) containerTileLevel.style.display = DisplayStyle.Flex;
                if (lblTileYield != null)
                {
                    // Pas de concept "Risk" dans le modèle actuel (aucune donnée par tuile) —
                    // seul un ratio dérivable proprement est affiché, cf. plan.
                    double yieldPct = prop.BasePrice > 0 ? prop.BaseRent / prop.BasePrice * 100.0 : 0.0;
                    lblTileYield.text = $"{yieldPct:0.0}%";
                }
            }
            else if (tile != null)
            {
                if (lblTileName != null) lblTileName.text = tile.Data != null ? tile.Data.TileName : tile.Type.ToString();
                if (lblTileFamily != null) lblTileFamily.text = "Special Tile";
                if (lblTileRent != null) lblTileRent.text = "—";
                if (lblTileYield != null) lblTileYield.text = "—";
                if (containerTileLevel != null) containerTileLevel.style.display = DisplayStyle.None;
            }
            else
            {
                if (lblTileName != null) lblTileName.text = "—";
                if (lblTileFamily != null) lblTileFamily.text = "";
                if (lblTileRent != null) lblTileRent.text = "—";
                if (lblTileYield != null) lblTileYield.text = "—";
                if (containerTileLevel != null) containerTileLevel.style.display = DisplayStyle.None;
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Phase D — Helpers Planning Phase
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>Retourne le nom d'une famille avec Ultime activable, ou null.</summary>
        private string FindUltimateAvailable(PlayerModel player)
        {
            if (abilitySystem == null || player == null) return null;
            if (!abilitySystem.HasEnoughEnergy(player)) return null;

            string[] families = { "Tech", "Armement", "Real Estate", "Pharma", "Luxe", "Oil", "Aerospace", "Automobile" };
            foreach (var fam in families)
                if (HasMonopoly(player, fam)) return fam;
            return null;
        }

        /// <summary>Active l'Ultime de la famille donnée.</summary>
        private void TriggerUltimate(PlayerModel player, string family)
        {
            if (abilitySystem == null) return;
            bool ok = family switch
            {
                "Tech"        => abilitySystem.Tech_MaJSysteme(player),
                "Armement"    => abilitySystem.Armement_CyberSabotage(player),
                "Real Estate" => abilitySystem.RealEstate_RappelDividendes(player),
                "Pharma"      => abilitySystem.Pharma_ProtocoleSanitaire(player),
                "Luxe"        => abilitySystem.Luxe_TaxeSomptuaire(player),
                "Oil"         => abilitySystem.OilGas_BoomEnergetique(player),
                "Aerospace"   => abilitySystem.Aerospace_SautQuantique(player),
                "Automobile"  => abilitySystem.Automobile_MoteurTurbo(player),
                _             => false
            };
            Debug.Log(ok
                ? $"[Planning UI] Player {player.PlayerId} activated {family} ultimate."
                : $"[Planning UI] Activation {family} ultimate failed.");
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
