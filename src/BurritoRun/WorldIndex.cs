using System;
using System.Collections.Generic;

namespace BurritoRun
{
    // Main-thread spatial hash. Query semantics are independent of Unity and camera state.
    internal sealed class WorldIndex
    {
        private const float CellSize = 6.4f;
        private readonly Dictionary<string, WorldEntitySnapshot> entities = new Dictionary<string, WorldEntitySnapshot>();
        private readonly Dictionary<long, HashSet<string>> cells = new Dictionary<long, HashSet<string>>();
        internal int Count => entities.Count;
        internal IEnumerable<WorldEntitySnapshot> Values => entities.Values;
        internal WorldEntitySnapshot Get(string key) => key != null && entities.TryGetValue(key, out var entity) ? entity : null;
        private static int Cell(float value) => (int)Math.Floor(value / CellSize);
        private static long Key(int x, int y) => ((long)x << 32) | (uint)y;
        private static long Key(WorldEntitySnapshot entity) => Key(Cell(entity.X), Cell(entity.Y));
        /// <summary>
        /// Insert or replace a detached world snapshot and update its spatial bucket.
        /// Removing the previous bucket entry prevents moving objects from appearing in two places.
        /// </summary>
        internal void Put(WorldEntitySnapshot entity)
        {
            if (entity == null || entity.Key == null || float.IsNaN(entity.X) || float.IsNaN(entity.Y) ||
                Math.Abs(entity.X) > 1000000 || Math.Abs(entity.Y) > 1000000)
                throw new ArgumentException("Invalid entity position/identity");
            if (entities.TryGetValue(entity.Key, out var previous)) RemoveCell(previous);
            entities[entity.Key] = entity;
            long cell = Key(entity);
            if (!cells.TryGetValue(cell, out var bucket)) cells.Add(cell, bucket = new HashSet<string>());
            bucket.Add(entity.Key);
        }
        private void RemoveCell(WorldEntitySnapshot entity)
        {
            long cell = Key(entity);
            if (!cells.TryGetValue(cell, out var bucket)) return;
            bucket.Remove(entity.Key);
            if (bucket.Count == 0) cells.Remove(cell);
        }
        /// <summary>
        /// Retire one activation from both identity lookup and spatial queries.
        /// Leaving either entry behind could offer a removed or recycled target to consumers.
        /// </summary>
        internal void Remove(string key)
        {
            if (!entities.TryGetValue(key, out var entity)) return;
            RemoveCell(entity); entities.Remove(key);
        }
        internal void Clear() { entities.Clear(); cells.Clear(); }
        /// <summary>
        /// Filter sampled entities and return a stable distance-ordered page of matches.
        /// The spatial hash limits nearby searches, while large requests enumerate entries instead
        /// of enormous empty grids.
        /// </summary>
        internal WorldQueryResult Query(WorldQuery query)
        {
            if (query == null || float.IsNaN(query.X) || float.IsInfinity(query.X) ||
                float.IsNaN(query.Y) || float.IsInfinity(query.Y) ||
                float.IsNaN(query.MaxDistance) || float.IsInfinity(query.MaxDistance) || query.MaxDistance < 0 ||
                float.IsNaN(query.MinDistance) || float.IsInfinity(query.MinDistance) || query.MinDistance < 0 ||
                query.Limit < 1 || query.Limit > 500 || query.Offset < 0 ||
                (query.Range == WorldRange.Band && query.MinDistance > query.MaxDistance))
                throw new ArgumentException("Invalid world query");
            var matches = new List<WorldEntitySnapshot>();
            int checkedCount = 0;
            bool bounded = query.Range == WorldRange.Within || query.Range == WorldRange.Band;
            double span = Math.Ceiling(query.MaxDistance / CellSize) * 2 + 3;
            // Large/whole-level requests enumerate entries, never an enormous empty grid.
            if (bounded && span * span < Math.Max(1, entities.Count) && query.MaxDistance < 100000 &&
                Math.Abs(query.X) < 1000000 && Math.Abs(query.Y) < 1000000)
            {
                for (int x = Cell(query.X - query.MaxDistance); x <= Cell(query.X + query.MaxDistance); x++)
                    for (int y = Cell(query.Y - query.MaxDistance); y <= Cell(query.Y + query.MaxDistance); y++)
                        if (cells.TryGetValue(Key(x, y), out var bucket))
                            foreach (string key in bucket) Add(entities[key]);
            }
            else foreach (var entity in entities.Values) Add(entity);
            matches.Sort((a, b) =>
            {
                int order = WorldGeometry.DistanceSquared(query.X, query.Y, a).CompareTo(WorldGeometry.DistanceSquared(query.X, query.Y, b));
                return order != 0 ? order : string.CompareOrdinal(a.Key, b.Key);
            });
            int offset = Math.Min(query.Offset, matches.Count);
            return new WorldQueryResult
            {
                TotalMatched = matches.Count,
                CandidatesChecked = checkedCount,
                Entities = matches.GetRange(offset, Math.Min(query.Limit, matches.Count - offset)).ToArray()
            };

            void Add(WorldEntitySnapshot entity)
            {
                checkedCount++;
                if ((!query.IncludeInactive && (!entity.Active || entity.Destroyed)) ||
                    (!query.IncludeHelpers && entity.Helper) || entity.Key == query.ObserverKey ||
                    (query.Kind.HasValue && entity.Kind != query.Kind.Value)) return;
                if (query.AnyViewport && (entity.ViewportCameraIds == null || entity.ViewportCameraIds.Length == 0)) return;
                if (query.CameraId.HasValue && (entity.ViewportCameraIds == null || Array.IndexOf(entity.ViewportCameraIds, query.CameraId.Value) < 0)) return;
                if (!string.IsNullOrEmpty(query.Tag) && Array.IndexOf(entity.Tags, query.Tag) < 0) return;
                if (!string.IsNullOrEmpty(query.Search) &&
                    (entity.Name ?? "").IndexOf(query.Search, StringComparison.OrdinalIgnoreCase) < 0 &&
                    (entity.NativeType ?? "").IndexOf(query.Search, StringComparison.OrdinalIgnoreCase) < 0 &&
                    entity.Key.IndexOf(query.Search, StringComparison.OrdinalIgnoreCase) < 0) return;
                float distance = WorldGeometry.DistanceSquared(query.X, query.Y, entity);
                if (query.Range == WorldRange.Within && distance > query.MaxDistance * query.MaxDistance) return;
                if (query.Range == WorldRange.Band && (distance < query.MinDistance * query.MinDistance || distance > query.MaxDistance * query.MaxDistance)) return;
                if (query.Range == WorldRange.Outside && distance <= query.MaxDistance * query.MaxDistance) return;
                matches.Add(entity);
            }
        }
    }

    internal sealed class AwarenessQuality
    {
        internal int Level { get; private set; }
        internal float Interval => Level >= 3 ? 1f : Level >= 2 ? 0.5f : 0.2f;
        internal int NearbyActorLimit => Level >= 1 ? 2 : 8;
        internal string Reason { get; private set; } = "within-budget";
        private int overruns;
        private float goodSince;
        private bool windowStarted;
        private float windowStart;
        private double windowWork;
        private int windowFrames;
        private int windowOverBudget;
        private int pressureWindows;
        internal double WindowMeanMs { get; private set; }
        internal int WindowFrames { get; private set; }
        internal int WindowOverBudget { get; private set; }
        internal float WindowCapturedAt { get; private set; }
        /// <summary>
        /// Adjust observation detail from measured work and writer pressure.
        /// Sustained overload reduces detail, while gradual recovery avoids bouncing between
        /// quality levels.
        /// </summary>
        internal void Observe(double milliseconds, bool saturated, float now)
        {
            if (!windowStarted || now < windowStart || now - windowStart > 2)
            {
                windowStarted = true; windowStart = goodSince = now;
                windowWork = 0; windowFrames = windowOverBudget = pressureWindows = 0;
                WindowMeanMs = 0; WindowFrames = WindowOverBudget = 0; WindowCapturedAt = 0;
            }
            windowWork += milliseconds; windowFrames++;
            if (milliseconds > 2) windowOverBudget++;
            bool windowReduction = false;
            if (now - windowStart >= 1)
            {
                WindowMeanMs = windowWork / windowFrames; WindowFrames = windowFrames; WindowOverBudget = windowOverBudget;
                WindowCapturedAt = now;
                // Mean >1 ms or more than 5% of frames >2 ms violates the measured overhead target.
                bool pressure = WindowMeanMs > 1 || (long)windowOverBudget * 20 > windowFrames;
                pressureWindows = pressure ? pressureWindows + 1 : 0;
                if (pressure) { goodSince = now; if (Level == 0) Reason = "window-budget-pressure"; }
                else if (Level == 0) Reason = "within-budget";
                windowReduction = pressureWindows >= 2;
                if (windowReduction) pressureWindows = 0;
                windowStart = now; windowWork = 0; windowFrames = windowOverBudget = 0;
            }
            bool frameReduction = false;
            if (milliseconds > 2 || saturated)
            {
                // Isolated outliers within the measured window targets are still headroom.
                // Sustained window pressure and actual reductions reset recovery below.
                frameReduction = ++overruns >= 10 || saturated;
            }
            else overruns = Math.Max(0, overruns - 1);
            if (frameReduction || windowReduction)
            {
                Level = Math.Min(4, Level + 1); overruns = 0; goodSince = now;
                Reason = saturated ? "writer-backpressure" : windowReduction ? "window-budget" : "frame-budget";
            }
            else if (now - goodSince >= 30 && Level > 0)
            { Level--; goodSince = now; Reason = "gradual-recovery"; }
        }
    }
}
