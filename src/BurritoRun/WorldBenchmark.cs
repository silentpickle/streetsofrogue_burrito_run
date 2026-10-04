using System;
using System.Collections.Generic;

namespace BurritoRun
{
    internal static class WorldFrameState
    {
        // Matches GameController.SetTimeScale: native freeze frames stop simulation without pausing play.
        // Unknown zero-scale states remain pauses; UI pauses take precedence over a coincident freeze.
        internal static bool IsNativeFreeze(float scale, float mainScale, float secondaryScale,
            float freezeFrame, bool freezeDisabled, bool justEndedFreeze, bool uiPaused)
        {
            float effectiveScale = mainScale != 0 && secondaryScale != -1 ? secondaryScale : mainScale;
            return scale == 0 && mainScale > 0 && effectiveScale > 0 && !uiPaused && !freezeDisabled &&
                (freezeFrame > -1 || justEndedFreeze);
        }
    }

    // Bounded detached trial accumulator. A discontinuity invalidates, rather than silently trimming, a trial.
    internal sealed class WorldBenchmark
    {
        private const int MaxFrames = 60000;
        private readonly List<double> frames = new List<double>();
        private readonly List<double> costs = new List<double>();
        private double started;
        private double measuredSince;
        private string scenario;
        private string mode;
        private int nativeFreezeFrames;
        internal bool Running { get; private set; }
        internal string Status { get; private set; } = "No trial started";
        internal void Start(string scenarioName, string modeName, double now)
        {
            if (Running) throw new InvalidOperationException("Trial already running");
            scenario = scenarioName; mode = modeName; started = now; measuredSince = 0;
            frames.Clear(); costs.Clear(); nativeFreezeFrames = 0; Running = true; Status = "Warm-up (10 seconds)";
        }
        internal Dictionary<string, object> Tick(double now, double frameMs, double costMs, string invalidReason = null, bool nativeFreeze = false)
        {
            if (!Running) return null;
            if (invalidReason != null || now < started || double.IsNaN(frameMs) || double.IsInfinity(frameMs) || frameMs < 0 ||
                double.IsNaN(costMs) || double.IsInfinity(costMs) || costMs < 0)
                return Finish(now, invalidReason ?? "invalid-clock-or-sample");
            if (now - started < 10) { Status = mode + " warm-up: " + Math.Ceiling(10 - (now - started)) + "s"; return null; }
            if (measuredSince == 0) { measuredSince = now; Status = "Measuring (60 seconds)"; return null; }
            if (frames.Count >= MaxFrames) return Finish(now, "sample-cap");
            Status = mode + " measuring: " + Math.Max(0, Math.Ceiling(60 - (now - measuredSince))) + "s remaining";
            frames.Add(frameMs); costs.Add(costMs);
            if (nativeFreeze) nativeFreezeFrames++;
            return now - measuredSince >= 60 ? Finish(now, null) : null;
        }
        internal Dictionary<string, object> Cancel(double now, string reason) => Running ? Finish(now, reason) : null;
        private Dictionary<string, object> Finish(double now, string reason)
        {
            Running = false; Status = reason == null ? "Trial captured; compare in offline report" : "Invalid trial: " + reason;
            double total = 0; int hitches = 0;
            foreach (double cost in costs) total += cost;
            foreach (double frame in frames) if (frame > 50) hitches++;
            return new Dictionary<string, object> {
                { "scenario", scenario }, { "mode", mode }, { "valid", reason == null && frames.Count > 0 }, { "reason", reason },
                { "warmupSeconds", 10 }, { "durationSeconds", measuredSince == 0 ? 0 : now - measuredSince }, { "frames", frames.Count },
                { "frameMedianMs", Percentile(frames, .5) }, { "frameP95Ms", Percentile(frames, .95) }, { "frameP99Ms", Percentile(frames, .99) },
                { "framesOver50Ms", hitches }, { "diagnosticMeanMs", costs.Count == 0 ? 0 : total / costs.Count },
                { "diagnosticP95Ms", Percentile(costs, .95) }, { "comparability", "operator-must-verify-same-scenario-and-busy-activity" },
                { "nativeFreezeFrames", nativeFreezeFrames }, { "frameCoverage", "includes-native-freeze-frames; UI-and-unknown-pauses-invalidate" }
            };
        }
        private static double Percentile(List<double> values, double fraction)
        { if (values.Count == 0) return 0; var copy = values.ToArray(); Array.Sort(copy); return copy[(int)((copy.Length - 1) * fraction)]; }
    }
}
