using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using PolyRisk.Core;
using PolyRisk.Systems;

namespace PolyRisk.DebugTools
{
    [AddComponentMenu("PolyRisk/Debug/Stress Test Runner")]
    public class StressTestRunner : MonoBehaviour
    {
        private static StressTestRunner instance;
        [SerializeField] private int numberOfGames = 5;
        [SerializeField] private float timeScale = 30f;
        [SerializeField] private float perGameTimeoutRealSeconds = 240f;
        [SerializeField] private float bootTimeoutRealSeconds = 10f;
        [SerializeField, Range(2, 6)] private int playerCount = 4;
        [SerializeField] private int baseSeed = 260926;
        public bool BatchRunning { get; private set; }
        public int CompletedGames { get; private set; }
        public int Anomalies { get; private set; }
        public string LastResultsPath { get; private set; }
        private bool gameEnded, bootReady;
        private string endReason;
        private int? winnerId;
        private int errorCount;
        private StreamWriter csv, trace;
        private readonly Dictionary<string, int> reasons = new Dictionary<string, int>();
        private readonly Dictionary<int, double> peaks = new Dictionary<int, double>();
        private int currentGame, startingFrame;
        private double totalWinnerCash;
        private int winnerSamples;
        private bool savedAI, savedSkip, savedDefer, savedRunInBackground;
        private int? savedPlayerCount;
        private float savedTimeScale, savedFixedDeltaTime, savedMaximumDeltaTime;
        private LogType savedFilter;
        private StackTraceLogType savedLogTrace, savedWarningTrace;
        private UnityEngine.Random.State savedRandom;

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        private static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }
        private void Start()
        {
            if (instance != this || Application.isEditor || !Application.isBatchMode || Arg("-polyGames") == null) return;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            StartBatch(int.Parse(Arg("-polyGames")), int.Parse(Arg("-polyPlayers") ?? "4"),
                int.Parse(Arg("-polySeed") ?? "260926"));
        }
        private void Update()
        {
            if (!Debug.isDebugBuild && !Application.isBatchMode) return;
            if (Input.GetKeyDown(KeyCode.F9) && !BatchRunning) StartBatch(numberOfGames, playerCount);
        }
        private void OnGUI()
        {
            if (!Application.isEditor) return;
            if (!Debug.isDebugBuild) return;
            GUI.Label(new Rect(10, 34, 600, 24), BatchRunning
                ? $"[Stress test] {CompletedGames}/{numberOfGames}, {playerCount} joueurs"
                : "[Stress test] F9 = lancer le batch");
        }
        public void StartBatch(int games, int players, int seed = 260926)
        {
            if (BatchRunning) return;
            numberOfGames = Mathf.Max(1, games);
            playerCount = Mathf.Clamp(players, 2, 6);
            baseSeed = seed;
            StartCoroutine(RunBatch());
        }
        private IEnumerator RunBatch()
        {
            BatchRunning = true;
            CompletedGames = Anomalies = winnerSamples = 0;
            totalWinnerCash = 0;
            reasons.Clear();
            savedAI = GameBootstrapper.ForceAllAI;
            savedSkip = GameBootstrapper.SkipMovementAnimation;
            savedDefer = GameBootstrapper.DeferGameLoop;
            savedPlayerCount = GameBootstrapper.TestPlayerCount;
            savedTimeScale = Time.timeScale;
            savedFixedDeltaTime = Time.fixedDeltaTime;
            savedMaximumDeltaTime = Time.maximumDeltaTime;
            savedRunInBackground = Application.runInBackground;
            savedRandom = UnityEngine.Random.state;
            savedFilter = Debug.unityLogger.filterLogType;
            savedLogTrace = Application.GetStackTraceLogType(LogType.Log);
            savedWarningTrace = Application.GetStackTraceLogType(LogType.Warning);
            GameBootstrapper.ForceAllAI = GameBootstrapper.SkipMovementAnimation = true;
            GameBootstrapper.DeferGameLoop = true;
            GameBootstrapper.TestPlayerCount = playerCount;
            Application.runInBackground = true;
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
            Debug.unityLogger.filterLogType = LogType.Warning;
            GameEvents.OnGameVictory += OnVictory;
            GameEvents.OnBalanceChanged += OnBalance;
            GameEvents.OnPlayerBankruptcy += OnBankruptcy;
            Application.logMessageReceived += OnLog;
            try
            {
                string dir = Arg("-polyResults") ?? Path.Combine(Application.dataPath, "..", "StressTestResults");
                Directory.CreateDirectory(dir);
                LastResultsPath = Path.Combine(dir, $"stress_test_{DateTime.Now:yyyyMMdd_HHmmss_fff}");
                csv = new StreamWriter(LastResultsPath + "_games.csv");
                trace = new StreamWriter(LastResultsPath + "_trace.log");
                var header = new List<string> { "GameIndex", "Seed", "PlayerCount", "WinnerId", "VictoryReason",
                    "TurnsStarted", "MinActiveLaps", "MaxLaps", "Sgo", "RealSecondsElapsed", "FramesElapsed", "ErrorCount" };
                for (int i = 0; i < 6; i++) header.AddRange(new[] { $"P{i}_Balance", $"P{i}_NetWorth",
                    $"P{i}_Bankrupt", $"P{i}_Profile", $"P{i}_Laps", $"P{i}_Turns", $"P{i}_PeakCash" });
                csv.WriteLine(string.Join(",", header));
                for (currentGame = 0; currentGame < numberOfGames; currentGame++)
                {
                    gameEnded = false; endReason = null; winnerId = null; errorCount = 0;
                    peaks.Clear();
                    Time.timeScale = 1;
                    Time.fixedDeltaTime = savedFixedDeltaTime;
                    Time.maximumDeltaTime = savedMaximumDeltaTime;
                    GameBootstrapper.DeferGameLoop = true;
                    yield return GameSession.Reload();
                    float bootStart = Time.realtimeSinceStartup;
                    bootReady = false;
                    while (Time.realtimeSinceStartup - bootStart < bootTimeoutRealSeconds)
                    {
                        var bootstrapper = FindObjectOfType<GameBootstrapper>();
                        if (bootstrapper != null && bootstrapper.CanStartDeferredGame)
                            bootstrapper.StartDeferredLocalGame(resumeLoop: false);
                        var tm = TurnManager.Instance;
                        if (tm != null && tm.Players != null && tm.Players.Count == playerCount)
                        { bootReady = true; break; }
                        yield return null;
                    }
                    if (!bootReady)
                    {
                        endReason = "ANOMALY_BOOT";
                        Capture(0);
                        break; // Invalid setup: don't produce a series of misleading games.
                    }
                    foreach (var p in TurnManager.Instance.Players) peaks[p.PlayerId] = p.CurrentBalance;
                    foreach (var probe in FindObjectsOfType<ReflectionProbe>()) probe.enabled = false;
                    UnityEngine.Random.InitState(baseSeed + currentGame);
                    FindObjectOfType<VentureSystem>()?.InitializeDeck();
                    trace.WriteLine($"GAME {currentGame} SEED {baseSeed + currentGame} PLAYERS {playerCount}");
                    // Keep the real-time physics frequency stable when accelerating the clock.
                    // Otherwise x100 drives thousands of unnecessary PhysX steps per second.
                    // Economic turns use Update/coroutines, not FixedUpdate or collisions.
                    Time.fixedDeltaTime = savedFixedDeltaTime * Mathf.Max(1f, timeScale);
                    Time.timeScale = timeScale;
                    float started = Time.realtimeSinceStartup;
                    startingFrame = Time.frameCount;
                    TurnManager.Instance.ResumeGameLoop();
                    while (!gameEnded && Time.realtimeSinceStartup - started < perGameTimeoutRealSeconds)
                        yield return null;
                    if (!gameEnded) endReason = "ANOMALY_TIMEOUT";
                    Capture(Time.realtimeSinceStartup - started);
                    // Stop before capturing another frame or reloading; no post-victory mutation.
                    TurnManager.Instance.StopAllCoroutines();
                    trace.Flush();
                }
                File.WriteAllLines(LastResultsPath + "_summary.txt", new[] {
                    $"Completed: {CompletedGames}; anomalies: {Anomalies}; players: {playerCount}; first seed: {baseSeed}",
                    string.Join("\n", reasons.Select(r => $"{r.Key}: {r.Value}")),
                    winnerSamples > 0 ? $"Mean winner cash: {F(totalWinnerCash / winnerSamples)}" : "No winner",
                    "TurnsStarted counts individual turns; laps are recorded per player. Real time is accelerated, not human session duration."
                });
            }
            finally { Restore(); }
            if (!Application.isEditor && Application.isBatchMode)
            {
                Debug.Log($"BATCH VALIDATION completed={CompletedGames} anomalies={Anomalies} path={LastResultsPath}");
                Application.Quit(Anomalies == 0 && CompletedGames == numberOfGames ? 0 : 1);
                yield break;
            }
            // Return to a clean normal game, not an extra unrecorded all-AI game.
            yield return GameSession.Reload();
        }
        private void Capture(float elapsed)
        {
            var tm = TurnManager.Instance;
            var players = bootReady ? tm.Players : new List<PlayerModel>();
            var active = players.Where(p => !p.IsBankrupt).ToList();
            foreach (var p in players)
                if (p.IsBankrupt && (p.CurrentBalance != 0 || p.OwnedProperties.Count != 0))
                    Debug.LogError($"Eliminated player {p.PlayerId} still has cash or property.");
            string reason = endReason ?? "UNKNOWN";
            if (errorCount > 0 && !reason.StartsWith("ANOMALY")) reason = "ANOMALY_ERRORS_" + reason;
            if (reason.StartsWith("ANOMALY") || reason == "UNKNOWN") Anomalies++;
            reasons[reason] = reasons.TryGetValue(reason, out int n) ? n + 1 : 1;
            var cells = new List<string> { currentGame.ToString(), (baseSeed + currentGame).ToString(), playerCount.ToString(),
                winnerId?.ToString() ?? "", reason, bootReady ? tm.TurnsStarted.ToString() : "",
                active.Count > 0 ? active.Min(p => p.LapsCompleted).ToString() : "",
                players.Count > 0 ? players.Max(p => p.LapsCompleted).ToString() : "",
                bootReady ? F(tm.financeSystem.CurrentSgo) : "", F(elapsed), (Time.frameCount - startingFrame).ToString(), errorCount.ToString() };
            for (int i = 0; i < 6; i++)
            {
                var p = players.FirstOrDefault(x => x.PlayerId == i);
                if (p == null) { cells.AddRange(Enumerable.Repeat("", 7)); continue; }
                cells.AddRange(new[] { F(p.CurrentBalance), F(tm.GetNetWorth(p)), p.IsBankrupt.ToString(), p.Profile.ToString(),
                    p.LapsCompleted.ToString(), p.TurnsStarted.ToString(),
                    F(peaks.TryGetValue(i, out double peak) ? peak : p.CurrentBalance) });
                if (winnerId == i) { totalWinnerCash += p.CurrentBalance; winnerSamples++; }
            }
            csv.WriteLine(string.Join(",", cells.Select(EscapeCsvField)));
            csv.Flush();
            CompletedGames++;
        }
        private void OnVictory(PlayerModel winner, string reason)
        {
            if (gameEnded) return;
            winnerId = winner?.PlayerId;
            endReason = winner == null ? "ANOMALY_ALL_BANKRUPT" : reason;
            gameEnded = true;
        }
        private void OnBalance(int id, double amount)
        {
            peaks[id] = peaks.TryGetValue(id, out double peak) ? Math.Max(peak, amount) : amount;
            trace?.WriteLine($"G={currentGame} BALANCE P={id} cash={F(amount)}");
        }
        private void OnBankruptcy(int id, int laps) =>
            trace?.WriteLine($"G={currentGame} BANKRUPTCY P={id} laps={laps}");
        private void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errorCount++;
            trace?.WriteLine($"G={currentGame} {type} {message}");
        }
        private static string F(double value) => value.ToString("F2", CultureInfo.InvariantCulture);
        private static string EscapeCsvField(string value)
        {
            if (string.IsNullOrEmpty(value) || (!value.Contains(",") && !value.Contains("\"")
                && !value.Contains("\r") && !value.Contains("\n"))) return value ?? "";
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        private void OnDisable() { if (BatchRunning) Restore(); }
        private void Restore()
        {
            GameEvents.OnGameVictory -= OnVictory;
            GameEvents.OnBalanceChanged -= OnBalance;
            GameEvents.OnPlayerBankruptcy -= OnBankruptcy;
            Application.logMessageReceived -= OnLog;
            csv?.Dispose(); csv = null;
            trace?.Dispose(); trace = null;
            GameBootstrapper.ForceAllAI = savedAI;
            GameBootstrapper.SkipMovementAnimation = savedSkip;
            GameBootstrapper.DeferGameLoop = savedDefer;
            GameBootstrapper.TestPlayerCount = savedPlayerCount;
            UnityEngine.Random.state = savedRandom;
            Time.timeScale = savedTimeScale;
            Time.fixedDeltaTime = savedFixedDeltaTime;
            Time.maximumDeltaTime = savedMaximumDeltaTime;
            Application.runInBackground = savedRunInBackground;
            Debug.unityLogger.filterLogType = savedFilter;
            Application.SetStackTraceLogType(LogType.Log, savedLogTrace);
            Application.SetStackTraceLogType(LogType.Warning, savedWarningTrace);
            BatchRunning = false;
        }
    }
}
