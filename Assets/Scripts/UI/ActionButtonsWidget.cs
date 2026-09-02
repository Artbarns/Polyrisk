using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using PolyRisk.Core;
using PolyRisk.Systems;

namespace PolyRisk.UI
{
    /// <summary>
    /// Manages the primary and secondary action buttons.
    /// Updates their state and text based on the selected tile and current player context.
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

        [Tooltip("The primary action button (e.g., Buy, Upgrade).")]
        [SerializeField] private Button primaryButton;
        [SerializeField] private TextMeshProUGUI primaryButtonText;

        [Tooltip("The secondary action button (e.g., Mortgage, Sell).")]
        [SerializeField] private Button secondaryButton;
        [SerializeField] private TextMeshProUGUI secondaryButtonText;

        private BoardTile currentSelectedTile;
        private float lastClickTime = 0f;
        private const float CLICK_COOLDOWN = 0.5f;

        private void OnEnable()
        {
            GameEvents.OnTileSelected += OnTileSelected;
            GameEvents.OnTurnStarted += OnTurnStarted;
            if (primaryButton != null) primaryButton.onClick.AddListener(OnPrimaryClicked);
            if (secondaryButton != null) secondaryButton.onClick.AddListener(OnSecondaryClicked);
        }

        private void OnDisable()
        {
            GameEvents.OnTileSelected -= OnTileSelected;
            GameEvents.OnTurnStarted -= OnTurnStarted;
            if (primaryButton != null) primaryButton.onClick.RemoveListener(OnPrimaryClicked);
            if (secondaryButton != null) secondaryButton.onClick.RemoveListener(OnSecondaryClicked);
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
                    SetButtonState(primaryButton, primaryButtonText, "AI TURN", false);
                    SetButtonState(secondaryButton, secondaryButtonText, "...", false);
                }
                else
                {
                    // Check for Audit state
                    if (activePlayer.IsInAudit)
                    {
                         double sgo = turnManager.financeSystem != null ? turnManager.financeSystem.CurrentSgo : 500;
                         SetButtonState(primaryButton, primaryButtonText, $"PAY FINE ({CurrencyFormatter.Format(sgo)})", true);
                         
                         if (turnManager.IsJustLandedInAudit)
                         {
                             SetButtonState(secondaryButton, secondaryButtonText, "STAY IN JAIL", true);
                         }
                         else
                         {
                             SetButtonState(secondaryButton, secondaryButtonText, "ROLL DOUBLES", true);
                         }
                    }
                    else
                    {
                        // Reset buttons for human player
                        SetButtonState(primaryButton, primaryButtonText, "WAIT", false);
                        SetButtonState(secondaryButton, secondaryButtonText, "...", false);
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

             // Initial check
             if (currentSelectedTile == null)
                 UpdateButtonsState(null, null);
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
                SetButtonState(primaryButton, primaryButtonText, $"PAY FINE ({CurrencyFormatter.Format(sgo)})", true);
                
                if (turnManager.IsJustLandedInAudit)
                {
                    if (currentPlayer.HasGetOutOfAuditCard)
                    {
                        SetButtonState(secondaryButton, secondaryButtonText, "USE ESCAPE CARD", true);
                        // We need to re-bind secondary listener to UseEscapeCard instead of StayInAudit?
                        // But ActionButtonsWidget relies on OnSecondaryClicked which calls methods based on state.
                        // We will update OnSecondaryClicked.
                    }
                    else
                    {
                        SetButtonState(secondaryButton, secondaryButtonText, "STAY IN JAIL", true);
                    }
                }
                else
                {
                    if (currentPlayer.HasGetOutOfAuditCard)
                    {
                        SetButtonState(secondaryButton, secondaryButtonText, "USE ESCAPE CARD", true);
                    }
                    else
                    {
                        SetButtonState(secondaryButton, secondaryButtonText, "ROLL DOUBLES", true);
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
            SetButtonState(primaryButton, primaryButtonText, "...", false);
            SetButtonState(secondaryButton, secondaryButtonText, "...", false);
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
                SetButtonState(primaryButton, primaryButtonText, "ROLL DICE", true);

                // Secondary = Use Ultimate (si dispo) sinon PASS
                string ultFam = FindUltimateAvailable(currentPlayer);
                if (ultFam != null)
                    SetButtonState(secondaryButton, secondaryButtonText, $"USE {ultFam.ToUpper()} ULT", true);
                else
                    SetButtonState(secondaryButton, secondaryButtonText, "PASS", true);
                return;
            }

            if (turnManager != null && (turnManager.CurrentState == TurnManager.TurnState.MovingPawn || turnManager.CurrentState == TurnManager.TurnState.RollDice))
            {
                ClearButtons();
                return;
            }

            if (currentPlayer == null)
            {
                SetButtonState(primaryButton, primaryButtonText, "WAIT", false);
                SetButtonState(secondaryButton, secondaryButtonText, "...", false);
                return;
            }

            if (currentPlayer.IsAI)
            {
                SetButtonState(primaryButton, primaryButtonText, "AI TURN", false);
                SetButtonState(secondaryButton, secondaryButtonText, "...", false);
                return;
            }

            // Check if player is in Audit first (Before rolling dice phase)
            if (currentPlayer.IsInAudit)
            {
                 double sgo = turnManager.financeSystem != null ? turnManager.financeSystem.CurrentSgo : 500;
                 SetButtonState(primaryButton, primaryButtonText, $"PAY FINE ({CurrencyFormatter.Format(sgo)})", true);
                 
                 if (turnManager.IsJustLandedInAudit)
                 {
                     SetButtonState(secondaryButton, secondaryButtonText, "STAY IN JAIL", true);
                 }
                 else
                 {
                     SetButtonState(secondaryButton, secondaryButtonText, "ROLL DOUBLES", true);
                 }
                
                // Re-bind listeners for Audit Actions if needed, but since we use OnPrimary/SecondaryClicked, we can branch there.
                return;
            }

            if (tile == null)
            {
                // Probably rolling dice phase
                SetButtonState(primaryButton, primaryButtonText, "ROLLING...", false);
                SetButtonState(secondaryButton, secondaryButtonText, "...", false);
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
                        SetButtonState(primaryButton, primaryButtonText, "MAX LEVEL", false);
                    }
                    else
                    {
                        // Only allow upgrade if they have full family? 
                        // Prompt said: "Show UPGRADE/PASS (if they have the full family, otherwise just PASS)"
                        if (rulesEngine != null && rulesEngine.HasFullFamily(currentPlayer, propData.FamilyData))
                        {
                            SetButtonState(primaryButton, primaryButtonText, "UPGRADE", true);
                        }
                        else
                        {
                            SetButtonState(primaryButton, primaryButtonText, "UPGRADE (Need Monopoly)", false);
                        }
                    }
                    SetButtonState(secondaryButton, secondaryButtonText, "END TURN", true);
                }
                else
                {
                    // Check ownership
                    if (owner == null)
                    {
                        // Unowned -> BUY
                        if (currentPlayer.CurrentBalance >= propData.BasePrice)
                        {
                            SetButtonState(primaryButton, primaryButtonText, $"BUY ({CurrencyFormatter.Format(propData.BasePrice)})", true);
                            SetButtonState(secondaryButton, secondaryButtonText, "PASS", true);
                        }
                        else
                        {
                            SetButtonState(primaryButton, primaryButtonText, "PASS (No Funds)", true);
                        }
                    }
                    else
                    {
                        // Owned by someone else -> Should have been auto-paid?
                        // If we are here, maybe auto-pay failed or we are viewing it?
                        // Prompt Bug 1: "Human players see a "BUY" button when landing on properties owned by opponents"
                        // Since we added Auto-Pay in TurnManager, this UI state might be transient or for "Just Visiting".
                        // We should show "RENT PAID" or similar.
                        SetButtonState(primaryButton, primaryButtonText, $"RENT PAID TO {owner.PlayerId}", false);
                        SetButtonState(secondaryButton, secondaryButtonText, "END TURN", true);
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
                
                SetButtonState(primaryButton, primaryButtonText, actionText, canInteract);
                if (canPass)
                {
                    SetButtonState(secondaryButton, secondaryButtonText, "PASS", true);
                }
                else
                {
                    SetButtonState(secondaryButton, secondaryButtonText, "...", false);
                }
            }
        }

        private void SetButtonState(Button btn, TextMeshProUGUI txt, string label, bool interactable)
        {
            if (btn != null)
            {
                btn.interactable = interactable;
                if (txt != null) txt.text = label;
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
