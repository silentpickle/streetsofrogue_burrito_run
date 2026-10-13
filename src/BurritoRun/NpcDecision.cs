using System;
using System.Collections.Generic;

namespace BurritoRun
{
    internal sealed class NpcCandidate
    {
        public string Key { get; set; }
        public string Item { get; set; }
        public double Distance { get; set; }
        public double Age { get; set; }
        public string Rejection { get; set; }
    }
    internal sealed class NpcDecisionInput
    {
        public int PolicyVersion { get; set; } = NpcDecision.Version;
        public bool PreferBanana { get; set; }
        public bool Emergency { get; set; }
        public string NativeObligation { get; set; }
        public string CurrentTarget { get; set; }
        public string Inhibition { get; set; }
        public NpcCandidate[] Candidates { get; set; } = Array.Empty<NpcCandidate>();
    }
    internal sealed class NpcCandidateScore
    {
        public string Key { get; set; }
        public string Item { get; set; }
        public double Age { get; set; }
        public double Preference { get; set; }
        public double TravelCost { get; set; }
        public double Score { get; set; }
        public string Rejection { get; set; }
    }
    internal sealed class NpcDecisionResult
    {
        public string Intention { get; set; }
        public string Target { get; set; }
        public string Reason { get; set; }
        public NpcCandidateScore[] Candidates { get; set; }
    }
    internal sealed class NpcDecisionCapture
    {
        public NpcDecisionInput Input { get; set; }
        public NpcDecisionResult Output { get; set; }
        /// <summary>
        /// Copy a decision into plain values for recording and replay.
        /// The capture worker rejects arbitrary objects, and old policy versions must keep their
        /// original input shape.
        /// </summary>
        internal Dictionary<string, object> Detach()
        {
            var candidates = new List<object>();
            foreach (var c in Input.Candidates) candidates.Add(new Dictionary<string, object> {
                { "Key", c.Key }, { "Item", c.Item }, { "Distance", c.Distance }, { "Age", c.Age }, { "Rejection", c.Rejection } });
            var scores = new List<object>();
            foreach (var c in Output.Candidates) scores.Add(new Dictionary<string, object> {
                { "Key", c.Key }, { "Item", c.Item }, { "Age", c.Age }, { "Preference", c.Preference },
                { "TravelCost", c.TravelCost }, { "Score", c.Score }, { "Rejection", c.Rejection } });
            var input = new Dictionary<string, object> {
                    { "PolicyVersion", Input.PolicyVersion }, { "PreferBanana", Input.PreferBanana }, { "Emergency", Input.Emergency },
                    { "CurrentTarget", Input.CurrentTarget }, { "Inhibition", Input.Inhibition }, { "Candidates", candidates } };
            if (Input.PolicyVersion >= 2) input["NativeObligation"] = Input.NativeObligation;
            return new Dictionary<string, object> {
                { "Input", input },
                { "Output", new Dictionary<string, object> {
                    { "Intention", Output.Intention }, { "Target", Output.Target }, { "Reason", Output.Reason }, { "Candidates", scores } } } };
        }

    }
    // Pure, deterministic policy. It never commands the game or assumes registry visibility.
    internal static class NpcDecision
    {
        internal const int Version = 2;
        // Optional activity may replace ordinary idling/wandering, never a specialist or emergency goal.
        /// <summary>
        /// Recognize ordinary idling and wandering that optional food pickup may interrupt.
        /// WanderFar is a normal civilian state too; treating it as a specialist job excluded real
        /// residents.
        /// </summary>
        internal static bool OptionalIdleRoot(string name) => name == "GoalIdle" || name == "GoalWander" || name == "GoalWanderFar";
        /// <summary>
        /// Choose or continue one food action using only the supplied observations.
        /// Keeping scoring and precedence free of game calls makes decisions explainable and old
        /// captures replayable.
        /// </summary>
        internal static NpcDecisionResult Evaluate(NpcDecisionInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.PolicyVersion != 1 && input.PolicyVersion != Version) throw new ArgumentException("Incompatible NPC policy version: " + input.PolicyVersion);
            if (input.Candidates == null || input.Candidates.Length > 16) throw new ArgumentException("Expected at most 16 candidates");
            var scores = new List<NpcCandidateScore>();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            NpcCandidateScore winner = null;
            foreach (var candidate in input.Candidates)
            {
                if (candidate == null || string.IsNullOrEmpty(candidate.Key) || !keys.Add(candidate.Key) ||
                    double.IsNaN(candidate.Distance) || double.IsInfinity(candidate.Distance) || candidate.Distance < 0 ||
                    double.IsNaN(candidate.Age) || double.IsInfinity(candidate.Age) || candidate.Age < 0)
                    throw new ArgumentException("Invalid candidate identity, distance or age");
                string rejection = candidate.Rejection;
                bool supported = candidate.Item == "Fud" || candidate.Item == "Banana";
                if (!supported) rejection = rejection ?? "unsupported-item";
                if (candidate.Age > 2) rejection = rejection ?? "stale-observation";
                if (candidate.Distance > 6) rejection = rejection ?? "out-of-range";
                double preference = !supported ? 0 : (candidate.Item == "Banana") == input.PreferBanana ? 8 : 5;
                var score = new NpcCandidateScore
                {
                    Key = candidate.Key,
                    Item = candidate.Item,
                    Age = candidate.Age,
                    Preference = preference,
                    TravelCost = candidate.Distance,
                    Score = preference - candidate.Distance,
                    Rejection = rejection
                };
                scores.Add(score);
                if (score.Rejection != null || score.Score <= 0) continue;
                if (winner == null || score.Score > winner.Score || score.Score == winner.Score &&
                    (score.TravelCost < winner.TravelCost || score.TravelCost == winner.TravelCost && string.CompareOrdinal(score.Key, winner.Key) < 0)) winner = score;
            }
            scores.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
            // Version 1 retains its original commitment precedence for historical replays.
            string obligation = input.PolicyVersion >= 2 ? input.NativeObligation : null;
            return new NpcDecisionResult
            {
                Intention = input.Emergency ? "cancel" : obligation != null ? input.CurrentTarget != null ? "cancel" : "idle" : input.CurrentTarget != null ? "continue" : input.Inhibition != null ? "idle" : winner != null ? "pickup" : "idle",
                Target = input.Emergency || obligation != null ? null : input.CurrentTarget ?? (input.Inhibition == null ? winner?.Key : null),
                Reason = input.Emergency ? "native-emergency" : obligation ?? (input.CurrentTarget != null ? "committed-action" : input.Inhibition ?? (winner != null ? "highest-positive-score" : "no-positive-eligible-action")),
                Candidates = scores.ToArray()
            };
        }
    }
}
