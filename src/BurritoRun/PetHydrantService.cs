using System;
using System.Collections.Generic;
using UnityEngine;

namespace BurritoRun
{
    // Read-only discovery, native dog movement, and a cosmetic marking pose.
    // Never interact with, damage, switch or wet the native hydrant/world.
    internal sealed class PetHydrantService
    {
        private readonly Plugin plugin;
        private readonly PetService pets;
        private readonly PetHydrantPolicy policy = new PetHydrantPolicy();
        private FireHydrant target;
        private float deadline;
        private float markStarted;
        private Vector2 originalAnchor;
        private Agent anchoredPet;
        private int anchoredUid;
        internal string TargetKey { get; private set; }
        internal Dictionary<string, object> LastAction { get; private set; }
        internal bool Active => TargetKey != null;
        internal bool Marking { get; private set; }
        internal PetHydrantService(Plugin plugin, PetService pets) { this.plugin = plugin; this.pets = pets; }

        internal void TryNotice(Agent pet, Agent owner)
        {
            if (!policy.Ready(Time.time) || Vector2.Distance(pet.tr.position, owner.tr.position) > PetHydrantPolicy.OwnerRadius) return;
            var query = plugin.Awareness.Index.Query(new WorldQuery
            {
                X = pet.tr.position.x,
                Y = pet.tr.position.y,
                Kind = WorldKind.Object,
                Tag = "hydrant",
                MaxDistance = PetHydrantPolicy.NoticeRadius,
                Limit = 8
            });
            foreach (var snapshot in query.Entities)
            {
                if (!policy.CanNotice(snapshot.Key, Time.time) || snapshot.Collection.StartsWith("lifecycle:", StringComparison.Ordinal) ||
                    Time.time - snapshot.GameTime > 2 || !(plugin.Awareness.ResolveLive(snapshot.Key) is FireHydrant hydrant) ||
                    !Eligible(hydrant) || Vector2.Distance(pet.tr.position, hydrant.tr.position) > PetHydrantPolicy.NoticeRadius ||
                    !pet.movement.HasLOSPosition360(hydrant.tr.position) || !FindApproach(pet, hydrant, out var spot)) continue;
                target = hydrant; TargetKey = snapshot.Key; Marking = false;
                deadline = Time.time + PetHydrantPolicy.ChaseSeconds;
                // Suspend native following during the detour so it cannot compete with GoHere.
                pet.SetFollowing(null);
                pet.agentInteractions.GoHere(pet, owner, spot);
                pets.BeginHydrant(false);
                Record("noticed", TraceWorld.Values("key", TargetKey, "uid", hydrant.UID,
                    "snapshot", WorldNative.Detach(snapshot), "petPosition", TraceWorld.Position(pet.tr.position),
                    "targetPosition", TraceWorld.Position(hydrant.tr.position), "destination", TraceWorld.Position(spot),
                    "gameTime", Time.time));
                plugin.Notice("The dog is checking out a hydrant.");
                return;
            }
        }

        private static bool Eligible(FireHydrant hydrant) => hydrant != null && WorldNative.Active(hydrant) &&
            !hydrant.destroyed && hydrant.tr != null && hydrant.state == 0;

        private static bool FindApproach(Agent pet, FireHydrant hydrant, out Vector3 spot)
        {
            spot = default;
            float nearest = float.MaxValue;
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4;
                var candidate = hydrant.tr.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * .96f;
                // Object transforms carry sorting depth; native paths use the ground plane.
                candidate.z = 0;
                float distance = Vector2.Distance(pet.tr.position, candidate);
                if (distance >= nearest || !pet.gc.tileInfo.PossibleToWalk(pet, candidate) ||
                    !pet.gc.tileInfo.SafeToWalk(pet, candidate) || !pet.movement.HasLOSPosition360(candidate)) continue;
                nearest = distance; spot = candidate;
            }
            return nearest < float.MaxValue;
        }

        internal void Tick(Agent pet, Agent owner)
        {
            if (!Active) return;
            if (pet.knockedOut || owner.knockedOut || owner.ghost) { Finish("incapacitated"); return; }
            if (pet.inCombat || owner.inCombat) { Finish("combat"); return; }
            if (Vector2.Distance(pet.tr.position, owner.tr.position) > PetHydrantPolicy.OwnerRadius)
            { Finish("owner-too-far"); return; }
            if (plugin.Awareness.ResolveLive(TargetKey) != target || !Eligible(target))
            { Finish("target-gone-or-ineligible"); return; }
            float distance = Vector2.Distance(pet.tr.position, target.tr.position);
            if (!pet.movement.HasLOSPosition360(target.tr.position)) { Finish("view-blocked"); return; }
            if (Marking)
            {
                if (distance > PetHydrantPolicy.HydrantRadius) { Finish("moved-away"); return; }
                if (Time.time - markStarted >= PetHydrantPolicy.MarkSeconds) Finish("marked");
                return;
            }
            // Native pathing may settle on a different face of this solid object.
            if (distance <= PetHydrantPolicy.ArrivalRadius)
            {
                // Brief native idle; restore Follow after the bounded cosmetic pose.
                originalAnchor = pet.startingPosition; anchoredPet = pet; anchoredUid = pet.UID;
                pet.startingPosition = pet.tr.position;
                pet.job = pet.jobBig = ""; pet.jobCode = pet.jobBigCode = jobType.None;
                pet.SetFollowing(null); pet.SetDefaultGoal("DoNothing");
                Marking = true; markStarted = Time.time;
                pets.BeginHydrant(true); pets.HydrantPose(true, target.tr.position);
                Record("marking", TraceWorld.Values("key", TargetKey, "distance", distance,
                    "petPosition", TraceWorld.Position(pet.tr.position), "gameTime", Time.time,
                    "visual", pets.HydrantVisualActive));
            }
            else if (Time.time >= deadline) Finish("timed-out");
        }

        private void Record(string name, Dictionary<string, object> data)
        {
            plugin.Log("BURRITO_PET_HYDRANT_" + name.ToUpperInvariant() + " " + Newtonsoft.Json.JsonConvert.SerializeObject(data));
            if (plugin.Harness.Enabled) plugin.Awareness.HarnessRecord("pet-hydrant-" + name, data);
        }
        internal void Finish(string outcome, bool follow = true)
        {
            if (!Active) return;
            var pet = pets.Actor;
            LastAction = TraceWorld.Values("key", TargetKey, "outcome", outcome, "gameTime", Time.time,
                "markSeconds", Marking ? Time.time - markStarted : 0,
                "distance", pet != null && target != null ? (object)Vector2.Distance(pet.tr.position, target.tr.position) : null,
                "petPosition", pet != null ? TraceWorld.Position(pet.tr.position) : null);
            policy.Finish(TargetKey, Time.time); pets.HydrantPose(false, default);
            if (anchoredPet != null && anchoredPet.UID == anchoredUid && WorldNative.Active(anchoredPet))
            {
                anchoredPet.startingPosition = originalAnchor;
                LastAction["idleAnchorBefore"] = TraceWorld.Position(originalAnchor);
                LastAction["idleAnchorAfter"] = TraceWorld.Position(anchoredPet.startingPosition);
            }
            anchoredPet = null;
            Record("action", LastAction);
            target = null; TargetKey = null; Marking = false;
            if (follow) pets.SetFollow("hydrant-" + outcome);
        }
        internal void Reset()
        { Finish("reset", false); policy.Reset(); LastAction = null; }
    }
}
