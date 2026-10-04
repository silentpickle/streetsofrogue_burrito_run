using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BurritoRun
{
    // Observation only. This service never issues an AI, movement or relationship command.
    internal sealed class BarkTrace
    {
        private readonly Plugin plugin;
        private readonly TraceStorage storage;
        private readonly TraceHistory<Dictionary<string, object>> history = new TraceHistory<Dictionary<string, object>>(101, 1024 * 1024);
        private readonly HashSet<int> responders = new HashSet<int>();
        private PetLevelScope scope;
        private bool scoped;
        private int level;
        private int nextAttempt;
        private int active;
        private int activeLevel;
        private Agent source;
        private Agent owner;
        private Agent target;
        private int sourceId;
        private int ownerId;
        private int targetId;
        private int targetInstance;
        private Noise noise;
        private int noiseId;
        private Vector3 start;
        private Vector3 origin;
        private float deadline;
        private float nextSample;
        private float lastSample;
        private float maxGap;
        private SampledApproach movement = new SampledApproach();
        private int samples;
        private bool targetReaction;
        private bool warningShown;
        private long sequence;
        private long droppedAtStart;
        private double maxCaptureMs;

        internal BarkTrace(Plugin plugin, string directory, string session)
        {
            this.plugin = plugin;
            var path = directory != null ? Path.Combine(directory, "trace-" +
                DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture) + "-" + session + ".jsonl") : null;
            storage = new TraceStorage(path, worker: plugin.CaptureWriter);
            Write("session", 0, TraceWorld.Values("path", path, "sampleIntervalSeconds", 0.2f,
                "historyCapacity", 101, "actorLimit", TraceWorld.ActorLimit, "radius", TraceWorld.Radius,
                "fileLimitBytes", TraceStorage.DefaultLimit, "testMode", plugin.TestMode));
            plugin.Log("BURRITO_TRACE path=" + (path ?? "unavailable"));
        }

        internal int Request(string trigger, Agent pet, Agent player, bool following, PetBarkPolicy policy)
        {
            int id = ++nextAttempt;
            Safely(() => RequestCore(id, trigger, pet, player, following, policy), id);
            return id;
        }

        private void RequestCore(int id, string trigger, Agent pet, Agent player, bool following, PetBarkPolicy policy)
        {
            RefreshScope();
            Write("requested", id, TraceWorld.Values("trigger", trigger, "following", following,
                "count", policy.Count, "levelCap", PetBarkPolicy.LevelLimit,
                "restReason", policy.RestReason(Time.time), "cooldownRemaining", policy.Remaining(Time.time),
                "minDistance", trigger == "guard-test" ? PetBarkPolicy.GuardMinDistance : 0f,
                "maxDistance", trigger == "guard-test" ? PetBarkPolicy.GuardMaxDistance : PetBarkPolicy.StrangerRadius));
            Write("request-state", id, TraceWorld.Values("source", TraceWorld.Actor(pet), "owner", TraceWorld.Actor(player)));
            Write("history", id, TraceWorld.Values("frames", history.Snapshot(), "evictedFrames", history.Evicted,
                "coverage", "sampled-neighborhood-not-continuous-world-state"));
            Write("environment", id, TraceWorld.Values("phase", "request", "world", TraceWorld.Environment(pet, null)));
        }

        internal void Candidates(int id, List<object> candidates, Dictionary<string, int> reasons, int omitted, int total)
        {
            Write("candidates", id, TraceWorld.Values("actors", candidates, "reasonCounts", reasons,
                "omitted", omitted, "totalScanned", total, "captureLimit", 32,
                "captureOrder", "native-list-order-within-radius",
                "nativeDecision", "unknown-selection-reasons-are-plugin-rules"));
        }

        internal void AutomaticState(string reason, PetBarkPolicy policy)
        {
            Write("automatic-state", 0, TraceWorld.Values("reason", reason, "count", policy.Count,
            "cooldownRemaining", policy.Remaining(Time.time), "levelCap", PetBarkPolicy.LevelLimit));
        }

        internal void CaptureError(int id, string phase, Exception error)
        { Write("capture-error", id, TraceWorld.Values("phase", phase, "error", error.GetType().Name + ": " + error.Message), true); }

        internal void Selected(int id, Agent selected, float volume)
        { Safely(() => Write("selected", id, TraceWorld.Values("target", TraceWorld.Actor(selected), "volume", volume)), id); }

        internal void AttemptFailed(int id, Exception error)
        {
            CaptureError(id, "native-emission", error);
            if (active == id) End("INCONCLUSIVE", "native-emission-exception");
            else Write("terminal", id, TraceWorld.Values("status", "INCONCLUSIVE",
                "reason", "native-emission-exception", "emissionState", "unknown-native-call-may-be-partial"), true);
        }

        internal void Reject(int id, string reason)
        {
            Write("terminal", id, TraceWorld.Values("status", "SKIPPED", "reason", reason,
                "emitted", false, "droppedRecords", storage.Dropped, "writerFailed", storage.Failed), true);
        }

        internal void Emitted(int id, Agent pet, Agent player, Agent selected, Noise emitted, float volume, bool audioRequested)
        { Safely(() => EmittedCore(id, pet, player, selected, emitted, volume, audioRequested)); }

        private void EmittedCore(int id, Agent pet, Agent player, Agent selected, Noise emitted, float volume, bool audioRequested)
        {
            Interrupt("superseded");
            active = id;
            activeLevel = level;
            source = pet;
            owner = player;
            target = selected;
            sourceId = pet.UID;
            ownerId = player.UID;
            targetId = selected.UID;
            targetInstance = selected.GetInstanceID();
            origin = pet.tr.position;
            start = selected.tr.position;
            noise = emitted;
            noiseId = emitted != null ? emitted.noiseID : -1;
            deadline = Time.time + 8f;
            lastSample = Time.time;
            samples = 0;
            maxGap = 0;
            movement = new SampledApproach();
            targetReaction = false;
            maxCaptureMs = 0;
            droppedAtStart = storage.Dropped;
            responders.Clear();
            Write("emitted", id, TraceWorld.Values("source", TraceWorld.Actor(pet),
                "owner", TraceWorld.Actor(player), "target", TraceWorld.Actor(selected, pet),
                "noiseId", noiseId, "noiseInstance", emitted != null ? (object)emitted.GetInstanceID() : null,
                "spawned", emitted != null, "volume", volume,
                "radius", emitted != null ? (object)emitted.noiseDistance : null,
                "noiseTimeLeft", emitted != null ? (object)emitted.timeLeft : null,
                "noiseInitialPause", emitted != null ? (object)emitted.initialPause : null,
                "audioPlaybackRequested", audioRequested, "audibleOutput", "unverified",
                "startDistance", Vector2.Distance(origin, start), "observationSeconds", 8f));
            Write("environment", id, TraceWorld.Values("phase", "emission", "world", TraceWorld.Environment(pet, selected)));
            if (emitted == null) { End("FAIL", "noise-spawn-returned-null"); return; }
            Capture();
            nextSample = Time.time + 0.2f;
        }

        internal void Reaction(GoalNoiseReact goal)
        { Safely(() => ReactionCore(goal)); }

        private void ReactionCore(GoalNoiseReact goal)
        {
            if (active == 0 || noise == null || noise.noiseID != noiseId ||
                goal.noise != noise || goal.agent == null || goal.agent.UID == sourceId ||
                Time.time > deadline || !responders.Add(goal.agent.UID)) return;
            if (goal.agent == target && target.UID == targetId && target.GetInstanceID() == targetInstance)
                targetReaction = true;
            Write("reaction", active, TraceWorld.Values("actor", TraceWorld.Actor(goal.agent, source),
                "noiseId", noiseId, "noisePosition", TraceWorld.Position(goal.noisePosition),
                "matchingNativeActivation", true, "targetReaction", targetReaction));
        }

        internal void Tick()
        { Safely(TickCore); }

        private void TickCore()
        {
            if (!plugin.TestMode) return;
            RefreshScope();
            if (active != 0)
            {
                string invalid = InvalidReason();
                if (invalid != null) { End("INTERRUPTED", invalid); return; }
                if (Time.time >= deadline)
                {
                    Capture();
                    bool measured = PetBarkPolicy.UsefulApproach(targetReaction, movement.Displacement, movement.Approach);
                    End(measured ? "PASS" : "INCONCLUSIVE", "window-complete");
                    return;
                }
            }
            if (!scoped || Time.timeScale <= 0 || Time.time < nextSample) return;
            nextSample = Time.time + 0.2f;
            Capture();
        }

        private string InvalidReason()
        {
            if (Time.timeScale <= 0) return "game-paused";
            if (source == null || source.UID != sourceId || source.dead || source.knockedOut) return "source-lost-or-incapacitated";
            if (owner == null || owner.UID != ownerId || owner.dead || owner.ghost) return "owner-lost-or-incapacitated";
            if (target == null || target.UID != targetId || target.GetInstanceID() != targetInstance ||
                target.initializingForPool || !target.gameObject.activeInHierarchy || target.dead || target.knockedOut)
                return "target-lost-or-incapacitated";
            if (source.inCombat || owner.inCombat || target.inCombat) return "combat-started";
            if (noise == null) return "noise-object-lost";
            if (noise.noiseID != noiseId) return "pooled-noise-reused";
            if (source.gc != GameController.gameController || target.gc != source.gc) return "actor-controller-changed";
            return null;
        }

        private void Capture()
        {
            var timer = Stopwatch.StartNew();
            var frame = TraceWorld.Frame(active != 0 ? source : plugin.Pets?.Actor,
                active != 0 ? owner : GameController.gameController?.playerAgent, active != 0 ? target : null);
            if (active != 0)
            {
                float gap = Time.time - lastSample;
                maxGap = Mathf.Max(maxGap, gap);
                lastSample = Time.time;
                samples++;
                var root = target.brain?.Goals != null && target.brain.Goals.Count > 0 ? target.brain.Goals[0] : null;
                bool matches = root is GoalNoiseReact reaction && reaction.noise == noise && noise.noiseID == noiseId;
                float currentDisplacement = Vector2.Distance(start, target.tr.position);
                float currentApproach = Vector2.Distance(start, origin) - Vector2.Distance(target.tr.position, origin);
                movement.Sample(matches && targetReaction, Time.time, target.tr.position.x,
                    target.tr.position.y, origin.x, origin.y);
                frame.Add("targetRootMatchesNoise", matches);
                frame.Add("matchingSegment", matches && targetReaction ? (object)movement.Segment : null);
                frame.Add("noiseIdObserved", noise.noiseID);
                frame.Add("noiseInstanceObserved", noise.GetInstanceID());
                frame.Add("noiseTimeLeft", noise.timeLeft);
                frame.Add("noiseState", noise.state);
                frame.Add("noiseActive", noise.gameObject.activeInHierarchy);
                frame.Add("nativeAgentsHeard", noise.numAgentsHeard);
                frame.Add("targetDisplacement", currentDisplacement);
                frame.Add("targetApproach", currentApproach);
                frame.Add("targetDistanceFromBark", Vector2.Distance(target.tr.position, origin));
                frame.Add("sampleGapSeconds", gap);
            }
            timer.Stop();
            frame.Add("captureMs", timer.Elapsed.TotalMilliseconds);
            maxCaptureMs = Math.Max(maxCaptureMs, timer.Elapsed.TotalMilliseconds);
            history.Add(frame, CaptureWorker.Measure(frame, 1024 * 1024));
            history.Prune(value => (float)value["gameTime"] < Time.time - 20f);
            if (active != 0) Write("sample", active, frame);
        }

        private void RefreshScope()
        {
            var gc = GameController.gameController;
            bool ready = gc != null && gc.loadComplete && !gc.returningToMainMenu && !gc.levelTransitioning;
            if (!ready)
            {
                if (scoped) { Interrupt("level-unavailable"); history.Clear(); scoped = false; }
                return;
            }
            var live = new PetLevelScope(gc.GetInstanceID(), SceneManager.GetActiveScene().handle,
                gc.agentsNest != null ? gc.agentsNest.GetInstanceID() : 0, gc.seedNum, gc.floorNameNum, gc.timeSinceLoad);
            var reason = scoped ? PetLevelScope.ResetReason(scope, live, true, false, false, true) : "first-ready-level";
            if (reason != null)
            {
                Interrupt(reason);
                history.Clear();
                scoped = true;
                scope = live;
                level++;
                nextSample = Time.time;
                Write("level", 0, TraceWorld.Values("reason", reason));
            }
            scope = live;
        }

        internal void Interrupt(string reason)
        { if (active != 0) End("INTERRUPTED", reason); }

        private void End(string status, string reason)
        {
            if (active == 0) return;
            Write("environment", active, TraceWorld.Values("phase", "end", "world", TraceWorld.Environment(source, target)));
            // Positive measurements must not advertise a complete evidence record after a write gap.
            if (status == "PASS" && (storage.Failed || storage.Limited || storage.Dropped > 0))
                status = "INCONCLUSIVE";
            Write("terminal", active, TraceWorld.Values("status", status, "reason", reason,
                "assertion", "matching-native-goal-approach", "attemptLevelId", activeLevel,
                "nativeResponders", responders.Count, "targetReaction", targetReaction,
                "maxMatchingDisplacement", movement.Displacement, "maxMatchingApproach", movement.Approach,
                "approached", PetBarkPolicy.UsefulApproach(targetReaction, movement.Displacement, movement.Approach),
                "samples", samples, "matchingGoalSamples", movement.MatchingSamples, "maxSampleGapSeconds", maxGap,
                "maxCaptureMs", maxCaptureMs, "droppedDuringObservation", storage.Dropped - droppedAtStart,
                "droppedRecords", storage.Dropped, "fileLimited", storage.Limited, "writerFailed", storage.Failed,
                "movementCoverage", "consecutive-sampled-root-goals-not-continuous-proof",
                "maxMatchingGapSeconds", SampledApproach.MaxGap, "usefulDistraction", "unverified",
                "nativeNonresponseReason", "unknown"), true);
            plugin.Log("BURRITO_TRACE_RESULT attempt=" + active + " status=" + status + " reason=" + reason +
                " reaction=" + targetReaction + " matchingSamples=" + movement.MatchingSamples +
                " displacement=" + movement.Displacement.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) +
                " approach=" + movement.Approach.ToString("F3", System.Globalization.CultureInfo.InvariantCulture));
            plugin.Pets?.Barks.TraceFinished(status, reason, movement.Approach);
            active = 0;
            source = null;
            owner = null;
            target = null;
            noise = null;
        }

        internal void Incident(string reason)
        { Safely(() => IncidentCore(reason)); }

        private void IncidentCore(string reason)
        {
            Write("incident", active, TraceWorld.Values("reason", reason, "frames", history.Snapshot(),
                "evictedFrames", history.Evicted, "historyBytes", history.RetainedBytes,
                "world", TraceWorld.Environment(plugin.Pets?.Actor, target)), true);
        }

        private void Write(string kind, int attempt, Dictionary<string, object> data, bool terminal = false)
        {
            if (data.TryGetValue("frames", out var raw) && raw is Dictionary<string, object>[] frames && frames.Length > 8)
            {
                for (int offset = 0; offset < frames.Length; offset += 8)
                {
                    int length = Math.Min(8, frames.Length - offset);
                    var page = new Dictionary<string, object>(data);
                    var chunk = new Dictionary<string, object>[length]; Array.Copy(frames, offset, chunk, 0, length);
                    page["frames"] = chunk; page["frameOffset"] = offset; page["totalFrames"] = frames.Length;
                    Write(kind, attempt, page, terminal);
                }
                return;
            }
            var record = TraceWorld.Values("schema", 1, "seq", ++sequence, "event", kind,
                "attempt", attempt, "session", plugin.Diagnostics.Session, "build", Plugin.BuildLabel,
                "utc", DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
                "gameTime", Time.time, "wallSeconds", Time.realtimeSinceStartup, "timeScale", Time.timeScale,
                "levelId", level, "scenario", plugin.Scenarios?.Name, "scenarioRun", plugin.Scenarios?.RunNumber,
                "scope", TraceWorld.Values("ready", scoped, "controller", scope.ControllerId,
                    "scene", scope.SceneHandle, "nest", scope.AgentsNestId, "seed", scope.Seed,
                    "floor", scope.Floor, "loadClock", scope.TimeSinceLoad), "data", data);
            if (!storage.Write(record, terminal) && !warningShown)
            {
                warningShown = true;
                plugin.Warn("Diagnostic trace incomplete: " + (storage.Failure ?? "file cap reached or path unavailable"));
                plugin.Log("BURRITO_TRACE_INCOMPLETE dropped=" + storage.Dropped);
            }
        }

        private void Safely(Action capture, int? attempt = null)
        {
            try { capture(); }
            catch (Exception error)
            {
                plugin.Warn("Trace capture incomplete: " + error.Message);
                Write("capture-error", attempt ?? active, TraceWorld.Values("error", error.GetType().Name + ": " + error.Message), true);
                if (active != 0 && (!attempt.HasValue || attempt.Value == active)) End("INCONCLUSIVE", "capture-error");
            }
        }
    }
}
