using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using PolyRisk.Core;
using PolyRisk.UI;

namespace PolyRisk.Systems
{
    [System.Serializable]
    public class MovementResumeState
    {
        public int startPosition;
        public int targetPosition;
        public bool goEconomicsApplied;
    }

    /// <summary>
    /// Manages the game loop, player turns, and state transitions.
    /// Implements a State Machine for the turn phases.
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        public static TurnManager Instance { get; private set; }
        private MovementResumeState pendingMovement;
        public bool HasPendingMovement => pendingMovement != null;

        public MovementResumeState CaptureMovementState() => pendingMovement == null ? null :
            new MovementResumeState { startPosition = pendingMovement.startPosition,
                targetPosition = pendingMovement.targetPosition,
                goEconomicsApplied = pendingMovement.goEconomicsApplied };

        public bool ApplyReplicatedMovement(MovementResumeState state)
        {
            if (state != null && (state.startPosition < 0 || state.startPosition >= 40
                || state.targetPosition < 0 || state.targetPosition >= 40)) return false;
            pendingMovement = state == null ? null : new MovementResumeState
            {
                startPosition = state.startPosition, targetPosition = state.targetPosition,
                goEconomicsApplied = state.goEconomicsApplied
            };
            return true;
        }

        public enum TurnState
        {
            TurnStart,
            WaitingForAuditDecision,
            Planning,        // Phase D — Pre-roll : activer Ultime + Auto Precision
            RollDice,
            AdjustDice,      // Automobile : choix -1 / conserver / +1 après le jet réel
            MovingPawn,
            WaitingForTransitChoice,
            ResolveTile,
            EndTurn,
            ChooseTurbo,
            ResolveDice
        }

        [Header("Game Settings")]
        [Tooltip("List of players in the game.")]
        [SerializeField] private List<PlayerModel> players;
        public List<PlayerModel> Players => players;

        // Track current tile index for each player
        private Dictionary<int, int> playerPositions = new Dictionary<int, int>();

        // Public accessor for current state
        public TurnState CurrentState => currentState;
        public bool IsGameOver => isGameOver;
        public int WinnerPlayerId { get; private set; } = -1;
        public string VictoryReason { get; private set; } = "";
        public int TurnsStarted { get; private set; }
        public int PendingDiceTotal { get; private set; }
        private int[] turboFirst;
        private int[] turboSecond;
        public int[] TurboFirst => turboFirst == null ? null : (int[])turboFirst.Clone();
        public int[] TurboSecond => turboSecond == null ? null : (int[])turboSecond.Clone();

        public static bool IsValidTurboCandidates(int[] first, int[] second, bool awaitingChoice)
        {
            bool absent(int[] roll) => roll == null || roll.Length == 0;
            bool valid(int[] roll) => roll != null && roll.Length == 2
                && roll[0] >= 1 && roll[0] <= 6 && roll[1] >= 1 && roll[1] <= 6;
            if (absent(first) && absent(second)) return !awaitingChoice;
            return valid(first) && valid(second);
        }

        public bool ApplyTurboCandidates(int[] first, int[] second)
        {
            if (!IsValidTurboCandidates(first, second, currentState == TurnState.ChooseTurbo)) return false;
            turboFirst = first == null || first.Length == 0 ? null : (int[])first.Clone();
            turboSecond = second == null || second.Length == 0 ? null : (int[])second.Clone();
            return true;
        }

        public bool ConfirmTurboChoice(int choice)
        {
            if (currentState != TurnState.ChooseTurbo || CurrentPlayer == null
                || choice < 0 || choice > 1 || turboFirst == null || turboSecond == null) return false;
            int[] selected = choice == 0 ? turboFirst : turboSecond;
            CommitSelectedRoll(selected);
            turboFirst = turboSecond = null;
            return true;
        }

        private void CommitSelectedRoll(int[] result)
        {
            diceSystem.CommitRoll(result);
            if (diceSystem.HasExceededDoublesLimit())
            {
                transactionManager.DeductMoney(CurrentPlayer, penaltyAmount);
                diceSystem.ResetDoubles();
                PendingDiceTotal = 0;
                currentState = TurnState.EndTurn;
            }
            else
            {
                PendingDiceTotal = result[0] + result[1];
                currentState = TurnState.ResolveDice;
            }
        }

        private IEnumerator WaitForTurboChoice()
        {
            float started = Time.realtimeSinceStartup;
            while (currentState == TurnState.ChooseTurbo && Time.realtimeSinceStartup - started < 60f)
                yield return null;
            if (currentState == TurnState.ChooseTurbo) ConfirmTurboChoice(0);
        }

        public double GetNetWorth(PlayerModel player) => NetWorth.Calculate(player,
            BoardManager.Instance != null ? BoardManager.Instance.tileDataList?.OfType<PropertyData>() : null);

        public void DeclareVictory(PlayerModel winner, string reason)
        {
            if (isGameOver) return;
            isGameOver = true;
            WinnerPlayerId = winner != null ? winner.PlayerId : -1;
            VictoryReason = reason ?? "";
            pendingMovement = null;
            PendingDiceTotal = 0;
            currentState = TurnState.EndTurn;
            actionButtonsWidget?.ClearButtons();
            GameEvents.OnGameVictory?.Invoke(winner, reason);
        }

        public bool ApplyReplicatedOutcome(bool ended, int winnerPlayerId, string reason)
        {
            var winner = winnerPlayerId >= 0 ? GetPlayerById(winnerPlayerId) : null;
            if (ended && winnerPlayerId >= 0 && winner == null) return false;
            bool changed = !isGameOver || WinnerPlayerId != winnerPlayerId || VictoryReason != (reason ?? "");
            isGameOver = ended;
            WinnerPlayerId = ended ? winnerPlayerId : -1;
            VictoryReason = ended ? reason ?? "" : "";
            if (ended)
            {
                StopAllCoroutines();
                currentState = TurnState.EndTurn;
                pendingMovement = null;
                PendingDiceTotal = 0;
                actionButtonsWidget?.ClearButtons();
                if (changed) GameEvents.OnGameVictory?.Invoke(winner, VictoryReason);
            }
            return true;
        }

        public bool CheckEndConditions()
        {
            if (isGameOver) return true;
            var survivors = players.Where(p => !p.IsBankrupt).ToList();
            if (survivors.Count == 0)
                DeclareVictory(null, "NO SURVIVORS");
            else if (survivors.Count == 1)
                DeclareVictory(survivors[0], "LAST PLAYER STANDING");
            else if (survivors.Min(p => p.LapsCompleted) >= 50)
                DeclareVictory(survivors.OrderByDescending(GetNetWorth).ThenBy(p => p.PlayerId).First(),
                    "TIME OUT — HIGHEST NET WORTH");
            return isGameOver;
        }

        public void AuditRollDice()
        {
            PlayerModel currentPlayer = players[currentPlayerIndex];
            if (!currentPlayer.IsInAudit) return;

            GameEvents.PublishActivity("audit", currentPlayer.PlayerId, "tente de sortir de l’Audit fiscal");

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

            pendingMovement = new MovementResumeState
            {
                startPosition = currentPos, targetPosition = (currentPos + steps) % 40
            };
            // Get path
            List<Vector3> path = BoardManager.Instance.GetPath(currentPos, steps);

            currentState = TurnState.MovingPawn;
            if (actionButtonsWidget != null) actionButtonsWidget.ClearButtons();

            // Bug trouvé via stress test (85% ANOMALY_TIMEOUT sur un run de 20 parties) :
            // si le chemin est vide (visualTiles[i] == null sur le trajet, cf. BoardManager.GetPath),
            // PawnMovement.MoveAlongPath termine de façon SYNCHRONE et invoque OnPawnMovementFinished
            // immédiatement. Il faut donc être abonné AVANT d'invoquer OnPlayerPathCalculated, sinon
            // l'event se déclenche dans le vide et le WaitUntil ci-dessous bloque indéfiniment
            // (aucun garde-fou avant ce fix, contrairement à la phase Planning humaine).
            bool movementFinished = false;
            System.Action onFinished = () => movementFinished = true;
            GameEvents.OnPawnMovementFinished += onFinished;

            GameEvents.OnPlayerPathCalculated?.Invoke(player.PlayerId, path);

            // Calculate new position logically BUT wait to apply effects
            int newPos = (currentPos + steps) % 40;

            // Check for GO passing during movement (simplified: we trigger it after movement for now to ensure sync)
            // Prompt Option 2: "trigger... immediately after the movement coroutine finishes"

            // Wait until visual movement is done — garde-fou 10s réelles en filet de sécurité
            // (même logique que le safety net de la phase Planning humaine) au cas où un autre
            // chemin d'échec non identifié empêcherait encore l'event de se déclencher.
            float moveWaitStart = Time.realtimeSinceStartup;
            const float MAX_MOVE_WAIT = 10f;
            while (!movementFinished && Time.realtimeSinceStartup - moveWaitStart < MAX_MOVE_WAIT)
                yield return null;
            if (!movementFinished)
                Debug.LogWarning($"[ExecuteMovement] Safety net : {MAX_MOVE_WAIT}s écoulées sans OnPawnMovementFinished pour Player {player.PlayerId} — on force la suite.");

            GameEvents.OnPawnMovementFinished -= onFinished;

            // Update logical position after movement
            playerPositions[player.PlayerId] = newPos;

            ApplyPendingMovementEconomics(player);
            while (financeSystem != null && financeSystem.IsSettlingGo && !isGameOver) yield return null;
            FinishMovementAfterSettlement(player, newPos < currentPos);
        }

        public bool ApplyPendingMovementEconomics(PlayerModel player)
        {
            if (pendingMovement == null || player == null || player != CurrentPlayer) return false;
            playerPositions[player.PlayerId] = pendingMovement.targetPosition;
            if (pendingMovement.goEconomicsApplied) return true;
            pendingMovement.goEconomicsApplied = true;
            if (pendingMovement.targetPosition >= pendingMovement.startPosition) return true;
            player.LapsCompleted++;
            if (financeSystem != null)
            {
                int activePlayers = players.Count(p => !p.IsBankrupt);
                if (activePlayers > 0)
                {
                    var cycle = CycleManager.Instance;
                    double cycleMultiplier = cycle != null && cycle.IsActive ? cycle.GetSgoMultiplier() : 1.0;
                    int growth = FinanceSystem.CalculateSgoGrowth(activePlayers, cycleMultiplier,
                        financeSystem.IsAggressiveGrowthMode);
                    financeSystem.CurrentSgo += growth;
                }
                financeSystem.ProcessGoIncome(player);
            }
            return true;
        }

        private IEnumerator ResumePendingMovement(PlayerModel player)
        {
            if (pendingMovement == null)
            {
                Debug.LogError("[Online] Mouvement interrompu sans destination sauvegardée.");
                yield return new WaitForSecondsRealtime(1f);
                yield break;
            }
            bool passedGo = pendingMovement.targetPosition < pendingMovement.startPosition;
            TeleportPlayer(player, pendingMovement.targetPosition);
            ApplyPendingMovementEconomics(player);
            while (financeSystem != null && financeSystem.IsSettlingGo && !isGameOver) yield return null;
            FinishMovementAfterSettlement(player, passedGo);
        }

        /// <summary>Resumes a restored GO debt after its payment, without repeating GO economics.</summary>
        public bool ResumeGoDebtSettlement(PlayerModel player, bool paid)
        {
            if (player == null || player != CurrentPlayer || currentState != TurnState.MovingPawn)
                return false;
            financeSystem?.CompleteGoSettlement(player, paid);
            FinishMovementAfterSettlement(player, true);
            return true;
        }

        private void FinishMovementAfterSettlement(PlayerModel player, bool passedGo)
        {
            PendingDiceTotal = 0;
            pendingMovement = null;
            if (isGameOver) return;
            if (player.IsBankrupt) { currentState = TurnState.EndTurn; return; }
            int newPos = playerPositions[player.PlayerId];
            if (passedGo)
            {
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

            // The GDD makes Transit Express optional. Preserve the Hub landing
            // and let the active player decide before the tile is resolved.
            if (TryGetTransitExpressDestination(player, newPos, out _))
            {
                currentState = TurnState.WaitingForTransitChoice;
                var landedHub = BoardManager.Instance != null ? BoardManager.Instance.GetTileAt(newPos) : null;
                if (landedHub != null) GameEvents.OnTileSelected?.Invoke(landedHub);
                GameEvents.PublishActivity("transit", player.PlayerId, "peut activer Transit Express");
                if (player.IsAI) ResolveTransitExpressChoice(true);
                return;
            }

            Debug.Log($"Player {player.PlayerId} arrived at tile {newPos}.");
            var landedTile = BoardManager.Instance != null ? BoardManager.Instance.GetTileAt(newPos) : null;
            string landedName = landedTile?.Data != null && !string.IsNullOrWhiteSpace(landedTile.Data.TileName)
                ? landedTile.Data.TileName
                : landedTile != null ? landedTile.Type.ToString() : $"case {newPos}";
            GameEvents.PublishActivity("move", player.PlayerId, $"arrive sur {landedName}");

            // Transition to ResolveTile
            currentState = TurnState.ResolveTile;
        }

        /// <summary>
        /// Returns the next clockwise Hub when a player owns all four Logistics Hubs
        /// and has just landed on one. The actual movement remains an optional choice.
        /// </summary>
        public static bool TryGetTransitExpressDestination(PlayerModel player, int currentPos, out int nextHub)
        {
            nextHub = -1;
            if (player == null) return false;
            int[] hubIndices = { 5, 15, 25, 35 };
            if (System.Array.IndexOf(hubIndices, currentPos) < 0) return false;

            // Vérifier que le joueur possède les 4 Hubs
            int hubCount = player.OwnedProperties.Count(p =>
                p.FamilyData != null && p.FamilyData.FamilyName.Contains("Logistics"));
            if (hubCount < 4) return false;

            // Hub suivant dans le sens horaire
            nextHub = currentPos switch
            {
                5  => 15,
                15 => 25,
                25 => 35,
                35 => 5,   // boucle — traverse Case 0
                _  => currentPos
            };

            return nextHub >= 0;
        }

        public bool ResolveTransitExpressChoice(bool takeTransit)
        {
            if (currentState != TurnState.WaitingForTransitChoice || CurrentPlayer == null) return false;
            int currentPosition = GetPlayerPosition(CurrentPlayer);
            if (!TryGetTransitExpressDestination(CurrentPlayer, currentPosition, out int nextHub)) return false;

            if (takeTransit)
            {
                playerPositions[CurrentPlayer.PlayerId] = nextHub;
                TeleportPlayer(CurrentPlayer, nextHub);
                GameEvents.PublishActivity("transit", CurrentPlayer.PlayerId,
                    $"utilise Transit Express · case {currentPosition} → {nextHub}", true);
            }
            else
            {
                GameEvents.PublishActivity("transit", CurrentPlayer.PlayerId, "reste sur son Hub");
            }

            currentState = TurnState.ResolveTile;
            var destination = BoardManager.Instance != null
                ? BoardManager.Instance.GetTileAt(GetPlayerPosition(CurrentPlayer))
                : null;
            if (destination != null) GameEvents.OnTileSelected?.Invoke(destination);
            return true;
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

        public int GetPlayerPosition(PlayerModel player)
            => player != null && playerPositions.TryGetValue(player.PlayerId, out int position) ? position : -1;

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
        public int ConsecutiveDoubles => diceSystem != null ? diceSystem.ConsecutiveDoubles : 0;
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
            var currentPlayer = players != null && currentPlayerIndex >= 0 && currentPlayerIndex < players.Count
                ? players[currentPlayerIndex]
                : null;
            if (currentPlayer != null)
                GameEvents.PublishActivity("audit", currentPlayer.PlayerId, "reste en Audit fiscal");
            if (justLandedInAudit)
            {
                Debug.Log("Player chose to stay in Audit (Just Landed). Ending turn.");
                // EnterAuditDecisionMode moved the state from ResolveTile to
                // WaitingForAuditDecision. CompleteTileResolution is guarded against
                // that state and used to no-op here, leaving the turn blocked forever.
                currentState = TurnState.EndTurn;
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
                GameEvents.PublishActivity("audit", currentPlayer.PlayerId, "utilise sa carte de sortie d’Audit", true);

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
            var abilities = AbilitySystem.Instance;
            if (abilities == null || !abilities.HasFamilyMonopoly(player, "Automobile"))
            {
                Debug.LogWarning($"[Auto Precision] Player {player.PlayerId} n'a pas le monopole Automobile.");
                return false;
            }
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

        /// <summary>Valide le choix Automobile humain et libère la coroutine de lancer.</summary>
        public bool ConfirmAutoPrecision(int delta)
        {
            if (currentState != TurnState.AdjustDice || CurrentPlayer == null || CurrentPlayer.IsAI) return false;
            if (delta < -1 || delta > 1) return false;

            if (delta == 0)
            {
                CurrentPlayer.AutoPrecisionAvailable = false;
                CurrentPlayer.PendingDiceDelta = 0;
            }
            else if (!UseAutoPrecision(CurrentPlayer, delta))
            {
                return false;
            }

            currentState = TurnState.RollDice;
            return true;
        }

        private IEnumerator ResolveSelectedDice(PlayerModel currentPlayer)
        {
            int steps = PendingDiceTotal;
            // Automobile : le choix se fait sur le résultat réel, juste avant le mouvement.
            var rollAbilities = AbilitySystem.Instance;
            bool canAdjust = currentPlayer.AutoPrecisionAvailable
                && rollAbilities != null
                && rollAbilities.HasFamilyMonopoly(currentPlayer, "Automobile");
            if (canAdjust)
            {
                PendingDiceTotal = steps;
                if (currentPlayer.IsAI)
                {
                    var aiManager = FindObjectOfType<AIManager>();
                    int delta = aiManager != null ? aiManager.ChooseAutoPrecisionDelta(currentPlayer, steps) : 0;
                    if (delta != 0) UseAutoPrecision(currentPlayer, delta);
                    else currentPlayer.AutoPrecisionAvailable = false;
                }
                else
                {
                    currentState = TurnState.AdjustDice;
                    yield return StartCoroutine(WaitForAutoPrecisionChoice(currentPlayer));
                }
            }

            // Strict sequential flow:
            // 1. Trigger Movement Coroutine and WAIT for it
            yield return StartCoroutine(ExecuteMovement(currentPlayer, steps));
            PendingDiceTotal = 0;

        }

        private IEnumerator WaitForAutoPrecisionChoice(PlayerModel player)
        {
            float waitStart = Time.realtimeSinceStartup;
            while (currentState == TurnState.AdjustDice && Time.realtimeSinceStartup - waitStart < 60f)
                yield return null;
            if (currentState == TurnState.AdjustDice)
            {
                Debug.LogWarning("[Auto Precision] Délai expiré : conservation automatique du jet.");
                player.AutoPrecisionAvailable = false;
                player.PendingDiceDelta = 0;
                currentState = TurnState.RollDice;
            }
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
                GameEvents.PublishActivity("audit", currentPlayer.PlayerId,
                    $"paie {fineAmount:N0} $ et quitte l’Audit fiscal", true);
                currentPlayer.IsInAudit = false;
                currentPlayer.AuditTurnsRemaining = 0;

                if (justLandedInAudit)
                {
                    Debug.Log("Paid fine immediately after landing. Turn Ends.");
                    // CompleteTileResolution() exige currentState == ResolveTile, mais on est
                    // ici en WaitingForAuditDecision — l'appel était un no-op silencieux qui
                    // bloquait la partie indéfiniment (jamais de transition vers EndTurn).
                    // Même correctif que la branche "IA ne peut pas payer" juste en dessous.
                    currentState = TurnState.EndTurn;
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

        /// <summary>
        /// Starts the game with the provided players.
        /// </summary>
        public void StartGame(List<PlayerModel> initialPlayers, bool resumeLoop = true)
        {
            if (initialPlayers == null || initialPlayers.Count < 2 || initialPlayers.Count > 6)
                throw new System.ArgumentException("A game requires 2–6 participants (humans or AI).");
            players = initialPlayers;
            GameEvents.BeginActivitySession();
            isGameOver = false;
            WinnerPlayerId = -1;
            VictoryReason = "";
            TurnsStarted = 0;
            currentPlayerIndex = 0;
            currentState = TurnState.TurnStart;
            turboFirst = turboSecond = null;
            playerPositions.Clear();
            pendingMovement = null;
            PendingDiceTotal = 0;
            financeSystem?.StartMatchClock();

            // Initialize positions
            foreach (var p in players)
            {
                playerPositions[p.PlayerId] = 0;
            }

            // Start the loop
            if (resumeLoop && !GameBootstrapper.DeferGameLoop) ResumeGameLoop();
        }

        public bool ApplyReplicaTurnState(
            int currentPlayerId,
            string stateName,
            int turnsStarted,
            int pendingDice,
            bool landedInAudit,
            int consecutiveDoubles = 0)
        {
            int index = players?.FindIndex(player => player.PlayerId == currentPlayerId) ?? -1;
            if (index < 0 || !System.Enum.TryParse(stateName, true, out TurnState parsedState)) return false;
            StopAllCoroutines();
            currentPlayerIndex = index;
            currentState = parsedState;
            TurnsStarted = Mathf.Max(0, turnsStarted);
            PendingDiceTotal = Mathf.Max(0, pendingDice);
            justLandedInAudit = landedInAudit;
            if (diceSystem == null) diceSystem = GetComponent<DiceSystem>();
            if (diceSystem == null) diceSystem = gameObject.AddComponent<DiceSystem>();
            diceSystem.ApplyReplicatedDoubles(consecutiveDoubles);
            return true;
        }

        public void ResumeGameLoop()
        {
            if (!isGameOver) StartCoroutine(GameLoop());
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEBUG_BUILD
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
#endif
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
            if (isGameOver || currentState != TurnState.ResolveTile) return;
            if (CurrentPlayer.IsBankrupt) { currentState = TurnState.EndTurn; return; }

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
            var online = PolyRisk.Multiplayer.OnlineGameController.Instance;
            // A restored liquidation already has an invoice and a continuation.
            // Keep processing network commands, but do not execute the tile again.
            if (online != null && online.IsOnlineGame && online.IsAuthority && online.HasPendingDebt)
            {
                yield return null;
                yield break;
            }
            PlayerModel currentPlayer = players[currentPlayerIndex];

            switch (currentState)
            {
                case TurnState.TurnStart:
                    if (CheckEndConditions()) yield break;
                    var abilities = AbilitySystem.Instance;
                    currentPlayer.AutoPrecisionAvailable = abilities != null
                        && abilities.HasFamilyMonopoly(currentPlayer, "Automobile");
                    currentPlayer.PendingDiceDelta = 0;
                    TurnsStarted++;
                    currentPlayer.TurnsStarted++;
                    Debug.Log($"Turn Start: Player {currentPlayer.PlayerId}");
                    GameEvents.OnTurnStarted?.Invoke(currentPlayer.PlayerId);
                    GameEvents.PublishActivity("turn", currentPlayer.PlayerId, "commence son tour");

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

                case TurnState.WaitingForTransitChoice:
                    if (currentPlayer.IsAI)
                    {
                        // Preserve the pre-existing AI policy; human players receive
                        // the optional choice specified by the board rules.
                        ResolveTransitExpressChoice(true);
                    }
                    else
                    {
                        float transitWaitStart = Time.realtimeSinceStartup;
                        while (currentState == TurnState.WaitingForTransitChoice
                               && Time.realtimeSinceStartup - transitWaitStart < 60f)
                            yield return null;
                        if (currentState == TurnState.WaitingForTransitChoice)
                        {
                            Debug.LogWarning("[Transit Express] Délai expiré : le joueur reste sur son Hub.");
                            ResolveTransitExpressChoice(false);
                        }
                    }
                    break;

                case TurnState.ChooseTurbo:
                    yield return StartCoroutine(WaitForTurboChoice());
                    if (currentState == TurnState.ResolveDice)
                        yield return StartCoroutine(ResolveSelectedDice(currentPlayer));
                    break;

                case TurnState.ResolveDice:
                    yield return StartCoroutine(ResolveSelectedDice(currentPlayer));
                    break;

                case TurnState.AdjustDice:
                    // A restored choice has no original RollDice coroutine to resume.
                    yield return StartCoroutine(WaitForAutoPrecisionChoice(currentPlayer));
                    yield return StartCoroutine(ExecuteMovement(currentPlayer, PendingDiceTotal));
                    PendingDiceTotal = 0;
                    break;

                case TurnState.RollDice:
                    // The choice may have been acknowledged just before disconnecting.
                    // A saved total identifies an already completed roll.
                    if (PendingDiceTotal > 0)
                    {
                        yield return StartCoroutine(ExecuteMovement(currentPlayer, PendingDiceTotal));
                        PendingDiceTotal = 0;
                        break;
                    }
                    if (actionButtonsWidget != null) actionButtonsWidget.ClearButtons();
                    Debug.Log($"Player {currentPlayer.PlayerId} is rolling dice...");

                    // Wait for roll input if human, or auto-roll if AI
                    yield return new WaitForSeconds(1.0f);

                    // Check if player is in Audit Fiscal (Prison) before rolling
                    // NOTE: This block is now handled in WaitingForAuditDecision state,
                    // but we keep it safe or remove it?
                    // The prompt implies we change the flow.
                    // If we come here, we are NOT in Audit (or escaped).

                    int[] result = diceSystem.GenerateRoll();

                    if (currentPlayer.AutoTurboReady)
                    {
                        currentPlayer.AutoTurboReady = false;
                        int[] reroll = diceSystem.GenerateRoll();
                        if (!currentPlayer.IsAI)
                        {
                            ApplyTurboCandidates(result, reroll);
                            currentState = TurnState.ChooseTurbo;
                            yield return StartCoroutine(WaitForTurboChoice());
                        }
                        else
                            CommitSelectedRoll(reroll[0] + reroll[1] > result[0] + result[1] ? reroll : result);
                    }
                    else CommitSelectedRoll(result);
                    if (currentState == TurnState.ResolveDice)
                        yield return StartCoroutine(ResolveSelectedDice(currentPlayer));
                    break;

                case TurnState.MovingPawn:
                    yield return StartCoroutine(ResumePendingMovement(currentPlayer));
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
                        // Fix double-dispatch (trouvé via [BANKRUPTCY-TRACE] : 45% des événements
                        // de faillite tracés étaient des doublons exacts, cash_before=0). Les blocs
                        // Auto-Pay Rent / Auto-Pay Tax / Audit / GO ci-dessus peuvent avoir déjà
                        // entièrement résolu la tuile et fait avancer currentState (via
                        // CompleteTileResolution, appelé en interne par RulesEngine). Ne déléguer à
                        // l'IA QUE si la tuile a réellement encore besoin d'une décision (case libre
                        // à acheter, Marketplace, Venture, Casino, Stock Market...) — sinon
                        // ExecutePrimaryAction était appelé une 2e fois pour la même tuile (double
                        // loyer/taxe potentiel, double comptage de WasVisitedSinceLastGO, etc.).
                        if (currentState == TurnState.ResolveTile)
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
                        }

                        // Wait indefinitely until state is changed externally (no-op si déjà résolu)
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

                    // Count survivors BEFORE searching for one: zero survivors must never spin.
                    if (CheckEndConditions()) yield break;
                    int previousPlayerIndex = currentPlayerIndex;
                    do { currentPlayerIndex = (currentPlayerIndex + 1) % players.Count; }
                    while (players[currentPlayerIndex].IsBankrupt);
                    if (currentPlayerIndex <= previousPlayerIndex && CycleManager.Instance != null)
                        CycleManager.Instance.TickOnTurnEnd();

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
