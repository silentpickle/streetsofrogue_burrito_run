using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace BurritoRun
{
    // A bounded orchestration loop, not a simulation or behavioral feature.
    internal sealed class WorldHarness
    {
        private enum Phase { Waiting, Discover, Retire, Performance, Pressure, Recovery, PressureRetire, Readiness, Finished }
        private readonly Plugin plugin;
        private readonly WorldFixture fixture = new WorldFixture();
        private Phase phase;
        private GameController controller;
        private Agent player;
        private Agent retiredActor;
        private int level;
        private int repeat;
        private string key;
        private float deadline;
        private float settleUntil;
        private float quitAt;
        private float readySince;
        private int trial;
        private Vector3 stationaryPosition;
        private float stationaryHealth;
        private bool stationaryFocus;
        private int batches;
        private float pressureStoppedAt;
        private float nextPressureSample;
        private bool detailRecorded;
        private bool pressureReduced;
        private WorldReadiness readiness;
        private PetFoodHarness foodHarness;
        private NpcProbeHarness npcHarness;
        private NpcDesireHarness desireHarness;
        private QuestHarness questHarness;
        private BananaEncounter startupEncounter;
        private PetHydrantHarness hydrantHarness;
        internal string Extension { get; }
        internal bool Pressure { get; }
        internal bool Performance { get; }
        internal bool Enabled { get; }

        internal WorldHarness(Plugin plugin)
        {
            this.plugin = plugin;
            Enabled = plugin.TestMode && Array.IndexOf(Environment.GetCommandLineArgs(), "--world-harness") >= 0;
            Performance = Enabled && Array.IndexOf(Environment.GetCommandLineArgs(), "--world-performance") >= 0;
            Pressure = Enabled && Array.IndexOf(Environment.GetCommandLineArgs(), "--world-pressure") >= 0;
            foreach (var mode in new[] { "advanced", "busy", "native", "food", "hydrant", "npc-probe", "npc-desires", "banana-quest" })
                if (Enabled && Array.IndexOf(Environment.GetCommandLineArgs(), "--world-" + mode) >= 0) Extension = mode;
            if (!Enabled) return;
            deadline = Time.realtimeSinceStartup + 120;
            // Running native menu/start mutations is allowed only in this command's disposable profile.
            string profile = Environment.GetEnvironmentVariable("BURRITO_PROFILE_PATH");
            string root = Environment.GetEnvironmentVariable("BURRITO_HARNESS_ROOT");
            if (string.IsNullOrEmpty(root) || !Path.IsPathRooted(root) ||
                profile != Path.Combine(root, "profile") || !File.Exists(Path.Combine(root, "harness-run.json")))
            { Finish("SKIPPED", "dedicated-harness-profile-required"); return; }
            if (Extension == "banana-quest") plugin.BananaMode = true;
            WorldFixture.ConfigureCollection();
            plugin.Awareness.HarnessRecord("start", TraceWorld.Values("fixtureRevision", 1, "repeats", 2,
                "profile", profile, "performance", Performance, "pressure", Pressure, "extension", Extension,
                "setup", "one native Courier; safe nearby tile; forced DoNothing; native removal",
                "scope", "actor discovery/query/read-invariance/retirement; not full WA or dog acceptance"));
        }

        internal void Tick()
        {
            if (!Enabled) return;
            if (phase == Phase.Finished)
            {
                if (Time.realtimeSinceStartup >= quitAt) Application.Quit();
                return;
            }
            try
            {
                var gc = GameController.gameController;
                if (Extension == "banana-quest" && phase != Phase.Readiness && startupEncounter == null && plugin.Quests.Encounter != null)
                {
                    startupEncounter = plugin.Quests.Encounter;
                    RecordQuestStartup("offered", gc);
                    if (!QuestService.Ready(gc?.playerAgent) || gc.sessionDataBig.curLevel <= 0 || gc.quests.numQuests <= 0)
                    { Finish("FAIL", "favor-started-before-playable-floor"); return; }
                }
                if (Extension == "banana-quest" && phase != Phase.Readiness && startupEncounter != null &&
                    (plugin.Quests.Encounter != startupEncounter || !startupEncounter.SameLevel || plugin.Quests.Policy.State != QuestState.Offered))
                { Finish("FAIL", "startup-favor-expired-before-cases"); return; }
                if (phase == Phase.Readiness)
                {
                    if (questHarness != null)
                    { questHarness.Tick(); if (questHarness.Status != null) Finish(questHarness.Status, questHarness.Reason); }
                    else if (desireHarness != null)
                    { desireHarness.Tick(); if (desireHarness.Status != null) Finish(desireHarness.Status, desireHarness.Reason); }
                    else if (npcHarness != null)
                    { npcHarness.Tick(); if (npcHarness.Status != null) Finish(npcHarness.Status, npcHarness.Reason); }
                    else if (hydrantHarness != null)
                    {
                        hydrantHarness.Tick();
                        if (hydrantHarness.Status != null) Finish(hydrantHarness.Status, hydrantHarness.Reason);
                    }
                    else if (foodHarness != null)
                    {
                        foodHarness.Tick();
                        if (foodHarness.Status != null) Finish(foodHarness.Status, foodHarness.Reason);
                    }
                    else
                    {
                        readiness.Tick();
                        if (readiness.Status != null) Finish(readiness.Status, readiness.Reason);
                    }
                    return;
                }
                if (Time.realtimeSinceStartup >= deadline)
                { Finish(phase == Phase.Waiting ? "SKIPPED" : "FAIL", "timeout-" + phase); return; }
                if (phase == Phase.Waiting)
                {
                    if (!WorldFixture.Ready(gc) || plugin.Awareness.HarnessLevel == 0 ||
                        Time.timeScale <= 0 && !plugin.Awareness.HarnessNativeFreeze)
                    {
                        readySince = 0;
                        string action = fixture.AdvanceMenu(gc);
                        if (action != null) plugin.Awareness.HarnessRecord("setup-action", TraceWorld.Values("action", action));
                        return;
                    }
                    if (controller != gc || player != gc.playerAgent || level != plugin.Awareness.HarnessLevel || readySince == 0)
                    {
                        controller = gc; player = gc.playerAgent; level = plugin.Awareness.HarnessLevel;
                        readySince = Time.realtimeSinceStartup;
                    }
                    if (Time.realtimeSinceStartup - readySince < 2) return;
                    Spawn(); return;
                }
                if (gc != controller || !WorldFixture.Ready(gc) || plugin.Awareness.HarnessLevel != level || gc.playerAgent != player)
                { Finish("INTERRUPTED", "world-or-player-changed"); return; }
                if (Time.timeScale <= 0 && !plugin.Awareness.HarnessNativeFreeze)
                { Finish("INTERRUPTED", "paused"); return; }
                if (phase == Phase.Discover)
                {
                    if (!fixture.OwnedActive || fixture.Actor.dead)
                    { Finish("INTERRUPTED", "fixture-actor-lost"); return; }
                    if (Time.realtimeSinceStartup < settleUntil) return;
                    var entity = plugin.Awareness.HarnessEntity(fixture.Actor);
                    // Require ordinary census/detail reads, not merely the lifecycle hook's partial snapshot.
                    if (entity == null || entity.Collection.StartsWith("lifecycle:", StringComparison.Ordinal) ||
                        Time.time - entity.GameTime > 1) return;
                    key = entity.Key;
                    var read = plugin.Awareness.HarnessRead(player, fixture.Actor, entity);
                    read["repeat"] = repeat;
                    plugin.Awareness.HarnessRecord("observation", read);
                    if (JsonConvert.SerializeObject(read["before"]) != JsonConvert.SerializeObject(read["after"]))
                    { Finish("FAIL", "inspection-mutated-native-state"); return; }
                    retiredActor = fixture.Actor;
                    fixture.Remove();
                    plugin.Awareness.HarnessRecord("cleanup", TraceWorld.Values("repeat", repeat, "key", key,
                        "mutation", "DestroyMe fixture-owned actor only"));
                    phase = Phase.Retire; deadline = Time.realtimeSinceStartup + 15; settleUntil = Time.realtimeSinceStartup + 0.5f;
                }
                else if (phase == Phase.Retire && Time.realtimeSinceStartup >= settleUntil)
                {
                    var old = plugin.Awareness.Index.Get(key);
                    bool active = retiredActor != null && WorldNative.Active(retiredActor);
                    if (active || (old != null && old.Active && !old.Destroyed)) return;
                    plugin.Awareness.HarnessRecord("retired", TraceWorld.Values("repeat", repeat, "key", key,
                        "nativeActive", active, "indexedActive", old != null && old.Active && !old.Destroyed,
                        "oldSnapshot", old != null ? WorldNative.Detach(old) : null));
                    if (repeat < 2) Spawn();
                    else if (Extension != null)
                    {
                        if (Extension == "banana-quest")
                        {
                            RecordQuestStartup("settled", gc);
                            if (startupEncounter == null) { Finish("FAIL", "normal-auto-start-missing"); return; }
                            questHarness = new QuestHarness(plugin, player);
                        }
                        else if (Extension == "npc-desires") desireHarness = new NpcDesireHarness(plugin, player);
                        else if (Extension == "npc-probe") npcHarness = new NpcProbeHarness(plugin, player);
                        else if (Extension == "food") foodHarness = new PetFoodHarness(plugin, player);
                        else if (Extension == "hydrant") hydrantHarness = new PetHydrantHarness(plugin, player);
                        else readiness = new WorldReadiness(plugin, Extension, player);
                        phase = Phase.Readiness;
                    }
                    else if (Pressure)
                    {
                        if (plugin.Awareness.Quality.Level != 0 || !plugin.Awareness.HarnessCaptureClean)
                        { Finish("INCONCLUSIVE", "pressure-baseline-not-normal-or-clean"); return; }
                        stationaryPosition = player.tr.position; stationaryHealth = player.health; stationaryFocus = Application.isFocused;
                        if (!fixture.Spawn(player)) { Finish("SKIPPED", "no-pressure-witness-spot"); return; }
                        var core = plugin.Awareness.HarnessSnapshot(fixture.Actor);
                        key = core?.Key;
                        plugin.Awareness.HarnessRecord("pressure-start", TraceWorld.Values("maxBatches", 32, "batchSize", 64,
                            "core", core != null ? WorldNative.Detach(core) : null,
                            "context", plugin.Awareness.HarnessPerformanceContext(player), "evidence", plugin.Awareness.HarnessPressureEvidence()));
                        phase = Phase.Pressure; deadline = Time.realtimeSinceStartup + 15;
                    }
                    else if (Performance)
                    {
                        stationaryPosition = player.tr.position; stationaryHealth = player.health; stationaryFocus = Application.isFocused;
                        phase = Phase.Performance;
                        StartTrial();
                    }
                    else Finish("COMPLETE", "two-fresh-fixtures-captured; offline-assertions-required");
                }
                else if (phase == Phase.Performance)
                {
                    if (!plugin.Awareness.HarnessCaptureClean)
                    { Finish("FAIL", "performance-capture-loss-or-error"); return; }
                    if (Vector3.Distance(player.tr.position, stationaryPosition) > 0.05f || player.health != stationaryHealth ||
                        player.inCombat || Application.isFocused != stationaryFocus)
                    {
                        plugin.Awareness.HarnessRecord("performance-interrupted", TraceWorld.Values("trial", trial,
                            "context", plugin.Awareness.HarnessPerformanceContext(player),
                            "positionDelta", Vector3.Distance(player.tr.position, stationaryPosition),
                            "expectedHealth", stationaryHealth, "expectedFocus", stationaryFocus));
                        Finish("INTERRUPTED", "stationary-player-health-combat-or-focus-changed"); return;
                    }
                    if (plugin.Awareness.HarnessBenchmarkRunning) return;
                    var result = plugin.Awareness.HarnessLastTrial;
                    plugin.Awareness.HarnessRecord("performance-trial", TraceWorld.Values("trial", trial, "result", result,
                        "context", plugin.Awareness.HarnessPerformanceContext(player)));
                    if (result == null || !(bool)result["valid"])
                    { Finish("FAIL", "invalid-performance-trial"); return; }
                    if (trial < 9) StartTrial();
                    else Finish("COMPLETE", "two-fixtures-and-nine-stationary-trials-captured; offline-assertions-required");
                }
                else if (phase == Phase.Pressure || phase == Phase.Recovery || phase == Phase.PressureRetire)
                    TickPressure();
            }
            catch (Exception error)
            { plugin.Error(error); Finish("INCONCLUSIVE", error.GetType().Name + ": " + error.Message); }
        }

        private void RecordQuestStartup(string phaseName, GameController gc)
        {
            plugin.Awareness.HarnessRecord("quest-startup", TraceWorld.Values("phase", phaseName,
                "state", plugin.Quests.Policy?.State.ToString(), "sameEncounter", startupEncounter != null &&
                    startupEncounter == plugin.Quests.Encounter && startupEncounter.SameLevel,
                "ready", QuestService.Ready(gc?.playerAgent), "floor", gc?.floorNameNum,
                "level", gc?.sessionDataBig?.curLevel, "nativeQuestCount", gc?.quests?.numQuests,
                "characterSelect", QuestService.SelectingCharacter(gc), "openedCharacterSelect", gc?.mainGUI?.openedCharacterSelect,
                "initialCharacterSelect", gc?.mainGUI?.doingInitialCharacterSelect, "onMenu", gc?.menuGUI?.onMenu));
        }

        private void TickPressure()
        {
            pressureReduced |= plugin.Awareness.Quality.Level > 0;
            if (!plugin.Awareness.HarnessCaptureClean)
            { Finish("FAIL", "pressure-capture-loss-or-error"); return; }
            if (Vector3.Distance(player.tr.position, stationaryPosition) > .05f || player.health != stationaryHealth ||
                player.inCombat || Application.isFocused != stationaryFocus)
            {
                plugin.Awareness.HarnessRecord("pressure-interrupted", TraceWorld.Values("context",
                    plugin.Awareness.HarnessPerformanceContext(player), "positionDelta", Vector3.Distance(player.tr.position, stationaryPosition),
                    "expectedHealth", stationaryHealth, "expectedFocus", stationaryFocus));
                Finish("INTERRUPTED", "pressure-player-health-combat-or-focus-changed"); return;
            }
            if (phase == Phase.Pressure)
            {
                if (plugin.Awareness.Quality.Level > 0 || plugin.CaptureWriter.Saturated || batches >= 32)
                {
                    pressureStoppedAt = Time.realtimeSinceStartup;
                    plugin.Awareness.HarnessRecord("pressure-stopped", TraceWorld.Values("batches", batches,
                        "evidence", plugin.Awareness.HarnessPressureEvidence()));
                    phase = Phase.Recovery; deadline = pressureStoppedAt + 150;
                    return;
                }
                var effects = new System.Collections.Generic.List<object>();
                System.Collections.Generic.Dictionary<string, object> effect = null;
                fixture.EmitPressure(player, (noise, birth) =>
                {
                    if (birth)
                    {
                        var indexed = plugin.Awareness.HarnessSnapshot(noise);
                        effect = TraceWorld.Values("indexed", indexed != null ? WorldNative.Detach(indexed) : null,
                            "volume", noise.volume, "dontAttract", noise.dontAttract);
                        effects.Add(effect);
                    }
                    else effect["nativeActiveAfterCleanup"] = WorldNative.Active(noise);
                });
                batches++;
                plugin.Awareness.HarnessRecord("pressure-batch", TraceWorld.Values("batch", batches, "effects", effects));
            }
            else if (phase == Phase.Recovery)
            {
                if (!fixture.OwnedActive || fixture.Actor.dead)
                { Finish("INTERRUPTED", "pressure-witness-lost"); return; }
                var detailed = plugin.Awareness.HarnessEntity(fixture.Actor);
                if (!detailRecorded && detailed != null && detailed.Collection != null &&
                    !detailed.Collection.StartsWith("lifecycle:", StringComparison.Ordinal) && Time.time - detailed.GameTime <= 1)
                {
                    detailRecorded = true;
                    plugin.Awareness.HarnessRecord("pressure-detail", TraceWorld.Values("indexed", WorldNative.Detach(detailed),
                        "evidence", plugin.Awareness.HarnessPressureEvidence()));
                }
                if (Time.realtimeSinceStartup >= nextPressureSample)
                {
                    plugin.Awareness.HarnessRecord("pressure-sample", plugin.Awareness.HarnessPressureEvidence());
                    nextPressureSample = Time.realtimeSinceStartup + 1;
                }
                if (Time.realtimeSinceStartup - pressureStoppedAt < 30 || !detailRecorded || plugin.Awareness.Quality.Level != 0) return;
                plugin.Awareness.HarnessRecord("pressure-recovered", TraceWorld.Values("evidence", plugin.Awareness.HarnessPressureEvidence(),
                    "context", plugin.Awareness.HarnessPerformanceContext(player)));
                retiredActor = fixture.Actor; fixture.Remove();
                phase = Phase.PressureRetire; deadline = Time.realtimeSinceStartup + 15;
            }
            else
            {
                var old = plugin.Awareness.Index.Get(key);
                if (retiredActor != null && WorldNative.Active(retiredActor) || old != null && old.Active && !old.Destroyed) return;
                plugin.Awareness.HarnessRecord("pressure-retired", TraceWorld.Values("key", key,
                    "nativeActive", false, "indexedActive", false));
                Finish(pressureReduced ? "COMPLETE" : "INCONCLUSIVE", pressureReduced ?
                    "pressure-slice-captured; offline-reduction-and-recovery-assertions-required" : "bounded-pressure-did-not-trigger-reduction");
            }
        }

        private void StartTrial()
        {
            trial++;
            var mode = (AwarenessMode)((trial - 1) % 3);
            plugin.Awareness.HarnessRecord("performance-start", TraceWorld.Values("trial", trial, "mode", mode.ToString(),
                "context", plugin.Awareness.HarnessPerformanceContext(player)));
            plugin.Awareness.StartBenchmark("harness-stationary", mode);
            deadline = Time.realtimeSinceStartup + 85;
        }

        private void Spawn()
        {
            repeat++;
            if (!fixture.Spawn(player)) { Finish("SKIPPED", "no-safe-native-spawn-spot"); return; }
            plugin.Awareness.HarnessRecord("fixture", TraceWorld.Values("repeat", repeat, "fixtureRevision", 1,
                "uid", fixture.Actor.UID, "instance", fixture.Actor.GetInstanceID(), "position", TraceWorld.Position(fixture.Actor.tr.position),
                "requestedGoal", "DoNothing", "controller", controller.GetInstanceID(), "floor", controller.floorNameNum));
            phase = Phase.Discover; deadline = Time.realtimeSinceStartup + 15; settleUntil = Time.realtimeSinceStartup + 1;
        }
        private void Finish(string status, string reason)
        {
            if (phase == Phase.Finished) return;
            try { fixture.Remove(); }
            catch (Exception error) { status = "INCONCLUSIVE"; reason += "; cleanup: " + error.Message; }
            try { questHarness?.Close(); desireHarness?.Close(); npcHarness?.Close(); readiness?.Close(); foodHarness?.Close(); hydrantHarness?.Close(); }
            catch (Exception error) { status = "INCONCLUSIVE"; reason += "; readiness cleanup: " + error.Message; }
            var gc = GameController.gameController;
            plugin.Awareness.HarnessRecord("ended", TraceWorld.Values("status", status, "reason", reason, "repeatsStarted", repeat,
                "performance", Performance, "pressure", Pressure, "extension", Extension, "trialsStarted", trial,
                "clock", TraceWorld.Values("timeScale", Time.timeScale, "mainScale", gc?.mainTimeScale,
                    "secondaryScale", gc?.secondaryTimeScale, "freezeFrame", gc?.freezeFrame,
                    "nativeFreeze", plugin.Awareness.HarnessNativeFreeze, "focused", Application.isFocused),
                "world", TraceWorld.Values("loadComplete", gc?.loadComplete, "loadCompleteReally", gc?.loadCompleteReally,
                    "transitioning", gc?.levelTransitioning, "returning", gc?.returningToMainMenu, "levelType", gc?.levelType,
                    "level", plugin.Awareness.HarnessLevel, "expectedLevel", level,
                    "playerInstance", gc?.playerAgent != null ? (object)gc.playerAgent.GetInstanceID() : null,
                    "expectedPlayerInstance", player != null ? (object)player.GetInstanceID() : null)));
            plugin.Log("BURRITO_WORLD_HARNESS " + status + " " + reason);
            phase = Phase.Finished; quitAt = Time.realtimeSinceStartup + 1;
        }
        internal void Close()
        {
            if (Enabled && phase != Phase.Finished) Finish("INTERRUPTED", "plugin-unload");
        }
    }
}
