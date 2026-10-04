using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BurritoRun
{
    // Emit a normal native noise; observe native AI without assigning NPC goals.
    internal sealed class PetBarkService
    {
        private readonly Plugin plugin;
        private readonly PetBarkPolicy policy = new PetBarkPolicy();
        private readonly HashSet<int> responders = new HashSet<int>();
        private AudioSource audio;
        private AudioClip clip;
        private Noise emittedNoise;
        private int emittedNoiseId;
        private float observeUntil;
        private int barkingPetId;
        private Agent guardTarget;
        private Vector3 guardStart;
        private Vector3 barkPosition;
        private string lastAutomaticState;
        // Off initially so the focused guard test has a quiet setup window.
        internal bool AutomaticBarks { get; set; }
        internal string Status { get; private set; } = "Barks: 0/6 this level.";

        internal PetBarkService(Plugin plugin) { this.plugin = plugin; }

        internal void Reset(string reason = "pet-reset")
        {
            plugin.Trace?.Interrupt(reason);
            FinishObservation();
            if (audio != null) UnityEngine.Object.Destroy(audio.gameObject);
            if (clip != null) UnityEngine.Object.Destroy(clip);
            audio = null;
            clip = null;
            policy.Reset(Time.time);
            lastAutomaticState = null;
            Status = "Barks: 0/6 this level.";
        }

        internal void Tick(Agent pet, Agent owner, bool following)
        {
            if (emittedNoise != null && Time.time >= observeUntil) FinishObservation();
            string state = !AutomaticBarks ? "disabled" : !following || !Available(pet, owner) ?
                AvailabilityReason(pet, owner, following) : policy.RestReason(Time.time);
            var stranger = state == null ? FindStranger(pet, owner) : null;
            if (state == null) state = stranger != null ? "ready-with-target" : "no-eligible-stranger";
            if (state != lastAutomaticState)
            {
                lastAutomaticState = state;
                plugin.Trace.AutomaticState(state, policy);
            }
            if (stranger != null)
            {
                int attempt = plugin.Trace.Request("automatic", pet, owner, following, policy);
                CaptureCandidates(attempt, pet, owner, false);
                Bark(pet, owner, stranger, "automatic", attempt);
            }
        }

        internal void TryManual(Agent pet, Agent owner, bool following)
        {
            int attempt = plugin.Trace.Request("manual", pet, owner, following, policy);
            CaptureCandidates(attempt, pet, owner, false);
            if (!following || !Available(pet, owner))
            { plugin.Trace.Reject(attempt, AvailabilityReason(pet, owner, following)); plugin.Notice("Spawn a following dog; barking waits during snack chases or combat."); return; }
            if (!policy.Ready(Time.time))
            { plugin.Trace.Reject(attempt, policy.RestReason(Time.time)); plugin.Notice("Bark resting: 20-second cooldown, six barks per level, five seconds after spawn."); return; }
            var stranger = FindStranger(pet, owner);
            if (stranger == null)
            { plugin.Trace.Reject(attempt, "no-eligible-stranger"); plugin.Notice("Bring the dog near a visible stranger it has not barked at yet."); return; }
            Bark(pet, owner, stranger, "manual", attempt);
        }

        internal void TryGuard(Agent pet, Agent owner, bool following)
        {
            AutomaticBarks = false;
            int attempt = plugin.Trace.Request("guard-test", pet, owner, following, policy);
            CaptureCandidates(attempt, pet, owner, true);
            if (!following || !Available(pet, owner))
            { plugin.Trace.Reject(attempt, AvailabilityReason(pet, owner, following)); plugin.Notice("Spawn a following dog, close Services and wait outside combat/snack chasing."); return; }
            if (!policy.Ready(Time.time))
            { plugin.Trace.Reject(attempt, policy.RestReason(Time.time)); plugin.Notice("Guard bark resting. Wait five seconds after spawn or 20 seconds after a bark."); return; }
            Agent target = null;
            float nearest = PetBarkPolicy.GuardMaxDistance;
            Agent nearestGuard = null;
            float nearestGuardDistance = float.MaxValue;
            foreach (var candidate in pet.gc.agentList)
            {
                if (!EligibleStranger(candidate, pet, owner)) continue;
                if (candidate.defaultGoalCode != goalType.Guard && candidate.defaultGoalCode != goalType.Patrol) continue;
                float distance = Vector2.Distance(pet.tr.position, candidate.tr.position);
                if (distance < nearestGuardDistance)
                {
                    nearestGuard = candidate;
                    nearestGuardDistance = distance;
                }
                if (!PetBarkPolicy.GuardInRange(distance) || distance > nearest ||
                    pet.movement == null || !pet.movement.HasLOSAgent360(candidate)) continue;
                target = candidate;
                nearest = distance;
            }
            if (target == null)
            {
                plugin.Trace.Reject(attempt, "no-eligible-guard");
                plugin.Log("BURRITO_PET_GUARD_SCAN pet=" + pet.UID + " selected=none nearest=" +
                    (nearestGuard != null ? nearestGuard.UID.ToString() : "none") + " distance=" +
                    (nearestGuard != null ? nearestGuardDistance.ToString("F2") : "none") +
                    " need=5.0-6.5-and-clear-view");
                plugin.Notice(nearestGuard != null ? "Nearest neutral guard is " +
                    nearestGuardDistance.ToString("F1") + " units from the dog; need 5–6.5 and clear view." :
                    "Need a neutral guard/patroller in clear view, about 8–10 floor tiles from the dog.");
                return;
            }
            Bark(pet, owner, target, "guard-test", attempt);
        }

        private bool Available(Agent pet, Agent owner) => plugin.TestMode && Time.timeScale > 0 &&
            pet != null && owner != null && !pet.dead && !pet.knockedOut && !pet.inCombat &&
            !owner.dead && !owner.ghost && !owner.inCombat && pet.gc == owner.gc &&
            pet.gc == GameController.gameController && plugin.Delivery.CanOrder(owner);

        private Agent FindStranger(Agent pet, Agent owner)
        {
            Agent nearest = null;
            float distance = PetBarkPolicy.StrangerRadius;
            foreach (var candidate in pet.gc.agentList)
            {
                if (!EligibleStranger(candidate, pet, owner)) continue;
                float next = Vector2.Distance(pet.tr.position, candidate.tr.position);
                if (next > distance || pet.movement == null || !pet.movement.HasLOSAgent360(candidate)) continue;
                nearest = candidate;
                distance = next;
            }
            return nearest;
        }

        private bool EligibleStranger(Agent candidate, Agent pet, Agent owner) =>
            candidate != null && candidate != pet && candidate != owner && candidate.tr != null &&
            candidate.isPlayer == 0 && !candidate.dead && !candidate.knockedOut && !candidate.ghost &&
            !candidate.inCombat && !candidate.hasFollowing && candidate.gameObject.activeInHierarchy &&
            !candidate.initializingForPool && !policy.HasBarkedAt(candidate.UID) &&
            pet.relationships.GetRel(candidate) == "Neutral";

        private List<string> CandidateReasons(Agent candidate, Agent pet, Agent owner)
        {
            var reasons = new List<string>();
            if (candidate == null) { reasons.Add("missing-actor"); return reasons; }
            if (candidate == pet || candidate == owner) reasons.Add("source-or-owner");
            if (candidate.tr == null) reasons.Add("missing-transform");
            if (candidate.isPlayer != 0) reasons.Add("player");
            if (candidate.dead) reasons.Add("dead");
            if (candidate.knockedOut) reasons.Add("knocked-out");
            if (candidate.ghost) reasons.Add("ghost");
            if (candidate.inCombat) reasons.Add("combat");
            if (candidate.hasFollowing) reasons.Add("following");
            if (!candidate.gameObject.activeInHierarchy) reasons.Add("inactive");
            if (candidate.initializingForPool) reasons.Add("pool-initializing");
            if (policy.HasBarkedAt(candidate.UID)) reasons.Add("already-barked-at");
            if (pet?.relationships == null || pet.relationships.GetRel(candidate) != "Neutral") reasons.Add("not-neutral-or-unknown");
            return reasons;
        }

        private string AvailabilityReason(Agent pet, Agent owner, bool following)
        {
            if (!plugin.TestMode) return "test-mode-off";
            if (pet == null) return "no-pet";
            if (owner == null) return "no-owner";
            if (!following) return "not-following-or-snack-chase";
            if (Time.timeScale <= 0) return "game-paused";
            if (pet.dead || pet.knockedOut || owner.dead || owner.ghost) return "source-or-owner-incapacitated";
            if (pet.inCombat || owner.inCombat) return "combat";
            if (pet.gc != owner.gc || pet.gc != GameController.gameController) return "controller-mismatch";
            return "world-or-delivery-unavailable";
        }

        private void CaptureCandidates(int attempt, Agent pet, Agent owner, bool guard)
        {
            // Diagnostics must not change whether a bark is allowed to execute.
            try
            {
                var snapshots = new List<object>();
                var counts = new Dictionary<string, int>();
                int omitted = 0;
                int total = 0;
                if (pet?.gc?.agentList != null)
                    foreach (var candidate in pet.gc.agentList)
                    {
                        total++;
                        var reasons = CandidateReasons(candidate, pet, owner);
                        float? distance = pet.tr != null && candidate?.tr != null ?
                            (float?)Vector2.Distance(pet.tr.position, candidate.tr.position) : null;
                        bool? los = null;
                        if (candidate != null && candidate != pet && candidate != owner &&
                            candidate.tr != null && distance.HasValue && distance <= TraceWorld.Radius &&
                            pet.movement != null) los = pet.movement.HasLOSAgent360(candidate);
                        if (guard && candidate != null && candidate.defaultGoalCode != goalType.Guard &&
                            candidate.defaultGoalCode != goalType.Patrol) reasons.Add("not-guard-or-patrol-goal");
                        if (!distance.HasValue || (guard ? !PetBarkPolicy.GuardInRange(distance.Value) :
                            distance.Value > PetBarkPolicy.StrangerRadius)) reasons.Add("out-of-range-or-unknown");
                        if (los != true) reasons.Add(los.HasValue ? "no-line-of-sight" : "line-of-sight-unknown");
                        foreach (var reason in reasons)
                            counts[reason] = counts.TryGetValue(reason, out int count) ? count + 1 : 1;
                        if (!distance.HasValue || distance > TraceWorld.Radius || snapshots.Count >= 32)
                        { omitted++; continue; }
                        snapshots.Add(TraceWorld.Values("actor", TraceWorld.Actor(candidate, pet),
                            "distance", distance, "lineOfSight", los, "eligible", reasons.Count == 0,
                            "reasons", reasons));
                    }
                plugin.Trace.Candidates(attempt, snapshots, counts, omitted, total);
            }
            catch (Exception error) { plugin.Trace.CaptureError(attempt, "candidates", error); }
        }

        private void Bark(Agent pet, Agent owner, Agent stranger, string trigger, int attempt)
        {
            try { BarkCore(pet, owner, stranger, trigger, attempt); }
            catch (Exception error)
            {
                plugin.Trace.AttemptFailed(attempt, error);
                plugin.Error(error);
            }
        }

        private void BarkCore(Agent pet, Agent owner, Agent stranger, string trigger, int attempt)
        {
            if (!policy.TryBegin(Time.time, stranger.UID))
            { plugin.Trace.Reject(attempt, "policy-reservation-rejected"); return; }
            FinishObservation();
            barkingPetId = pet.UID;
            responders.Clear();
            barkPosition = pet.tr.position;
            guardTarget = trigger == "guard-test" ? stranger : null;
            guardStart = stranger.tr.position;
            // Native volume 0.8 reaches 4.7 units; 1.0 reaches 7.4 for the guard probe.
            // The dog is the source. No player blame, crime, damage or hostility is added.
            float volume = guardTarget != null ? 1f : 0.8f;
            plugin.Trace.Selected(attempt, stranger, volume);
            emittedNoise = pet.gc.spawnerMain.SpawnNoise(barkPosition, volume, pet, "Normal", pet);
            emittedNoiseId = emittedNoise != null ? emittedNoise.noiseID : -1;
            observeUntil = Time.time + 8f;
            bool audible = false;
            try { audible = PlayBark(pet, owner); }
            catch (Exception error) { plugin.Error(error); }
            plugin.Trace.Emitted(attempt, pet, owner, stranger, emittedNoise, volume, audible);
            Status = "Barks: " + policy.Count + "/6. Watching native reactions.";
            plugin.Log("BURRITO_PET_BARK pet=" + pet.UID + " stranger=" + stranger.UID +
                " name=" + stranger.agentName + " trigger=" + trigger + " count=" + policy.Count +
                " noise=" + emittedNoiseId + " spawned=" + (emittedNoise != null) +
                " radius=" + (emittedNoise != null ? emittedNoise.noiseDistance.ToString("F2") : "none") +
                " audio=" + audible + " at=" + barkPosition + " targetStart=" + guardStart +
                " targetDistance=" + Vector2.Distance(barkPosition, guardStart).ToString("F2") +
                " targetDefaultGoal=" + stranger.defaultGoalCode);
            plugin.Notice(guardTarget != null ? "Woof! Stay here and watch whether the guard leaves position." :
                "Woof! Watch nearby NPCs. The city decides who investigates.");
        }

        internal void ObserveReaction(GoalNoiseReact goal)
        {
            if (emittedNoise == null || Time.time > observeUntil ||
                emittedNoise.noiseID != emittedNoiseId || goal.noise != emittedNoise ||
                goal.agent == null || goal.agent.UID == barkingPetId ||
                !responders.Add(goal.agent.UID)) return;
            var npc = goal.agent;
            Status = "Barks: " + policy.Count + "/6. Native responders: " + responders.Count;
            plugin.Log("BURRITO_PET_BARK_REACTION pet=" + barkingPetId + " noise=" + emittedNoiseId +
                " npc=" + npc.UID + " name=" + npc.agentName + " goal=NoiseReact" +
                " at=" + npc.tr.position + " source=" + goal.noisePosition);
        }

        private void FinishObservation()
        {
            if (emittedNoise != null)
                plugin.Log("BURRITO_PET_BARK_RESULT pet=" + barkingPetId + " noise=" + emittedNoiseId +
                    " nativeResponders=" + responders.Count + " evidence=native-goal-activation");
            // Noise is pooled and expires under native control; never destroy a reused noise.
            emittedNoise = null;
            guardTarget = null;
        }

        internal void TraceFinished(string status, string reason, float approach)
        { Status = "Trace " + status + ": " + reason + "; matching approach " + approach.ToString("F2") + "."; }

        private bool PlayBark(Agent pet, Agent owner)
        {
            var mixer = pet.gc.audioHandler?.threeDSounds;
            if (mixer == null) return false;
            if (audio == null)
            {
                var source = new GameObject("BurritoRun bark audio");
                source.transform.SetParent(pet.tr, false);
                audio = source.AddComponent<AudioSource>();
                audio.playOnAwake = false;
                audio.loop = false;
                audio.outputAudioMixerGroup = mixer;
                // The game is planar. Apply distance attenuation explicitly while
                // preserving the game's effects mixer and volume settings.
                audio.spatialBlend = 0;
                clip = MakeBark();
                audio.clip = clip;
            }
            audio.volume = 0.45f * Mathf.Clamp01(1f - Vector2.Distance(pet.tr.position, owner.tr.position) / 10f);
            audio.Play();
            return true;
        }

        private static AudioClip MakeBark()
        {
            // Original procedural placeholder: a short rough, falling-pitch woof.
            const int rate = 22050;
            const int length = 5512;
            var samples = new float[length];
            var random = new System.Random(512900);
            float phase = 0;
            float grit = 0;
            for (int index = 0; index < length; index++)
            {
                float t = (float)index / rate;
                phase += 2f * Mathf.PI * Mathf.Lerp(180f, 85f, t / 0.25f) / rate;
                grit = 0.7f * grit + 0.3f * ((float)random.NextDouble() * 2f - 1f);
                float envelope = Mathf.Min(1f, t / 0.012f) * Mathf.Pow(1f - t / 0.25f, 2f);
                samples[index] = envelope * (0.36f * Mathf.Sin(phase) +
                    0.22f * Mathf.Sin(3f * phase) + 0.15f * Mathf.Sin(7f * phase) + 0.25f * grit);
            }
            var result = AudioClip.Create("BurritoRun procedural woof", length, 1, rate, false);
            result.SetData(samples, 0);
            return result;
        }
    }

    [HarmonyPatch(typeof(GoalNoiseReact), nameof(GoalNoiseReact.Activate))]
    internal static class PetBarkReactionPatch
    {
        private static void Postfix(GoalNoiseReact __instance)
        {
            try
            {
                Plugin.Instance?.Trace?.Reaction(__instance);
                Plugin.Instance?.Pets?.Barks.ObserveReaction(__instance);
            }
            catch (Exception error) { Plugin.Instance?.Error(error); }
        }
    }
}
