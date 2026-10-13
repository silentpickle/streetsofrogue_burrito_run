using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;

namespace BurritoRun
{
    // Commands live here, never in the world inspector. A lease is one exact native goal instance.
    internal sealed class NpcPickupProbe
    {
        private readonly Plugin plugin;
        internal Agent Actor { get; private set; }
        internal string Status { get; private set; } = "Disabled";
        internal bool Active => lease != null;
        internal string Outcome { get; private set; }
        internal float CooldownUntil { get; private set; }
        private int actorUid, actorInstance, itemUid, itemInstance;
        private Item item;
        private PickupGoal lease;
        private float deadline;
        private Vector3 start;
        private string itemName;
        private int beforeCount;
        private int scene;
        private int level;
        private bool verifyPickup;
        private float verifyUntil;
        private bool observeRelease;
        private float observeAt;
        internal NpcPickupProbe(Plugin plugin) { this.plugin = plugin; }
        internal static Goal Root(Agent actor) => actor?.brain?.Goals?.Count > 0 ? actor.brain.Goals[0] : null;
        internal bool Identity => Actor != null && Actor.UID == actorUid && Actor.GetInstanceID() == actorInstance && WorldNative.Active(Actor);
        internal static bool Emergency(Agent actor) => actor.inCombat || actor.inFleeCombat || actor.mustFlee || actor.knockedOut ||
            actor.dangers?.Count > 0 || actor.dangersAvoid?.Count > 0;
        private static bool IdleRoot(Agent actor) => NpcDecision.OptionalIdleRoot(Root(actor)?.GetType().Name);
        /// <summary>
        /// Return the native commitment that prevents this actor from taking optional food work.
        /// Recruitment, employment and following outrank our pickup; only our exact owned job can
        /// be exempted.
        /// </summary>
        internal static string NativeObligation(Agent actor, bool ownsPickup = false)
        {
            if (actor == null) return "actor-missing";
            if (actor.relationships?.joiningParty == true) return "native-recruitment";
            if (actor.hasEmployer || actor.employer != null) return "native-employment";
            if (actor.hasFollowing || actor.following != null) return "native-following";
            if (!ownsPickup && (!string.IsNullOrEmpty(actor.job) || !string.IsNullOrEmpty(actor.jobBig))) return "native-job";
            return !ownsPickup && !IdleRoot(actor) ? "native-busy" : null;
        }
        internal string Obligation => NativeObligation(Actor, Active && ReferenceEquals(Root(Actor), lease) && !lease.Ended && lease.OwnsFields);
        internal static bool Idle(Agent actor) => actor != null && !actor.dead && !Emergency(actor) && NativeObligation(actor) == null;
        /// <summary>
        /// Attach the experiment to one idle native Hobo in a supported single-player world.
        /// The display name is Slum Dweller, but native identity is Hobo; enrollment preserves its
        /// existing allegiance and defaults.
        /// </summary>
        internal bool Enroll(Agent actor)
        {
            if (Actor != null || actor == null || actor.isPlayer != 0 || actor.agentName != "Hobo" || !Idle(actor) ||
                !actor.gc.serverPlayer || actor.gc.multiplayerMode || actor.gc.coopMode || actor.gc.fourPlayerMode) { Status = "Select an idle Slum Dweller in single player."; return false; }
            Actor = actor; actorUid = actor.UID; actorInstance = actor.GetInstanceID();
            scene = actor.gameObject.scene.handle; level = plugin.Awareness.HarnessLevel;
            item = null; itemName = null; itemUid = itemInstance = beforeCount = 0;
            CooldownUntil = 0; Outcome = null; Status = "Enrolled " + actorUid; Record("enrolled"); return true;
        }
        /// <summary>
        /// Explain why a live ground item cannot be collected, or return null when it is eligible.
        /// Sampled discovery cannot establish current ownership, availability or line of sight
        /// immediately before a command.
        /// </summary>
        internal string Eligibility(Item target)
        {
            if (!Identity || target == null || !WorldNative.Active(target) || target.invItem == null || target.tr == null) return "missing";
            if (target.destroying || target.pickingUp || target.cantPickUp || target.airborne || target.beingThrown) return "unavailable";
            if (target.invItem.invItemCount != 1) return "not-single";
            if (target.startingOwner != 0 || target.invItem.stealable || target.invItem.questItem) return "protected";
            if (target.invItem.invItemName != "Fud" && target.invItem.invItemName != "Banana") return "unsupported-item";
            if (Actor.inventory.FindItem(target.invItem.invItemName) != null) return "already-held";
            if (Vector2.Distance(Actor.tr.position, target.tr.position) > 6) return "out-of-range";
            if (!Actor.movement.HasLOSPosition360(target.tr.position)) return "obstructed";
            return null;
        }
        /// <summary>
        /// Start a timed native approach and install one pickup goal that this probe owns.
        /// Exact goal ownership lets cleanup distinguish our work from a newer command, even at the
        /// same destination.
        /// </summary>
        internal bool Begin(Item target)
        {
            if (Active || !Identity || !Idle(Actor) || Time.timeScale <= 0 || Time.time < CooldownUntil) return false;
            string rejection = Eligibility(target);
            if (rejection != null) { Status = rejection; return false; }
            item = target; itemUid = target.UID; itemInstance = target.GetInstanceID(); itemName = target.invItem.invItemName;
            start = Actor.tr.position; beforeCount = Count(); Outcome = null; verifyPickup = false; observeRelease = false;
            deadline = Time.time + 8;
            var newLease = new PickupGoal(Actor, target.tr.position);
            // No relationship/default-goal changes. SwitchGoal terminates only the idle root selected above.
            Actor.agentInteractions.GoHere(Actor, Actor, target.tr.position);
            lease = newLease;
            Actor.brainUpdate.SwitchGoal(lease);
            Status = "Approaching " + itemName; Record("approach"); return true;
        }
        private int Count() => Identity ? Actor.inventory.FindItem(itemName)?.invItemCount ?? 0 : -1;
        private bool ItemIdentity => item != null && item.UID == itemUid && item.GetInstanceID() == itemInstance;
        /// <summary>
        /// Advance the owned action, yielding to danger, native work, target loss or timeout.
        /// Calling pickup is not success: inventory must gain one item and the original ground item
        /// must retire.
        /// </summary>
        internal void Tick()
        {
            if (Actor == null) return;
            if (!Identity) { Outcome = "actor-removed"; Record("ended"); Forget("actor-removed"); return; }
            var gc = Actor.gc;
            if (!gc.serverPlayer || gc.multiplayerMode || gc.coopMode || gc.fourPlayerMode) { Disable("unsupported-mode"); return; }
            if (!WorldFixture.Ready(gc) || Actor.gameObject.scene.handle != scene || level != plugin.Awareness.HarnessLevel) { Disable("level-change"); return; }
            if (Actor.dead) { Disable("actor-dead"); return; }
            if (Time.timeScale <= 0) return;
            if (observeRelease && Time.time >= observeAt)
            { observeRelease = false; Record("native-after-release"); }
            if (!Active) return;
            if (Emergency(Actor)) { Cancel("emergency"); return; }
            string obligation = Obligation;
            if (obligation != null) { Cancel(obligation); return; }
            if (!ReferenceEquals(Root(Actor), lease) || lease.Ended || !lease.OwnsFields)
            { Cancel("native-task-replaced"); return; }
            if (verifyPickup)
            {
                if (Count() == beforeCount + 1 && (!ItemIdentity || !WorldNative.Active(item))) { Cancel("picked-up"); return; }
                if (Time.time >= verifyUntil) Cancel("pickup-unconfirmed");
                return;
            }
            if (!ItemIdentity || Eligibility(item) != null) { Cancel("target-unavailable"); return; }
            if (Time.time >= deadline) { Cancel("timed-out"); return; }
            if (Vector2.Distance(Actor.tr.position, item.tr.position) > 0.8f) return;
            Record("arrived");
            item.Interact(Actor);
            verifyPickup = true; verifyUntil = Time.time + 1;
            Record("pickup-called");
        }
        /// <summary>
        /// Stop our action, preserve any replacement native goal, and start the pickup cooldown.
        /// Cleanup must not erase a job that recruitment or another command has already assigned.
        /// </summary>
        internal void Cancel(string reason = "cancelled")
        {
            if (!Active) return;
            var owned = lease;
            // Never terminate a newer root, even if it happens to have the same type or destination.
            if (Identity && ReferenceEquals(Root(Actor), owned) && !owned.Ended)
                Actor.brainUpdate.SwitchGoal(new GoalIdle());
            lease = null; verifyPickup = false; Outcome = reason; Status = reason;
            CooldownUntil = Time.time + 8;
            Record("ended"); observeRelease = Identity; observeAt = Time.time + 0.5f;
        }
        /// <summary>
        /// Cancel owned work, turn off autonomous choices and forget the enrolled actor.
        /// Releasing control must clear both the decision loop and its native action, not just hide
        /// the panel.
        /// </summary>
        internal void Disable(string reason = "disabled") { Cancel(reason); plugin.NpcDesires?.SetEnabled(false); Forget(reason); }
        private void Forget(string reason) { lease = null; Actor = null; item = null; observeRelease = false; Outcome = reason; Status = reason; }
        /// <summary>
        /// Capture actor, item, job and inventory state at an action transition.
        /// The independent reader needs observable outcomes and ownership fields rather than a
        /// success label alone.
        /// </summary>
        private void Record(string phase)
        {
            var data = TraceWorld.Values("phase", phase, "actor", actorUid, "outcome", Outcome,
                "item", itemUid, "itemName", itemName, "beforeCount", beforeCount, "count", Count(),
                "start", TraceWorld.Position(start), "position", Identity ? TraceWorld.Position(Actor.tr.position) : null,
                "itemActive", ItemIdentity && WorldNative.Active(item), "root", Root(Actor)?.GetType().Name,
                "job", Actor?.job, "jobBig", Actor?.jobBig, "jobCode", Actor?.jobCode.ToString(), "jobBigCode", Actor?.jobBigCode.ToString(),
                "assignedPos", Identity ? TraceWorld.Position(Actor.assignedPos) : null,
                "agentName", Actor?.agentName, "employer", Actor?.employer?.UID, "following", Actor?.following?.UID,
                "hasEmployer", Actor?.hasEmployer, "hasFollowing", Actor?.hasFollowing, "joiningParty", Actor?.relationships?.joiningParty,
                "obligation", Identity ? Obligation : null, "gameTime", Time.time);
            plugin.Log("BURRITO_NPC_PROBE " + JsonConvert.SerializeObject(data));
            if (plugin.Harness?.Enabled == true) plugin.Awareness.HarnessRecord("npc-probe", data);
        }
        internal void DrawCommands()
        {
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(210));
            var wrapped = new GUIStyle(GUI.skin.label) { wordWrap = true };
            GUILayout.Label("NPC EXPERIMENT — " + Status, wrapped, GUILayout.Width(200));
            GUILayout.Label("Select in World Awareness; close it to command.", wrapped, GUILayout.Width(200));
            if (GUILayout.Button("Enroll selected NPC")) Enroll(plugin.Awareness.ResolveLive(plugin.Awareness.TargetKey) as Agent);
            if (GUILayout.Button("Pick up selected food")) Begin(plugin.Awareness.ResolveLive(plugin.Awareness.TargetKey) as Item);
            if (GUILayout.Button("Cancel action")) Cancel();
            if (GUILayout.Button("Disable / release NPC")) Disable();
            GUILayout.EndVertical();
        }

        [HarmonyPatch(typeof(AgentInteractions), "GoHere")]
        private static class ExternalCommandPatch
        {
            /// <summary>
            /// Cancel our pickup before a newer native GoHere command writes its job fields.
            /// A replacement can have the same destination, so checking coordinates afterward
            /// cannot establish ownership.
            /// </summary>
            [HarmonyPrefix]
            internal static void Prefix(Agent __0)
            {
                var probe = Plugin.Instance?.NpcProbe;
                if (probe?.Active == true && probe.Identity && probe.Actor == __0) probe.Cancel("new-native-command");
            }
        }

        private sealed class PickupGoal : GoalDoJob
        {
            private readonly Agent subject;
            private readonly Vector3 destination;
            private readonly Vector3 previousPosition;
            internal bool Ended { get; private set; }
            internal bool OwnsFields => subject.job == "GoHere" && subject.jobCode == subject.GetJobCode("GoHere") &&
                subject.jobBig == "GoHere" && subject.jobBigCode == subject.GetJobCode("GoHere") && subject.assignedPos == destination;
            internal PickupGoal(Agent actor, Vector3 position)
            {
                subject = actor; destination = position; previousPosition = actor.assignedPos;
                curJob = "GoHere"; curJobCode = actor.GetJobCode("GoHere"); assignedPos = position;
            }
            /// <summary>
            /// End the leased native job and clear each job field only while it still belongs to
            /// us.
            /// Native commands can replace only part of the job before replacing the root; their
            /// newer fields must survive.
            /// </summary>
            public override void Terminate()
            {
                string job = subject.job; var code = subject.jobCode;
                string big = subject.jobBig; var bigCode = subject.jobBigCode; var position = subject.assignedPos;
                bool ownsJob = job == "GoHere" && code == subject.GetJobCode("GoHere");
                bool ownsBig = big == "GoHere" && bigCode == subject.GetJobCode("GoHere");
                base.Terminate(); Ended = true;
                // Native assignments can replace only job (Ruckus/Follow) before replacing the root.
                // Clear our remaining fields independently; restore every field owned by that newer assignment.
                subject.job = ownsJob ? "" : job; subject.jobCode = ownsJob ? subject.GetJobCode("") : code;
                subject.jobBig = ownsBig ? "" : big; subject.jobBigCode = ownsBig ? subject.GetJobCode("") : bigCode;
                subject.assignedPos = ownsJob && ownsBig && position == destination ? previousPosition : position;
            }
        }
    }
}
