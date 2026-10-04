using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using BurritoRun;

internal static class TraceChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var directory = Path.Combine(Path.GetTempPath(), "burrito-trace-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var approach = new SampledApproach();
        approach.Sample(true, 1, 4, 0, 0, 0);
        check(approach.Approach == 0, "first matching sample cannot credit earlier patrol movement");
        approach.Sample(true, 1.2f, 3, 0, 0, 0);
        check(approach.Approach == 1 && approach.Displacement == 1, "consecutive matching samples measure approach");
        approach.Sample(false, 1.4f, 2, 0, 0, 0);
        approach.Sample(true, 1.6f, 1, 0, 0, 0);
        check(approach.Approach == 1 && approach.Segment == 2, "goal interruption excludes movement between segments");
        approach.Sample(true, 3, 0, 0, 0, 0);
        check(approach.Approach == 1 && approach.Segment == 3, "large sampling gap cannot credit unseen movement");
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var path = Path.Combine(directory, "values.jsonl");
            var storage = new TraceStorage(path);
            var values = new Dictionary<string, object>
            {
                { "number", 1.25f }, { "missing", null }, { "text", "woof\n\"\\🐕" },
                { "point", new[] { 0.64f, -2.5f, 0f } }, { "flag", false }
            };
            check(storage.Write(values), "trace writes numeric snapshot");
            using var json = JsonDocument.Parse(File.ReadAllText(path));
            check(json.RootElement.GetProperty("number").GetSingle() == 1.25f &&
                json.RootElement.GetProperty("missing").ValueKind == JsonValueKind.Null &&
                json.RootElement.GetProperty("text").GetString() == "woof\n\"\\🐕" &&
                json.RootElement.GetProperty("point")[1].GetSingle() == -2.5f,
                "trace preserves numeric types, missing values, escaping and non-English culture");
            var capped = new TraceStorage(Path.Combine(directory, "capped.jsonl"), 256);
            check(!capped.Write(new Dictionary<string, object> { { "payload", new string('x', 200) } }) &&
                capped.Dropped == 1 && capped.Limited, "trace cap records dropped snapshot");
            check(capped.Write(new Dictionary<string, object> { { "event", "terminal" } }, true),
                "trace reserves capacity for terminal result");
            var failed = new TraceStorage(Path.Combine(directory, "missing", "trace.jsonl"));
            check(!failed.Write(values) && failed.Failed && failed.Failure != null &&
                !failed.Write(values) && failed.Dropped == 2, "trace write failure is explicit and bounded");
            var history = new TraceHistory<int>(3);
            history.Add(1); history.Add(2); history.Add(3);
            var prior = history.Snapshot();
            history.Add(4);
            check(prior[0] == 1 && history.Snapshot()[0] == 2 && history.Evicted == 1,
                "history evicts oldest and preserves exported ordering");
            history.Prune(value => value < 4);
            check(history.Snapshot().Length == 1 && history.Snapshot()[0] == 4,
                "history prunes time-expired samples even under slow capture");
            history.Clear();
            check(history.Snapshot().Length == 0 && history.Evicted == 0,
                "history clears across level identities");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            Directory.Delete(directory, true);
        }
    }
}
