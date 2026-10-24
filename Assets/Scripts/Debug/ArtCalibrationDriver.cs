using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using PolyRisk.Multiplayer;
using PolyRisk.Systems;
using PolyRisk.UI;
using PolyRisk.Visuals;
using Unity.Profiling;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
#endif

namespace PolyRisk.DebugTools
{
    /// <summary>
    /// Opt-in art calibration on the real board, at the real camera scale. Mounting,
    /// fixtures and measurements are compiled out of a non-development Release.
    /// Fixtures replace presentation pixels only; they never manufacture ownership,
    /// income, movement permission, a turn, or a network command.
    /// </summary>
    [DefaultExecutionOrder(125)]
    public sealed class ArtCalibrationDriver : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static readonly FieldInfo StateField = typeof(PolyRiskBoard).GetField("statePixels", Private);
        static readonly MethodInfo PushState = typeof(PolyRiskBoard).GetMethod("PushStateTextureImmediate", Private);
        static readonly FieldInfo ContactTexture = typeof(BoardContactFeedback).GetField("texture", Private);
        static readonly MethodInfo CaptureEconomy = typeof(OnlineGameController).GetMethod("CaptureSnapshot", Private);
        PolyRiskBoard board;
        BoardSurfaceLighting lighting;
        BoardContactFeedback contact;
        BoardInteractionManager rig;
        Color[] originalPixels, originalContactPixels, sampleContactPixels;
        CompactPawnVisual[] originalPawns;
        bool[] originalActive;
        string originalEconomy;
        string fixture = "live game";
        int fixtureIndex = -1, stage = 4;
        bool baseline, fixedContact, fixedArrival, running, measuring, recording;
        float nextTrace, captureEpoch;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Mount()
        {
            if (!Application.isBatchMode)
                new GameObject("[QA] ArtCalibration").AddComponent<ArtCalibrationDriver>();
        }

        bool Cache()
        {
            if (board == null) board = FindObjectOfType<PolyRiskBoard>();
            if (board != null && lighting == null) lighting = board.GetComponent<BoardSurfaceLighting>();
            if (board != null && contact == null) contact = board.GetComponent<BoardContactFeedback>();
            if (rig == null) rig = FindObjectOfType<BoardInteractionManager>();
            return board != null && lighting != null && Camera.main != null;
        }

        public void Baseline() { SetTreatment(true, stage); }
        public void NewProposal() { SetTreatment(false, stage); }
        public void StageNeutral() { SetTreatment(baseline, 0); }
        public void StageArchitecture() { SetTreatment(baseline, 1); }
        public void StageSources() { SetTreatment(baseline, 2); }
        public void StageResponse() { SetTreatment(baseline, 3); }
        public void StageOptics() { SetTreatment(baseline, 4); }
        void SetTreatment(bool useBaseline, int nextStage)
        {
            if (running || measuring || !Cache()) return;
            baseline = useBaseline; stage = Mathf.Clamp(nextStage, 0, 4);
            lighting.ApplyArtCalibration(baseline, stage);
            Describe();
        }

        static string Hash(string value)
        {
            if (value == null) return "unavailable";
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "").ToLowerInvariant();
        }
        static string EconomyHash()
        {
            var online = OnlineGameController.Instance;
            var turns = TurnManager.Instance;
            if (turns?.CurrentPlayer == null || BoardManager.Instance == null) return "unavailable";
            if (online != null && CaptureEconomy != null && LobbySession.Instance?.CurrentLobby?.players != null)
                return Hash(JsonUtility.ToJson(CaptureEconomy.Invoke(online, null)));
            // The shipping online snapshot assumes a lobby, so it cannot be called
            // in an offline calibration game. Read scalar model properties instead.
            // Public scalar getters contain no economy operations; rents are never
            // calculated to create this checksum.
            var text = new StringBuilder();
            text.Append(turns.CurrentState).Append('|').Append(turns.TurnsStarted).Append('|')
                .Append(turns.CurrentPlayer.PlayerId).Append('|').Append(turns.PendingDiceTotal).Append('|');
            foreach (var player in turns.Players.OrderBy(p => p.PlayerId))
            {
                AppendScalars(text, player);
                text.Append("position=").Append(turns.GetPlayerPosition(player)).Append('|');
                foreach (var property in player.OwnedProperties.OrderBy(p => p.TileIndex)) text.Append(property.TileIndex).Append(',');
            }
            foreach (var tile in BoardManager.Instance.Tiles.OrderBy(t => t.TileIndex))
            {
                var property = tile.Data as PolyRisk.Core.PropertyData;
                if (property == null) continue;
                AppendScalars(text, property);
                text.Append("owner=").Append(turns.GetOwnerOfProperty(property)?.PlayerId ?? -1).Append('|');
                text.Append("hijacker=").Append(property.HijackedBy?.PlayerId ?? -1).Append('|');
            }
            AppendScalars(text, turns.financeSystem);
            text.Append(JsonUtility.ToJson(turns.CaptureMovementState()));
            return Hash(text.ToString());
        }
        static void AppendScalars(StringBuilder text, object source)
        {
            if (source == null) return;
            foreach (var property in source.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).OrderBy(p => p.Name))
            {
                var type = property.PropertyType;
                if (!property.CanRead || property.GetIndexParameters().Length != 0 ||
                    !(type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal))) continue;
                text.Append(property.Name).Append('=').Append(Convert.ToString(property.GetValue(source), System.Globalization.CultureInfo.InvariantCulture)).Append('|');
            }
        }

        bool StartFixture(string name)
        {
            if (running || measuring || !Cache() || TurnManager.Instance?.CurrentPlayer == null) return false;
            if (OnlineGameController.Instance != null && OnlineGameController.Instance.IsOnlineGame)
            {
                Debug.LogWarning("ART_CALIBRATION_REFUSED presentation fixtures require an isolated offline game; A/B remains available online");
                return false;
            }
            if (contact != null && contact.HasActiveContacts)
            {
                Debug.LogWarning("ART_CALIBRATION_REFUSED wait for the real transient contact to decay before freezing a specimen");
                return false;
            }
            RestorePresentation(false);
            var pixels = StateField?.GetValue(board) as Color[];
            if (pixels == null) return false;
            originalPixels = (Color[])pixels.Clone();
            var texture = contact != null ? ContactTexture?.GetValue(contact) as Texture2D : null;
            originalContactPixels = texture != null ? texture.GetPixels() : null;
            sampleContactPixels = originalContactPixels != null ? new Color[originalContactPixels.Length] : null;
            originalPawns = CompactPawnVisual.Instances.Where(p => p != null).ToArray();
            originalActive = originalPawns.Select(p => p.IsActive).ToArray();
            originalEconomy = EconomyHash();
            fixture = name;
            // Choose an actual visible tile beside the active pawn. Never reframe the
            // camera to make a material look better than its shipping presentation.
            var turns = TurnManager.Instance;
            int logical = turns.GetPlayerPosition(turns.CurrentPlayer);
            var camera = Camera.main;
            var viewport = rig != null ? rig.BoardViewport : new Rect(0, 0, Screen.width, Screen.height);
            var candidates = board.GetComponentsInChildren<TileIdentifier>()
                .Where(a => a.TileIndex >= 0 && a.TileIndex < 40)
                .Where(a => viewport.Contains(camera.WorldToScreenPoint(a.transform.position)))
                .OrderBy(a => a.TileIndex == logical ? 10000f : (a.transform.position - BoardManager.Instance.visualTiles[logical].position).sqrMagnitude)
                .ToArray();
            fixtureIndex = candidates.Length > 0 ? candidates[0].TileIndex : logical;
            return true;
        }

        public void FixtureInactive()
        {
            if (!StartFixture("inactive graphite")) return;
            board.SetTile(fixtureIndex, new Color(.35f, .39f, .43f), 0);
            Describe();
        }
        public void FixtureOwned()
        {
            if (!StartFixture("colored presentation sample — ownership model unchanged")) return;
            board.SetTile(fixtureIndex, PlayerVisualManager.GetPlayerColor(0), .42f);
            Describe();
        }
        public void FixtureSelected()
        {
            if (!StartFixture("selected presentation sample")) return;
            board.SetTile(fixtureIndex, new Color(.22f, .84f, .73f), board.highlightIntensity);
            Describe();
        }
        public void FixtureContact() { SetContactFixture(false, "contact peak, frozen for A/B"); }
        public void FixtureArrival() { SetContactFixture(true, "arrival peak, frozen for A/B"); }
        public void FixtureReflection() { SetContactFixture(false, "local neon response, frozen for A/B"); }
        public void FixtureCornerContact() { SetCornerFixture(false); }
        public void FixtureCornerArrival() { SetCornerFixture(true); }
        void SetCornerFixture(bool arrival)
        {
            SetContactFixture(arrival, arrival ? "corner arrival, frozen for A/B" : "corner contact, frozen for A/B");
            if (!fixedContact) return;
            var camera = Camera.main;
            var viewport = rig != null ? rig.BoardViewport : new Rect(0, 0, Screen.width, Screen.height);
            var visible = board.GetComponentsInChildren<TileIdentifier>()
                .Where(a => a.TileIndex % 10 == 0 && viewport.Contains(camera.WorldToScreenPoint(a.transform.position)))
                .OrderBy(a => ((Vector2)camera.WorldToScreenPoint(a.transform.position) - viewport.center).sqrMagnitude).ToArray();
            if (visible.Length == 0)
            {
                RestorePresentation(false);
                Debug.LogWarning("ART_CALIBRATION_NOT_READY no corner currently visible; pan using the shipping camera controls"); return;
            }
            fixtureIndex = visible[0].TileIndex; WriteContactSample(); Describe();
        }
        void SetContactFixture(bool arrival, string name)
        {
            if (!StartFixture(name)) return;
            if (contact == null || originalContactPixels == null)
            {
                Debug.LogWarning("ART_CALIBRATION_NOT_READY contact texture unavailable");
                RestorePresentation(true); return;
            }
            fixedContact = true; fixedArrival = arrival;
            WriteContactSample(); Describe();
        }
        public void FixturePawnIdle()
        {
            if (!StartFixture("monolith at rest — no geometry enlargement")) return;
            foreach (var pawn in originalPawns) pawn.SetActive(false, false);
            Describe();
        }
        public void FixturePawnActive()
        {
            if (!StartFixture("active monolith — no geometry enlargement")) return;
            int active = TurnManager.Instance.CurrentPlayer.PlayerId;
            foreach (var pawn in originalPawns) pawn.SetActive(pawn.PlayerId == active, false);
            Describe();
        }
        public void FixtureMetal()
        {
            if (!StartFixture("architectural edge and bevel — original camera")) return;
            Describe();
        }

        void WriteContactSample()
        {
            var texture = contact != null ? ContactTexture.GetValue(contact) as Texture2D : null;
            if (texture == null || fixtureIndex < 0) return;
            // This is explicitly a frozen peak specimen, not evidence of temporal
            // quality. Real timing is validated by the production movement below.
            var pixels = sampleContactPixels;
            Array.Clear(pixels, 0, pixels.Length);
            var color = PlayerVisualManager.GetPlayerColor(TurnManager.Instance.CurrentPlayer.PlayerId);
            color.a = fixedArrival ? 1.35f : .90f;
            pixels[fixtureIndex] = color;
            pixels[texture.width + fixtureIndex] = new Color(1, .075f, fixedArrival ? 1 : 0, 0);
            texture.SetPixels(pixels); texture.Apply(false, false);
        }

        public void RestoreScene()
        {
            if (running || measuring) return;
            RestorePresentation(true); Describe();
        }
        void RestorePresentation(bool report)
        {
            fixedContact = false; fixedArrival = false;
            if (originalPixels != null && board != null)
            {
                StateField.SetValue(board, (Color[])originalPixels.Clone());
                PushState.Invoke(board, null);
                // Only transient fixture illumination is cleared. No ownership or
                // economic state is ever replayed or overwritten by this tool.
                var texture = contact != null ? ContactTexture.GetValue(contact) as Texture2D : null;
                if (texture != null && originalContactPixels != null)
                { texture.SetPixels(originalContactPixels); texture.Apply(false, false); }
                if (originalPawns != null)
                    for (int i = 0; i < originalPawns.Length; i++)
                        if (originalPawns[i] != null) originalPawns[i].SetActive(originalActive[i], false);
                if (report)
                    Debug.Log("ART_CALIBRATION_RESTORE economyUnchanged=" + (originalEconomy == EconomyHash()) + " cameraNotWritten=true");
            }
            originalPixels = null; originalContactPixels = null; sampleContactPixels = null; originalPawns = null; originalActive = null;
            fixture = "live game"; fixtureIndex = -1;
        }

        public void Describe()
        {
            if (!Cache()) { Debug.Log("ART_CALIBRATION_NOT_READY"); return; }
            var camera = Camera.main;
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            var data = camera.GetUniversalAdditionalCameraData();
            Debug.Log("ART_CALIBRATION_STATUS " + JsonUtility.ToJson(new Status {
                variant = baseline ? "A — baseline" : "B — proposal", stage = stage,
                treatment = lighting.Treatment.ToString(), fixture = fixture, fixtureIndex = fixtureIndex,
                width = Screen.width, height = Screen.height, quality = QualitySettings.names[QualitySettings.GetQualityLevel()],
                pipeline = pipeline != null ? pipeline.name : "unavailable", renderScale = pipeline != null ? pipeline.renderScale : 0,
                antialiasing = data.antialiasing.ToString(), postProcessing = data.renderPostProcessing,
                camera = camera.transform.position, angles = camera.transform.eulerAngles, orthographicSize = camera.orthographicSize,
                closeFactor = rig != null ? rig.StrategicReferenceOrthographicSize / camera.orthographicSize : 0,
                activePlayer = TurnManager.Instance?.CurrentPlayer?.PlayerId ?? -1,
                logicalTile = TurnManager.Instance?.CurrentPlayer != null ? TurnManager.Instance.GetPlayerPosition(TurnManager.Instance.CurrentPlayer) : -1,
                economyHash = EconomyHash(), finish = lighting.Finish,
                anchors = board.GetComponentsInChildren<TileIdentifier>().Length,
                fusedMeshes = board.GetComponentsInChildren<MeshFilter>().Length,
                realLights = FindObjectsOfType<Light>().Count(l => l.enabled),
                realtimeProbes = FindObjectsOfType<ReflectionProbe>().Count(p => p.enabled && p.mode == ReflectionProbeMode.Realtime),
                reducedMotion = UIUXRuntime.ReducedMotion, frozenContactPeak = fixedContact
            }));
        }
        [Serializable] sealed class Status
        {
            public string variant, treatment, fixture, quality, pipeline, antialiasing, economyHash;
            public int stage, fixtureIndex, width, height, activePlayer, logicalTile, anchors, fusedMeshes, realLights, realtimeProbes;
            public float renderScale, orthographicSize, closeFactor;
            public Vector3 camera, angles;
            public bool postProcessing, reducedMotion, frozenContactPeak;
            public BoardVisualTheme.SurfaceFinish finish;
        }

        public void RunChecks()
        {
            if (running || measuring || !Cache()) return;
            int checks = 0, failures = 0;
            Action<bool, string> check = (ok, name) => {
                checks++; if (!ok) failures++;
                Debug.Log("ART_CALIBRATION_CASE " + (ok ? "PASS " : "FAIL ") + name);
            };
            var camera = Camera.main;
            var pose = camera.transform.position; var rotation = camera.transform.rotation; float size = camera.orthographicSize;
            var anchors = board.GetComponentsInChildren<TileIdentifier>().OrderBy(a => a.TileIndex).ToArray();
            var poses = anchors.Select(a => a.transform.localToWorldMatrix).ToArray();
            var colliders = anchors.Select(a => a.GetComponent<BoxCollider>()).ToArray();
            var centers = colliders.Select(c => c.center).ToArray(); var sizes = colliders.Select(c => c.size).ToArray();
            var meshes = board.GetComponentsInChildren<MeshFilter>().Select(f => f.sharedMesh.GetInstanceID()).ToArray();
            var state = (Color[])((Color[])StateField.GetValue(board)).Clone();
            var economy = EconomyHash();
            bool oldBaseline = baseline; int oldStage = stage;
            check(anchors.Select(a => a.TileIndex).SequenceEqual(Enumerable.Range(0, 40)), "40 unique logical anchors");
            check(meshes.Length == 4, "four original fused meshes");
            check(camera.orthographic && Mathf.Abs(camera.transform.eulerAngles.x - 40) < .001f, "approved orthographic isometry");
            check(rig != null && Mathf.Abs(rig.StrategicReferenceOrthographicSize / size - 1.4f) < .00001f, "permanent x1.40");
            var uiRoot=FindObjectOfType<TopHUDWidget>()?.uiDocument?.rootVisualElement;
            var overlay=uiRoot?.Q<VisualElement>("BoardTileIdentityOverlay");
            check(overlay!=null&&overlay.childCount==40,"40 retained presentation identities");
            if(overlay!=null&&uiRoot.worldBound.width>0&&uiRoot.worldBound.height>0&&rig!=null)
            {
                foreach(var chip in overlay.Children())
                {
                    if(chip.resolvedStyle.display==DisplayStyle.None)continue;
                    Rect bounds=chip.worldBound;
                    Vector2 low=new Vector2((bounds.xMin-uiRoot.worldBound.xMin)*Screen.width/uiRoot.worldBound.width,
                        Screen.height-(bounds.yMax-uiRoot.worldBound.yMin)*Screen.height/uiRoot.worldBound.height);
                    Vector2 high=new Vector2((bounds.xMax-uiRoot.worldBound.xMin)*Screen.width/uiRoot.worldBound.width,
                        Screen.height-(bounds.yMin-uiRoot.worldBound.yMin)*Screen.height/uiRoot.worldBound.height);
                    check(rig.BoardViewport.Contains(low)&&rig.BoardViewport.Contains(high),"visible identity stays outside HUD/cockpit "+chip.name);
                }
            }
            foreach (bool useBaseline in new[] { true, false })
            for (int s = 0; s < 5; s++)
            {
                lighting.ApplyArtCalibration(useBaseline, s);
                string label = (useBaseline ? "A" : "B") + " stage " + s;
                check(camera.transform.position == pose && camera.transform.rotation == rotation && camera.orthographicSize == size, label + " camera unchanged");
                check(poses.SequenceEqual(anchors.Select(a => a.transform.localToWorldMatrix)), label + " exact anchors unchanged");
                check(centers.SequenceEqual(colliders.Select(c => c.center)) && sizes.SequenceEqual(colliders.Select(c => c.size)), label + " collider contracts unchanged");
                check(meshes.SequenceEqual(board.GetComponentsInChildren<MeshFilter>().Select(f => f.sharedMesh.GetInstanceID())), label + " same fused mesh instances");
                check(state.SequenceEqual((Color[])StateField.GetValue(board)), label + " presentation state unchanged by treatment");
                check(economy != "unavailable" && economy == EconomyHash(), label + " economic readback unchanged (full snapshot online, scalar models offline)");
            }
            lighting.ApplyArtCalibration(oldBaseline, oldStage);
            Physics.SyncTransforms();
            foreach (var anchor in anchors)
            {
                RaycastHit hit;
                check(anchor.GetComponent<BoxCollider>().Raycast(new Ray(anchor.transform.position + Vector3.up * 2, Vector3.down), out hit, 3)
                    && hit.collider.gameObject == anchor.gameObject, "raycast contract " + anchor.TileIndex);
            }
            Describe(); Debug.Log("ART_CALIBRATION_CHECKS_DONE checks=" + checks + " failed=" + failures);
        }

        public void Measure() { if (!measuring && !running && Cache()) StartCoroutine(MeasureRoutine()); }
        IEnumerator MeasureRoutine()
        {
            measuring = true;
            yield return new WaitForSecondsRealtime(.5f);
            var draw = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            var batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            var passes = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            var main = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread");
            var values = new List<float>(1024);
            long maxDraw = 0, maxBatches = 0, maxPasses = 0, mainSum = 0; int mainSamples = 0;
            bool beganBaseline = baseline; int beganStage = stage;
            string beganEconomy = EconomyHash(); string beganFixture = fixture;
            float began = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - began < 6)
            {
                yield return null;
                values.Add(Time.unscaledDeltaTime * 1000);
                if (draw.Valid) maxDraw = Math.Max(maxDraw, draw.LastValue);
                if (batches.Valid) maxBatches = Math.Max(maxBatches, batches.LastValue);
                if (passes.Valid) maxPasses = Math.Max(maxPasses, passes.LastValue);
                if (main.Valid && main.LastValue > 0) { mainSum += main.LastValue; mainSamples++; }
            }
            values.Sort();
            var measurement = new Measurement {
                variant = baseline ? "A baseline" : "B proposal", stage = stage, fixture = fixture,
                quality = QualitySettings.names[QualitySettings.GetQualityLevel()], width = Screen.width, height = Screen.height,
                seconds = Time.realtimeSinceStartup - began, frames = values.Count,
                meanFrameMs = values.Count > 0 ? values.Average() : 0,
                medianFrameMs = values.Count > 0 ? values[values.Count / 2] : 0,
                p95FrameMs = values.Count > 0 ? values[Mathf.Min(values.Count - 1, Mathf.FloorToInt(values.Count * .95f))] : 0,
                maxFrameMs = values.Count > 0 ? values[values.Count - 1] : 0,
                cpuMainThreadAvailable = mainSamples > 0, cpuMainThreadMs = mainSamples > 0 ? mainSum / (double)mainSamples / 1000000 : 0,
                gpuTimeAvailable = false, renderCountersAvailable = draw.Valid && maxDraw > 0,
                drawCalls = maxDraw, batches = maxBatches, setPass = maxPasses,
                totalAllocatedBytes = Profiler.GetTotalAllocatedMemoryLong(), totalReservedBytes = Profiler.GetTotalReservedMemoryLong(),
                unchangedDuringSample = beganBaseline == baseline && beganStage == stage && beganFixture == fixture && beganEconomy == EconomyHash()
            };
            draw.Dispose(); batches.Dispose(); passes.Dispose(); main.Dispose(); measuring = false;
            Debug.Log("ART_CALIBRATION_MEASURE " + JsonUtility.ToJson(measurement));
        }
        [Serializable] sealed class Measurement
        {
            public string variant, quality, fixture;
            public int stage, width, height, frames;
            public float seconds, meanFrameMs, medianFrameMs, p95FrameMs, maxFrameMs;
            public double cpuMainThreadMs;
            public bool cpuMainThreadAvailable, gpuTimeAvailable, renderCountersAvailable, unchangedDuringSample;
            public long drawCalls, batches, setPass, totalAllocatedBytes, totalReservedBytes;
        }

        public void MoveShort() { BeginMovement(3, "short"); }
        public void MoveLong() { BeginMovement(12, "long"); }
        public void MoveCorner()
        {
            var turns = TurnManager.Instance;
            if (turns?.CurrentPlayer == null) return;
            int current = turns.GetPlayerPosition(turns.CurrentPlayer);
            BeginMovement(10 - current % 10 + 2, "corner");
        }
        void BeginMovement(int steps, string name)
        {
            if (running || measuring || !Cache() || TurnManager.Instance?.CurrentPlayer == null) return;
            var online = OnlineGameController.Instance;
            if (online != null && online.IsOnlineGame)
            {
                Debug.LogWarning("ART_CALIBRATION_REFUSED visual movement fixture requires isolated offline game"); return;
            }
            if (TurnManager.Instance.HasPendingMovement || TurnManager.Instance.CurrentState == TurnManager.TurnState.MovingPawn)
            {
                Debug.LogWarning("ART_CALIBRATION_REFUSED a real gameplay movement is already active"); return;
            }
            RestorePresentation(false);
            StartCoroutine(MovementRoutine(steps, name));
        }
        IEnumerator MovementRoutine(int steps, string name)
        {
            running = true;
            var turns = TurnManager.Instance; int actor = turns.CurrentPlayer.PlayerId;
            var pawn = GameObject.Find("Pawn_Player_" + actor)?.GetComponent<PawnMovement>();
            if (pawn == null || rig == null) { running = false; yield break; }
            int logical = turns.GetPlayerPosition(turns.CurrentPlayer);
            var originalPosition = pawn.transform.position; var scale = pawn.transform.localScale;
            var visual = pawn.GetComponent<CompactPawnVisual>();
            string economy = EconomyHash(); int callbacks = 0;
            var path = BoardManager.Instance.GetPath(logical, steps);
            fixture = "production movement fixture " + name;
            recording = true; captureEpoch = Time.unscaledTime;
            PlayerVisualManager.NotifyVisualMovementStarted(actor, pawn, path);
            float began = Time.unscaledTime;
            pawn.MoveAlongPath(path, () => { callbacks++; rig.CompleteVisualMovement(pawn.transform); });
            while (callbacks == 0 && Time.unscaledTime - began < 20) yield return null;
            bool destination = Vector3.Distance(pawn.transform.position, path[path.Count - 1]) < .001f;
            yield return new WaitForSecondsRealtime(1.2f);
            Debug.Log("ART_CALIBRATION_MOVEMENT " + JsonUtility.ToJson(new Movement {
                name = name, steps = steps, callbacks = callbacks, arrived = destination,
                economicSnapshotUnchanged = economy == EconomyHash(),
                logicalPositionUnchanged = turns.GetPlayerPosition(turns.CurrentPlayer) == logical,
                reducedMotion = UIUXRuntime.ReducedMotion, seconds = Time.unscaledTime - began
            }));
            recording = false;
            // The production MoveAlongPath callback has completed. Restoring this
            // QA-only visual pose does not call TurnManager or resolve a tile.
            if (callbacks == 1)
            {
                pawn.transform.position = originalPosition; pawn.transform.localScale = scale;
                visual?.ResetPresentation(); contact?.Clear(); rig.RestoreOverview();
            }
            fixture = "live game"; running = false; Describe();
        }
        [Serializable] sealed class Movement
        { public string name; public int steps, callbacks; public bool arrived, economicSnapshotUnchanged, logicalPositionUnchanged, reducedMotion; public float seconds; }

        public void CaptureMarker() { recording = true; captureEpoch = Time.unscaledTime; Describe(); }
        public void EndCapture() { recording = false; Describe(); }
        void LateUpdate()
        {
            if (fixedContact) WriteContactSample();
            if (!recording || Time.unscaledTime < nextTrace || !Cache()) return;
            nextTrace = Time.unscaledTime + .025f;
            var pawn = CompactPawnVisual.Instances.FirstOrDefault(p => p != null && p.IsActive);
            var texture = contact != null ? ContactTexture.GetValue(contact) as Texture2D : null;
            float alpha = texture != null && contact.LastIndex >= 0 ? texture.GetPixel(contact.LastIndex, 0).a : 0;
            Debug.Log("ART_CALIBRATION_FRAME " + JsonUtility.ToJson(new Trace {
                seconds = Time.unscaledTime - captureEpoch, variant = baseline ? "A" : "B", fixture = fixture,
                contactIndex = contact != null ? contact.LastIndex : -1, contactAlpha = alpha,
                arrival = contact != null && contact.LastArrival, contactActive = contact != null && contact.HasActiveContacts,
                pawnRoot = pawn != null ? pawn.transform.position : Vector3.zero,
                pawnVisual = pawn?.Visual != null ? pawn.Visual.position : Vector3.zero,
                camera = Camera.main.transform.position, size = Camera.main.orthographicSize,
                cameraHold = rig != null && rig.IsArrivalHold, moving = pawn != null && pawn.IsMoving,
                locks = pawn != null ? pawn.FinalLocks : 0, contacts = pawn != null ? pawn.AudibleContacts : 0
            }));
        }
        [Serializable] sealed class Trace
        { public string variant, fixture; public float seconds, contactAlpha, size; public int contactIndex, locks, contacts; public bool arrival, contactActive, cameraHold, moving; public Vector3 pawnRoot, pawnVisual, camera; }
        void OnDisable() { RestorePresentation(false); }
#endif
    }
}
