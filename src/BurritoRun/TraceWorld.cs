using System;
using System.Collections.Generic;
using UnityEngine;

namespace BurritoRun
{
    internal static class TraceWorld
    {
        internal const float Radius = 12f;
        internal const int ActorLimit = 8;
        internal static Dictionary<string, object> Values(params object[] pairs)
        {
            var result = new Dictionary<string, object>();
            for (int index = 0; index < pairs.Length; index += 2)
                result.Add((string)pairs[index], pairs[index + 1]);
            return result;
        }

        internal static object Position(Vector3 value) => new[] { value.x, value.y, value.z };
        internal static object Actor(Agent actor, Agent source = null)
        {
            if (actor == null) return null;
            var goals = new List<string>();
            var children = new List<string>();
            if (actor.brain?.Goals != null)
                foreach (var goal in actor.brain.Goals)
                {
                    if (goals.Count >= 4) break;
                    goals.Add(goal?.GetType().Name);
                }
            var root = actor.brain?.Goals != null && actor.brain.Goals.Count > 0 ? actor.brain.Goals[0] : null;
            var rootNoise = (root as GoalNoiseReact)?.noise;
            if (root?.SubGoals != null)
                foreach (var child in root.SubGoals)
                {
                    if (children.Count >= 8) break;
                    children.Add(child?.GetType().Name);
                }
            return Values("uid", actor.UID, "instance", actor.GetInstanceID(), "name", actor.agentName,
                "position", actor.tr != null ? Position(actor.tr.position) : null,
                "health", actor.health, "healthMax", actor.healthMax, "dead", actor.dead,
                "knockedOut", actor.knockedOut, "ghost", actor.ghost, "combat", actor.inCombat,
                "player", actor.isPlayer, "following", actor.hasFollowing,
                "active", actor.gameObject.activeInHierarchy, "poolInitializing", actor.initializingForPool,
                "job", actor.jobCode.ToString(), "defaultGoal", actor.defaultGoalCode.ToString(),
                "rootGoal", root?.GetType().Name, "topGoals", goals, "rootSubGoals", children,
                "rootNoiseId", rootNoise != null ? (object)rootNoise.noiseID : null,
                "rootNoiseInstance", rootNoise != null ? (object)rootNoise.GetInstanceID() : null,
                "goalCount", actor.brain?.Goals?.Count, "subGoalCount", root?.SubGoals?.Count,
                "hearingRange", actor.hearingRange, "investigation", actor.investigation,
                "owner", actor.ownerID, "flee", actor.mustFlee,
                "relationFromSource", source?.relationships?.GetRel(actor),
                "distanceFromSource", source?.tr != null && actor.tr != null ?
                    (object)Vector2.Distance(source.tr.position, actor.tr.position) : null);
        }

        internal static Dictionary<string, object> Frame(Agent source, Agent owner, Agent target)
        {
            var gc = GameController.gameController;
            var origin = source?.tr != null ? source.tr.position : gc?.playerAgent?.tr?.position;
            var actors = new List<Agent>();
            AddDistinct(actors, source);
            AddDistinct(actors, owner);
            AddDistinct(actors, target);
            var nearby = new List<Agent>();
            if (origin.HasValue && gc?.agentList != null)
                foreach (var actor in gc.agentList)
                    if (actor != null && actor.tr != null && !actors.Contains(actor) &&
                        Vector2.Distance(origin.Value, actor.tr.position) <= Radius) nearby.Add(actor);
            if (origin.HasValue) nearby.Sort((first, second) =>
                Vector2.Distance(origin.Value, first.tr.position).CompareTo(Vector2.Distance(origin.Value, second.tr.position)));
            int remaining = ActorLimit - actors.Count;
            for (int index = 0; index < Math.Min(remaining, nearby.Count); index++) actors.Add(nearby[index]);
            var snapshots = new List<object>();
            foreach (var actor in actors) snapshots.Add(Actor(actor, source));
            return Values("actors", snapshots, "omittedNearbyActors", Math.Max(0, nearby.Count - remaining),
                "radius", Radius, "actorLimit", ActorLimit, "gameTime", Time.time,
                "wallSeconds", Time.realtimeSinceStartup, "timeScale", Time.timeScale);
        }

        private static void AddDistinct(List<Agent> values, Agent actor)
        { if (actor != null && !values.Contains(actor)) values.Add(actor); }

        internal static object Environment(Agent source, Agent target)
        {
            var gc = GameController.gameController;
            var origin = source?.tr != null ? source.tr.position : gc?.playerAgent?.tr?.position;
            if (!origin.HasValue || gc == null || !gc.loadComplete || gc.tileInfo == null)
                return Values("available", false, "reason", "world-not-ready");
            try
            {
                var grids = new List<object> { Grid(gc, source ?? gc.playerAgent, origin.Value, "source") };
                if (target?.tr != null) grids.Add(Grid(gc, target, target.tr.position, "target"));
                var objects = new List<ObjectReal>();
                foreach (var obj in gc.objectRealList)
                    if (obj != null && obj.tr != null && obj.GetType().Name != "DoorHelper" &&
                        (Vector2.Distance(origin.Value, obj.tr.position) <= Radius ||
                        (target?.tr != null && Vector2.Distance(target.tr.position, obj.tr.position) <= Radius))) objects.Add(obj);
                objects.Sort((first, second) => Vector2.Distance(origin.Value, first.tr.position)
                    .CompareTo(Vector2.Distance(origin.Value, second.tr.position)));
                var snapshots = new List<object>();
                foreach (var obj in objects)
                {
                    if (snapshots.Count >= 24) break;
                    var data = Values("uid", obj.UID, "type", obj.GetType().Name,
                        "prefab", obj.prefabName, "position", Position(obj.tr.position),
                        "destroyed", obj.destroyed, "electronic", obj.electronic);
                    if (obj is Door door)
                    { data.Add("open", door.open); data.Add("locked", door.locked); data.Add("direction", door.direction); }
                    snapshots.Add(data);
                }
                return Values("available", true, "grids", grids, "objects", snapshots,
                    "omittedObjects", Math.Max(0, objects.Count - 24), "radius", Radius,
                    "sourceToTargetLOS", source?.movement != null && target != null ?
                        (object)source.movement.HasLOSAgent360(target) : null,
                    "routeReachability", "unknown-local-walkability-is-not-a-route");
            }
            catch (Exception error)
            { return Values("available", false, "reason", "capture-error", "error", error.GetType().Name + ": " + error.Message); }
        }

        private static object Grid(GameController gc, Agent actor, Vector3 origin, string role)
        {
            var cells = new List<object>();
            int x = Mathf.FloorToInt((origin.x + 0.32f) / 0.64f);
            int y = Mathf.FloorToInt((origin.y + 0.32f) / 0.64f);
            for (int dy = -2; dy <= 2; dy++)
                for (int dx = -2; dx <= 2; dx++)
                {
                    var point = new Vector3((x + dx) * 0.64f, (y + dy) * 0.64f, origin.z);
                    var tile = gc.tileInfo.GetTileData(point);
                    cells.Add(tile == null ? Values("x", x + dx, "y", y + dy, "missing", true) :
                        Values("x", x + dx, "y", y + dy, "position", Position(point),
                            "chunk", tile.chunkID, "owner", tile.owner, "wall", tile.wallMaterial,
                            "destroyedWall", tile.destroyedWall, "floor", tile.floorMaterial,
                            "solid", tile.solidObject, "hazard", tile.dangerousToWalk, "hole", tile.hole,
                            "indoor", gc.tileInfo.IsIndoors(point),
                            "possibleToWalk", actor != null ? (object)gc.tileInfo.PossibleToWalk(actor, point) : null,
                            "safeToWalk", actor != null ? (object)gc.tileInfo.SafeToWalk(actor, point) : null));
                }
            return Values("role", role, "actor", actor != null ? (object)actor.UID : null,
                "origin", Position(origin), "cellSize", 0.64f, "cells", cells);
        }
    }
}
