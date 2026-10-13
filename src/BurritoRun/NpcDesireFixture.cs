using System;
using System.Collections.Generic;
using UnityEngine;

namespace BurritoRun
{
    // Only this disposable fixture spawns/removes NPCs or changes their test state.
    internal sealed class NpcDesireFixture
    {
        internal Agent Actor { get; private set; }
        internal Item Food { get; private set; }
        internal Item Second { get; private set; }
        private int secondUid, secondInstance;
        private int actorUid, actorInstance, foodUid, foodInstance;
        private GameObject sightBlocker;
        internal NpcDesireFixture(Agent player, bool wander = false)
        {
            var spot = WorldFixture.ObjectSpot(player, new List<PlayfieldObject>());
            Actor = player.gc.spawnerMain.SpawnAgent(spot, (PlayfieldObject)null, "Hobo");
            if (Actor == null) throw new InvalidOperationException("Civilian fixture failed");
            actorUid = Actor.UID; actorInstance = Actor.GetInstanceID();
            Actor.SetDefaultGoal(wander ? "WanderFar" : "DoNothing");
        }
        internal bool Owned => Actor != null && WorldNative.Active(Actor) && Actor.UID == actorUid && Actor.GetInstanceID() == actorInstance;
        internal void SpawnFood(string name = "Fud")
        {
            RemoveFood();
            if (!Owned) throw new InvalidOperationException("Civilian fixture lost");
            var spot = WorldFixture.ObjectSpot(Actor, new List<PlayfieldObject>());
            Food = Actor.gc.spawnerMain.SpawnItem(spot, name);
            if (Food == null) throw new InvalidOperationException("Food fixture failed");
            foodUid = Food.UID; foodInstance = Food.GetInstanceID();
        }
        internal void Pair(bool distanceCase)
        {
            RemoveFood();
            for (int i = 0; i < 16; i++)
            {
                float angle = i * Mathf.PI / 8;
                var dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                var a = Actor.tr.position + dir * (distanceCase ? 4.5f : 2f);
                var b = Actor.tr.position - dir * (distanceCase ? 1f : 2f);
                if (!Actor.movement.HasLOSPosition360(a) || !Actor.movement.HasLOSPosition360(b) ||
                    !Actor.gc.tileInfo.SafeToWalk(Actor, a) || !Actor.gc.tileInfo.SafeToWalk(Actor, b) ||
                    !Actor.gc.tileInfo.PossibleToWalk(Actor, a) || !Actor.gc.tileInfo.PossibleToWalk(Actor, b)) continue;
                Food = Actor.gc.spawnerMain.SpawnItem(a, "Fud");
                if (Food == null) throw new InvalidOperationException("Pair Fud failed");
                foodUid = Food.UID; foodInstance = Food.GetInstanceID();
                Second = Actor.gc.spawnerMain.SpawnItem(b, "Banana");
                if (Second == null) throw new InvalidOperationException("Pair Banana failed");
                secondUid = Second.UID; secondInstance = Second.GetInstanceID();
                return;
            }
            throw new InvalidOperationException("No safe visible pair positions");
        }
        internal bool BlockedFood()
        {
            SpawnFood();
            int mask = Actor.movement.myLayerMaskEfficient;
            int layer = 0;
            while (layer < 32 && (mask & (1 << layer)) == 0) layer++;
            if (layer == 32) throw new InvalidOperationException("Native LOS mask empty");
            // Isolated geometry fixture only; native HasLOSPosition360 performs the actual query.
            sightBlocker = new GameObject("NpcDesireFixture.SightBlocker") { layer = layer };
            sightBlocker.transform.position = (Actor.tr.position + Food.tr.position) / 2;
            sightBlocker.AddComponent<BoxCollider2D>().size = new Vector2(0.6f, 0.6f);
            Physics2D.SyncTransforms();
            return !Actor.movement.HasLOSPosition360(Food.tr.position);
        }
        // Labeled fault: native Item.Interact rejects NonItem. Never used by normal behavior.
        internal void RefusePickup() { Food.invItem.itemType = "NonItem"; }
        internal void Combat() { if (Owned) Actor.inCombat = true; }
        internal void MoveFoodToOtherSide()
        {
            if (Owned && Food != null) Food.tr.position = Actor.tr.position - (Food.tr.position - Actor.tr.position).normalized * 2;
        }
        internal void RemoveFood()
        {
            if (Food != null && Food.GetInstanceID() == foodInstance) WorldFixture.RemoveObject(Food, foodUid);
            Food = null;
            if (Second != null && Second.GetInstanceID() == secondInstance) WorldFixture.RemoveObject(Second, secondUid);
            Second = null;
        }
        internal void Emergency(bool value) { if (Owned) Actor.mustFlee = value; }
        internal void Close() { if (sightBlocker != null) UnityEngine.Object.Destroy(sightBlocker); sightBlocker = null; RemoveFood(); if (Owned) Actor.DestroyMe(); Actor = null; }
    }
}
