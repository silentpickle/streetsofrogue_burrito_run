using System.Collections.Generic;
using UnityEngine;

namespace BurritoRun
{
    internal sealed partial class WorldAwareness
    {
        // Evidence access only. Do not seed the registry: the normal census must discover the fixture.
        internal int HarnessLevel => level;
        internal bool HarnessNativeFreeze => NativeFreeze(GameController.gameController);
        internal bool HarnessBenchmarkRunning => benchmark.Running;
        internal bool HarnessCaptureClean => exceptions == 0 && journal.Dropped == 0 && plugin.CaptureWriter.Dropped == 0 &&
            journal.Failure == null && plugin.CaptureWriter.Failure == null;
        internal Dictionary<string, object> HarnessLastTrial { get; private set; }
        internal WorldEntitySnapshot HarnessSnapshot(Component entity) => entity != null &&
            entries.TryGetValue(entity.GetInstanceID(), out var entry) ? Index.Get(entry.Key) : null;
        internal Dictionary<string, object> HarnessPressureEvidence() => TraceWorld.Values("quality", Quality.Level,
            "reason", Quality.Reason, "interval", Quality.Interval, "nearbyActorLimit", Quality.NearbyActorLimit,
            "frameWorkMs", lastFrameWorkMs, "windowMeanMs", Quality.WindowMeanMs, "windowFrames", Quality.WindowFrames,
            "windowOverBudget", Quality.WindowOverBudget, "windowWallSeconds", Quality.WindowCapturedAt,
            "hookMs", hookMs, "hookMaxMs", hookMaxMs, "deferredLifecycleDetails", deferredLifecycleDetails,
            "saturated", plugin.CaptureWriter.Saturated, "queueDepth", plugin.CaptureWriter.Depth,
            "queueBytes", plugin.CaptureWriter.QueuedBytes, "clean", HarnessCaptureClean);
        internal Dictionary<string, object> HarnessPerformanceContext(Agent player)
        {
            WorldPerception.Cameras(player.gc, out var mapping);
            var views = new List<object>();
            foreach (var camera in mapping)
                views.Add(TraceWorld.Values("camera", camera.CameraId, "players", camera.PlayerIds,
                    "rect", camera.Rect, "mapping", camera.Mapping, "enabled", camera.Enabled));
            return TraceWorld.Values("level", level, "controller", player.gc.GetInstanceID(), "floor", player.gc.floorNameNum,
                "playerInstance", player.GetInstanceID(), "playerUid", player.UID, "position", TraceWorld.Position(player.tr.position),
                "health", player.health, "combat", player.inCombat, "focused", Application.isFocused,
                "nativeActors", player.gc.agentList.Count, "cameras", views);
        }
        internal WorldEntitySnapshot HarnessEntity(Agent actor)
        {
            if (actor == null) return null;
            foreach (var entity in Index.Values)
                if (entity.Instance == actor.GetInstanceID() && entity.NativeId == actor.UID && entity.Active &&
                    !entity.Destroyed && entity.Level == level) return entity;
            return null;
        }
        internal void HarnessRecord(string kind, Dictionary<string, object> data) =>
            journal.Write("native-test-" + kind, level, Time.time, Time.realtimeSinceStartup, data, true);

        internal Dictionary<string, object> HarnessRead(Agent observer, Agent actor, WorldEntitySnapshot indexed)
        {
            var before = HarnessState(observer, actor);
            var views = WorldPerception.Cameras(observer.gc, out var mapping);
            var native = WorldNative.Capture(actor, level, indexed.Activation, "harness-read-only");
            var perceptionEvidence = WorldPerception.Detach(WorldPerception.Capture(observer, actor,
                HarnessEntity(observer)?.Key, indexed.Key, views));
            var navigation = WorldNative.Navigation(actor, Index, indexed.Key);
            var queries = new List<object>();
            float distance = Vector2.Distance(observer.tr.position, actor.tr.position);
            foreach (var range in new[] { WorldRange.Within, WorldRange.Band, WorldRange.Outside, WorldRange.All })
            {
                var query = new WorldQuery
                {
                    X = observer.tr.position.x,
                    Y = observer.tr.position.y,
                    Kind = WorldKind.Actor,
                    Range = range,
                    MinDistance = range == WorldRange.Band ? Mathf.Max(0, distance - 0.25f) : 0,
                    MaxDistance = range == WorldRange.Outside ? Mathf.Max(0, distance - 0.25f) : distance + 0.25f,
                    Search = indexed.Key,
                    Limit = 1
                };
                var result = Index.Query(query);
                queries.Add(TraceWorld.Values("range", range.ToString(), "origin", new[] { query.X, query.Y },
                    "min", query.MinDistance, "max", query.MaxDistance, "total", result.TotalMatched,
                    "keys", System.Array.ConvertAll(result.Entities, e => e.Key)));
            }
            return TraceWorld.Values("before", before, "after", HarnessState(observer, actor),
                "native", WorldNative.Detach(native), "indexed", WorldNative.Detach(indexed),
                "perception", perceptionEvidence, "navigation", navigation, "tile", WorldNative.Tile(actor, actor.tr.position),
                "cameraCount", mapping.Length, "queries", queries, "censusComplete", complete,
                "generation", generation, "errors", exceptions, "worldDrops", journal.Dropped,
                "workerDrops", plugin.CaptureWriter.Dropped);
        }
        private static object HarnessState(Agent player, Agent actor)
        {
            object State(Agent a) => TraceWorld.Values("uid", a.UID, "instance", a.GetInstanceID(),
                "position", TraceWorld.Position(a.tr.position), "health", a.health, "dead", a.dead,
                "combat", a.inCombat, "job", a.jobCode.ToString(), "defaultGoal", a.defaultGoalCode.ToString(),
                "rootGoal", a.brain?.Goals?.Count > 0 ? a.brain.Goals[0]?.GetType().Name : null,
                "following", a.hasFollowing, "followUid", a.following != null ? (object)a.following.UID : null,
                "owner", a.ownerID, "targetPosition", WorldNative.Read(a.pathfindingAI, "targetPosition") is Vector3 p ? TraceWorld.Position(p) : null,
                "money", a.inventory?.FindItem("Money")?.invItemCount ?? 0);
            return TraceWorld.Values("player", State(player), "actor", State(actor), "timeScale", Time.timeScale);
        }
    }
}
