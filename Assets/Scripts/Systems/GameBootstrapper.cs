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

        /// <summary>
        /// Debug/stress-test uniquement — force le joueur 0 à être IA aussi (par défaut il
        /// est toujours humain). Piloté par StressTestRunner.cs, jamais par le jeu normal.
        /// </summary>
        public static bool ForceAllAI = false;

        /// <summary>
        /// Debug/stress-test uniquement — saute l'animation de déplacement du pion (hop
        /// case par case) au profit d'une téléportation directe à la case d'arrivée.
        /// L'animation impose au moins 1 frame réelle par case traversée, indépendamment de
        /// Time.timeScale (qui n'accélère que le temps de jeu, pas le nombre de frames
        /// réelles nécessaires) — un coût pur sans intérêt pour un batch 100% IA.
        /// Piloté par StressTestRunner.cs, jamais par le jeu normal.
        /// </summary>
        public static bool SkipMovementAnimation = false;

        public static int? TestPlayerCount;
        public static bool DeferGameLoop;
        private List<PlayerModel> deferredPlayers;
        private Vector3 deferredStartPosition;
        private bool deferredGameStarted;
        public bool CanStartDeferredGame => deferredPlayers != null && !deferredGameStarted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeConfiguration()
        {
            DeferGameLoop = false;
        }

        private void Awake()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // Les probes temps réel URP peuvent dépasser la mémoire WASM dès le premier rendu.
            // Le plateau conserve son éclairage statique ; seule la capture cubemap dynamique
            // est désactivée sur le build navigateur.
            foreach (var probe in FindObjectsOfType<ReflectionProbe>(true))
                if (probe != null && probe.mode == UnityEngine.Rendering.ReflectionProbeMode.Realtime)
                    probe.enabled = false;
#endif
        }

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

            PolyRisk.UI.BankruptcyScreenController.EnsureExists();
            PolyRisk.UI.VictoryScreenController.EnsureExists();
            PolyRisk.UI.VentureCardModalController.EnsureExists();
            numberOfPlayers = Mathf.Clamp(TestPlayerCount ?? numberOfPlayers, 2, 6);

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
                bool isAI = ForceAllAI || (i != 0);
                PlayerModel newPlayer = new PlayerModel(i, startingBalance, isAI);

                if (isAI)
                {
                    // Modulo signé en C# (contrairement à Python) : (i-1) peut valoir -1 quand
                    // ForceAllAI rend le joueur 0 IA lui aussi — le +Length avant le second %
                    // ramène toujours l'index dans [0, Length).
                    int profileIndex = ((i - 1) % profileRoster.Length + profileRoster.Length) % profileRoster.Length;
                    newPlayer.Profile = profileRoster[profileIndex];
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
            deferredStartPosition = startPos;

            // 6. Start the Game Loop, or wait behind the home/lobby screen.
            Debug.Log("Bootstrapper: Starting Game Loop...");
            if (DeferGameLoop)
            {
                deferredPlayers = playersList;
                Debug.Log("Bootstrapper: Game loop deferred until a game mode is selected.");
                yield break;
            }
            turnManager.StartGame(playersList);
            
            Debug.Log("Bootstrapper: Initialization Complete.");
        }

        public bool StartDeferredLocalGame(bool resumeLoop = true)
        {
            if (!CanStartDeferredGame) return false;
            deferredGameStarted = true;
            DeferGameLoop = false;
            turnManager.StartGame(deferredPlayers, resumeLoop);
            deferredPlayers = null;
            Debug.Log("Bootstrapper: Deferred local game started.");
            return true;
        }

        public bool StartDeferredOnlineGame(int playerCount, bool runAuthoritativeLoop)
        {
            if (!CanStartDeferredGame) return false;
            playerCount = Mathf.Clamp(playerCount, 2, 6);

            var onlinePlayers = new List<PlayerModel>(playerCount);
            for (int i = 0; i < playerCount; i++)
                onlinePlayers.Add(new PlayerModel(i, startingBalance, isAI: false));

            visualManager.SpawnPlayers(onlinePlayers, deferredStartPosition);
            deferredGameStarted = true;
            DeferGameLoop = false;
            turnManager.StartGame(onlinePlayers, runAuthoritativeLoop);
            deferredPlayers = null;
            Debug.Log($"Bootstrapper: Deferred online game initialized with {playerCount} human seats (authority={runAuthoritativeLoop}).");
            return true;
        }
    }
}
