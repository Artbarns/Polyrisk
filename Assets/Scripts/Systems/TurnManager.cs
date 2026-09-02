using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using PolyRisk.Core;
using PolyRisk.UI;

namespace PolyRisk.Systems
{
    /// <summary>
    /// Manages the game loop, player turns, and state transitions.
    /// Implements a State Machine for the turn phases.
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        public static TurnManager Instance { get; private set; }

        public enum TurnState
        {
            TurnStart,
            WaitingForAuditDecision,
            Planning,        // Phase D — Pre-roll : activer Ultime + Auto Precision
            RollDice,
            MovingPawn,
            ResolveTile,
            EndTurn
        }

        [Header("Game Settings")]
        [Tooltip("List of players in the game.")]
        [SerializeField] private List<PlayerModel> players;
        public List<PlayerModel> Players => players;
        
        // Track current tile index for each player
        private Dictionary<int, int> playerPositions = new Dictionary<int, int>();

        // Public accessor for current state
        public TurnState CurrentState => currentState;

        public void AuditRollDice()
        {
            PlayerModel currentPlayer = players[currentPlayerIndex];
            if (!currentPlayer.IsInAudit) return;

            int[] result = diceSystem.RollDice();
            Debug.Log($"Audit Roll: {result[0]} + {result[1]}");

            if (result[0] == result[1])
            {
                Debug.Log("Rolled Doubles! Escaped Audit Fiscal immediately.");
                currentPlayer.IsInAudit = false;
                currentPlayer.AuditTurnsRemaining = 0;
                
                // Move with this roll
                StartCoroutine(ExecuteMovement(currentPlayer, result[0] + result[1]));
            }
            else
            {
                currentPlayer.AuditTurnsRemaining--;
                if (currentPlayer.AuditTurnsRemaining <= 0)
                {
                    Debug.Log("Audit sentence served. Paying fine (500) and moving.");
                    transactionManager.DeductMoney(currentPlayer, 500);
                    currentPlayer.IsInAudit = false;
                    currentPlayer.AuditTurnsRemaining = 0;
                    
                    // Move with this roll
                    StartCoroutine(ExecuteMovement(currentPlayer, result[0] + result[1]));
                }
                else
                {
                    Debug.Log("Failed to roll doubles. Staying in Audit.");
                    currentState = TurnState.EndTurn;
                }
            }
        }

        private IEnumerator ExecuteMovementAfterAuditEscape()
        {
             // Wait a frame
             yield return null;
             int[] result = diceSystem.RollDice();
             yield return StartCoroutine(ExecuteMovement(players[currentPlayerIndex], result[0] + result[1]));
        }

        private IEnumerator ExecuteMovement(PlayerModel player, int steps)
        {
            int currentPos = playerPositions[player.PlayerId];

            // ── Automobile Passif "Assistance de Précision" : appliquer le delta si actif ──
            if (player.PendingDiceDelta != 0)
            {
                int newSteps = Mathf.Max(1, steps + player.PendingDiceDelta);
                Debug.Log($"[Auto Precision] Movement steps {steps} → {newSteps} (delta {player.PendingDiceDelta:+#;-#})");
                steps = newSteps;
                player.PendingDiceDelta = 0;
            }

            // Get path
            List<Vector3> path = BoardManager.Instance.GetPath(currentPos, steps);
            GameEvents.OnPlayerPathCalculated?.Invoke(player.PlayerId, path);
            
            currentState = TurnState.MovingPawn;
            if (actionButtonsWidget != null) actionButtonsWidget.ClearButtons();
            
            // Wait for movement to finish (Assume movement takes time proportional to steps or fixed duration)
            // Ideally we should hook into OnPawnMovementFinished, but coroutines are cleaner for sequential logic.
            // Since we have OnPawnMovementFinished event, let's use a flag.
            
            bool movementFinished = false;
            System.Action onFinished = () => movementFinished = true;
            GameEvents.OnPawnMovementFinished += onFinished;
            
            // Calculate new position logically BUT wait to apply effects
            int newPos = (currentPos + steps) % 40;
            
            // Check for GO passing during movement (simplified: we trigger it after movement for now to ensure sync)
            // Prompt Option 2: "trigger... immediately after the movement coroutine finishes"
            
            // Wait until visual movement is done
            yield return new WaitUntil(() => movementFinished);
            
            GameEvents.OnPawnMovementFinished -= onFinished;
            
            // Update logical position after movement
            playerPositions[player.PlayerId] = newPos;
            
            // Handle GO Logic
            if (newPos < currentPos)
            {
                Debug.Log($"Player {player.PlayerId} passed GO.");
                player.LapsCompleted++;

                // ── Sgo inflation ──────────────────────────────────────────────
                if (financeSystem != null)
                {
                    int activePlayers = players.Count(p => !p.IsBankrupt);
                    if (activePlayers > 0)
                    {
                        int sgoGrowth = Mathf.FloorToInt(1000f / activePlayers);

                        // Phase C : modificateur de cycle (BOOM ×1.3 / CRASH ×0.6)
                        var cm = CycleManager.Instance;
                        if (cm != null && cm.IsActive)
                            sgoGrowth = Mathf.FloorToInt(sgoGrowth * (float)cm.GetSgoMultiplier());

                        financeSystem.CurrentSgo += sgoGrowth;
                        Debug.Log($"<color=green>[GO] Sgo +{sgoGrowth} (1000÷{activePlayers}) → {financeSystem.CurrentSgo}</color>");
                    }
                    financeSystem.ProcessGoIncome(player);
                }

                // ── Passif Automobile — Assistance de Précision reset ─────────
                player.AutoPrecisionAvailable = true;

                // ── Énergie Ability System ─────────────────────────────────────
                var abilitySystem = AbilitySystem.Instance;
                if (abilitySystem != null) abilitySystem.ChargeEnergyOnGo(player);

                // ── Décrémenter les buffs en cours ─────────────────────────────
                if (player.OilGasBoostTurnsRemaining > 0)
                    player.OilGasBoostTurnsRemaining--;

                if (player.ImmunityTurnsRemaining > 0)
                {
                    player.ImmunityTurnsRemaining--;
                    if (player.ImmunityTurnsRemaining <= 0)
                        player.HasImmunityCard = false;
                }
            }

            // ── Phase C : Transit Express 4 Hubs (GDD_Board v7) ──────────────────
            int finalPos = HandleTransitExpress(player, newPos);
            if (finalPos != newPos)
            {
                playerPositions[player.PlayerId] = finalPos;
                TeleportPlayer(player, finalPos);
                newPos = finalPos;
            }

            Debug.Log($"Player {player.PlayerId} arrived at tile {newPos}.");

            // Transition to ResolveTile
            currentState = TurnState.ResolveTile;
        }

        /// <summary>
        /// Transit Express : si le joueur atterit sur un Hub et possède les 4 Hubs,
        /// il est téléporté au Hub suivant. Pas de Sgo collecté si traverse Case 0 (Hub 35 → Hub 5).
        /// IA : auto-active. Humain : auto-active aussi (UI optionnelle pour Phase D).
        /// </summary>
        private int HandleTransitExpress(PlayerModel player, int currentPos)
        {
            int[] hubIndices = { 5, 15, 25, 35 };
            if (System.Array.IndexOf(hubIndices, currentPos) < 0) return currentPos;

            // Vérifier que le joueur possède les 4 Hubs
            int hubCount = player.OwnedProperties.Count(p =>
                p.FamilyData != null && p.FamilyData.FamilyName.Contains("Logistics"));
            if (hubCount < 4) return currentPos;

            // Hub suivant dans le sens horaire
            int nextHub = currentPos switch
            {
                5  => 15,
                15 => 25,
                25 => 35,
                35 => 5,   // boucle — traverse Case 0
                _  => currentPos
            };

            Debug.Log($"[Transit Express] Player {player.PlayerId} possède les 4 Hubs → téléportation {currentPos} → {nextHub} (pas de Sgo si traverse Case 0).");
            return nextHub;
        }

        /// <summary>
        /// Instantly moves a player to a specific tile index.
        /// </summary>
        public void TeleportPlayer(PlayerModel player, int targetIndex)
        {
            if (player == null) return;
            
            // Update logical position
            playerPositions[player.PlayerId] = targetIndex;
            
            // Update visual position
            // Find the pawn GameObject by name (created by PlayerVisualManager)
            GameObject pawn = GameObject.Find($"Pawn_Player_{player.PlayerId}");
            if (pawn != null)
            {
                if (BoardManager.Instance != null && BoardManager.Instance.visualTiles != null && targetIndex >= 0 && targetIndex < BoardManager.Instance.visualTiles.Length)
                {
                    Transform targetTile = BoardManager.Instance.visualTiles[targetIndex];
                    if (targetTile != null)
                    {
                        pawn.transform.position = targetTile.position;
                        Debug.Log($"Player {player.PlayerId} visually teleported to tile {targetIndex}.");
                    }
                    else
                    {
                        Debug.LogWarning($"Teleport failed: Visual tile at index {targetIndex} is null.");
                    }
                }
                else
                {
                    Debug.LogWarning($"Teleport failed: BoardManager or visualTiles invalid.");
                }
            }
            else
            {
                Debug.LogWarning($"Teleport visual failed: Pawn 'Pawn_Player_{player.PlayerId}' not found in scene.");
            }
        }

        /// <summary>
        /// Retrieves a player by their ID.
        /// </summary>
        public PlayerModel GetPlayerById(int playerId)
        {
            return players.Find(p => p.PlayerId == playerId);
        }

        /// <summary>
        /// Finds the owner of a given property.
        /// </summary>
        /// <param name="property">The property to check.</param>
        /// <returns>The owning PlayerModel, or null if unowned.</returns>
        public PlayerModel GetOwnerOfProperty(PropertyData property)
        {
            foreach (var p in players)
            {
                if (p.OwnedProperties.Contains(property))
                {
                    return p;
                }
            }
            return null;
        }
        
        [Tooltip("The index of the player whose turn it is.")]
        [SerializeField] private int currentPlayerIndex = 0;

        /// <summary>
        /// Public accessor for the current player model.
        /// </summary>
        public PlayerModel CurrentPlayer => players != null && players.Count > 0 ? players[currentPlayerIndex] : null;

        [Tooltip("Penalty amount for excessive doubles.")]
        [SerializeField] private double penaltyAmount = 500.0;

        // Current state of the turn
        private TurnState currentState;
        private bool isGameOver = false;
        
        // Dependencies
        private DiceSystem diceSystem;
        private TransactionManager transactionManager;

        [Tooltip("Reference to the FinanceSystem for GO logic.")]
        [SerializeField] public FinanceSystem financeSystem;

        [Tooltip("Reference to the RulesEngine.")]
        public RulesEngine rulesEngine;

        [Tooltip("Reference to the ActionButtonsWidget.")]
        public ActionButtonsWidget actionButtonsWidget;

        private bool justLandedInAudit = false;
        public bool IsJustLandedInAudit => justLandedInAudit;

        // Global Sgo Inflation Tracking
        public int GlobalLapsCompleted { get; private set; } = 0;

        public bool IsInSelectionMode { get; set; }  // Supprime le { get; set; } pour que le Header fonctionne
        
        [Tooltip("Reference to the Marketplace UI Manager for selection callback.")]
        public Marketplace_UIManager marketplaceUIManager;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                // In a real scenario, players would be initialized via a GameSetup manager
                players = new List<PlayerModel>(); 
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void EnterAuditDecisionMode()
        {
            justLandedInAudit = true;
            currentState = TurnState.WaitingForAuditDecision;
            // Notify UI to update
            GameEvents.OnTurnStarted?.Invoke(players[currentPlayerIndex].PlayerId);
        }

        public void StayInAudit()
        {
            if (justLandedInAudit)
            {
                Debug.Log("Player chose to stay in Audit (Just Landed). Ending turn.");
                CompleteTileResolution();
            }
            else
            {
                Debug.Log("Player chose to stay in Audit (Start of Turn). Ending turn.");
                currentState = TurnState.EndTurn;
            }
            justLandedInAudit = false;
        }

        public void UseEscapeCard()
        {
            PlayerModel currentPlayer = players[currentPlayerIndex];
            if (currentPlayer != null && currentPlayer.HasGetOutOfAuditCard)
            {
                currentPlayer.HasGetOutOfAuditCard = false;
                currentPlayer.IsInAudit = false;
                currentPlayer.AuditTurnsRemaining = 0;
                justLandedInAudit = false;
                
                Debug.Log($"Player {currentPlayer.PlayerId} used a Get Out Of Audit Free card!");
                
                // Allow movement immediately
                StartCoroutine(ExecuteMovementAfterAuditEscape());
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // Phase B — Automobile Passif "Assistance de Précision"
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Applique le passif Automobile : ajuste le total des dés de ±1.
        /// À appeler depuis l'UI ou l'IA après le RollDice et AVANT ExecuteMovement.
        /// Le flag AutoPrecisionAvailable est consommé.
        /// </summary>
        /// <param name="delta">+1 ou -1 (clamp automatique).</param>
        /// <returns>true si le passif a été consommé.</returns>
        public bool UseAutoPrecision(PlayerModel player, int delta)
        {
            if (player == null) return false;
            if (!player.AutoPrecisionAvailable)
            {
                Debug.LogWarning($"[Auto Precision] Player {player.PlayerId} a déjà utilisé l'Assistance ce tour.");
                return false;
            }
            int clamped = Mathf.Clamp(delta, -1, 1);
            if (clamped == 0) return false;

            player.AutoPrecisionAvailable = false;
            // Le delta est stocké et appliqué dans ExecuteMovement via player.PendingDiceDelta
            player.PendingDiceDelta = clamped;
            Debug.Log($"[Auto Precision] Player {player.PlayerId} ajuste le total des dés de {clamped:+#;-#}.");
            return true;
        }

        /// <summary>Helper : true si player a le monopole Pharma complet.</summary>
        private bool PharmaPassiveCheck(PlayerModel player)
        {
            if (player == null) return false;
            var board = BoardManager.Instance;
            if (board?.tileDataList == null) return false;
            int total = 0, owned = 0;
            foreach (var td in board.tileDataList)
            {
                if (td is PolyRisk.Core.PropertyData p && p.FamilyData != null && p.FamilyData.FamilyName.Contains("Pharma"))
                {
                    total++;
                    if (player.OwnedProperties.Contains(p)) owned++;
                }
            }
            return total > 0 && owned == total;
        }

        public void AuditPayFine()
        {
            PlayerModel currentPlayer = players[currentPlayerIndex];
            if (!currentPlayer.IsInAudit) return;

            double fineAmount = financeSystem != null ? financeSystem.CurrentSgo : 500;

            // Passif Pharma — Optimisation Fiscale : amende Audit réduite de 25%
            if (rulesEngine != null && PharmaPassiveCheck(currentPlayer))
            {
                fineAmount *= 0.75;
                Debug.Log($"[Passif Pharma] Optimisation Fiscale : amende Audit -25% → ${fineAmount:N0}");
            }

            if (transactionManager.DeductMoney(currentPlayer, fineAmount))
            {
                Debug.Log($"Player {currentPlayer.PlayerId} paid {fineAmount} fine to leave Audit Fiscal.");
                currentPlayer.IsInAudit = false;
                currentPlayer.AuditTurnsRemaining = 0;
                
                if (justLandedInAudit)
                {
                    Debug.Log("Paid fine immediately after landing. Turn Ends.");
                    CompleteTileResolution();
                }
                else
                {
                    // Start of turn: Roll dice immediately to move
                    StartCoroutine(ExecuteMovementAfterAuditEscape());
                }
                justLandedInAudit = false;
            }
            else
            {
                Debug.LogWarning($"Player {currentPlayer.PlayerId} cannot afford the fine!");
                // Fix #7 Phase F — Fallback anti-livelock : si IA ne peut pas payer, on force fin de tour.
                if (currentPlayer.IsAI)
                {
                    Debug.LogWarning($"[Audit] IA {currentPlayer.PlayerId} en fallback : fin de tour forcée.");
                    justLandedInAudit = false;
                    currentState = TurnState.EndTurn;
                }
            }
        }

        private void OnEnable()
        {
            GameEvents.OnPawnMovementFinished += OnPawnMovementFinished;
        }

        private void OnDisable()
        {
            GameEvents.OnPawnMovementFinished -= OnPawnMovementFinished;
        }

        private void OnPawnMovementFinished()
        {
            if (currentState == TurnState.MovingPawn)
            {
                currentState = TurnState.ResolveTile;
            }
        }

        /// <summary>
        /// Starts the game with the provided players.
        /// </summary>
        public void StartGame(List<PlayerModel> initialPlayers)
        {
            players = initialPlayers;
            
            // Initialize positions
            foreach (var p in players)
            {
                playerPositions[p.PlayerId] = 0;
            }

            // Start the loop
            StartCoroutine(GameLoop());
        }

        private void Start()
        {
            // Initialize dependencies
            // Assumes DiceSystem is on the same GameObject or findable
            diceSystem = GetComponent<DiceSystem>();
            if (diceSystem == null)
            {
                diceSystem = gameObject.AddComponent<DiceSystem>();
            }
            transactionManager = TransactionManager.Instance;

            // Wait for GameBootstrapper to call StartGame.
            // Do NOT auto-start if players are empty.
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Space))
            {
                UnityEngine.Debug.LogWarning("[DEBUG] Spacebar pressed. Forcing Tile Resolution.");
                CompleteTileResolution();
            }

            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.M))
            {
                // Only allow if the game is waiting for a decision/roll and not already moving or resolving
                if (currentState == TurnState.RollDice || currentState == TurnState.WaitingForAuditDecision || currentState == TurnState.TurnStart)
                {
                    UnityEngine.Debug.Log("[DEBUG] Shortcut M: Teleporting to Marketplace 20");
                    
                    PlayerModel currentPlayer = players[currentPlayerIndex];
                    
                    // Force the player's logical and visual position to Marketplace (index 20)
                    TeleportPlayer(currentPlayer, 20);
                    
                    // Transition the state to trigger the tile resolution logic for tile 20
                    currentState = TurnState.ResolveTile;
                }
            }
        }

        public void ResetDoublesState()
        {
            if (diceSystem != null)
            {
                diceSystem.ResetDoubles();
            }
        }

        /// <summary>
        /// Transitions the turn to the next player.
        /// Publicly accessible for AIManager and UI.
        /// </summary>
        /// <summary>
        /// Phase D — Termine la phase Planning et transitionne vers RollDice.
        /// Appelé par l'UI humain via le bouton "ROLL DICE" du widget Planning,
        /// ou implicitement par l'IA après HandlePlanningPhase.
        /// </summary>
        public void EndPlanningPhase()
        {
            if (currentState != TurnState.Planning)
            {
                Debug.LogWarning($"[Planning] EndPlanningPhase appelé hors état Planning (state={currentState}).");
                return;
            }
            currentState = TurnState.RollDice;
        }

        public void CompleteTileResolution()
        {
            // Guard : n'agit que si le tour est réellement en attente de résolution.
            // Évite les appels en double (Marketplace AI seal + fallback AIManager, etc.)
            if (currentState != TurnState.ResolveTile) return;

            if (diceSystem.ConsecutiveDoubles > 0 && diceSystem.ConsecutiveDoubles < 5)
            {
                Debug.Log($"Player rolled doubles! Rolling again.");
                currentState = TurnState.RollDice;
            }
            else
            {
                currentState = TurnState.EndTurn;
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// DEBUG ONLY — Stoppe la loop en cours, place un joueur sur une case et relance
        /// le GameLoop directement en ResolveTile. Ne touche pas aux propriétés.
        /// Appelé par DebugMarketplaceSetup.
        /// </summary>
        public void DEBUG_ForcePlayerOnTile(int playerIndex, int tileIndex)
        {
            if (players == null || players.Count == 0)
            {
                Debug.LogError("[DEBUG] TurnManager.DEBUG_ForcePlayerOnTile : liste de joueurs vide.");
                return;
            }

            playerIndex = Mathf.Clamp(playerIndex, 0, players.Count - 1);

            StopAllCoroutines();

            currentPlayerIndex = playerIndex;
            playerPositions[players[playerIndex].PlayerId] = tileIndex;
            TeleportPlayer(players[playerIndex], tileIndex);
            currentState = TurnState.ResolveTile;

            Debug.Log($"[DEBUG] Player {players[playerIndex].PlayerId} forcé sur tile {tileIndex}. Relance GameLoop en ResolveTile.");
            StartCoroutine(GameLoop());
        }
#endif

        /// <summary>
        /// The main game loop coroutine handling state transitions.
        /// </summary>
        private IEnumerator GameLoop()
        {
            while (!isGameOver)
            {
                yield return StartCoroutine(HandleTurnState());
            }
        }

        /// <summary>
        /// Handles the logic for the current state.
        /// </summary>
        private IEnumerator HandleTurnState()
        {
            PlayerModel currentPlayer = players[currentPlayerIndex];

            switch (currentState)
            {
                case TurnState.TurnStart:
                    Debug.Log($"Turn Start: Player {currentPlayer.PlayerId}");
                    GameEvents.OnTurnStarted?.Invoke(currentPlayer.PlayerId);
                    
                    // Process Stock Market Investments
                    if (financeSystem != null)
                    {
                        financeSystem.ProcessActiveInvestments(currentPlayer);
                    }

                    // Fix #2 Phase F — DistributePassiveIncome (5% placeholder) supprimé.
                    // Les vrais dividendes tiérisés (T1=10%/T2=14%/T3=25% des loyers théoriques)
                    // sont versés au passage GO par FinanceSystem.ProcessGoIncome, avec condition
                    // de monopole. Pas de double source de revenu.


                    // Transition to appropriate state
                    if (currentPlayer.IsInAudit)
                    {
                        if (currentPlayer.IsAI)
                        {
                            // AI logic handles it in RollDice or we can do it here immediately?
                            // Let's stick to the prompt: Check if player is in audit.
                            // If true, do NOT proceed to normal WaitingForRoll.
                            
                            // Since AI logic was previously in RollDice, we should move it or adapt.
                            // But prompt says: "Set a state like WaitingForAuditDecision"
                            
                            currentState = TurnState.WaitingForAuditDecision;
                        }
                        else
                        {
                            Debug.Log("Player in Audit. Waiting for decision.");
                            currentState = TurnState.WaitingForAuditDecision;
                            // Trigger UI update
                            GameEvents.OnTurnStarted?.Invoke(currentPlayer.PlayerId);
                        }
                    }
                    else
                    {
                        // Phase D — Planning Phase avant le RollDice
                        currentState = TurnState.Planning;
                    }
                    break;

                case TurnState.Planning:
                    if (currentPlayer.IsAI)
                    {
                        var aiMgr = FindObjectOfType<AIManager>();
                        if (aiMgr != null)
                            yield return StartCoroutine(aiMgr.HandlePlanningPhase(currentPlayer));
                        currentState = TurnState.RollDice;
                    }
                    else
                    {
                        // Humain : attendre que l'UI appelle EndPlanningPhase()
                        // Safety net : si pas de réponse après 60s, auto-skip pour éviter le freeze permanent.
                        float waitTimer = 0f;
                        const float MAX_WAIT = 60f;
                        while (currentState == TurnState.Planning && waitTimer < MAX_WAIT)
                        {
                            yield return null;
                            waitTimer += Time.deltaTime;
                        }
                        if (currentState == TurnState.Planning)
                        {
                            Debug.LogWarning("[Planning] Safety net : 60s écoulées sans EndPlanningPhase — auto-skip vers RollDice. Vérifie que le Planning Widget est dans la scène.");
                            currentState = TurnState.RollDice;
                        }
                    }
                    break;

                case TurnState.WaitingForAuditDecision:
                    if (currentPlayer.IsAI)
                    {
                        // Fix #7 Phase F — Comparer à l'amende réelle (Sgo) + marge de sécurité 10%.
                        // Évite le livelock : si l'IA a > 2000$ mais < Sgo (~$10-17k en late game),
                        // DeductMoney échouait à chaque frame sans avancer l'état.
                        double auditFine = financeSystem != null ? financeSystem.CurrentSgo : 500;
                        if (currentPlayer.CurrentBalance >= auditFine * 1.1)
                            AuditPayFine();
                        else
                            AuditRollDice();
                    }
                    else
                    {
                        // Human: Wait for UI call to AuditPayFine or AuditRollDice
                        yield return null;
                    }
                    break;

                case TurnState.RollDice:
                    if (actionButtonsWidget != null) actionButtonsWidget.ClearButtons();
                    Debug.Log($"Player {currentPlayer.PlayerId} is rolling dice...");
                    
                    // Wait for roll input if human, or auto-roll if AI
                    yield return new WaitForSeconds(1.0f); 

                    // Check if player is in Audit Fiscal (Prison) before rolling
                    // NOTE: This block is now handled in WaitingForAuditDecision state, 
                    // but we keep it safe or remove it?
                    // The prompt implies we change the flow.
                    // If we come here, we are NOT in Audit (or escaped).
                    
                    int[] result = diceSystem.RollDice();

                    // ── Automobile Ultime — Moteur Turbo : relancer + choisir meilleur ──
                    if (currentPlayer.AutoTurboReady)
                    {
                        currentPlayer.AutoTurboReady = false;
                        int[] reroll = diceSystem.RollDice();
                        int firstSum  = result[0] + result[1];
                        int secondSum = reroll[0] + reroll[1];
                        if (secondSum > firstSum)
                        {
                            Debug.Log($"[Auto Turbo] Player {currentPlayer.PlayerId} : reroll {secondSum} > {firstSum}, on garde reroll.");
                            result = reroll;
                        }
                        else
                        {
                            Debug.Log($"[Auto Turbo] Player {currentPlayer.PlayerId} : original {firstSum} ≥ reroll {secondSum}, on garde original.");
                        }
                    }

                    // Check for excessive doubles (only if not just escaped from audit, or standard rules apply)
                    // Standard rules: If you roll doubles to escape, you move but do NOT roll again. 
                    // However, our code checks for excessive doubles. Let's keep it simple.
                    if (diceSystem.HasExceededDoublesLimit())
                    {
                        Debug.LogWarning($"Player {currentPlayer.PlayerId} rolled too many doubles! Penalty applied.");
                        transactionManager.DeductMoney(currentPlayer, penaltyAmount);
                        diceSystem.ResetDoubles();
                        currentState = TurnState.EndTurn; // Force end turn
                    }
                    else
                    {
                        // Calculate new position
                        int currentPos = playerPositions[currentPlayer.PlayerId];
                        int steps = result[0] + result[1];
                        
                        // Strict sequential flow:
                        // 1. Trigger Movement Coroutine and WAIT for it
                        yield return StartCoroutine(ExecuteMovement(currentPlayer, steps));
                        
                        // 2. ExecuteMovement will transition state to ResolveTile when done
                        // So we don't need to do anything else here.
                    }
                    break;
                
                case TurnState.MovingPawn:
                    // Wait until OnPawnMovementFinished changes state to ResolveTile
                    // This case is actually redundant if we yield wait in RollDice/ExecuteMovement
                    // But we keep it as a safety buffer
                    yield return null;
                    break;

                case TurnState.ResolveTile:
                    Debug.Log($"Resolving tile for Player {currentPlayer.PlayerId}...");
                    
                    int tileIndex = playerPositions[currentPlayer.PlayerId];
                    BoardTile landedTile = BoardManager.Instance.GetTileAt(tileIndex);
                    
                    // Trigger selection for UI
                    GameEvents.OnTileSelected?.Invoke(landedTile);

                    // Auto-Pay Rent for Properties
                    if ((landedTile.Type == TileType.Property || landedTile.Type == TileType.Hub) && landedTile.Data is PropertyData propData)
                    {
                        PlayerModel owner = GetOwnerOfProperty(propData);
                        if (owner != null && owner.PlayerId != currentPlayer.PlayerId)
                        {
                             // It's owned by someone else -> Auto Pay Rent
                             if (rulesEngine != null)
                             {
                                 rulesEngine.ExecutePrimaryAction(currentPlayer, landedTile);
                                 // Skip UI and complete turn
                                 // ExecutePrimaryAction calls CompleteTileResolution internally for rent payment
                                 // so we can just break/return or wait for state change
                             }
                             else
                             {
                                 Debug.LogError("TurnManager: RulesEngine reference missing for Rent Auto-Pay.");
                             }
                        }
                    }

                    // Auto-Pay Tax
                    if (landedTile.Type == TileType.Tax)
                    {
                        if (rulesEngine != null)
                        {
                            rulesEngine.ExecutePrimaryAction(currentPlayer, landedTile);
                            // ExecutePrimaryAction calls CompleteTileResolution, which changes state to EndTurn
                            // So the WaitUntil below will exit immediately
                        }
                        else
                        {
                            Debug.LogError("TurnManager: RulesEngine reference missing for Tax Auto-Pay.");
                        }
                    }
                    else if (landedTile.TileIndex == 10 && !currentPlayer.IsInAudit)
                    {
                        // Just Visiting Audit (Prison)
                        Debug.Log("Just visiting Audit. Auto-skipping.");
                        CompleteTileResolution();
                    }
                    else if (landedTile.TileIndex == 0)
                    {
                        Debug.Log("Landed on GO. Auto-skipping.");
                        CompleteTileResolution();
                    }
                    
                    if (currentPlayer.IsAI)
                    {
                        // Delegate to AIManager
                        AIManager aiManager = FindObjectOfType<AIManager>();
                        if (aiManager != null)
                        {
                            aiManager.RequestAIDecision(currentPlayer, landedTile);
                            // The AI Manager will call CompleteTileResolution when done
                        }
                        else
                        {
                            Debug.LogWarning("TurnManager: AIManager not found! Skipping AI turn.");
                            currentState = TurnState.EndTurn;
                        }
                        
                        // Wait indefinitely until state is changed externally
                        yield return new WaitUntil(() => currentState != TurnState.ResolveTile);
                    }
                    else
                    {
                        // Update UI with current tile for Human player
                        GameEvents.OnTileSelected?.Invoke(landedTile);
                        
                        // Wait for UI input from human player
                        // This is a placeholder for waiting for a "End Turn" button press or purchase decision
                        Debug.Log("Waiting for Human Input...");
                        
                        // Wait indefinitely until UI calls CompleteTileResolution
                        yield return new WaitUntil(() => currentState != TurnState.ResolveTile);
                    }
                    break; // Break the switch to re-evaluate loop state

                case TurnState.EndTurn:
                    Debug.Log($"End of Turn for Player {currentPlayer.PlayerId}.");
                    diceSystem.ResetDoubles();
                    
                    // Financial Report
                    string report = "[FINANCE REPORT] ";
                    foreach (var p in players)
                    {
                        report += $"Player {p.PlayerId}: {CurrencyFormatter.Format(p.CurrentBalance)} | ";
                    }
                    Debug.Log(report);
                    
                    // Move to next player
                    // Simple loop to find next non-bankrupt player
                    int previousPlayerIndex = currentPlayerIndex;
                    do
                    {
                        currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
                    } while (players[currentPlayerIndex].IsBankrupt);

                    // Phase C : Tick du cycle macro à chaque round-robin complet (retour au plus petit index)
                    if (currentPlayerIndex <= previousPlayerIndex && CycleManager.Instance != null)
                        CycleManager.Instance.TickOnTurnEnd();
                    
                    // Check win condition
                    int nonBankruptCount = players.Count(p => !p.IsBankrupt);
                    if (nonBankruptCount == 1)
                    {
                        PlayerModel winner = players.FirstOrDefault(p => !p.IsBankrupt);
                        if (winner != null)
                        {
                            Debug.Log($"[GAME OVER] We have a winner! Player {winner.PlayerId}");
                            GameEvents.OnGameVictory?.Invoke(winner, "LAST PLAYER STANDING");
                            isGameOver = true;
                            yield break;
                        }
                    }

                    // Phase E.3 — Time-out tournoi à 50 tours globaux (GDD §6.3)
                    int globalTurn = players.Min(p => p.LapsCompleted);
                    if (globalTurn >= 50)
                    {
                        PlayerModel winnerByNV = players
                            .Where(p => !p.IsBankrupt)
                            .OrderByDescending(p => p.CurrentBalance + p.OwnedProperties.Sum(prop => prop.BasePrice))
                            .FirstOrDefault();
                        if (winnerByNV != null)
                        {
                            Debug.Log($"[GAME OVER] Turn 50 reached — winner by Net Worth: Player {winnerByNV.PlayerId}");
                            GameEvents.OnGameVictory?.Invoke(winnerByNV, "TIME OUT — HIGHEST NET WORTH");
                            isGameOver = true;
                            yield break;
                        }
                    }

                    if (!isGameOver)
                    {
                        currentState = TurnState.TurnStart;
                    }
                    yield return null;
                    break;
            }
        }

        /// <summary>
        /// Placeholder for AI logic.
        /// </summary>
        private void ProcessAIDecision(PlayerModel aiPlayer)
        {
            Debug.Log($"AI Player {aiPlayer.PlayerId} is thinking...");
            // Logic to buy property, upgrade, etc.
        }
    }
}
