using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BurritoRun
{
    internal enum AwarenessMode { Off, Inspect, Record }
    internal sealed partial class WorldAwareness
    {
        private sealed class Entry
        {
            internal Component Native;
            internal int Id;
            internal int Activation;
            internal string Key;
            internal int Seen;
            internal bool Retired;
        }
        private readonly Plugin plugin;
        private readonly WorldJournal journal;
        private readonly Dictionary<int, Entry> entries = new Dictionary<int, Entry>();
        private readonly Dictionary<string, Component> handles = new Dictionary<string, Component>();
        private readonly Dictionary<string, PerceptionEvidence> perception = new Dictionary<string, PerceptionEvidence>();
        private readonly Dictionary<string, int> counts = new Dictionary<string, int>();
        private readonly Dictionary<string, long> lifecycleCounts = new Dictionary<string, long>();
        private readonly TraceHistory<Dictionary<string, object>> history = new TraceHistory<Dictionary<string, object>>(101, 8 * 1024 * 1024);
        internal readonly WorldIndex Index = new WorldIndex();
        internal readonly AwarenessQuality Quality = new AwarenessQuality();
        internal AwarenessMode Mode { get; private set; } = AwarenessMode.Record;
        internal WorldQuery Query { get; } = new WorldQuery();
        internal WorldQueryResult Results { get; private set; } = new WorldQueryResult { Entities = Array.Empty<WorldEntitySnapshot>() };
        internal string ObserverKey { get; private set; }
        internal string TargetKey { get; private set; }
        internal Dictionary<string, object> Navigation { get; private set; }
        internal bool DetailOpen { get; private set; }
        internal bool Overlay { get; private set; } = true;
        private int level;
        private int activation;
        private int generation;
        private int collection;
        private int cursor;
        private bool scanning;
        private bool complete;
        private int exceptions;
        private int scopeController;
        private int scopeScene;
        private int scopeNest;
        private string scopeFloor;
        private float loadClock;
        private float nextScan;
        private float nextDetail;
        private float nextStats;
        private float nextPerceptionWindow;
        private int perceptionChecks;
        private int pendingChecks;
        private int perceptionCursor;
        private bool previousFrameLive;
        private bool censusMissingSource;
        private List<Camera> cameras = new List<Camera>();
        private CameraViewSnapshot[] cameraSnapshots = Array.Empty<CameraViewSnapshot>();
        private double frameWorkMs;
        private double lastFrameWorkMs;
        private double scanMs, queryMs, detailMs, overlayMs, hookMs;
        private double hookMaxMs;
        private int hookOverBudget;
        private int deferredLifecycleDetails;
        private readonly List<double> frameTimes = new List<double>(512);
        private readonly List<double> workTimes = new List<double>(512);
        private long allocatedAt;
        private int gc0;
        private Vector2 previousObserver;
        private string previousObserverKey;
        private float previousObserverTime;
        private float stationarySince;
        private readonly WorldBenchmark benchmark = new WorldBenchmark();
        private long trialDropped;
        private int trialErrors;
        private int trialLevel;
        private int suiteIndex = -1;
        private string suiteScenario;
        private bool closed;
        private string error;
        private Dictionary<string, object>[] incidentFrames = Array.Empty<Dictionary<string, object>>();
        private int incidentCursor;
        private Dictionary<string, object> latestDetailFrame;
        private int exportRemaining;
        private int exportOffset;
        internal WorldAwareness(Plugin plugin)
        {
            this.plugin = plugin;
            if (!plugin.TestMode) Mode = AwarenessMode.Off;
            foreach (string arg in Environment.GetCommandLineArgs())
            {
                if (arg == "--awareness-off") Mode = AwarenessMode.Off;
                if (arg == "--awareness-inspect") Mode = AwarenessMode.Inspect;
            }
            journal = new WorldJournal(plugin.CaptureWriter, plugin.Diagnostics.LogDirectory, plugin.Diagnostics.Session, Plugin.BuildLabel);
            Write("session", TraceWorld.Values("mode", Mode.ToString(), "sampleHz", 5, "budgetMs", 1,
                "losPerWindow", 32, "historySeconds", 20, "collections", WorldNative.Collections,
                "frameTarget", 30, "hardwareValidation", "development-host-only", "splitScreen", "prepared-not-live-verified"), true);
        }
        /// <summary>
        /// Account for the previous frame and feed valid timing samples into benchmarks and quality
        /// control.
        /// Native freeze frames remain measured; real pauses, world changes and capture loss
        /// invalidate trials.
        /// </summary>
        internal void BeginFrame()
        {
            var accountingTimer = Stopwatch.StartNew();
            lastFrameWorkMs = frameWorkMs;
            var gc = GameController.gameController;
            bool nativeFreeze = NativeFreeze(gc);
            if (benchmark.Running)
            {
                string invalid = !Ready(gc) || level != trialLevel ? "level-or-world-changed" :
                    Time.timeScale <= 0 && !nativeFreeze ? "paused" : exceptions != trialErrors ? "capture-error" :
                    plugin.CaptureWriter.Dropped + journal.Dropped != trialDropped ? "capture-loss" : null;
                SaveTrial(benchmark.Tick(Time.realtimeSinceStartup, Time.unscaledDeltaTime * 1000d, frameWorkMs, invalid, nativeFreeze));
            }
            bool live = plugin.TestMode && Ready(gc) && (Time.timeScale > 0 || nativeFreeze);
            if (live && previousFrameLive)
            { frameTimes.Add(Time.unscaledDeltaTime * 1000d); workTimes.Add(frameWorkMs); }
            if (frameTimes.Count > 600) { frameTimes.RemoveAt(0); workTimes.RemoveAt(0); }
            if (live && previousFrameLive) Quality.Observe(frameWorkMs, plugin.CaptureWriter.Saturated, Time.realtimeSinceStartup);
            previousFrameLive = live;
            frameWorkMs = accountingTimer.Elapsed.TotalMilliseconds;
        }
        private bool NativeFreeze(GameController gc) => gc != null && WorldFrameState.IsNativeFreeze(Time.timeScale,
            gc.mainTimeScale, gc.secondaryTimeScale, gc.freezeFrame, gc.freezeFramesDisabled, gc.justEndedFreezeFrame,
            DetailOpen || plugin.Phone.IsOpen || gc.screenshotMode);
        internal void AddWork(double milliseconds) => frameWorkMs += milliseconds;
        /// <summary>
        /// Advance the rolling census, selected detail, perception checks and pending exports on
        /// the main thread.
        /// Cooperative budgets keep world inspection bounded; a rolling sample is never an atomic
        /// view of the whole level.
        /// </summary>
        internal void Tick()
        {
            if (closed || !plugin.TestMode) return;
            var timer = Stopwatch.StartNew();
            try
            {
                var gc = GameController.gameController;
                if (!Ready(gc))
                {
                    if (scopeController != 0) ResetScope("world-unavailable");
                    return;
                }
                int controller = gc.GetInstanceID(), scene = SceneManager.GetActiveScene().handle,
                    nest = gc.agentsNest != null ? gc.agentsNest.GetInstanceID() : 0;
                if (scopeController != controller || scopeScene != scene || scopeNest != nest || scopeFloor != gc.floorNameNum || gc.timeSinceLoad + 0.25f < loadClock)
                {
                    ResetScope("level-change"); level++;
                    scopeController = controller; scopeScene = scene; scopeNest = nest; scopeFloor = gc.floorNameNum;
                    Write("level", TraceWorld.Values("controller", controller, "scene", scene, "nest", nest, "floor", scopeFloor), true);
                }
                loadClock = gc.timeSinceLoad;
                if (Mode == AwarenessMode.Off) { Stats(); return; }
                if (Time.timeScale > 0)
                {
                    if (!scanning && Time.time >= nextScan)
                    {
                        generation++; scanning = true; complete = false; collection = cursor = 0; counts.Clear(); censusMissingSource = false;
                        cameras = WorldPerception.Cameras(gc, out cameraSnapshots);
                    }
                    double start = timer.Elapsed.TotalMilliseconds;
                    while (scanning && frameWorkMs + timer.Elapsed.TotalMilliseconds < 1) ScanOne(gc);
                    scanMs += timer.Elapsed.TotalMilliseconds - start;
                }
                if (Time.time >= nextPerceptionWindow) { perceptionChecks = 0; nextPerceptionWindow = Time.time + 0.2f; }
                EnsureObserver(gc);
                if (Time.realtimeSinceStartup >= nextDetail && (!scanning || DetailOpen) &&
                    (DetailOpen || frameWorkMs + timer.Elapsed.TotalMilliseconds < 1))
                {
                    double start = timer.Elapsed.TotalMilliseconds;
                    RefreshDetails(gc);
                    detailMs += timer.Elapsed.TotalMilliseconds - start;
                    nextDetail = Time.realtimeSinceStartup + Quality.Interval;
                }
                while (perceptionChecks < 32 && pendingChecks > 0 && (DetailOpen || frameWorkMs + timer.Elapsed.TotalMilliseconds < 1))
                {
                    var e = Results.Entities[perceptionCursor % Results.Entities.Length]; perceptionCursor++; pendingChecks--;
                    if (e.Key == TargetKey) continue;
                    if (Quality.Level > 0 && e.Key != TargetKey) continue;
                    if (!(Resolve(ObserverKey) is Agent observer) || Resolve(e.Key) == null) continue;
                    perceptionChecks++;
                    perception[e.Key] = WorldPerception.Capture(observer, Resolve(e.Key), ObserverKey, e.Key, cameras);
                }
                if (incidentCursor < incidentFrames.Length && (DetailOpen || frameWorkMs + timer.Elapsed.TotalMilliseconds < 1))
                {
                    Write("incident-frame", incidentFrames[incidentCursor++]);
                    if (incidentCursor == incidentFrames.Length) incidentFrames = Array.Empty<Dictionary<string, object>>();
                }
                if (exportRemaining > 0 && (DetailOpen || frameWorkMs + timer.Elapsed.TotalMilliseconds < 1)) ExportPage();
                Stats();
            }
            catch (Exception failure)
            {
                error = failure.GetType().Name + ": " + failure.Message;
                exceptions++; complete = false;
                if (exceptions <= 5) Write("capture-error", TraceWorld.Values("reason", error), true);
            }
            finally { frameWorkMs += timer.Elapsed.TotalMilliseconds; }
        }
        private static bool Ready(GameController gc) => gc != null && gc.loadComplete && !gc.returningToMainMenu &&
            !gc.levelTransitioning && gc.levelType != "HomeBase" && gc.levelType != "Attract";
        /// <summary>
        /// Clear observations and pending exports when their native world is no longer valid.
        /// A familiar UID or Unity instance in a new level must not inherit the old level's
        /// identity or history.
        /// </summary>
        private void ResetScope(string reason)
        {
            if (scopeController != 0) Write("scope-ended", TraceWorld.Values("reason", reason), true);
            Index.Clear(); entries.Clear(); handles.Clear(); perception.Clear(); history.Clear(); latestDetailFrame = null;
            ObserverKey = TargetKey = previousObserverKey = null; Navigation = null;
            Results = new WorldQueryResult { Entities = Array.Empty<WorldEntitySnapshot>() };
            scopeController = 0; scopeFloor = null; scanning = complete = false; nextScan = 0; nextDetail = 0;
            pendingChecks = exportRemaining = 0; incidentCursor = 0; incidentFrames = Array.Empty<Dictionary<string, object>>(); counts.Clear();
            if (DetailOpen) CloseDetail();
        }
        /// <summary>
        /// Read one collection step and reconcile absent entities when a census finishes.
        /// Spreading the census across frames avoids an expensive full-world scan during one
        /// gameplay frame.
        /// </summary>
        private void ScanOne(GameController gc)
        {
            if (collection >= WorldNative.Collections.Length)
            {
                var remove = new List<int>();
                foreach (var pair in entries) if (pair.Value.Seen < generation - 1 ||
                    (pair.Value.Seen != generation && (pair.Value.Retired || pair.Value.Native == null || !WorldNative.Active(pair.Value.Native)))) remove.Add(pair.Key);
                foreach (int id in remove) Remove(id, "census-absent-or-inactive");
                scanning = false; complete = !censusMissingSource; nextScan = Time.time + Quality.Interval;
                Write("census", TraceWorld.Values("generation", generation, "entities", Index.Count,
                    "sourceCounts", new Dictionary<string, int>(counts), "complete", complete,
                    "coverage", "rolling-census-not-atomic; absent-handles-evicted-after-two-cycles", "errors", exceptions));
                return;
            }
            string name = WorldNative.Collections[collection];
            IList list = WorldNative.List(gc, name);
            if (list == null) { censusMissingSource = true; counts[name] = -1; collection++; cursor = 0; return; }
            counts[name] = list.Count;
            if (cursor >= list.Count) { collection++; cursor = 0; return; }
            var entity = list[cursor++] as Component;
            if (entity == null) return;
            Upsert(entity, name);
        }
        /// <summary>
        /// Refresh an observed entity, assigning a new activation identity when native reuse is
        /// detected.
        /// Pooled objects can reuse instances or IDs, so consumers need more than a native UID to
        /// identify a target.
        /// </summary>
        private Entry Upsert(Component entity, string source, bool lifecycleCore = false)
        {
            int instance = entity.GetInstanceID(), id = WorldNative.NativeId(entity);
            bool fresh = !entries.TryGetValue(instance, out var entry) || entry.Id != id || entry.Native != entity || (entry.Retired && WorldNative.Active(entity));
            if (fresh)
            {
                if (entry != null) { Index.Remove(entry.Key); handles.Remove(entry.Key); perception.Remove(entry.Key); }
                entry = new Entry { Native = entity, Id = id, Activation = ++activation };
                entries[instance] = entry;
            }
            entry.Seen = generation;
            var snapshot = WorldNative.Capture(entity, level, entry.Activation, source, lifecycleCore);
            entry.Retired = !snapshot.Active;
            if (!lifecycleCore) ProjectViews(snapshot);
            entry.Key = snapshot.Key; handles[entry.Key] = entity; Index.Put(snapshot);
            if (fresh) Write("entity-observed", WorldNative.Detach(snapshot), true);
            return entry;
        }
        private void Remove(int instance, string reason)
        {
            if (!entries.TryGetValue(instance, out var e)) return;
            Write("entity-removed", TraceWorld.Values("key", e.Key, "reason", reason), true);
            Index.Remove(e.Key); handles.Remove(e.Key); perception.Remove(e.Key); entries.Remove(instance);
        }
        internal bool LifecycleEnabled => Mode != AwarenessMode.Off && plugin.TestMode && scopeController != 0 && Ready(GameController.gameController);
        internal void AddHookWork(double cost)
        {
            frameWorkMs += cost; hookMs += cost;
            hookMaxMs = Math.Max(hookMaxMs, cost); if (cost > 1) hookOverBudget++;
        }
        /// <summary>
        /// Record native creation or retirement requests with immediate identity and bounded
        /// metadata.
        /// Heavy detail waits for the census, and a retirement request alone does not prove
        /// deactivation.
        /// </summary>
        internal void Lifecycle(Component entity, bool birth, string method, double lookupMs = 0)
        {
            if (!LifecycleEnabled || entity == null) { if (lookupMs > 0) AddHookWork(lookupMs); return; }
            var timer = Stopwatch.StartNew();
            try
            {
                // Before the first ready Tick there is no reliable level identity: label rather than assign to an old level.
                if (scopeController == 0) return;
                string countKey = entity.GetType().Name + (birth ? ":spawn" : ":retire-request");
                lifecycleCounts.TryGetValue(countKey, out long count); lifecycleCounts[countKey] = count + 1;
                int instance = entity.GetInstanceID(), id = WorldNative.NativeId(entity);
                if (birth)
                {
                    if (entries.TryGetValue(instance, out var known) && known.Id == id && !known.Retired) return;
                    // Essential identity/transient metadata stays immediate; actor/object detail and camera work
                    // are deferred to the cooperative census. Short-lived effect values are still captured.
                    var e = Upsert(entity, "lifecycle:" + method, true); deferredLifecycleDetails++;
                    Write("effect-created", TraceWorld.Values("key", e.Key, "nativeId", id, "instance", instance,
                        "type", entity.GetType().Name, "position", TraceWorld.Position(entity.transform.position), "method", method), true);
                }
                else
                {
                    entries.TryGetValue(instance, out var e);
                    Write("effect-retire-request", TraceWorld.Values("key", e?.Key, "nativeId", id, "instance", instance,
                        "type", entity.GetType().Name, "position", TraceWorld.Position(entity.transform.position),
                        "method", method, "coverage", "native-retirement-request-not-proof-of-deactivation"), true);
                    if (e != null && (method == "FakeSetInactive" || method == "PoolRecycle")) { e.Retired = true; Index.Remove(e.Key); handles.Remove(e.Key); perception.Remove(e.Key); }
                }
            }
            catch (Exception failure) { exceptions++; error = failure.Message; }
            finally
            {
                AddHookWork(timer.Elapsed.TotalMilliseconds + lookupMs);
            }
        }
        private void ProjectViews(WorldEntitySnapshot snapshot)
        {
            var views = new List<int>();
            foreach (var camera in cameras)
            {
                var point = camera.WorldToViewportPoint(new Vector3(snapshot.X, snapshot.Y, 0));
                if (WorldGeometry.InViewport(point.x, point.y, point.z)) views.Add(camera.GetInstanceID());
            }
            snapshot.ViewportCameraIds = cameras.Count == 0 ? null : views.ToArray();
            snapshot.ViewportCapturedAt = Time.time;
        }
        private Component Resolve(string key)
        {
            if (key == null || !handles.TryGetValue(key, out var entity) || entity == null) return null;
            var captured = Index.Get(key);
            return captured != null && WorldNative.NativeId(entity) == captured.NativeId ? entity : null;
        }
        // Read-only consumer boundary. Reject retired scopes and recycled native activations.
        /// <summary>
        /// Resolve a sampled key only while its exact activation is live in the current ready
        /// world.
        /// Consumers must cross this boundary before acting on observations that may already be
        /// stale.
        /// </summary>
        internal Component ResolveLive(string key)
        {
            if (Mode == AwarenessMode.Off || !Ready(GameController.gameController)) return null;
            var captured = key != null ? Index.Get(key) : null;
            var native = Resolve(key);
            return captured != null && captured.Level == level && captured.Active && !captured.Destroyed &&
                native != null && WorldNative.Active(native) &&
                entries.TryGetValue(native.GetInstanceID(), out var entry) && entry.Key == key ? native : null;
        }
        private void EnsureObserver(GameController gc)
        {
            if (Resolve(ObserverKey) is Agent) return;
            var preferred = plugin.Pets.Actor != null && !plugin.Pets.Actor.dead ? plugin.Pets.Actor : gc.playerAgent;
            if (preferred == null) return;
            if (entries.TryGetValue(preferred.GetInstanceID(), out var entry)) ObserverKey = entry.Key;
        }
        /// <summary>
        /// Choose the actor that anchors queries and discard perception from the previous observer.
        /// Line of sight and distance belong to a particular observer; reusing another actor's
        /// evidence would mislead.
        /// </summary>
        internal void SelectObserver(string key)
        { if (!(Resolve(key) is Agent)) return; ObserverKey = key; perception.Clear(); nextDetail = 0; Query.Offset = 0; }
        internal void SelectTarget(string key) { TargetKey = key; nextDetail = 0; }
        /// <summary>
        /// Refresh the query, selected perception, navigation and a bounded nearby snapshot.
        /// Observation times and coverage travel with the data so the UI and decisions can
        /// distinguish samples from current truth.
        /// </summary>
        private void RefreshDetails(GameController gc)
        {
            var observer = Resolve(ObserverKey) as Agent;
            if (observer?.tr == null) return;
            cameras = WorldPerception.Cameras(gc, out cameraSnapshots);
            var start = Stopwatch.StartNew();
            Query.X = observer.tr.position.x; Query.Y = observer.tr.position.y; Query.ObserverKey = ObserverKey;
            Results = Index.Query(Query); Results.Generation = generation; Results.CapturedAt = Time.time;
            Results.Complete = complete; Results.Coverage = "rolling-census; sampled positions; perception-progressive";
            queryMs += start.Elapsed.TotalMilliseconds;
            pendingChecks = Results.Entities.Length;
            if (perceptionCursor > int.MaxValue - 1000) perceptionCursor = 0;
            if ((DetailOpen || perceptionChecks < 32) && Resolve(TargetKey) is Component target && target != null)
            {
                perception[TargetKey] = WorldPerception.Capture(observer, target, ObserverKey, TargetKey, cameras);
                perceptionChecks++;
            }
            Navigation = WorldNative.Navigation(observer, Index, ObserverKey);
            var position = (Vector2)observer.tr.position;
            if (previousObserverKey != ObserverKey || Time.time < previousObserverTime || Vector2.Distance(position, previousObserver) > 0.05f)
                stationarySince = Time.time;
            Navigation["stationarySeconds"] = Time.time - stationarySince;
            Navigation["stationaryMeaning"] = "observed-low-displacement-not-a-stuck-verdict";
            previousObserver = position; previousObserverKey = ObserverKey; previousObserverTime = Time.time;
            var snapshots = new List<object>();
            var nearbyActors = Index.Query(new WorldQuery
            {
                X = position.x,
                Y = position.y,
                Kind = WorldKind.Actor,
                MaxDistance = 12,
                Limit = Quality.NearbyActorLimit
            });
            var tracked = new HashSet<string>();
            void Track(string key)
            {
                if (key == null || !tracked.Add(key) || !(Resolve(key) is Component native) || native == null) return;
                if (!entries.TryGetValue(native.GetInstanceID(), out var entry)) return;
                var snap = WorldNative.Capture(native, level, entry.Activation, "detail");
                ProjectViews(snap); Index.Put(snap); snapshots.Add(WorldNative.Detach(snap));
            }
            Track(ObserverKey); Track(TargetKey); foreach (var e in nearbyActors.Entities) Track(e.Key);
            var frame = TraceWorld.Values("gameTime", Time.time, "wallSeconds", Time.realtimeSinceStartup,
                "observer", ObserverKey, "target", TargetKey, "entities", snapshots, "navigation", Navigation,
                "tile", WorldNative.Tile(observer, observer.tr.position), "query", QueryData(),
                "selectedPerception", TargetKey != null && perception.TryGetValue(TargetKey, out var p) ? WorldPerception.Detach(p) : null,
                "quality", Quality.Level, "sampleInterval", Quality.Interval);
            latestDetailFrame = frame;
            if (Time.timeScale > 0)
            {
                history.Add(frame, CaptureWorker.Measure(frame, 8 * 1024 * 1024)); history.Prune(f => (float)f["gameTime"] < Time.time - 20);
                if (Quality.Level < 4 && !plugin.CaptureWriter.Saturated) Write("sample", frame);
            }
        }
        private object QueryData()
        {
            var matches = new List<object>(); foreach (var e in Results.Entities) matches.Add(WorldNative.Detach(e));
            return TraceWorld.Values("observer", ObserverKey, "origin", new[] { Query.X, Query.Y }, "range", Query.Range.ToString(),
                "min", Query.MinDistance, "max", Query.MaxDistance, "kind", Query.Kind?.ToString(), "tag", Query.Tag,
                "search", Query.Search, "cameraId", Query.CameraId, "anyViewport", Query.AnyViewport, "offset", Query.Offset, "limit", Query.Limit, "totalMatched", Results.TotalMatched,
                "checked", Results.CandidatesChecked, "generation", generation, "complete", complete,
                "coverage", Results.Coverage, "entities", matches);
        }
        /// <summary>
        /// Apply recording-mode rules before queuing a detached journal event.
        /// Explicit incidents and session metadata remain available even when continuous recording
        /// is disabled.
        /// </summary>
        private void Write(string kind, object data, bool essential = false)
        {
            bool metadata = kind == "session" || kind == "session-ended" || kind == "performance" || kind == "mode" ||
                kind == "benchmark-trial" || kind == "benchmark-start" || kind == "level" || kind == "scope-ended" || kind == "capture-error";
            bool explicitCapture = kind == "incident" || kind == "incident-frame" || kind == "census-page" || kind == "paused-capture";
            if (Mode == AwarenessMode.Record || metadata || explicitCapture)
                journal.Write(kind, level, Time.time, Time.realtimeSinceStartup, data, essential);
        }
        /// <summary>
        /// Queue the retained history and a paged world snapshot around a surprising event.
        /// Splitting the export into bounded records avoids serializing the whole incident during
        /// one gameplay frame.
        /// </summary>
        internal void Incident(string reason)
        {
            if (!plugin.TestMode) return;
            Write("incident", TraceWorld.Values("reason", reason, "observer", ObserverKey, "target", TargetKey,
                "historyFrames", history.Snapshot().Length, "historyEvicted", history.Evicted, "historyBytes", history.RetainedBytes, "query", QueryData(),
                "current", latestDetailFrame, "worldErrors", exceptions, "writerDrops", journal.Dropped, "workerDrops", plugin.CaptureWriter.Dropped), true);
            // Separate bounded records instead of one giant synchronous serialization/export.
            if (incidentCursor < incidentFrames.Length)
                Write("capture-error", TraceWorld.Values("reason", "previous-incident-export-superseded", "remainingFrames", incidentFrames.Length - incidentCursor), true);
            incidentFrames = history.Snapshot(); incidentCursor = 0;
            exportRemaining = Index.Count; exportOffset = 0;
        }
        /// <summary>
        /// Export the next bounded page of the sampled world index.
        /// Each page retains its rolling-snapshot qualification because the world can change
        /// between pages.
        /// </summary>
        private void ExportPage()
        {
            var result = Index.Query(new WorldQuery
            {
                Range = WorldRange.All,
                IncludeInactive = true,
                IncludeHelpers = true,
                Limit = 50,
                Offset = exportOffset
            });
            var values = new List<object>(); foreach (var e in result.Entities) values.Add(WorldNative.Detach(e));
            Write("census-page", TraceWorld.Values("offset", exportOffset, "entities", values, "generation", generation,
                "total", result.TotalMatched, "coverage", "paged-rolling-snapshot"), true);
            exportOffset += result.Entities.Length; exportRemaining = result.TotalMatched - exportOffset;
            if (result.Entities.Length == 0) exportRemaining = 0;
        }
        /// <summary>
        /// Change observation mode and reset the measurements associated with the old mode.
        /// Mixing costs or a running trial across mode changes would produce misleading
        /// comparisons.
        /// </summary>
        internal void SetMode(AwarenessMode mode)
        {
            SaveTrial(benchmark.Cancel(Time.realtimeSinceStartup, "mode-changed"));
            frameTimes.Clear(); workTimes.Clear(); frameWorkMs = 0; previousFrameLive = false;
            scanMs = queryMs = detailMs = overlayMs = hookMs = 0;
            hookMaxMs = 0; hookOverBudget = deferredLifecycleDetails = 0;
            allocatedAt = GC.GetTotalMemory(false); gc0 = GC.CollectionCount(0);
            Mode = mode; if (mode == AwarenessMode.Off && DetailOpen) CloseDetail();
            Write("mode", TraceWorld.Values("mode", mode.ToString()), true); nextScan = 0; nextDetail = 0;
        }
        /// <summary>
        /// Start the sequence of labeled benchmark modes for one scenario.
        /// Comparable trials need a declared scenario and consistent mode ordering, not unrelated
        /// frame-rate snapshots.
        /// </summary>
        internal void StartBenchmarkSuite(string scenario)
        {
            if (benchmark.Running || !Ready(GameController.gameController)) return;
            suiteScenario = scenario; suiteIndex = 0;
            StartBenchmark(scenario, AwarenessMode.Off);
        }
        /// <summary>
        /// Start one measured trial and remember its world, error and loss baselines.
        /// Later changes to those baselines can invalidate the trial rather than silently yielding
        /// a passing result.
        /// </summary>
        internal void StartBenchmark(string scenario, AwarenessMode mode)
        {
            if (!Ready(GameController.gameController)) return;
            SetMode(mode);
            if (DetailOpen) CloseDetail();
            trialDropped = plugin.CaptureWriter.Dropped + journal.Dropped; trialErrors = exceptions; trialLevel = level;
            benchmark.Start(scenario, mode.ToString(), Time.realtimeSinceStartup);
            Write("benchmark-start", TraceWorld.Values("scenario", scenario, "mode", mode.ToString(), "warmupSeconds", 10,
                "measurementSeconds", 60, "level", level), true);
            plugin.Notice("Benchmark: 10s warm-up then 60s capture. Play the chosen scenario without pausing.");
        }
        private void SaveTrial(Dictionary<string, object> trial)
        {
            if (trial == null) return;
            trial["level"] = level; trial["quality"] = Quality.Level; trial["entities"] = Index.Count;
            trial["workerDropped"] = plugin.CaptureWriter.Dropped; trial["worldDropped"] = journal.Dropped;
            var gc = GameController.gameController;
            trial["clockState"] = TraceWorld.Values("timeScale", Time.timeScale, "mainScale", gc?.mainTimeScale,
                "secondaryScale", gc?.secondaryTimeScale, "freezeFrame", gc?.freezeFrame,
                "freezeDisabled", gc?.freezeFramesDisabled, "justEndedFreeze", gc?.justEndedFreezeFrame,
                "nativeFreeze", NativeFreeze(gc), "inspectorOpen", DetailOpen, "phoneOpen", plugin.Phone.IsOpen,
                "appFocused", Application.isFocused);
            Write("benchmark-trial", trial, true);
            HarnessLastTrial = trial;
            bool valid = (bool)trial["valid"];
            plugin.Notice(valid ? "Benchmark captured: " + trial["mode"] + ". All 60 measurement seconds saved." : "Benchmark incomplete: " + trial["reason"]);
            if (suiteIndex < 0) return;
            if (!valid)
            {
                suiteIndex = -1;
                if ((string)trial["reason"] != "shutdown") SetMode(AwarenessMode.Record);
                return;
            }
            suiteIndex++;
            if (suiteIndex < 9) StartBenchmark(suiteScenario, (AwarenessMode)(suiteIndex % 3));
            else
            {
                suiteIndex = -1; SetMode(AwarenessMode.Record);
                plugin.Notice("Nine benchmark trials captured. Inspection restored to Record; offline comparison still required.");
            }
        }
        private static double Percentile(List<double> values, double fraction)
        { if (values.Count == 0) return 0; var sorted = values.ToArray(); Array.Sort(sorted); return sorted[(int)((sorted.Length - 1) * fraction)]; }
        /// <summary>
        /// Periodically publish measured costs, queues, quality and frame-tail statistics.
        /// Recording diagnostic overhead and loss alongside timings makes performance claims
        /// auditable.
        /// </summary>
        private void Stats()
        {
            if (Time.realtimeSinceStartup < nextStats) return;
            nextStats = Time.realtimeSinceStartup + 5;
            long memory = GC.GetTotalMemory(false); int collections0 = GC.CollectionCount(0);
            var views = new List<object>(); foreach (var c in cameraSnapshots)
                views.Add(TraceWorld.Values("camera", c.CameraId, "players", c.PlayerIds, "rect", c.Rect, "mapping", c.Mapping));
            int hitches = 0; double totalWork = 0;
            foreach (double value in frameTimes) if (value > 50) hitches++;
            foreach (double value in workTimes) totalWork += value;
            Write("performance", TraceWorld.Values("mode", Mode.ToString(), "paused", Time.timeScale <= 0 && !NativeFreeze(GameController.gameController),
                "nativeFreeze", NativeFreeze(GameController.gameController), "appFocused", Application.isFocused,
                "windowFrames", frameTimes.Count, "frameMedianMs", Percentile(frameTimes, 0.5), "frameP95Ms", Percentile(frameTimes, 0.95),
                "frameP99Ms", Percentile(frameTimes, 0.99), "framesOver50Ms", hitches,
                "diagnosticMeanMs", workTimes.Count > 0 ? totalWork / workTimes.Count : 0, "diagnosticP95Ms", Percentile(workTimes, 0.95),
                "registryMs", scanMs, "queryMs", queryMs, "detailMs", detailMs, "overlayMs", overlayMs, "hookMs", hookMs,
                "hookMaxMs", hookMaxMs, "hookOverBudget", hookOverBudget, "deferredLifecycleDetails", deferredLifecycleDetails,
                "qualityWindowMeanMs", Quality.WindowMeanMs, "qualityWindowFrames", Quality.WindowFrames,
                "qualityWindowOverBudget", Quality.WindowOverBudget, "qualityWindowWallSeconds", Quality.WindowCapturedAt,
                "serializationMsCumulative", journal.SerializationMs, "writesMsCumulative", journal.WriteMs,
                "writerWorkMsCumulative", plugin.CaptureWriter.WorkMs, "managedBytes", memory, "managedBytesDelta", memory - allocatedAt,
                "allocationCoverage", "retained-memory-delta-not-total-allocations", "gc0Delta", collections0 - gc0,
                "entities", Index.Count, "quality", Quality.Level, "qualityReason", Quality.Reason, "interval", Quality.Interval,
                "pendingPerception", pendingChecks, "queueDepth", plugin.CaptureWriter.Depth, "queueBytes", plugin.CaptureWriter.QueuedBytes,
                "workerDropped", plugin.CaptureWriter.Dropped, "worldDropped", journal.Dropped, "traceBytes", journal.Bytes,
                "captureErrors", exceptions, "error", error ?? journal.Failure ?? plugin.CaptureWriter.Failure,
                "cameras", views, "lifecycleCounts", new Dictionary<string, long>(lifecycleCounts)), true);
            allocatedAt = memory; gc0 = collections0; frameTimes.Clear(); workTimes.Clear();
            scanMs = queryMs = detailMs = overlayMs = hookMs = 0;
            hookMaxMs = 0; hookOverBudget = deferredLifecycleDetails = 0;
        }
        /// <summary>
        /// Cancel unfinished measurement and queue the session end and journal closure.
        /// Readers need explicit shutdown evidence to distinguish a completed stream from an abrupt
        /// exit.
        /// </summary>
        internal void Close()
        {
            if (closed) return; SaveTrial(benchmark.Cancel(Time.realtimeSinceStartup, "shutdown")); closed = true;
            Write("session-ended", TraceWorld.Values("errors", exceptions, "workerDrops", plugin.CaptureWriter.Dropped,
                "worldDrops", journal.Dropped), true); journal.Close();
        }
    }
}
