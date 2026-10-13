using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace BurritoRun
{
    internal sealed class PetHydrantHarness
    {
        private readonly Plugin plugin;
        private readonly Agent player;
        private readonly PetHydrantFixture fixture;
        private readonly int level;
        private readonly string playerState;
        private readonly Vector3 playerPosition;
        private readonly bool focus;
        private string stage = "mark";
        private float deadline;
        private float due;
        private FireHydrant hydrant;
        private string key;
        private int uid;
        private string hydrantState;
        private Dictionary<string, object> action;
        private bool noticed;
        private Agent retiredDog;
        private string dogKey;
        private float nextMotion;
        internal string Status { get; private set; }
        internal string Reason { get; private set; }
        internal PetHydrantHarness(Plugin plugin, Agent player)
        {
            this.plugin = plugin; this.player = player;
            level = plugin.Awareness.HarnessLevel; playerState = JsonConvert.SerializeObject(PlayerState());
            playerPosition = player.tr.position; focus = Application.isFocused;
            fixture = new PetHydrantFixture(plugin, player);
            Record("start", TraceWorld.Values("player", PlayerState(), "pet", plugin.Pets.Actor.UID, "level", level));
            Setup();
        }
        private object PlayerState()
        {
            var inventory = new List<object>();
            foreach (var i in player.inventory.InvItemList) inventory.Add(TraceWorld.Values("id", i.invItemID, "name", i.invItemName, "count", i.invItemCount));
            return TraceWorld.Values("uid", player.UID, "health", player.health, "inventory", inventory);
        }
        private object HydrantState() => TraceWorld.Values("uid", hydrant.UID, "instance", hydrant.GetInstanceID(),
            "state", hydrant.state, "destroyed", hydrant.destroyed, "position", TraceWorld.Position(hydrant.tr.position));
        private void Record(string name, Dictionary<string, object> data) => plugin.Awareness.HarnessRecord("hydrant-" + name, data);
        private void Stop(string status, string reason)
        { Status = status; Reason = reason; Record("ended", TraceWorld.Values("status", status, "reason", reason, "player", PlayerState())); }
        private void Setup()
        {
            fixture.Spawn(plugin.Pets.Actor); hydrant = fixture.Hydrant; uid = hydrant.UID;
            key = null; action = null; noticed = false; hydrantState = JsonConvert.SerializeObject(HydrantState());
            deadline = Time.realtimeSinceStartup + 22;
            Record("fixture", TraceWorld.Values("case", stage, "uid", uid, "instance", hydrant.GetInstanceID(),
                "hydrant", HydrantState(), "health", plugin.Pets.Actor.health, "gameTime", Time.time));
        }
        internal void Tick()
        {
            if (Status != null) return;
            if (Time.realtimeSinceStartup >= deadline) { Stop("FAIL", "hydrant-timeout-" + stage); return; }
            if (stage == "cleanup")
            {
                var indexed = plugin.Awareness.Index.Get(dogKey);
                var oldHydrant = plugin.Awareness.Index.Get(key);
                if (WorldNative.Active(retiredDog) || !hydrant.destroyed ||
                    indexed != null && indexed.Active && !indexed.Destroyed ||
                    oldHydrant != null && oldHydrant.Active && !oldHydrant.Destroyed || plugin.Pets.Actor != null) return;
                Record("cleanup", TraceWorld.Values("dogKey", dogKey, "key", key,
                    "dogActive", WorldNative.Active(retiredDog), "hydrantDestroyed", hydrant.destroyed,
                    "indexedDogLive", indexed != null && indexed.Active && !indexed.Destroyed,
                    "indexedHydrantLive", oldHydrant != null && oldHydrant.Active && !oldHydrant.Destroyed,
                    "visual", plugin.Pets.HydrantVisualActive));
                Stop("COMPLETE", "hydrant-cases-captured"); return;
            }
            var pet = plugin.Pets.Actor;
            if (plugin.Pets.Hydrants.Active && Time.time >= nextMotion)
            {
                nextMotion = Time.time + .5f;
                var observedRoot = pet?.brain?.Goals?.Count > 0 ? pet.brain.Goals[0] : null;
                Record("motion", TraceWorld.Values("key", plugin.Pets.Hydrants.TargetKey,
                    "assigned", pet != null ? TraceWorld.Position(pet.assignedPos) : null,
                    "final", pet != null ? TraceWorld.Position(pet.finalDestPosition) : null,
                    "curJob", WorldNative.Read(observedRoot, "curJob"), "curAssigned",
                    WorldNative.Read(observedRoot, "assignedPos") is Vector3 assigned ? TraceWorld.Position(assigned) : null,
                    "navigation", pet != null ? WorldNative.Navigation(pet, plugin.Awareness.Index, "fixture-dog") : null));
            }
            if (pet == null || pet.dead || pet.inCombat || player.inCombat || !WorldFixture.Ready(player.gc) ||
                plugin.Awareness.HarnessLevel != level || Application.isFocused != focus || Time.timeScale <= 0 ||
                Vector3.Distance(playerPosition, player.tr.position) > .05f ||
                playerState != JsonConvert.SerializeObject(PlayerState()) || !plugin.Awareness.HarnessCaptureClean)
            { Stop("INTERRUPTED", "hydrant-world-player-or-capture-changed"); return; }
            if (stage == "wait")
            {
                if (plugin.Pets.Hydrants.Active || plugin.Pets.Hydrants.LastAction != action || plugin.Pets.HydrantVisualActive)
                { Stop("FAIL", "hydrant-repeated-or-visual-leaked"); return; }
                if (Time.time < due) return;
                Record("quiet", TraceWorld.Values("key", key, "seconds", Time.time - (float)action["gameTime"],
                    "unchanged", hydrantState == JsonConvert.SerializeObject(HydrantState()), "player", PlayerState()));
                stage = (string)action["outcome"] == "marked" ? "removed" :
                    (string)action["outcome"] == "target-gone-or-ineligible" ? "timeout" : "release";
                Setup(); return;
            }
            if (key == null)
            {
                var snap = plugin.Awareness.HarnessSnapshot(hydrant);
                if (snap == null || snap.NativeId != uid || snap.Collection.StartsWith("lifecycle:", System.StringComparison.Ordinal) ||
                    Time.time - snap.GameTime > 1) return;
                key = snap.Key; Record("discovered", TraceWorld.Values("case", stage, "snapshot", WorldNative.Detach(snap)));
            }
            if (!noticed && plugin.Pets.Hydrants.TargetKey == key)
            {
                noticed = true;
                if (stage == "removed") fixture.Remove();
                if (stage == "timeout") PetHydrantFixture.StopDog(pet);
                if (stage == "release") { plugin.Pets.ReleaseTestPet(); due = Time.time + 2; }
            }
            var current = plugin.Pets.Hydrants.LastAction;
            if (!noticed || current == null || (string)current["key"] != key) return;
            if (action == null) { action = current; due = Time.time + 1; }
            if (Time.time < due) return;
            string root = pet.brain?.Goals?.Count > 0 ? pet.brain.Goals[0].GetType().Name : null;
            bool following = plugin.Pets.Intent == PetIntent.Follow && pet.following == player && root == "GoalFollow";
            if (stage != "release" && !following) return;
            string expected = stage == "mark" ? "marked" : stage == "removed" ? "target-gone-or-ineligible" :
                stage == "timeout" ? "timed-out" : "test-release";
            bool unchanged = hydrantState == JsonConvert.SerializeObject(HydrantState());
            Record("case", TraceWorld.Values("case", stage, "key", key, "action", action, "following", following,
                "rootGoal", root, "intent", plugin.Pets.Intent.ToString(), "followingUid", pet.following?.UID,
                "defaultGoal", pet.defaultGoal, "hydrantUnchanged", unchanged, "hydrant", HydrantState(),
                "health", pet.health, "visual", plugin.Pets.HydrantVisualActive, "player", PlayerState()));
            if ((string)action["outcome"] != expected || stage != "removed" && !unchanged || plugin.Pets.HydrantVisualActive)
            { Stop("FAIL", "unexpected-hydrant-outcome-" + stage); return; }
            if (stage == "release")
            {
                retiredDog = pet; dogKey = plugin.Awareness.HarnessEntity(pet)?.Key;
                fixture.Close(); stage = "cleanup"; deadline = Time.realtimeSinceStartup + 20; return;
            }
            stage = "wait"; due = Time.time + 46; deadline = Time.realtimeSinceStartup + 70;
        }
        internal void Close() => fixture.Close();
    }
}
