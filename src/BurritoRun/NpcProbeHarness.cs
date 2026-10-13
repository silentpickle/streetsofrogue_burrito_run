using UnityEngine;

namespace BurritoRun
{
    internal sealed class NpcProbeHarness
    {
        private readonly Plugin plugin;
        private readonly NpcDesireFixture fixture;
        private int stage;
        private float due, deadline;
        internal string Status { get; private set; }
        internal string Reason { get; private set; }
        internal NpcProbeHarness(Plugin plugin, Agent player)
        {
            this.plugin = plugin; fixture = new NpcDesireFixture(player);
            due = Time.time + 2; deadline = Time.realtimeSinceStartup + 60;
        }
        private void Stop(string status, string reason)
        {
            Status = status; Reason = reason;
            plugin.Awareness.HarnessRecord("npc-gate", TraceWorld.Values("status", status, "reason", reason, "stage", stage));
        }
        internal void Tick()
        {
            if (Status != null) return;
            if (Time.realtimeSinceStartup > deadline) { Stop("FAIL", "npc-probe-timeout-" + stage); return; }
            if (!fixture.Owned) { Stop("FAIL", "fixture-lost"); return; }
            if (Time.timeScale <= 0 || Time.time < due) return;
            var probe = plugin.NpcProbe;
            if (stage == 0)
            {
                if (!probe.Enroll(fixture.Actor)) return;
                fixture.SpawnFood(); stage = 1; due = Time.time + 1;
            }
            else if (stage == 1)
            {
                if (!probe.Begin(fixture.Food)) { Stop("FAIL", "begin-refused-" + probe.Status); return; }
                stage = 2;
            }
            else if (stage == 2 && !probe.Active)
            {
                if (probe.Outcome != "picked-up") { Stop("FAIL", probe.Outcome); return; }
                stage = 3; due = probe.CooldownUntil + 0.5f;
            }
            else if (stage == 3)
            {
                if (!NpcPickupProbe.Idle(fixture.Actor)) { Stop("FAIL", "not-idle-after-pickup"); return; }
                fixture.SpawnFood("Banana"); stage = 4; due = Time.time + 1;
            }
            else if (stage == 4)
            {
                if (!probe.Begin(fixture.Food)) { Stop("FAIL", "cancel-begin-refused"); return; }
                stage = 5; due = Time.time + 0.1f;
            }
            else if (stage == 5)
            { probe.Cancel(); stage = 6; due = probe.CooldownUntil + 0.5f; }
            else if (stage == 6)
            {
                if (!NpcPickupProbe.Idle(fixture.Actor) || fixture.Actor.inventory.FindItem("Banana") != null)
                { Stop("FAIL", "cancel-did-not-release"); return; }
                if (!probe.Begin(fixture.Food)) { Stop("FAIL", "emergency-begin-refused"); return; }
                fixture.Emergency(true); stage = 7;
            }
            else if (stage == 7 && !probe.Active)
            {
                fixture.Emergency(false);
                if (probe.Outcome != "emergency" || fixture.Actor.inventory.FindItem("Banana") != null)
                { Stop("FAIL", "emergency-not-released"); return; }
                stage = 8; due = Time.time + 1;
            }
            else if (stage == 8)
            {
                if (fixture.Actor.job == "GoHere" || fixture.Actor.jobBig == "GoHere") { Stop("FAIL", "stale-job"); return; }
                Stop("COMPLETE", "pickup-cancel-emergency-native-release");
            }
        }
        internal void Close() { plugin.NpcProbe.Disable("fixture-close"); fixture.Close(); }
    }
}
