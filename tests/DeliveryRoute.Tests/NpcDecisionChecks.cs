using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BurritoRun;

internal static class NpcDecisionChecks
{
    internal static void Run(Action<bool, string> check)
    {
        check(NpcDecision.OptionalIdleRoot("GoalWanderFar"), "NPC naturally generated WanderFar resident permits enrollment");
        check(NpcDecision.OptionalIdleRoot("GoalIdle") && NpcDecision.OptionalIdleRoot("GoalWander"), "NPC existing idle roots remain eligible");
        foreach (string name in new[] { null, "Goal", "GoalPatrol", "GoalGuard", "GoalDoJob", "GoalFlee", "GoalCuriousObject", "GoalWanderFarAttack" })
            check(!NpcDecision.OptionalIdleRoot(name), "NPC non-idle root remains protected: " + (name ?? "missing"));
        var input = new NpcDecisionInput { Candidates = new[] {
            new NpcCandidate { Key = "f", Item = "Fud", Distance = 2 },
            new NpcCandidate { Key = "b", Item = "Banana", Distance = 2 } } };
        check(NpcDecision.Evaluate(input).Target == "f", "NPC Fud preference");
        input.PreferBanana = true;
        check(NpcDecision.Evaluate(input).Target == "b", "NPC preference reversal");
        input.Candidates[1].Distance = 6; input.Candidates[0].Distance = 1;
        check(NpcDecision.Evaluate(input).Target == "f", "NPC travel cost changes winner");
        input.Candidates[0].Rejection = "protected";
        check(NpcDecision.Evaluate(input).Target == "b", "NPC property rejection");
        input.Candidates[1].Age = 3;
        check(NpcDecision.Evaluate(input).Intention == "idle", "NPC stale observations rejected");
        input.CurrentTarget = "committed";
        check(NpcDecision.Evaluate(input).Target == "committed", "NPC commits to action");
        input.Emergency = true;
        check(NpcDecision.Evaluate(input).Intention == "cancel", "NPC emergency interrupts commitment");
        input = new NpcDecisionInput { Candidates = new[] { new NpcCandidate { Key = "b", Item = "Banana", Distance = 5 } } };
        check(NpcDecision.Evaluate(input).Target == null, "NPC zero score chooses idle");
        input.Candidates = new[] { new NpcCandidate { Key = "z", Item = "Fud", Distance = 2 }, new NpcCandidate { Key = "a", Item = "Fud", Distance = 2 } };
        check(NpcDecision.Evaluate(input).Target == "a", "NPC stable identity tie break");
        Array.Reverse(input.Candidates);
        var capture = new NpcDecisionCapture { Input = input, Output = NpcDecision.Evaluate(input) };
        string json = JsonConvert.SerializeObject(capture);
        check(Replay(json), "NPC detached capture replay");
        check(Replay(JsonConvert.SerializeObject(capture.Detach())), "NPC journal payload replay");
        check(CaptureWorker.Measure(capture.Detach()) < 65536, "NPC capture worker accepts bounded detached payload");
        var saved = JObject.Parse(json); saved["Output"]["Target"] = "wrong";
        check(!Replay(saved.ToString()), "NPC replay detects changed answer");
        saved = JObject.Parse(json); ((JObject)saved["Input"]).Remove("PolicyVersion");
        bool malformedRejected = false;
        try { Replay(saved.ToString()); } catch (ArgumentException) { malformedRejected = true; }
        check(malformedRejected, "NPC replay refuses missing version");
        input.Inhibition = "cooldown";
        check(NpcDecision.Evaluate(input).Intention == "idle", "NPC cooldown inhibits new actions");
        input.Inhibition = null;
        foreach (string rejection in new[] { "obstructed", "protected", "not-single", "unavailable", "already-held" })
        {
            foreach (var c in input.Candidates) c.Rejection = rejection;
            check(NpcDecision.Evaluate(input).Intention == "idle", "NPC rejects " + rejection);
        }
        foreach (var c in input.Candidates) c.Rejection = null;
        input.Candidates[0].Item = "Banana"; input.Candidates[0].Distance = 1;
        input.Candidates[1].Distance = 4;
        check(NpcDecision.Evaluate(input).Target == input.Candidates[0].Key, "NPC equal utility prefers shorter travel");
        input.PolicyVersion = 99;
        bool rejected = false;
        try { NpcDecision.Evaluate(input); } catch (ArgumentException) { rejected = true; }
        check(rejected, "NPC incompatible version explicit");
        input.PolicyVersion = 1; input.Candidates[0].Distance = double.NaN; rejected = false;
        try { NpcDecision.Evaluate(input); } catch (ArgumentException) { rejected = true; }
        check(rejected, "NPC nonfinite input rejected");
        input = new NpcDecisionInput { CurrentTarget = "food", Inhibition = "cooldown", NativeObligation = "native-employment" };
        var obligation = NpcDecision.Evaluate(input);
        check(obligation.Intention == "cancel" && obligation.Target == null && obligation.Reason == "native-employment", "NPC employment interrupts commitment");
        input.Emergency = true;
        check(NpcDecision.Evaluate(input).Reason == "native-emergency", "NPC danger precedes employment");
        input.Emergency = false; input.CurrentTarget = null;
        check(NpcDecision.Evaluate(input).Intention == "idle" && NpcDecision.Evaluate(input).Reason == "native-employment", "NPC employment blocks new action before cooldown");
        foreach (string reason in new[] { "native-recruitment", "native-following", "native-job", "native-busy" })
        {
            input.CurrentTarget = "food"; input.NativeObligation = reason;
            check(NpcDecision.Evaluate(input).Intention == "cancel" && NpcDecision.Evaluate(input).Reason == reason, "NPC obligation interrupts: " + reason);
        }
        input.NativeObligation = null;
        check(NpcDecision.Evaluate(input).Intention == "continue", "NPC cooldown does not interrupt owned action");
        input.CurrentTarget = null;
        check(NpcDecision.Evaluate(input).Intention == "idle" && NpcDecision.Evaluate(input).Reason == "cooldown", "NPC cooldown blocks only new action");
        capture = new NpcDecisionCapture { Input = input, Output = NpcDecision.Evaluate(input) };
        check(Replay(JsonConvert.SerializeObject(capture.Detach())), "NPC v2 detached capture replay");
        saved = JObject.FromObject(capture); ((JObject)saved["Input"]).Remove("NativeObligation"); malformedRejected = false;
        try { Replay(saved.ToString()); } catch (ArgumentException) { malformedRejected = true; }
        check(malformedRejected, "NPC v2 replay requires obligation field");
        input.PolicyVersion = 1; input.CurrentTarget = "food"; input.NativeObligation = "native-employment";
        check(NpcDecision.Evaluate(input).Intention == "continue", "NPC v1 preserves historical commitment precedence");
        const string historical = "{\"Input\":{\"PolicyVersion\":1,\"PreferBanana\":false,\"Emergency\":false,\"CurrentTarget\":\"food\",\"Inhibition\":\"native-busy\",\"Candidates\":[]},\"Output\":{\"Intention\":\"continue\",\"Target\":\"food\",\"Reason\":\"committed-action\",\"Candidates\":[]}}";
        check(Replay(historical), "NPC original v1 JSON without obligation replays unchanged");
    }
    private static bool Replay(string json)
    {
        var envelope = JObject.Parse(json);
        if (envelope["Input"] is not JObject input || input["PolicyVersion"]?.Type != JTokenType.Integer ||
            input["Candidates"] is not JArray || input["PreferBanana"]?.Type != JTokenType.Boolean ||
            input["Emergency"]?.Type != JTokenType.Boolean || envelope["Output"] is not JObject ||
            input["PolicyVersion"]?.Value<int>() == 2 && input["NativeObligation"]?.Type != JTokenType.Null && input["NativeObligation"]?.Type != JTokenType.String)
            throw new ArgumentException("Replay requires versioned Input, candidates, preferences, emergency and Output");
        var capture = envelope.ToObject<NpcDecisionCapture>();
        return capture?.Output != null && JToken.DeepEquals(JToken.FromObject(NpcDecision.Evaluate(capture.Input)), JToken.FromObject(capture.Output));
    }
    internal static int ReplayFile(string path)
    {
        try
        {
            bool match = Replay(File.ReadAllText(path));
            Console.WriteLine(match ? "PASS: decision replay matches" : "FAIL: decision replay differs or output missing");
            return match ? 0 : 1;
        }
        catch (Exception error) { Console.Error.WriteLine(error.Message); return 2; }
    }
}
