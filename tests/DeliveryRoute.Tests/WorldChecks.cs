using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using BurritoRun;

internal static class WorldChecks
{
    internal static void Run(Action<bool, string> check)
    {
        WorldEntitySnapshot Entity(string key, float x, float y, WorldKind kind = WorldKind.Item) =>
            new WorldEntitySnapshot { Key = key, X = x, Y = y, Kind = kind, Active = true, Name = key, NativeType = "Item", Tags = new[] { "food" } };
        var index = new WorldIndex();
        index.Put(Entity("edge", 6, 0)); index.Put(Entity("origin", 0, 0)); index.Put(Entity("beyond", 6.001f, 0));
        check(index.Query(new WorldQuery()).TotalMatched == 2, "within radius includes exact boundary");
        check(index.Query(new WorldQuery { Range = WorldRange.Outside }).Entities.Single().Key == "beyond", "outside excludes exact boundary");
        check(index.Query(new WorldQuery { Range = WorldRange.Band, MinDistance = 6 }).Entities.Single().Key == "edge", "band includes endpoints");
        check(index.Query(new WorldQuery { ObserverKey = "origin", Limit = 1 }).Entities[0].Key == "edge", "nearest excludes observer");
        index.Put(Entity("edge", -20, -20));
        check(index.Query(new WorldQuery()).TotalMatched == 1, "moving entry leaves its old spatial bucket");
        var hidden = Entity("hidden", 1, 1); hidden.Active = false; index.Put(hidden);
        var helper = Entity("helper", 2, 2); helper.Helper = true; index.Put(helper);
        check(index.Query(new WorldQuery()).TotalMatched == 1 && index.Query(new WorldQuery { IncludeHelpers = true, IncludeInactive = true }).TotalMatched == 3,
            "inactive entities and engine helpers require explicit inclusion");
        var cameraA = Entity("cameraA", 1, 1); cameraA.ViewportCameraIds = new[] { 11 }; index.Put(cameraA);
        var shared = Entity("shared", 2, 2); shared.ViewportCameraIds = new[] { 11, 22 }; index.Put(shared);
        check(index.Query(new WorldQuery { CameraId = 22 }).Entities.Single().Key == "shared" &&
            index.Query(new WorldQuery { AnyViewport = true }).TotalMatched == 2, "per-camera queries differ from any-view and unknown visibility");
        check(WorldGeometry.InViewport(0, 1, 1) && !WorldGeometry.InViewport(0.5f, 0.5f, -1) &&
            WorldGeometry.IntersectsViewport(-1, 0.1f, 0.1f, 0.2f), "viewport boundary, behind-camera and partially visible bounds");
        check(WorldGeometry.InsideCone(1, 0, 1, 0, 90) && !WorldGeometry.InsideCone(1, 0, -1, 0, 90) &&
            !WorldGeometry.InsideCone(1, 0, 0, 1, 90), "facing cone is distinct from obstruction and range");
        bool rejected = false;
        try { index.Query(new WorldQuery { MaxDistance = float.NaN }); } catch (ArgumentException) { rejected = true; }
        check(rejected, "invalid distances cannot become successful empty results");
        index.Clear();
        var random = new Random(17);
        for (int i = 0; i < 10000; i++) index.Put(Entity("item-" + i, (float)(random.NextDouble() * 1000 - 500), (float)(random.NextDouble() * 1000 - 500)));
        var timer = Stopwatch.StartNew();
        int totalChecked = 0;
        for (int trial = 0; trial < 100; trial++)
        {
            var q = new WorldQuery { X = (float)(random.NextDouble() * 100 - 50), Y = (float)(random.NextDouble() * 100 - 50), MaxDistance = 12,
                MinDistance = 4, Range = trial % 2 == 0 ? WorldRange.Within : WorldRange.Band, Limit = 500 };
            var result = index.Query(q); totalChecked += result.CandidatesChecked;
            var reference = index.Values.Where(e => WorldGeometry.DistanceSquared(q.X, q.Y, e) <= 144 &&
                (q.Range != WorldRange.Band || WorldGeometry.DistanceSquared(q.X, q.Y, e) >= 16))
                .OrderBy(e => WorldGeometry.DistanceSquared(q.X, q.Y, e)).ThenBy(e => e.Key, StringComparer.Ordinal).Select(e => e.Key);
            check(reference.SequenceEqual(result.Entities.Select(e => e.Key)), "spatial reference parity " + trial);
        }
        check(totalChecked < 10000, "spatial index avoids whole-level scans for local queries");
        Console.WriteLine("WORLD_SCALE entities=10000 queries=100 candidates=" + totalChecked + " elapsedWithReferenceMs=" + timer.Elapsed.TotalMilliseconds);
        var all = index.Query(new WorldQuery { Range = WorldRange.All, Offset = 9999, Limit = 50 });
        check(all.TotalMatched == 10000 && all.Entities.Length == 1, "whole-level queries paginate without silently losing totals");
        index.Clear(); check(index.Count == 0 && !index.Values.Any(), "level clear removes entities and spatial cells");
        var quality = new AwarenessQuality();
        for (int i = 0; i < 40; i++) quality.Observe(4, false, i * .1f);
        check(quality.Level == 4 && quality.Interval == 1, "sustained overruns reduce diagnostic quality");
        quality.Observe(.1, false, 40); check(quality.Level == 4, "unobserved time gap cannot count as sustained headroom");
        for (int i = 1; i <= 300; i++) quality.Observe(.1, false, 40 + i * .1f);
        check(quality.Level == 3, "quality recovers one level after sustained observed headroom");
        quality.Observe(.1, false, 71); check(quality.Level == 3, "quality does not oscillate every frame");
        var burstQuality = new AwarenessQuality();
        for (int i = 0; i < 180; i++) burstQuality.Observe(i % 10 == 0 ? 12 : .1, false, i / 60f);
        check(burstQuality.Level > 0 && burstQuality.Reason == "window-budget", "separated burst frames reduce optional work despite cheap intervening frames");
        check(burstQuality.WindowMeanMs > 1 && burstQuality.WindowOverBudget > 0, "budget window exports numerical pressure evidence");
        var averageQuality = new AwarenessQuality();
        for (int i = 0; i < 180; i++) averageQuality.Observe(1.2, false, i / 60f);
        check(averageQuality.Level > 0, "sustained mean above 1ms triggers reduction without any 2ms frames");
        var isolatedQuality = new AwarenessQuality();
        for (int i = 0; i < 240; i++) isolatedQuality.Observe(i == 10 ? 100 : .1, false, i / 60f);
        check(isolatedQuality.Level == 0, "one isolated hitch does not cause repeated quality reductions");
        var recoverOutliers = new AwarenessQuality();
        recoverOutliers.Observe(.1, true, 0);
        for (int i = 1; i <= 1860; i++) recoverOutliers.Observe(i % 300 == 0 ? 2.1 : .1, false, i / 60f);
        check(recoverOutliers.Level == 0,
            "within-budget isolated outliers do not indefinitely prevent natural recovery");
        var recoverPressure = new AwarenessQuality();
        recoverPressure.Observe(.1, true, 0);
        for (int i = 1; i <= 1860; i++) recoverPressure.Observe(1.2, false, i / 60f);
        check(recoverPressure.Level > 0 && recoverPressure.Reason == "window-budget",
            "sustained mean pressure still prevents recovery");
        var queueQuality = new AwarenessQuality(); queueQuality.Observe(.1, true, 1);
        check(queueQuality.Level == 1 && queueQuality.Reason == "writer-backpressure", "writer saturation reduces recording quality immediately");
        // Synthetic spaced spikes match the worst recorded window's mean/p95, not its unavailable raw frame sequence.
        foreach (int fps in new[] { 30, 60, 120 })
        {
            var replayQuality = new AwarenessQuality();
            const double high = 10.6038, recordedMean = 2.062139130434782;
            double low = (recordedMean - high * .1) / .9;
            int oldOverruns = 0, oldLevel = 0;
            for (int i = 0; i < fps * 6; i++)
            {
                double cost = i % 10 == 0 ? high : low;
                if (cost > 2) { if (++oldOverruns >= 10) { oldLevel++; oldOverruns = 0; } }
                else oldOverruns = Math.Max(0, oldOverruns - 1);
                replayQuality.Observe(cost, false, i / (float)fps);
            }
            check(oldLevel == 0 && replayQuality.Level > 0, "summary-matched spaced spikes trigger budget reduction at " + fps + " fps");
            Console.WriteLine(FormattableString.Invariant($"WORLD_BURST_REPLAY fps={fps} meanMs={recordedMean:F4} p95Ms={high:F4} oldQuality={oldLevel} newQuality={replayQuality.Level} scope=synthetic-summary-matched-not-native"));
        }
        var benchmark = new WorldBenchmark();
        benchmark.Start("ordinary", "Off", 100);
        check(benchmark.Tick(109, 500, 90) == null, "benchmark excludes warm-up hitches");
        benchmark.Tick(110, 16, .2);
        Dictionary<string, object> trialResult = null;
        for (int i = 1; i <= 3600; i++) trialResult = benchmark.Tick(110 + i / 60d, 16, .2) ?? trialResult;
        check(trialResult != null && (bool)trialResult["valid"] && (double)trialResult["frameP95Ms"] == 16 &&
            (int)trialResult["frames"] == 3600, "benchmark captures a full warmed 60-second trial");
        benchmark.Start("busy", "Record", 200);
        var invalidTrial = benchmark.Tick(211, 16, .2, "paused");
        check(!(bool)invalidTrial["valid"] && !benchmark.Running, "pauses invalidate benchmark rather than omitting slow time");
        check(WorldFrameState.IsNativeFreeze(0, 1, -1, 2, false, false, false), "native freeze frame with running main clock is gameplay");
        check(WorldFrameState.IsNativeFreeze(0, 1, -1, -1, false, true, false), "freeze end transition preserves gameplay frame");
        check(!WorldFrameState.IsNativeFreeze(0, 0, -1, 2, false, false, false), "main clock menu pause overrides freeze evidence");
        check(!WorldFrameState.IsNativeFreeze(0, 1, 0, 2, false, false, false), "secondary clock pause overrides freeze evidence");
        check(!WorldFrameState.IsNativeFreeze(0, 1, -1, 2, false, false, true), "inspector pause overrides coincident native freeze");
        check(!WorldFrameState.IsNativeFreeze(0, 1, -1, -1, false, false, false), "unexplained zero scale remains a pause");
        check(!WorldFrameState.IsNativeFreeze(0, 1, -1, 2, true, false, false), "disabled native freeze cannot excuse zero scale");
        benchmark.Start("busy", "Record", 300);
        benchmark.Tick(310, 16, .2);
        trialResult = null;
        for (int i = 1; i <= 60; i++) trialResult = benchmark.Tick(310 + i, i % 2 == 0 ? 80 : 16, .2,
            nativeFreeze: i % 2 == 0) ?? trialResult;
        check((bool)trialResult["valid"] && (int)trialResult["frames"] == 60 && (int)trialResult["nativeFreezeFrames"] == 30,
            "native freezes count within full trial duration rather than being omitted");
        check((int)trialResult["framesOver50Ms"] == 30 && (double)trialResult["frameP95Ms"] == 80,
            "slow frames during native freezes remain in performance statistics");
        benchmark.Start("busy", "Record", 400);
        invalidTrial = benchmark.Tick(411, 80, .2, "paused", true);
        check(!(bool)invalidTrial["valid"], "explicit pause still invalidates while freeze evidence is present");
        var boundedHistory = new TraceHistory<string>(10, 100);
        boundedHistory.Add("first", 60); boundedHistory.Add("second", 60);
        check(boundedHistory.Snapshot().SequenceEqual(new[] { "second" }) && boundedHistory.Evicted == 1 && boundedHistory.RetainedBytes == 60,
            "history enforces payload size independently of count");
        boundedHistory.Add("oversize", 101);
        check(boundedHistory.Evicted == 2 && boundedHistory.RetainedBytes == 60, "oversized history record is explicit eviction");
        TestWriter(check);
    }
    private static void TestWriter(Action<bool, string> check)
    {
        var directory = Path.Combine(Path.GetTempPath(), "world-capture-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            using (var worker = new CaptureWorker(4, 1024))
            using (var entered = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            {
                var seen = new List<int>();
                try
                {
                    check(worker.Enqueue(() => { entered.Set(); release.Wait(); seen.Add(1); }, 64), "writer accepts detached work");
                    check(entered.Wait(2000), "writer actually runs off-thread");
                    check(worker.Enqueue(() => seen.Add(2), 64) && worker.Enqueue(() => seen.Add(3), 64), "queue retains FIFO records");
                    check(!worker.Enqueue(() => seen.Add(99), 64) && worker.Dropped == 1, "normal records cannot consume essential reserve");
                    check(worker.Enqueue(() => seen.Add(4), 64, true), "essential event uses reserved capacity");
                    check(!worker.Enqueue(() => seen.Add(98), 2048, true), "byte capacity is enforced even for essential events");
                }
                finally { release.Set(); }
                check(worker.Drain() && seen.SequenceEqual(new[] { 1, 2, 3, 4 }), "writer drains FIFO without dropped work reappearing");
            }
            using (var failedWorker = new CaptureWorker())
            {
                failedWorker.Enqueue(() => throw new IOException("test failure"), 32);
                check(failedWorker.Drain() && failedWorker.Dropped == 1 && failedWorker.Failure != null, "worker failures count as lost evidence");
            }
            check(CaptureWorker.Measure(new Dictionary<string, object> { { "nativeWallEnum", DayOfWeek.Monday } }) < 1024,
                "native enum values are detached data rather than oversized references");
            check(CaptureWorker.Measure(new object()) > 4 * 1024 * 1024, "unknown reference payloads cannot cross writer boundary");
            using (var worker = new CaptureWorker())
            {
                var storage = new TraceStorage(Path.Combine(directory, "bark.jsonl"), 512, worker);
                storage.Write(new Dictionary<string, object> { { "event", "large" }, { "data", new string('x', 600) } });
                storage.Write(new Dictionary<string, object> { { "event", "terminal" }, { "data", new Dictionary<string, object> { { "status", "PASS" } } } }, true);
                check(worker.Drain(), "queued bark compatibility writes finish");
                using var terminal = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "bark.jsonl")));
                check(terminal.RootElement.GetProperty("data").GetProperty("status").GetString() == "INCONCLUSIVE" &&
                    terminal.RootElement.GetProperty("data").GetProperty("droppedRecords").GetInt32() == 1,
                    "terminal observes earlier asynchronous cap losses and cannot claim PASS");
                var enumPath = Path.Combine(directory, "enum.jsonl");
                var enumStorage = new TraceStorage(enumPath, worker: worker);
                enumStorage.Write(new Dictionary<string, object> { { "wall", DayOfWeek.Monday } }, true);
                check(worker.Drain() && File.ReadAllText(enumPath).Trim() == "{\"wall\":1}",
                    "buffered native enum serialization preserves legacy numeric format");
                var journal = new WorldJournal(worker, directory, "test", "build", 1024, 2);
                for (int i = 0; i < 20; i++) journal.Write("sample", 1, i, i, new Dictionary<string, object> { { "payload", new string('a', 120) } });
                journal.Close(); check(worker.Drain(), "rotating journal drains and closes manifest");
                check(Directory.GetFiles(directory, "world-test-*.jsonl").Length == 2, "world retention bounds segment count");
                using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "world-test-manifest.json")));
                check(manifest.RootElement.GetProperty("retiredSegments").GetInt32() > 0 && manifest.RootElement.GetProperty("closed").GetBoolean(),
                    "manifest declares historical rotation and clean shutdown");
            }
        }
        finally { Directory.Delete(directory, true); }
    }
}
