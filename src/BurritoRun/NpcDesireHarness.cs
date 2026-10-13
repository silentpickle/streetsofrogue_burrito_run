using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace BurritoRun
{
    internal sealed class NpcDesireHarness
    {
        private readonly Plugin plugin;
        private readonly Agent player;
        private readonly string baseline;
        private NpcDesireFixture fixture;
        private readonly string[] cases = { "prefer-fud", "prefer-banana", "distance", "protected", "removed", "timeout", "new-command", "actor-removed", "blocked", "refused", "pause", "combat",
            "employment-before", "following-before", "recruit-before", "employment-during", "recruit-during", "specialist-task", "replacement-root", "native-wander", "level-change" };
        private int index, stage;
        private readonly int startingLevel;
        private float due, deadline;
        private string expected;
        private string enrollmentRoot;
        private float pausedUntil, pausedGameTime;
        private Vector3 pausedPosition;
        private Agent retiredActor;
        private bool beginRejected, enrollRejected;
        private Goal replacementRoot;
        private Vector3 nativeDestination;
        internal string Status { get; private set; }
        internal string Reason { get; private set; }
        internal NpcDesireHarness(Plugin plugin, Agent player)
        { this.plugin = plugin; this.player = player; baseline = PlayerState(); startingLevel = plugin.Awareness.HarnessLevel; Setup(); }
        private string PlayerState()
        {
            var inventory = new List<object>();
            foreach (var item in player.inventory.InvItemList) inventory.Add(TraceWorld.Values("id", item.invItemID, "name", item.invItemName, "count", item.invItemCount));
            return JsonConvert.SerializeObject(TraceWorld.Values("health", player.health, "inventory", inventory));
        }
        private void Setup()
        {
            fixture = new NpcDesireFixture(player, cases[index] == "native-wander"); retiredActor = null; replacementRoot = null; beginRejected = enrollRejected = false; enrollmentRoot = null; stage = 0;
            due = Time.time + 2; deadline = Time.realtimeSinceStartup + 25;
        }
        private void Record(string phase) => plugin.Awareness.HarnessRecord("npc-desire-case", TraceWorld.Values(
            "phase", phase, "case", cases[index], "coordinationRevision", 3, "agentName", fixture.Actor?.agentName, "expected", expected, "outcome", plugin.NpcProbe.Outcome,
            "actor", fixture.Actor?.UID, "foodUid", fixture.Food?.UID, "secondUid", fixture.Second?.UID,
            "eligibility", plugin.NpcProbe.Identity ? plugin.NpcProbe.Eligibility(fixture.Food) : null, "fud", fixture.Owned ? fixture.Actor.inventory.FindItem("Fud")?.invItemCount ?? 0 : -1,
            "banana", fixture.Owned ? fixture.Actor.inventory.FindItem("Banana")?.invItemCount ?? 0 : -1,
            "enrolled", plugin.NpcProbe.Actor != null, "retiredDead", retiredActor != null && retiredActor.dead,
            "root", NpcPickupProbe.Root(fixture.Actor)?.GetType().Name, "active", plugin.NpcProbe.Active,
            "enrollmentRoot", enrollmentRoot, "defaultGoal", fixture.Actor?.defaultGoal,
            "job", fixture.Actor?.job, "jobBig", fixture.Actor?.jobBig,
            "jobCode", fixture.Actor?.jobCode.ToString(), "jobBigCode", fixture.Actor?.jobBigCode.ToString(),
            "assignedPos", fixture.Owned ? TraceWorld.Position(fixture.Actor.assignedPos) : null, "nativeDestination", TraceWorld.Position(nativeDestination),
            "employer", fixture.Actor?.employer?.UID, "following", fixture.Actor?.following?.UID, "player", player.UID,
            "joiningParty", fixture.Actor?.relationships?.joiningParty, "hasEmployer", fixture.Actor?.hasEmployer, "hasFollowing", fixture.Actor?.hasFollowing,
            "beginRejected", beginRejected, "enrollRejected", enrollRejected, "replacementRootPreserved", replacementRoot != null && ReferenceEquals(NpcPickupProbe.Root(fixture.Actor), replacementRoot),
            "decision", plugin.NpcDesires.Latest?.Output.Reason,
            "playerUnchanged", PlayerState() == baseline, "status", Status, "reason", Reason));
        private void Stop(string reason)
        { Status = "FAIL"; Reason = cases[index] + ":" + reason; Record("failed"); }
        internal void Tick()
        {
            if (Status != null) return;
            if (stage == 5)
            {
                if (Time.realtimeSinceStartup < deadline) return;
                plugin.Awareness.HarnessCloseDetail();
                Status = "COMPLETE"; Reason = "npc-desire-cases-complete"; return;
            }
            if (stage == 4)
            {
                var gc = GameController.gameController;
                WorldFixture.AcceptReload(gc);
                if (Time.realtimeSinceStartup > deadline) { Status = "FAIL"; Reason = "reload-timeout"; return; }
                if (!WorldFixture.Ready(gc) || plugin.Awareness.HarnessLevel == startingLevel) return;
                bool cleared = plugin.NpcProbe.Actor == null && !plugin.NpcProbe.Active && !plugin.NpcDesires.Enabled;
                plugin.Awareness.HarnessRecord("npc-scope-ended", TraceWorld.Values("cleared", cleared,
                    "oldLevel", startingLevel, "newLevel", plugin.Awareness.HarnessLevel, "ready", true,
                    "outcome", plugin.NpcProbe.Outcome));
                if (!cleared) { Status = "FAIL"; Reason = "scope-not-cleared"; return; }
                plugin.Awareness.ToggleDetail(); stage = 5; deadline = Time.realtimeSinceStartup + 20;
                plugin.Log("BURRITO_NPC_REVIEW inspector open for 20 seconds after native validation"); return;
            }
            if (Time.realtimeSinceStartup > deadline) { Stop("timeout-stage-" + stage); return; }
            if (pausedUntil > 0)
            {
                if (Time.realtimeSinceStartup < pausedUntil) return;
                bool unchanged = Time.timeScale == 0 && Time.time == pausedGameTime && plugin.NpcProbe.Active &&
                    fixture.Actor.tr.position == pausedPosition && fixture.Actor.inventory.FindItem("Fud") == null;
                plugin.Awareness.HarnessRecord("npc-pause", TraceWorld.Values("unchanged", unchanged));
                plugin.Awareness.HarnessCloseDetail(); pausedUntil = 0;
                if (!unchanged) { Stop("pause-state-changed"); return; }
                stage = 3; due = Time.time + 0.5f;
            }
            if (Time.timeScale <= 0 || Time.time < due) return;
            var probe = plugin.NpcProbe;
            string name = cases[index];
            if (stage == 0)
            {
                enrollmentRoot = NpcPickupProbe.Root(fixture.Actor)?.GetType().Name;
                if (name == "native-wander" && enrollmentRoot != "GoalWanderFar") return;
                if (!probe.Enroll(fixture.Actor)) return;
                if (name == "blocked")
                { if (!fixture.BlockedFood()) { Stop("no-native-obstruction-available"); return; } }
                else fixture.Pair(name == "distance");
                if (name == "refused") fixture.RefusePickup();
                expected = name == "prefer-fud" || name == "native-wander" ? "Fud" : "Banana";
                if (name == "protected") fixture.Food.invItem.stealable = fixture.Second.invItem.stealable = true;
                stage = 1; due = Time.time + (name == "native-wander" ? 0.05f : 2);
            }
            else if (stage == 1)
            {
                Record("start");
                if (name == "employment-before" || name == "following-before" || name == "recruit-before")
                {
                    if (name == "employment-before") fixture.Actor.SetEmployer(player);
                    else if (name == "following-before") fixture.Actor.SetFollowing(player);
                    else fixture.Actor.agentInteractions.AssistMe(fixture.Actor, player);
                    stage = 6; due = Time.time + 0.5f; return;
                }
                if (index <= 3 || name == "recruit-during")
                { plugin.NpcDesires.PreferBanana = name == "prefer-banana"; plugin.NpcDesires.SetEnabled(true); }
                else if (name == "blocked")
                {
                    if (probe.Eligibility(fixture.Food) != "obstructed" || probe.Begin(fixture.Food)) { Stop("obstructed-begin-accepted"); return; }
                    stage = 3; due = Time.time + 0.5f; return;
                }
                else if (!probe.Begin(fixture.Food)) { Stop("probe-begin-refused-" + probe.Status); return; }
                stage = 2; due = Time.time + (index <= 3 ? 0 : 0.05f);
            }
            else if (stage == 6)
            {
                if (name == "recruit-before" && (fixture.Actor.employer != player || fixture.Actor.following != player)) return;
                beginRejected = !probe.Begin(fixture.Food);
                if (!beginRejected || probe.Active) { Stop("native-commitment-accepted-pickup"); return; }
                // Also prove an already committed native actor cannot be newly enrolled.
                probe.Disable(); enrollRejected = !probe.Enroll(fixture.Actor);
                if (!enrollRejected) { Stop("native-commitment-accepted-enrollment"); return; }
                stage = 3; due = Time.time + 0.6f;
            }
            else if (stage == 2)
            {
                if (name == "protected")
                {
                    if (probe.Active) { Stop("protected-action-started"); return; }
                    if (Time.time < due + 2) return;
                    if (plugin.NpcDesires.Latest?.Output.Intention != "idle") { Stop("protected-not-idle"); return; }
                    stage = 3;
                }
                else if (index <= 2 || name == "native-wander")
                {
                    if (probe.Outcome == null || probe.Active) return;
                    if (probe.Outcome != "picked-up" || fixture.Actor.inventory.FindItem(expected)?.invItemCount != 1)
                    { Stop("wrong-pickup-" + probe.Outcome); return; }
                    stage = 3;
                }
                else
                {
                    if (name == "recruit-during" && !probe.Active) return;
                    if (name == "pause")
                    {
                        plugin.Awareness.ToggleDetail(); pausedUntil = Time.realtimeSinceStartup + 0.5f;
                        pausedGameTime = Time.time; pausedPosition = fixture.Actor.tr.position; return;
                    }
                    if (name == "combat") fixture.Combat();
                    else if (name == "employment-during") fixture.Actor.SetEmployer(player);
                    else if (name == "recruit-during") fixture.Actor.agentInteractions.AssistMe(fixture.Actor, player);
                    else if (name == "specialist-task" || name == "replacement-root")
                    {
                        nativeDestination = fixture.Second.tr.position;
                        fixture.Actor.agentInteractions.CauseRuckus(fixture.Actor, player, nativeDestination);
                        if (name == "replacement-root")
                        {
                            replacementRoot = new GoalDoJob { curJob = "Ruckus", curJobCode = fixture.Actor.GetJobCode("Ruckus"), assignedPos = nativeDestination };
                            fixture.Actor.brainUpdate.SwitchGoal(replacementRoot);
                        }
                        Record("native-command");
                    }
                    else if (name == "level-change")
                    {
                        WorldFixture.Reload(player.gc); stage = 4;
                        deadline = Time.realtimeSinceStartup + 120; return;
                    }
                    else if (name == "removed") fixture.RemoveFood();
                    else if (name == "timeout") fixture.MoveFoodToOtherSide();
                    else if (name == "new-command") fixture.Actor.agentInteractions.GoHere(fixture.Actor, fixture.Actor, fixture.Food.tr.position);
                    else if (name == "actor-removed") { retiredActor = fixture.Actor; fixture.Close(); }
                    stage = 3; due = Time.time + (name == "timeout" ? 9 : name == "recruit-during" ? 9 : name == "refused" ? 5 : 0.6f);
                }
                if (index <= 3 || name == "native-wander") { plugin.NpcDesires.SetEnabled(false); due = Time.time + 0.6f; }
            }
            else if (stage == 3)
            {
                if (name == "pause" && probe.Active) return;
                if (probe.Active) { Stop("still-active"); return; }
                if (name == "pause" && probe.Outcome != "picked-up") { Stop("pause-resume-no-pickup"); return; }
                string wanted = name == "refused" ? "pickup-unconfirmed" : name == "combat" ? "emergency" : name == "removed" ? "target-unavailable" : name == "timeout" ? "timed-out" : name == "new-command" ? "new-native-command" : name == "actor-removed" ? "actor-removed" : null;
                if (wanted != null && probe.Outcome != wanted && !(name == "actor-removed" && probe.Outcome == "actor-dead" && retiredActor.dead && probe.Actor == null)) { Stop("wrong-interruption-" + probe.Outcome); return; }
                if (name == "new-command" && fixture.Actor.jobBig != "GoHere") { Stop("new-command-cleared"); return; }
                if (name == "employment-during" && (probe.Outcome != "native-employment" || fixture.Actor.employer != player)) { Stop("employment-not-preserved"); return; }
                if (name == "recruit-during" && (probe.Outcome != "native-recruitment" && probe.Outcome != "native-employment" ||
                    fixture.Actor.employer != player || fixture.Actor.following != player || plugin.NpcDesires.Latest?.Output.Intention != "idle"))
                { Stop("recruitment-not-preserved"); return; }
                if (name == "specialist-task" || name == "replacement-root")
                {
                    if (probe.Outcome != "native-job" || fixture.Actor.job != "Ruckus" || fixture.Actor.jobCode != fixture.Actor.GetJobCode("Ruckus") ||
                        fixture.Actor.jobBig == "GoHere" || fixture.Actor.assignedPos != nativeDestination ||
                        name == "replacement-root" && !ReferenceEquals(NpcPickupProbe.Root(fixture.Actor), replacementRoot))
                    { Stop("native-specialist-command-changed"); return; }
                }
                if (PlayerState() != baseline) { Stop("player-changed"); return; }
                Record("complete");
                plugin.NpcDesires.SetEnabled(false); probe.Disable(); fixture.Close();
                index++;
                if (index == cases.Length) { Status = "COMPLETE"; Reason = "npc-desire-cases-complete"; return; }
                Setup();
            }
        }
        internal void Close() { if (pausedUntil > 0) plugin.Awareness.HarnessCloseDetail(); plugin.NpcDesires.SetEnabled(false); plugin.NpcProbe.Disable("fixture-close"); fixture?.Close(); }
    }
}
