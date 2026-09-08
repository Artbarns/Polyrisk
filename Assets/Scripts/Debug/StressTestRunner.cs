using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using PolyRisk.Core;
using PolyRisk.Systems;

namespace PolyRisk.DebugTools
{
    /// <summary>
    /// DEBUG ONLY — Fait tourner N parties 100% IA d'affilée (une seule session Play,
    /// aucun rechargement du domaine C#) pour vérifier la stabilité économique dans la
    /// durée (backlog Phase F : "Stress test IA x1000 parties").
    ///
    /// F9 : lance le batch avec les paramètres de l'Inspector.
    ///
    /// Accélération par Time.timeScale (aucun bypass des délais du tour — la partie se
    /// joue exactement comme en vrai, juste plus vite). Entre deux parties : arrêt de la
    /// coroutine de TurnManager, destruction des singletons DontDestroyOnLoad, rechargement
    /// de la scène — un seul chemin de reset, utilisé aussi bien en fin de partie normale
    /// qu'en cas d'anomalie (timeout, faillite générale).
    ///
    /// Résultats écrits hors de Assets/ (StressTestResults/ à la racine du projet) pour
    /// qu'Unity n'essaie pas de les importer.
    ///
    /// ⚠️ Supprimer ce GameObject avant le build de prod.
    /// </summary>
    [AddComponentMenu("PolyRisk/Debug/Stress Test Runner")]
    public class StressTestRunner : MonoBehaviour
    {
        [Header("Config")]
        [Tooltip("Nombre de parties à jouer. Commencer par 5 (smoke test) avant de monter à 1000.")]
        [SerializeField] private int numberOfGames = 5;
        [Tooltip("Accélération du temps de jeu.")]
        [SerializeField] private float timeScale = 30f;
        [Tooltip("Plafond en secondes RÉELLES (pas affecté par timeScale) avant d'abandonner une partie bloquée. " +
            "90s s'est avéré trop juste pour une partie complète légitime (juste lente, pas bloquée) — 240s laisse " +
            "de la marge même à timeScale modéré.")]
        [SerializeField] private float perGameTimeoutRealSeconds = 240f;
        [Tooltip("Plafond en secondes RÉELLES pour qu'une nouvelle partie finisse de démarrer après le rechargement de scène.")]
        [SerializeField] private float bootTimeoutRealSeconds = 10f;

        bool batchRunning;
        bool gameEnded;
        string endReason;
        int errorCount;

        readonly HashSet<int> bankruptPlayerIds = new HashSet<int>();

        StreamWriter csvWriter;
        readonly List<string> summaryLines = new List<string>();
        int normalEndCount, anomalyCount;
        readonly Dictionary<string, int> reasonCounts = new Dictionary<string, int>();
        double sumBalance, minBalance = double.MaxValue, maxBalance = double.MinValue;
        int sumEndedOnLap, minEndedOnLap = int.MaxValue, maxEndedOnLap = int.MinValue;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F9) && !batchRunning)
                StartCoroutine(RunBatch());
        }

        void OnGUI()
        {
            GUI.color = new Color(1f, 0.8f, 0f, 0.9f);
            GUI.Label(new Rect(10, 34, 460, 20),
                batchRunning
                    ? "[DEBUG Stress Test] En cours..."
                    : "[DEBUG Stress Test] F9 = lancer le batch");
            GUI.color = Color.white;
        }

        // ─────────────────────────────────────────────────────────────────────

        StackTraceLogType savedLogTrace, savedWarningTrace;

        IEnumerator RunBatch()
        {
            batchRunning = true;
            GameBootstrapper.ForceAllAI = true;
            Application.logMessageReceived += OnLogMessage;
            GameEvents.OnGameVictory += HandleGameVictory;
            GameEvents.OnPlayerBankruptcy += HandlePlayerBankruptcy;

            // Goulot confirmé : générer une stack trace à chaque Debug.Log/Warning est un
            // coût CPU fixe, indépendant de Time.timeScale — avec 150+ points de log rien
            // que dans TurnManager/RulesEngine/MarketplaceSystem/FinanceSystem, c'est ça qui
            // dominait le temps réel par partie, pas les délais de jeu. Error/Exception
            // gardent leur trace complète (utile pour repérer un vrai crash) ; restauré à la
            // fin du batch pour ne pas affecter le debug normal en dehors du stress test.
            savedLogTrace = Application.GetStackTraceLogType(LogType.Log);
            savedWarningTrace = Application.GetStackTraceLogType(LogType.Warning);
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);

            string resultsDir = Path.Combine(Application.dataPath, "..", "StressTestResults");
            Directory.CreateDirectory(resultsDir);
            string stamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            csvWriter = new StreamWriter(Path.Combine(resultsDir, $"stress_test_{stamp}_games.csv"), false);
            csvWriter.WriteLine("GameIndex,WinnerId,VictoryReason,EndedOnLap,Sgo,MarketState,RealSecondsElapsed,ErrorCount," +
                "P0_Balance,P0_NetWorth,P0_Bankrupt,P0_Profile," +
                "P1_Balance,P1_NetWorth,P1_Bankrupt,P1_Profile," +
                "P2_Balance,P2_NetWorth,P2_Bankrupt,P2_Profile," +
                "P3_Balance,P3_NetWorth,P3_Bankrupt,P3_Profile");

            Debug.Log($"[STRESS TEST] Démarrage — {numberOfGames} parties, timeScale={timeScale}.");

            // La partie en cours au moment où F9 est pressé a déjà été bootstrappée par
            // GameBootstrapper.Start() (exécuté automatiquement à l'entrée en Play Mode,
            // AVANT ce coroutine) avec ForceAllAI encore à false — joueur 0 humain. Un reset
            // ici, avant la boucle, garantit que même la partie 0 tourne avec ForceAllAI=true.
            Debug.Log("[STRESS TEST] Reset initial pour appliquer ForceAllAI avant la partie 0...");
            yield return DoReset();

            for (int gameIndex = 0; gameIndex < numberOfGames; gameIndex++)
            {
                yield return RunOneGame(gameIndex);
            }

            csvWriter.Flush();
            csvWriter.Close();
            csvWriter = null;

            WriteSummary(resultsDir, stamp);

            GameEvents.OnGameVictory -= HandleGameVictory;
            GameEvents.OnPlayerBankruptcy -= HandlePlayerBankruptcy;
            Application.logMessageReceived -= OnLogMessage;
            Application.SetStackTraceLogType(LogType.Log, savedLogTrace);
            Application.SetStackTraceLogType(LogType.Warning, savedWarningTrace);
            foreach (var probe in FindObjectsOfType<ReflectionProbe>()) probe.enabled = true;
            Time.timeScale = 1f;
            batchRunning = false;
            Debug.Log($"[STRESS TEST] Terminé — {numberOfGames} parties dans {resultsDir}.");
        }

        IEnumerator RunOneGame(int gameIndex)
        {
            gameEnded = false;
            endReason = null;
            errorCount = 0;
            bankruptPlayerIds.Clear();
            string winnerId = "", p0 = "", p1 = "", p2 = "", p3 = "";
            int endedOnLap = 0;
            double sgo = 0;
            string marketState = "";

            float startReal = Time.realtimeSinceStartup;
            Time.timeScale = timeScale;

            // Diagnostic — heartbeat pour distinguer un vrai gel de l'Editor/moteur (Time.frameCount
            // qui stagne) d'un blocage purement logique (les frames avancent mais rien ne progresse
            // côté jeu). Nécessaire car le garde-fou 10s ajouté dans ExecuteMovement ne s'est jamais
            // déclenché sur un run où le jeu est resté totalement silencieux ~230s d'affilée.
            float lastHeartbeat = startReal;
            int lastHeartbeatFrame = Time.frameCount;
            while (!gameEnded && Time.realtimeSinceStartup - startReal < perGameTimeoutRealSeconds)
            {
                if (Time.realtimeSinceStartup - lastHeartbeat > 2f)
                {
                    int framesSince = Time.frameCount - lastHeartbeatFrame;
                    Debug.Log($"[DIAG-HEARTBEAT] +{Time.realtimeSinceStartup - startReal:F1}s réelles, {framesSince} frame(s) depuis le dernier battement, currentState={TurnManager.Instance?.CurrentState}.");
                    lastHeartbeat = Time.realtimeSinceStartup;
                    lastHeartbeatFrame = Time.frameCount;
                }
                yield return null;
            }

            float realElapsed = Time.realtimeSinceStartup - startReal;

            if (!gameEnded)
            {
                // Timeout réel dépassé — capturer ce qui est encore lisible avant reset.
                endReason = "ANOMALY_TIMEOUT";
            }

            // Capture (fonctionne aussi bien pour une fin normale, une faillite générale,
            // ou un timeout — TurnManager.Instance peut encore être valide dans les 3 cas).
            var tm = TurnManager.Instance;
            var fs = FindObjectOfType<FinanceSystem>();
            if (tm != null && tm.Players != null)
            {
                endedOnLap = tm.Players.Count > 0 ? tm.Players.Min(p => p.LapsCompleted) : 0;
                sgo = fs != null ? fs.CurrentSgo : 0;
                marketState = fs != null ? fs.CurrentMarketState : "";

                string[] cells = new string[4];
                for (int i = 0; i < tm.Players.Count && i < 4; i++)
                {
                    var p = tm.Players[i];
                    double netWorth = p.CurrentBalance + p.OwnedProperties.Sum(pr => pr.BasePrice);
                    cells[i] = $"{FormatDouble(p.CurrentBalance)},{FormatDouble(netWorth)},{p.IsBankrupt},{p.Profile}";
                }
                p0 = cells[0] ?? ""; p1 = cells[1] ?? ""; p2 = cells[2] ?? ""; p3 = cells[3] ?? "";
            }
            if (lastWinnerId.HasValue) winnerId = lastWinnerId.Value.ToString();

            // ── Reset : un seul chemin, fin normale ou anomalie ──────────────────
            Time.timeScale = 1f;
            yield return DoReset();

            // ── Écriture CSV ──────────────────────────────────────────────────
            string reason = endReason ?? "UNKNOWN";
            csvWriter.WriteLine($"{gameIndex},{winnerId},\"{reason}\",{endedOnLap},{FormatDouble(sgo)},{marketState},{realElapsed:F2},{errorCount},{p0},{p1},{p2},{p3}");
            csvWriter.Flush();

            // ── Agrégation pour le résumé ─────────────────────────────────────
            if (reason.StartsWith("ANOMALY")) anomalyCount++; else normalEndCount++;
            reasonCounts[reason] = reasonCounts.TryGetValue(reason, out int c) ? c + 1 : 1;
            if (endedOnLap > 0)
            {
                sumEndedOnLap += endedOnLap;
                minEndedOnLap = Mathf.Min(minEndedOnLap, endedOnLap);
                maxEndedOnLap = Mathf.Max(maxEndedOnLap, endedOnLap);
            }
            foreach (var cellStr in new[] { p0, p1, p2, p3 })
            {
                if (string.IsNullOrEmpty(cellStr)) continue;
                var parts = cellStr.Split(',');
                if (parts.Length > 0 && double.TryParse(parts[0], out double bal))
                {
                    sumBalance += bal;
                    minBalance = System.Math.Min(minBalance, bal);
                    maxBalance = System.Math.Max(maxBalance, bal);
                }
            }

            if (gameIndex % 25 == 0)
                Debug.Log($"[STRESS TEST] Partie {gameIndex}/{numberOfGames} — {reason}, {realElapsed:F1}s réelles.");
        }

        int? lastWinnerId;

        void HandleGameVictory(PlayerModel winner, string reason)
        {
            if (gameEnded) return; // déjà géré (ex. faillite générale détectée avant l'event)
            lastWinnerId = winner != null ? winner.PlayerId : (int?)null;
            endReason = reason;
            gameEnded = true;
        }

        void HandlePlayerBankruptcy(int playerId, int turnNumber)
        {
            bankruptPlayerIds.Add(playerId);
            var tm = TurnManager.Instance;
            int totalPlayers = tm != null && tm.Players != null ? tm.Players.Count : 4;
            if (!gameEnded && bankruptPlayerIds.Count >= totalPlayers)
            {
                // Bug latent connu : si tous les joueurs font faillite au même tour,
                // TurnManager ne désigne jamais de gagnant et boucle indéfiniment.
                // Détecté ici plutôt que d'attendre le plafond de 90s.
                lastWinnerId = null;
                endReason = "ANOMALY_ALL_BANKRUPT";
                gameEnded = true;
            }
        }

        void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception) errorCount++;
        }

        /// <summary>
        /// Arrête la coroutine de TurnManager (nécessaire dans TOUS les cas — la victoire
        /// à 10M$ déclenchée par FinanceSystem ne positionne jamais isGameOver), détruit
        /// les singletons DontDestroyOnLoad, recharge la scène, attend que le nouveau
        /// GameBootstrapper ait fini.
        /// </summary>
        IEnumerator DoReset()
        {
            TurnManager.Instance?.StopAllCoroutines();

            var singletonObjects = new HashSet<GameObject>();
            void Collect(Component c) { if (c != null) singletonObjects.Add(c.gameObject); }
            Collect(TurnManager.Instance);
            Collect(BoardManager.Instance);
            Collect(TransactionManager.Instance);
            Collect(AbilitySystem.Instance);
            Collect(AuctionManager.Instance);
            Collect(CycleManager.Instance);
            foreach (var go in singletonObjects) Destroy(go);

            lastWinnerId = null;
            yield return null; // laisser la destruction différée d'Unity se terminer

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

            float bootStart = Time.realtimeSinceStartup;
            while ((TurnManager.Instance == null || TurnManager.Instance.Players == null || TurnManager.Instance.Players.Count < 4)
                   && Time.realtimeSinceStartup - bootStart < bootTimeoutRealSeconds)
                yield return null;

            if (TurnManager.Instance == null || TurnManager.Instance.Players == null || TurnManager.Instance.Players.Count < 4)
                Debug.LogError("[STRESS TEST] La partie suivante n'a pas démarré dans les temps après le rechargement de scène.");

            // Autre goulot indépendant de Time.timeScale : la Reflection Probe du plateau est
            // en Realtime / Refresh "Every Frame" / 6 faces d'un coup (réglée ainsi pour voir
            // les reflets en direct pendant le travail visuel) — un coût GPU réel à chaque
            // frame, peu importe la vitesse simulée. Elle n'est pas DontDestroyOnLoad, donc
            // SceneManager.LoadScene() en recrée une neuve (enabled=true) à chaque partie —
            // à désactiver ici, après CHAQUE reset, pas juste une fois en début de batch.
            // Rendu du plateau sans importance pour un stress test.
            foreach (var probe in FindObjectsOfType<ReflectionProbe>()) probe.enabled = false;
        }

        static string FormatDouble(double d) =>
            (double.IsNaN(d) || double.IsInfinity(d)) ? d.ToString() : d.ToString("F2");

        void WriteSummary(string resultsDir, string stamp)
        {
            int total = normalEndCount + anomalyCount;
            int balanceSamples = total * 4;
            var lines = new List<string>
            {
                $"Stress test PolyRisk — {total} parties, timeScale={timeScale}",
                $"Normal : {normalEndCount}  |  Anomalies : {anomalyCount}",
                "",
                "Répartition des raisons de fin :"
            };
            foreach (var kv in reasonCounts.OrderByDescending(kv => kv.Value))
                lines.Add($"  {kv.Key} : {kv.Value} ({100.0 * kv.Value / total:F1}%)");

            lines.Add("");
            lines.Add(minEndedOnLap != int.MaxValue
                ? $"Tour de fin — min {minEndedOnLap}, max {maxEndedOnLap}, moy {(double)sumEndedOnLap / total:F1}"
                : "Tour de fin — aucune donnée");
            lines.Add(minBalance != double.MaxValue
                ? $"Balance finale (tous joueurs confondus) — min {minBalance:F2}, max {maxBalance:F2}, moy {sumBalance / balanceSamples:F2}"
                : "Balance finale — aucune donnée");
            lines.Add("");
            lines.Add(anomalyCount > 0
                ? $"⚠️ {anomalyCount} partie(s) anormale(s) — voir le CSV pour les GameIndex concernés."
                : "Aucune anomalie détectée.");

            File.WriteAllLines(Path.Combine(resultsDir, $"stress_test_{stamp}_summary.txt"), lines);
        }
    }
}
