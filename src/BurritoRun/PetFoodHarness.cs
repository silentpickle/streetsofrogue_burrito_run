using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace BurritoRun
{
    internal sealed class PetFoodHarness
    {
        private readonly Plugin plugin;
        private readonly Agent player;
        private readonly PetFoodFixture fixture;
        private readonly int level;
        private readonly string playerState;
        private readonly Vector3 playerPosition;
        private readonly bool focused;
        private string stage = "stack";
        private float deadline;
        private float due;
        private string key;
        private Item item;
        private int uid;
        private Dictionary<string, object> action;
        private Vector3 startPosition;
        private float startHealth;
        private int noticed;
        private Agent retiredActor;
        private string actorKey;
        internal string Status { get; private set; }
        internal string Reason { get; private set; }
        internal PetFoodHarness(Plugin plugin, Agent player)
        {
            this.plugin = plugin; this.player = player; level = plugin.Awareness.HarnessLevel;
            playerState = JsonConvert.SerializeObject(PlayerState()); playerPosition = player.tr.position; focused = Application.isFocused;
            fixture = new PetFoodFixture(plugin, player);
            Record("start", TraceWorld.Values("level", level, "pet", plugin.Pets.Actor.UID, "player", PlayerState(), "focused", focused));
            Setup(true);
        }
        private object PlayerState()
        {
            var inventory = new List<object>();
            foreach (var i in player.inventory.InvItemList) inventory.Add(TraceWorld.Values("id", i.invItemID, "name", i.invItemName, "count", i.invItemCount));
            return TraceWorld.Values("uid", player.UID, "instance", player.GetInstanceID(), "health", player.health, "money", player.inventory.FindItem("Money")?.invItemCount ?? 0, "inventory", inventory);
        }
        private void Record(string name, Dictionary<string, object> data) => plugin.Awareness.HarnessRecord("food-" + name, data);
        private void Stop(string status, string reason)
        { Status = status; Reason = reason; Record("ended", TraceWorld.Values("status", status, "reason", reason, "player", PlayerState())); }
        private void Setup(bool stack = false)
        {
            fixture.SpawnFood(stack); item = fixture.Food; uid = item.UID; key = null; action = null; noticed = 0;
            startPosition = plugin.Pets.Actor.tr.position; startHealth = plugin.Pets.Actor.health;
            due = Time.realtimeSinceStartup + 2; deadline = Time.realtimeSinceStartup + 20;
            Record("fixture", TraceWorld.Values("case", stage, "uid", uid, "instance", item.GetInstanceID(),
                "count", item.invItem.invItemCount, "health", startHealth, "maximum", plugin.Pets.Actor.healthMax,
                "petPosition", TraceWorld.Position(startPosition), "foodPosition", TraceWorld.Position(item.tr.position)));
        }
        internal void Tick()
        {
            if (Status != null) return;
            if (Time.realtimeSinceStartup >= deadline) { Stop("FAIL", "food-timeout-" + stage); return; }
            if (stage == "cleanup")
            {
                var oldActor = plugin.Awareness.Index.Get(actorKey);
                var oldFood = plugin.Awareness.Index.Get(key);
                if (WorldNative.Active(retiredActor) || WorldNative.Active(item) ||
                    oldActor != null && oldActor.Active && !oldActor.Destroyed ||
                    oldFood != null && oldFood.Active && !oldFood.Destroyed) return;
                if (plugin.Pets.Actor != null) return;
                bool spawnRejected = fixture.RejectSecondSpawn(plugin);
                Record("cleanup", TraceWorld.Values("actorKey", actorKey, "foodKey", key, "secondSpawnRejected", spawnRejected,
                    "actorActive", WorldNative.Active(retiredActor), "foodActive", WorldNative.Active(item),
                    "indexedActorLive", oldActor != null && oldActor.Active && !oldActor.Destroyed,
                    "indexedFoodLive", oldFood != null && oldFood.Active && !oldFood.Destroyed));
                Stop(spawnRejected ? "COMPLETE" : "FAIL", "food-cases-captured"); return;
            }
            if (!fixture.OwnedActor || plugin.Pets.Actor.dead || plugin.Pets.Actor.inCombat || player.inCombat ||
                !WorldFixture.Ready(player.gc) || plugin.Awareness.HarnessLevel != level ||
                player.gc.playerAgent != player || Time.timeScale <= 0 || Application.isFocused != focused ||
                Vector3.Distance(playerPosition, player.tr.position) > .05f ||
                JsonConvert.SerializeObject(PlayerState()) != playerState || !plugin.Awareness.HarnessCaptureClean)
            { Stop("INTERRUPTED", "food-world-player-actor-or-capture-changed"); return; }
            if (stage.StartsWith("cooldown-", StringComparison.Ordinal))
            {
                if (plugin.Pets.Intent != PetIntent.Follow || plugin.Pets.LastFoodAction != action)
                { Stop("FAIL", "food-repeated-during-cooldown"); return; }
                if (Time.time < due) return;
                Record("cooldown", TraceWorld.Values("key", key, "seconds", Time.time - (float)action["gameTime"], "following", true));
                stage = stage.Substring(9);
                if (stage == "eat") fixture.Injure();
                Setup(); return;
            }
            if (key == null)
            {
                var snapshot = plugin.Awareness.HarnessSnapshot(item);
                if (snapshot == null || snapshot.NativeId != uid || snapshot.Collection.StartsWith("lifecycle:", StringComparison.Ordinal) ||
                    Time.time - snapshot.GameTime > 1) return;
                key = snapshot.Key;
                Record("discovered", TraceWorld.Values("case", stage, "snapshot", WorldNative.Detach(snapshot)));
            }
            if (stage == "stack")
            {
                if (Time.realtimeSinceStartup < due) return;
                bool untouched = plugin.Pets.FoodTargetKey != key && plugin.Pets.Intent == PetIntent.Follow && item.invItem.invItemCount == 2 && WorldNative.Active(item);
                Record("case", TraceWorld.Values("case", stage, "key", key, "untouched", untouched, "player", PlayerState()));
                if (!untouched) { Stop("FAIL", "stack-not-preserved"); return; }
                fixture.RemoveFood(); stage = "full"; Setup(); return;
            }
            if (plugin.Pets.FoodTargetKey == key && noticed == 0)
            {
                noticed = 1;
                if (stage == "removed") fixture.RemoveFood();
                if (stage == "release")
                {
                    plugin.Pets.ReleaseTestPet(); due = Time.realtimeSinceStartup + 2;
                    Record("released", TraceWorld.Values("key", key, "pet", plugin.Pets.Actor.UID));
                }
            }
            if (stage == "release")
            {
                if (noticed == 0 || Time.realtimeSinceStartup < due) return;
                bool untouched = WorldNative.Active(item) && item.UID == uid && item.invItem.invItemCount == 1;
                Record("case", TraceWorld.Values("case", stage, "key", key, "noticed", noticed, "untouched", untouched,
                    "intent", plugin.Pets.Intent.ToString(), "following", plugin.Pets.Actor.following != null,
                    "defaultGoal", plugin.Pets.Actor.defaultGoal, "player", PlayerState()));
                if (!untouched || plugin.Pets.Intent != PetIntent.Wander || plugin.Pets.Actor.following != null)
                { Stop("FAIL", "release-food-not-preserved"); return; }
                retiredActor = plugin.Pets.Actor; actorKey = plugin.Awareness.HarnessEntity(retiredActor)?.Key;
                if (actorKey == null) { Stop("FAIL", "dog-not-indexed"); return; }
                fixture.Close(); stage = "cleanup"; deadline = Time.realtimeSinceStartup + 15; return;
            }
            if (action == null && plugin.Pets.LastFoodAction != null && (string)plugin.Pets.LastFoodAction["key"] == key)
            { action = plugin.Pets.LastFoodAction; due = Time.realtimeSinceStartup + 1; }
            if (action == null || Time.realtimeSinceStartup < due) return;
            bool active = item != null && item.UID == uid && WorldNative.Active(item);
            var indexed = plugin.Awareness.Index.Get(key);
            bool retired = indexed == null || !indexed.Active || indexed.Destroyed;
            string outcome = (string)action["outcome"];
            bool expected = stage == "full" ? outcome == "sniff-full-health" && active && item.invItem.invItemCount == 1 :
                stage == "eat" ? outcome == "ate" : outcome == "food-gone-or-ineligible";
            if (!expected) { Stop("FAIL", "unexpected-food-outcome-" + stage); return; }
            if (stage != "full" && (active || !retired)) return;
            var actor = plugin.Pets.Actor;
            string rootGoal = actor.brain?.Goals?.Count > 0 ? actor.brain.Goals[0].GetType().Name : null;
            bool following = plugin.Pets.Intent == PetIntent.Follow && actor.following == player && actor.hasFollowing && rootGoal == "GoalFollow";
            if (expected && !following) return;
            Record("case", TraceWorld.Values("case", stage, "key", key, "action", action, "nativeActive", active,
                "indexedRetired", retired, "count", active ? (object)item.invItem.invItemCount : null,
                "following", following, "rootGoal", rootGoal, "followingUid", actor.following?.UID, "job", actor.job, "resultDisplacement", Vector3.Distance(startPosition, plugin.Pets.Actor.tr.position),
                "startHealth", startHealth, "endHealth", plugin.Pets.Actor.health, "player", PlayerState()));
            if (!expected || !following) { Stop("FAIL", "unexpected-food-outcome-" + stage); return; }
            fixture.RemoveFood();
            stage = "cooldown-" + (stage == "full" ? "eat" : stage == "eat" ? "removed" : "release");
            due = Time.time + 8.5f; deadline = Time.realtimeSinceStartup + 30;
        }
        internal void Close() => fixture.Close();
    }
}
