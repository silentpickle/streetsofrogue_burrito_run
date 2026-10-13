using System;
using System.Collections.Generic;
using UnityEngine;

namespace BurritoRun
{
    // Test orchestration only: native writes stay in WorldFixture. Never feeds the index or quality policy.
    internal sealed class WorldReadiness
    {
        private readonly Plugin plugin;
        private readonly string mode;
        private Agent player;
        private int level;
        private Vector3 origin;
        private float health;
        private bool focused;
        private readonly List<WorldFixture> crowd = new List<WorldFixture>();
        private readonly List<Vector3> homes = new List<Vector3>();
        private readonly List<PlayfieldObject> objects = new List<PlayfieldObject>();
        private readonly List<int> objectIds = new List<int>();
        private readonly List<PlayfieldObject> retired = new List<PlayfieldObject>();
        private readonly List<int> retiredIds = new List<int>();
        private readonly List<string> oldKeys = new List<string>();
        private string[] oldWorldKeys = Array.Empty<string>();
        private string stage = "settle";
        private float deadline;
        private float due;
        private float nextSample;
        private Action releaseWriter;
        private int emissions;
        private int trial;
        private float trialStart;
        private int pulses;
        private int commands;
        private float nextMove;
        private float nextPulse;
        private float movement;
        private readonly List<Vector3> previous = new List<Vector3>();
        internal string Status { get; private set; }
        internal string Reason { get; private set; }
        internal WorldReadiness(Plugin plugin, string mode, Agent player)
        {
            this.plugin = plugin; this.mode = mode; this.player = player;
            level = plugin.Awareness.HarnessLevel; origin = player.tr.position; health = player.health; focused = Application.isFocused;
            deadline = Time.realtimeSinceStartup + 45; due = Time.realtimeSinceStartup + 2;
            Record("start", TraceWorld.Values("mode", mode, "context", Context()));
            if (mode != "native")
            {
                try
                {
                    for (int i = 0; i < 4; i++)
                    {
                        var fixture = new WorldFixture();
                        if (!fixture.Spawn(player)) throw new InvalidOperationException("No safe nearby crowd fixture spot");
                        crowd.Add(fixture); homes.Add(fixture.Actor.tr.position); previous.Add(fixture.Actor.tr.position);
                    }
                }
                catch { Close(); throw; }
            }
        }
        private object Context() => plugin.Awareness.HarnessPerformanceContext(player);
        private object Evidence() => plugin.Awareness.HarnessPressureEvidence();
        private void Record(string name, Dictionary<string, object> data) => plugin.Awareness.HarnessRecord("readiness-" + name, data);
        private void Stop(string status, string reason)
        { Status = status; Reason = reason; Record("ended", TraceWorld.Values("mode", mode, "status", status, "reason", reason)); }
        internal void Tick()
        {
            if (Status != null) return;
            if (Time.realtimeSinceStartup >= deadline) { Stop("INCONCLUSIVE", "readiness-timeout-" + stage); return; }
            if (!plugin.Awareness.HarnessCaptureClean) { Stop("FAIL", "readiness-capture-loss-or-error"); return; }
            var gc = GameController.gameController;
            if (stage == "reload")
            {
                WorldFixture.AcceptReload(gc);
                if (!WorldFixture.Ready(gc) || plugin.Awareness.HarnessLevel == level || Time.realtimeSinceStartup < due) return;
                var stale = new List<string>();
                foreach (var key in oldKeys) if (plugin.Awareness.Index.Get(key) != null) stale.Add(key);
                var staleWorld = new List<string>();
                foreach (var key in oldWorldKeys) if (plugin.Awareness.Index.Get(key) != null) staleWorld.Add(key);
                Record("reloaded", TraceWorld.Values("oldLevel", level, "newLevel", plugin.Awareness.HarnessLevel,
                    "oldKeys", oldKeys.ToArray(), "staleKeys", stale.ToArray(), "oldWorldKeys", oldWorldKeys, "staleWorldKeys", staleWorld.ToArray(), "context", plugin.Awareness.HarnessPerformanceContext(gc.playerAgent)));
                Stop(stale.Count == 0 && staleWorld.Count == 0 ? "COMPLETE" : "FAIL", "native-scope-reload-captured"); return;
            }
            if (!WorldFixture.Ready(gc) || gc.playerAgent != player || plugin.Awareness.HarnessLevel != level)
            { Stop("INTERRUPTED", "readiness-world-or-player-changed"); return; }
            if (stage != "ui-open" && stage != "ui-closed" && Time.timeScale <= 0 && !plugin.Awareness.HarnessNativeFreeze)
            { Stop("INTERRUPTED", "readiness-paused"); return; }
            if (Vector3.Distance(player.tr.position, origin) > .05f || player.health != health || player.inCombat || Application.isFocused != focused)
            {
                Record("interrupted", TraceWorld.Values("context", Context(), "positionDelta", Vector3.Distance(player.tr.position, origin)));
                Stop("INTERRUPTED", "readiness-player-health-combat-or-focus-changed"); return;
            }
            foreach (var fixture in crowd)
                if (stage != "cleanup" && (!fixture.OwnedActive || fixture.Actor.dead || fixture.Actor.inCombat))
                { Stop("INTERRUPTED", "readiness-crowd-lost-or-in-combat"); return; }
            if (stage == "settle")
            {
                if (Time.realtimeSinceStartup < due || plugin.Awareness.Quality.Level != 0) return;
                if (mode == "native") { SetupObjects(); return; }
                var frame = plugin.Awareness.HarnessDetailFrame;
                if (frame == null || (int)frame["quality"] != 0 || ActorCount(frame) < 3) return;
                Record("crowd-baseline", TraceWorld.Values("frame", frame, "context", Context()));
                if (mode == "busy") StartTrial();
                else { stage = "window"; emissions = 0; deadline = Time.realtimeSinceStartup + 12; }
            }
            else if (stage == "window" || stage == "backpressure")
            {
                var quality = plugin.Awareness.Quality;
                if (quality.Level > 0)
                {
                    if (stage == "window" && quality.Reason != "window-budget")
                    { Record("other-reduction", TraceWorld.Values("evidence", Evidence(), "emitted", emissions)); Stop("INCONCLUSIVE", "window-probe-hit-" + quality.Reason); return; }
                    Record(stage + "-reduced", TraceWorld.Values("evidence", Evidence(), "emitted", emissions));
                    releaseWriter?.Invoke();
                    stage = stage == "window" ? "window-recover" : "backpressure-recover";
                    due = Time.realtimeSinceStartup + 30; deadline = Time.realtimeSinceStartup + 150;
                    return;
                }
                if (emissions >= (stage == "window" ? 4800 : 1024))
                { Stop("INCONCLUSIVE", "bounded-" + stage + "-did-not-reduce"); return; }
                if (Time.realtimeSinceStartup >= nextSample)
                { Record("load-sample", TraceWorld.Values("stage", stage, "evidence", Evidence(), "emitted", emissions)); nextSample = Time.realtimeSinceStartup + .25f; }
                // Real native emissions; no synthetic costs or quality-state writes.
                // Pace the sustained probe after indivisible-call spikes rather than manufacturing a burst.
                if (stage == "window" && plugin.Awareness.HarnessFrameWorkMs > 2) return;
                int count = 8;
                crowd[0].EmitPressure(player, (noise, birth) => { }, count); emissions += count;
            }
            else if (stage == "window-recover" || stage == "backpressure-recover")
            {
                var frame = plugin.Awareness.HarnessDetailFrame;
                if (Time.realtimeSinceStartup >= nextSample)
                { Record("sample", TraceWorld.Values("stage", stage, "evidence", Evidence(), "frame", frame)); nextSample = Time.realtimeSinceStartup + 1; }
                if (Time.realtimeSinceStartup < due || plugin.Awareness.Quality.Level != 0 || frame == null ||
                    (int)frame["quality"] != 0 || ActorCount(frame) < 3) return;
                Record(stage + "ed", TraceWorld.Values("evidence", Evidence(), "frame", frame, "context", Context()));
                if (stage == "window-recover")
                {
                    Record("slow-writer", TraceWorld.Values("milliseconds", 2000, "scope", "labeled test-only writer fault, at most 2000 ms, released on first trigger; not performance evidence"));
                    releaseWriter = WorldFixture.SlowWriter(plugin.CaptureWriter);
                    stage = "backpressure"; emissions = 0; deadline = Time.realtimeSinceStartup + 10;
                }
                else BeginCleanup();
            }
            else if (stage == "busy") TickBusy();
            else if (stage == "objects") ReadObjects();
            else if (stage == "doors") ReadDoors();
            else if (stage == "ui-open")
            {
                if (Time.realtimeSinceStartup < due) return;
                Record("ui-paused", TraceWorld.Values("open", plugin.Awareness.IsPausedDetail, "blocked", plugin.BlocksWorldInput,
                    "timeScale", Time.timeScale, "player", plugin.Awareness.HarnessStateRead(player, player)));
                plugin.Awareness.HarnessCloseDetail(); stage = "ui-closed"; due = Time.realtimeSinceStartup + .5f;
            }
            else if (stage == "ui-closed")
            {
                if (Time.realtimeSinceStartup < due) return;
                Record("ui-resumed", TraceWorld.Values("open", plugin.Awareness.IsPausedDetail, "blocked", plugin.BlocksWorldInput,
                    "timeScale", Time.timeScale, "player", plugin.Awareness.HarnessStateRead(player, player)));
                BeginCleanup();
            }
            else if (stage == "cleanup")
            {
                foreach (var key in oldKeys) if (plugin.Awareness.Index.Get(key) is WorldEntitySnapshot e && e.Active && !e.Destroyed) return;
                var retirement = new List<object>();
                for (int i = 0; i < retired.Count; i++)
                {
                    var entity = retired[i];
                    bool same = entity != null && entity.UID == retiredIds[i];
                    bool active = same && WorldNative.Active(entity);
                    bool destroyed = same && entity is ObjectReal obj && obj.destroyed;
                    if (active && !destroyed) return;
                    retirement.Add(TraceWorld.Values("uid", retiredIds[i], "sameIdentity", same,
                        "nativeActive", active, "nativeDestroyed", destroyed, "nativeLive", active && !destroyed));
                }
                Record("cleanup", TraceWorld.Values("keys", oldKeys.ToArray(), "activeKeys", Array.Empty<string>(),
                    "nativeLive", false, "retirement", retirement, "ownedCount", retired.Count));
                if (mode == "native")
                {
                    oldWorldKeys = plugin.Awareness.HarnessLiveKeys();
                    Record("reload-request", TraceWorld.Values("oldLevel", level, "oldKeys", oldKeys.ToArray(), "oldWorldKeys", oldWorldKeys));
                    WorldFixture.Reload(gc); stage = "reload"; due = Time.realtimeSinceStartup + 5; deadline = Time.realtimeSinceStartup + 120;
                }
                else Stop("COMPLETE", "readiness-" + mode + "-captured; offline-assertions-required");
            }
        }
        private static int ActorCount(Dictionary<string, object> frame)
        {
            int count = 0;
            if (frame["entities"] is List<object> entities)
                foreach (var value in entities)
                    if (value is Dictionary<string, object> e && (string)e["kind"] == "Actor") count++;
            return count;
        }
        private void StartTrial()
        {
            trial++; pulses = commands = 0; movement = 0; trialStart = Time.realtimeSinceStartup;
            nextPulse = trialStart + 10.05f; nextMove = trialStart + .5f;
            for (int i = 0; i < crowd.Count; i++) previous[i] = crowd[i].Actor.tr.position;
            var modeName = (AwarenessMode)((trial - 1) % 3);
            Record("busy-start", TraceWorld.Values("trial", trial, "mode", modeName.ToString(), "context", Context(), "actors", ActorStates()));
            plugin.Awareness.StartBenchmark("harness-controlled-busy", modeName);
            stage = "busy"; deadline = trialStart + 85;
        }
        private object ActorStates()
        {
            var states = new List<object>();
            foreach (var f in crowd)
                states.Add(TraceWorld.Values("state", plugin.Awareness.HarnessStateRead(player, f.Actor),
                    "navigation", WorldNative.Navigation(f.Actor, plugin.Awareness.Index, plugin.Awareness.HarnessSnapshot(f.Actor)?.Key)));
            return states;
        }
        private void TickBusy()
        {
            for (int i = 0; i < crowd.Count; i++)
            { var current = crowd[i].Actor.tr.position; movement += Vector3.Distance(current, previous[i]); previous[i] = current; }
            if (!plugin.Awareness.HarnessBenchmarkRunning)
            {
                var result = plugin.Awareness.HarnessLastTrial;
                Record("busy-trial", TraceWorld.Values("trial", trial, "result", result, "pulses", pulses,
                    "noises", pulses * 4, "projectiles", pulses, "commands", commands, "movement", movement,
                    "context", Context(), "actors", ActorStates()));
                if (result == null || !(bool)result["valid"] || pulses != 300 || movement <= .1f)
                { Stop("INCONCLUSIVE", "busy-trial-invalid-or-native-activity-missed"); return; }
                if (trial < 9) StartTrial(); else { plugin.Awareness.SetMode(AwarenessMode.Record); BeginCleanup(); }
                return;
            }
            if (Time.realtimeSinceStartup >= nextMove)
            {
                for (int i = 0; i < crowd.Count; i++)
                {
                    var actor = crowd[i].Actor;
                    var target = homes[i] + (commands % 2 == 0 ? Vector3.right : Vector3.left) * .8f;
                    if (player.gc.tileInfo.PossibleToWalk(actor, target) && player.gc.tileInfo.SafeToWalk(actor, target) &&
                        actor.movement.HasLOSPosition360(target)) crowd[i].Move(player, target);
                }
                commands++; nextMove += 4;
            }
            if (pulses < 300 && Time.realtimeSinceStartup >= nextPulse)
            {
                crowd[0].EmitPressure(player, (noise, birth) => { }, 4);
                WorldFixture.Projectile(crowd[0].Actor);
                pulses++; nextPulse += .2f;
            }
        }
        private void SetupObjects()
        {
            var finder = new WorldFixture();
            if (!finder.Spawn(player)) throw new InvalidOperationException("No object fixture spot");
            crowd.Add(finder);
            foreach (string name in new[] { Names.Food, "FireHydrant", "Door" })
            {
                var entity = WorldFixture.Object(player, name, WorldFixture.ObjectSpot(player, objects));
                if (entity == null) throw new InvalidOperationException("Native object fixture missing: " + name);
                objects.Add(entity); objectIds.Add(entity.UID);
            }
            stage = "objects"; due = Time.realtimeSinceStartup + 2; deadline = Time.realtimeSinceStartup + 20;
        }
        private void ReadObjects()
        {
            if (Time.realtimeSinceStartup < due) return;
            var snapshots = new List<object>();
            foreach (var entity in objects)
            {
                var e = plugin.Awareness.HarnessSnapshot(entity);
                if (e == null || e.Collection.StartsWith("lifecycle:", StringComparison.Ordinal) || Time.time - e.GameTime > 1) return;
                snapshots.Add(WorldNative.Detach(e));
            }
            Record("objects", TraceWorld.Values("entities", snapshots));
            stage = "doors"; emissions = 0;
        }
        private void ReadDoors()
        {
            var door = objects[2] as Door;
            if (door == null) throw new InvalidOperationException("Native Door fixture type mismatch");
            bool open = emissions == 1, locked = emissions == 2;
            WorldFixture.SetDoor(door, open, locked);
            var before = plugin.Awareness.HarnessStateRead(player, crowd[0].Actor);
            var current = plugin.Awareness.HarnessSnapshot(door);
            var read = WorldNative.Capture(door, level, current.Activation, "harness-read-only");
            Record("door-state", TraceWorld.Values("state", emissions, "native", WorldNative.Detach(read),
                "before", before, "after", plugin.Awareness.HarnessStateRead(player, crowd[0].Actor),
                "scope", "owned native state-field fixture; no collider/reachability claim"));
            if (++emissions < 3) return;
            Record("ui-before", TraceWorld.Values("player", plugin.Awareness.HarnessStateRead(player, player)));
            plugin.Awareness.ToggleDetail(); stage = "ui-open"; due = Time.realtimeSinceStartup + .5f;
        }
        private void BeginCleanup()
        {
            oldKeys.Clear(); retired.Clear(); retiredIds.Clear();
            foreach (var f in crowd)
            { retired.Add(f.Actor); retiredIds.Add(f.Actor.UID); var e = plugin.Awareness.HarnessSnapshot(f.Actor); if (e != null) oldKeys.Add(e.Key); }
            foreach (var entity in objects)
            { retired.Add(entity); retiredIds.Add(entity.UID); var e = plugin.Awareness.HarnessSnapshot(entity); if (e != null) oldKeys.Add(e.Key); }
            Close(); stage = "cleanup"; deadline = Time.realtimeSinceStartup + 20;
        }
        internal void Close()
        {
            releaseWriter?.Invoke();
            if (plugin.Awareness.IsPausedDetail) plugin.Awareness.HarnessCloseDetail();
            foreach (var f in crowd) f.Remove();
            for (int i = 0; i < objects.Count; i++)
                WorldFixture.RemoveObject(objects[i], objectIds[i]);
        }
    }
}
