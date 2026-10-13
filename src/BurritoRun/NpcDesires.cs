using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace BurritoRun
{
    internal sealed class NpcDesires
    {
        private readonly Plugin plugin;
        internal bool Enabled { get; private set; }
        internal bool PreferBanana { get; set; }
        internal NpcDecisionCapture Latest { get; private set; }
        private float nextDecision;
        private string signature;
        private string targetKey;
        private int decisionActor;
        private float decisionTime;
        internal NpcDesires(Plugin plugin) { this.plugin = plugin; }
        /// <summary>
        /// Enable optional food choices for the enrolled actor, or cancel that work when disabled.
        /// Enrollment and autonomy are separate choices so inspecting a resident does not silently
        /// take control.
        /// </summary>
        internal void SetEnabled(bool enabled)
        {
            Enabled = enabled && plugin.NpcProbe.Identity;
            if (Enabled && plugin.Awareness.Mode == AwarenessMode.Off) plugin.Awareness.SetMode(AwarenessMode.Inspect);
            nextDecision = 0; signature = null;
            if (!Enabled) { plugin.NpcProbe.Cancel("autonomy-disabled"); targetKey = null; }
        }
        /// <summary>
        /// Gather nearby food observations, score them, and request the chosen native action.
        /// Bounded sampling and change-only recording keep this adapter affordable; the executor
        /// rechecks live safety.
        /// </summary>
        internal void Tick()
        {
            if (!Enabled) return;
            var probe = plugin.NpcProbe;
            if (!probe.Identity) { Enabled = false; targetKey = null; return; }
            if (Time.timeScale <= 0 || Time.time < nextDecision) return;
            nextDecision = Time.time + 0.4f;
            if (!probe.Active) targetKey = null;
            var actor = probe.Actor;
            var candidates = new List<NpcCandidate>();
            var query = plugin.Awareness.Index.Query(new WorldQuery
            { X = actor.tr.position.x, Y = actor.tr.position.y, Kind = WorldKind.Item, Tag = "food", MaxDistance = 6, Limit = 16 });
            foreach (var snapshot in query.Entities)
            {
                var item = plugin.Awareness.ResolveLive(snapshot.Key) as Item;
                candidates.Add(new NpcCandidate
                {
                    Key = snapshot.Key,
                    Item = item?.invItem?.invItemName ?? snapshot.Name,
                    Distance = item?.tr != null ? Vector2.Distance(actor.tr.position, item.tr.position) :
                        Math.Sqrt(WorldGeometry.DistanceSquared(actor.tr.position.x, actor.tr.position.y, snapshot)),
                    Age = Math.Max(0, Time.time - snapshot.GameTime),
                    Rejection = snapshot.Collection.StartsWith("lifecycle:", StringComparison.Ordinal) ? "pending-detail" : probe.Eligibility(item)
                });
            }
            var input = new NpcDecisionInput { PreferBanana = PreferBanana, Emergency = NpcPickupProbe.Emergency(actor), NativeObligation = probe.Obligation, CurrentTarget = targetKey, Inhibition = Time.time < probe.CooldownUntil ? "cooldown" : null, Candidates = candidates.ToArray() };
            var output = NpcDecision.Evaluate(input);
            decisionActor = actor.UID; decisionTime = Time.time;
            Latest = new NpcDecisionCapture { Input = input, Output = output };
            // Changes in winner/eligibility/action are events; age and small distance changes are not a log stream.
            string current = output.Intention + ":" + output.Reason + ":" + output.Target + ":" + PreferBanana;
            foreach (var row in output.Candidates) current += "|" + row.Key + ":" + row.Rejection;
            if (signature != current)
            {
                signature = current;
                plugin.Log("BURRITO_NPC_DECISION " + JsonConvert.SerializeObject(Latest));
                if (plugin.Harness?.Enabled == true) plugin.Awareness.HarnessRecord("npc-decision", TraceWorld.Values("capture", Latest.Detach()));
            }
            if (output.Intention == "cancel") probe.Cancel(input.Emergency ? "emergency" : input.NativeObligation);
            else if (output.Intention == "pickup" && NpcPickupProbe.Idle(actor) && Time.time >= probe.CooldownUntil &&
                probe.Begin(plugin.Awareness.ResolveLive(output.Target) as Item)) targetKey = output.Target;
        }
        /// <summary>
        /// Show the explicit autonomy and food-preference controls.
        /// Commands stay outside the read-only inspector so viewing a decision does not change it.
        /// </summary>
        internal void DrawCommands()
        {
            bool enabled = GUILayout.Toggle(Enabled, "Autonomous choices");
            if (enabled != Enabled) SetEnabled(enabled);
            PreferBanana = GUILayout.Toggle(PreferBanana, "Prefer Banana (8 vs 5)");
        }
        /// <summary>
        /// Explain the last sampled decision, including scores, rejection reasons and sample time.
        /// This is retained diagnostic history, which may outlive the actor or level it describes.
        /// </summary>
        internal void DrawInspection()
        {
            var wrapped = new GUIStyle(GUI.skin.label) { wordWrap = true };
            GUILayout.Label("NPC DECISION — " + plugin.NpcProbe.Status, wrapped, GUILayout.MaxWidth(360));
            if (Latest == null) { GUILayout.Label("No decision captured."); return; }
            GUILayout.Label("Last sample: NPC " + decisionActor + " at game time " + decisionTime.ToString("0.0"));
            GUILayout.Label("Policy " + Latest.Input.PolicyVersion + " | " + Latest.Output.Intention + " | " + Latest.Output.Reason, wrapped, GUILayout.MaxWidth(360));
            foreach (var row in Latest.Output.Candidates)
                GUILayout.Label(row.Item + " [" + row.Key + "] age " + row.Age.ToString("0.0") + "s: " +
                    row.Preference + " - " + row.TravelCost.ToString("0.00") + " = " + row.Score.ToString("0.00") +
                    " | " + (row.Rejection ?? (row.Key == Latest.Output.Target ? "chosen" : "eligible")), wrapped, GUILayout.MaxWidth(360));
        }
    }
}
