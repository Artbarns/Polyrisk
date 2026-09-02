using System.Collections.Generic;
using UnityEngine;
using PolyRisk.Core;
using PolyRisk.Visuals;

namespace PolyRisk.Systems
{
    /// <summary>
    /// The central bootstrapper for the game.
    /// Orchestrates the initialization sequence of all major systems.
    /// Ensures dependencies are ready before the game loop starts.
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
        [Header("System References")]
        [Tooltip("Manages board data and tile logic.")]
        [SerializeField] public BoardManager boardManager;
        
        [Tooltip("Manages turn loop and game state.")]
        [SerializeField] public TurnManager turnManager;
        
        [Tooltip("Manages visual representation of players.")]
        [SerializeField] public PlayerVisualManager visualManager;
        
        [Tooltip("Core Rules Engine.")]
        [SerializeField] public RulesEngine rulesEngine;
        
        [Tooltip("Core Transaction Manager.")]
        [SerializeField] public TransactionManager transactionManager;

        [Header("Game Configuration")]
        [Tooltip("Capital de départ GDD §1 — NE PAS MODIFIER ($18,000 verrouillé).")]
        [SerializeField] private double startingBalance = 18000;

        // OnValidate : force la valeur correcte même si le fichier de scène contient une ancienne valeur sérialisée.
        private void OnValidate()
        {
            if (startingBalance != 18000)
            {
                startingBalance = 18000;
                Debug.LogWarning("[GameBootstrapper] startingBalance réinitialisé à $18,000 (GDD §1 — valeur sérialisée écrasée).");
            }
        }
        
        [Tooltip("Number of players to initialize.")]
        [SerializeField] private int numberOfPlayers = 4;

        private void Start()
        {
            StartCoroutine(InitializeGameSequence());
        }

        /// <summary>
        /// Sequences the game initialization steps.
        /// </summary>
        private System.Collections.IEnumerator InitializeGameSequence()
        {
            Debug.Log("Bootstrapper: Starting initialization...");

            // 1. Validate References
            if (boardManager == null || turnManager == null || visualManager == null || rulesEngine == null || transactionManager == null)
            {
                Debug.LogError("Bootstrapper: Missing system references! Please assign them in the Inspector.");
                yield break;
            }

            // Fix #1 Phase F — Validation des singletons Phase B/C requis en runtime
            if (AbilitySystem.Instance == null)
                Debug.LogError("[Bootstrapper] AbilitySystem.Instance == null. Ajouter le composant AbilitySystem à la scène (sur GameController) sinon les 8 Ultimes et l'énergie GO seront désactivés.");
            if (CycleManager.Instance == null)
                Debug.LogError("[Bootstrapper] CycleManager.Instance == null. Ajouter le composant CycleManager à la scène sinon BOOM/CRASH/WAR seront désactivés.");

            // 1.5. Ensure RulesEngine dependencies are injected manually if not set in inspector
            // Note: Since we are using strict injection, these should be set in inspector too.
            // But as a failsafe if RulesEngine slots are empty but Bootstrapper has them:
            if (rulesEngine.turnManager == null) rulesEngine.turnManager = turnManager;
            if (rulesEngine.transactionManager == null) rulesEngine.transactionManager = transactionManager;
            if (rulesEngine.upgradeSystem == null) rulesEngine.upgradeSystem = FindObjectOfType<UpgradeSystem>();

            // 3. Initialize Board Data
            Debug.Log("Bootstrapper: Initializing Board Data...");
            boardManager.InitializeStandardBoard();
            
            // Wait a frame to ensure any internal board setup (like visual caching) is done
            yield return null;

            // 3. Create Players
            Debug.Log("Bootstrapper: Creating Players...");
            List<PlayerModel> playersList = new List<PlayerModel>();
            
            // Phase D — Profils psychologiques assignés round-robin pour les IA :
            // P1 Predator, P2 Rentier, P3 Negotiator, P4 Predator, P5 Rentier...
            PsychoProfile[] profileRoster = {
                PsychoProfile.Predator, PsychoProfile.Rentier, PsychoProfile.Negotiator
            };

            for (int i = 0; i < numberOfPlayers; i++)
            {
                bool isAI = (i != 0);
                PlayerModel newPlayer = new PlayerModel(i, startingBalance, isAI);

                if (isAI)
                {
                    newPlayer.Profile = profileRoster[(i - 1) % profileRoster.Length];
                    Debug.Log($"[Bootstrap] Player {i} assigned profile: {newPlayer.Profile}");
                }

                playersList.Add(newPlayer);
            }

            // 4. Get Start Position (Tile 0)
            Vector3 startPos = Vector3.zero;
            // We need to get the position from the visual board. 
            // BoardManager has cached visuals in Start(), but we just called InitializeStandardBoard.
            // BoardManager.GetPath relies on visuals being ready.
            // Let's try to get Tile 0 path of length 0 or access via BoardManager if possible.
            // Actually, BoardManager.GetPath uses visuals.
            
            // NOTE: BoardManager's Start() runs before this Start() usually, initializing the cache.
            // We assume BoardGenerator has already run in Editor or via a separate process.
            
            List<Vector3> startPath = boardManager.GetPath(39, 1); // Get position of tile 0 (39+1 wrapped)
            if (startPath != null && startPath.Count > 0)
            {
                startPos = startPath[0];
            }
            else
            {
                Debug.LogWarning("Bootstrapper: Could not find visual tile 0. Using Vector3.zero.");
            }

            // 5. Spawn Player Visuals
            Debug.Log("Bootstrapper: Spawning Visuals...");
            visualManager.SpawnPlayers(playersList, startPos);

            // 6. Start the Game Loop
            Debug.Log("Bootstrapper: Starting Game Loop...");
            turnManager.StartGame(playersList);
            
            Debug.Log("Bootstrapper: Initialization Complete.");
        }
    }
}
