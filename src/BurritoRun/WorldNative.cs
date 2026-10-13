using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BurritoRun
{
    internal static class WorldNative
    {
        internal static readonly string[] Collections = { "agentList", "playerAgentList", "deadAgentList", "itemList",
            "objectRealList", "bulletList", "noiseList", "explosionList", "firesList", "gasesList", "watersList",
            "dangerList", "dangerListNoCollider" };
        private static readonly Dictionary<Type, Dictionary<string, FieldInfo>> Fields = new Dictionary<Type, Dictionary<string, FieldInfo>>();
        private static readonly string[] EffectFields = { "dangerType", "dangerRadius", "timeLeft", "damage", "explosionType" };
        // Field reads only: arbitrary native properties/methods can mutate the simulation.
        /// <summary>
        /// Read an optional native field, caching reflection lookups across the type hierarchy.
        /// Different native entity types expose different fields; an unavailable value stays
        /// unknown instead of becoming zero.
        /// </summary>
        internal static object Read(object source, string name)
        {
            if (source == null) return null;
            var type = source.GetType();
            if (!Fields.TryGetValue(type, out var cache)) Fields[type] = cache = new Dictionary<string, FieldInfo>();
            if (!cache.TryGetValue(name, out var field))
            {
                var search = type;
                while (search != null && field == null)
                { field = search.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly); search = search.BaseType; }
                cache[name] = field;
            }
            return field?.GetValue(source);
        }
        internal static IList List(GameController controller, string name) => Read(controller, name) as IList;
        internal static WorldKind Kind(Component entity)
        {
            if (entity is Agent actor) return actor.isPlayer > 0 ? WorldKind.Player : WorldKind.Actor;
            if (entity is Item) return WorldKind.Item;
            if (entity is Bullet) return WorldKind.Projectile;
            if (entity is Noise) return WorldKind.Noise;
            if (entity is Explosion) return WorldKind.Explosion;
            if (entity is Fire) return WorldKind.Fire;
            if (entity is Gas) return WorldKind.Gas;
            if (entity is Water) return WorldKind.Water;
            if (entity is Danger) return WorldKind.Danger;
            return WorldKind.Object;
        }
        /// <summary>
        /// Read the appropriate native identifier for this entity type.
        /// Effects and actors use different ID fields, so a universal UID lookup would misidentify
        /// some observations.
        /// </summary>
        internal static int NativeId(Component entity)
        {
            string field = entity is Noise ? "noiseID" : entity is Bullet ? "bulletID" :
                entity is Fire ? "fireID" : entity is Explosion ? "explosionNetID" : "UID";
            return Read(entity, field) is int id ? id : entity is PlayfieldObject obj ? obj.UID : 0;
        }
        internal static bool Active(Component entity) => entity != null && entity.gameObject.activeInHierarchy &&
            (!(entity is Bullet || entity is Noise || entity is Item) || GameController.gameController?.activePooling != true ||
                !(Read(entity, "fakeActive") is bool fake && !fake)) && !(Read(entity, "initializingForPool") is bool initializing && initializing);
        /// <summary>
        /// Copy native identity, position and supported detail into a detached world snapshot.
        /// Lifecycle callbacks can request a smaller core snapshot to avoid expensive actor and
        /// object inspection during bursts.
        /// </summary>
        internal static WorldEntitySnapshot Capture(Component entity, int level, int activation, string collection, bool lifecycleCore = false)
        {
            var position = entity.transform.position;
            var actor = entity as Agent;
            var item = entity as Item;
            var obj = entity as ObjectReal;
            string name = actor != null ? actor.agentName : item?.invItem?.invItemName ?? obj?.prefabName ?? entity.GetType().Name;
            var tags = new List<string>();
            if (entity is Door) tags.Add("door");
            if (entity is FireHydrant) tags.Add("hydrant");
            if (item?.invItem != null && (item.invItem.invItemName == Names.Food || item.invItem.invItemName == "HotFud" ||
                item.invItem.Categories?.Contains("Food") == true)) tags.Add("food");
            int native = NativeId(entity), instance = entity.GetInstanceID();
            var data = new Dictionary<string, object>();
            data["coverage"] = lifecycleCore ? "lifecycle-core; census-details-pending; viewport-unknown" : "native-fields-at-sample";
            if (actor != null && !lifecycleCore)
            {
                data["health"] = actor.health; data["healthMax"] = actor.healthMax; data["dead"] = actor.dead;
                data["knockedOut"] = actor.knockedOut; data["combat"] = actor.inCombat; data["owner"] = actor.ownerID;
                data["following"] = actor.hasFollowing; data["followingUid"] = actor.following != null ? (object)actor.following.UID : null;
                data["rootGoal"] = actor.brain?.Goals?.Count > 0 ? actor.brain.Goals[0]?.GetType().Name : null;
                data["defaultGoal"] = actor.defaultGoalCode.ToString(); data["job"] = actor.jobCode.ToString();
                data["nativeOnCamera"] = actor.onCamera; data["invisible"] = actor.invisible;
            }
            if (item?.invItem != null && !lifecycleCore)
            {
                data["nativeCategories"] = item.invItem.Categories?.ToArray();
                data["count"] = item.invItem.invItemCount; data["owner"] = item.invItem.ownerID;
                data["itemType"] = item.invItem.itemType;
            }
            if (obj != null && !lifecycleCore) { data["owner"] = Read(obj, "ownerID"); data["electronic"] = obj.electronic; }
            if (entity is Door door && !lifecycleCore) { data["open"] = door.open; data["locked"] = door.locked; data["direction"] = door.direction; data["outsideFlag"] = door.outsideDoor; }
            if (entity is Noise noise)
            {
                data["radius"] = noise.noiseDistance; data["volume"] = noise.volume;
                data["timeLeft"] = noise.timeLeft; data["noiseType"] = noise.type; data["state"] = noise.state;
                data["sourceUid"] = noise.playfieldObject != null ? (object)noise.playfieldObject.UID : null;
            }
            if (entity is Bullet bullet)
            { data["damage"] = bullet.damage; data["speed"] = bullet.speed; data["sourceUid"] = bullet.belongsToUID; data["weapon"] = bullet.cameFromWeapon; }
            foreach (string field in EffectFields)
            { var value = Read(entity, field); if (value is string || value is float || value is int) data[field] = value; }
            return new WorldEntitySnapshot
            {
                Key = level + ":" + instance + ":" + native + ":" + activation,
                Level = level,
                NativeId = native,
                Instance = instance,
                Activation = activation,
                Kind = Kind(entity),
                Name = string.IsNullOrEmpty(name) ? entity.GetType().Name : name,
                NativeType = entity.GetType().Name,
                Collection = collection,
                Tags = tags.ToArray(),
                ClassificationSource = "native-type; Food category or explicit HotFud/BurritoRun_Food",
                X = position.x,
                Y = position.y,
                Active = Active(entity),
                Destroyed = actor != null ? actor.dead : obj != null && obj.destroyed,
                Helper = (entity.GetType().Name.EndsWith("Helper", StringComparison.Ordinal) || entity.GetType().Name == "DoorHelper2"),
                GameTime = Time.time,
                WallTime = Time.realtimeSinceStartup,
                Data = data
            };
        }
        /// <summary>
        /// Convert a world snapshot to plain capture values.
        /// The background writer must not retain live Unity components or need to inspect the game
        /// itself.
        /// </summary>
        internal static Dictionary<string, object> Detach(WorldEntitySnapshot e) => TraceWorld.Values(
            "key", e.Key, "level", e.Level, "nativeId", e.NativeId, "instance", e.Instance, "activation", e.Activation,
            "kind", e.Kind.ToString(), "name", e.Name, "nativeType", e.NativeType, "collection", e.Collection,
            "tags", e.Tags, "classificationSource", e.ClassificationSource, "x", e.X, "y", e.Y,
            "gameTime", e.GameTime, "wallSeconds", e.WallTime, "active", e.Active, "destroyed", e.Destroyed,
            "helper", e.Helper, "viewportCameraIds", e.ViewportCameraIds, "viewportCapturedAt", e.ViewportCapturedAt, "data", e.Data);

        /// <summary>
        /// Describe the actor's current goals, path, following state and nearby door candidates.
        /// Intent and local candidates explain movement but cannot establish reachability, arrival
        /// or a stall by themselves.
        /// </summary>
        internal static Dictionary<string, object> Navigation(Agent actor, WorldIndex index, string key)
        {
            var root = actor.brain?.Goals?.Count > 0 ? actor.brain.Goals[0] : null;
            var following = actor.following;
            var ai = actor.pathfindingAI;
            var children = new List<string>();
            if (root?.SubGoals != null) foreach (var sub in root.SubGoals) { if (children.Count == 8) break; children.Add(sub?.GetType().Name); }
            var destination = Read(ai, "targetPosition");
            var waypointList = Read(Read(ai, "path"), "vectorPath") as IList;
            var waypoints = new List<object>();
            if (waypointList != null)
                for (int i = 0; i < Math.Min(128, waypointList.Count); i++)
                    if (waypointList[i] is Vector3 point) waypoints.Add(TraceWorld.Position(point));
            var doors = index.Query(new WorldQuery
            {
                ObserverKey = key,
                X = actor.tr.position.x,
                Y = actor.tr.position.y,
                Tag = "door",
                MaxDistance = 12,
                Limit = 12
            });
            var candidates = new List<object>();
            foreach (var door in doors.Entities) candidates.Add(Detach(door));
            var nav = new NavigationEvidence
            {
                Actor = key,
                CapturedAt = Time.time,
                DoorCandidates = Array.ConvertAll(doors.Entities, d => d.Key),
                State = TraceWorld.Values(
                    "rootGoal", root?.GetType().Name, "subGoals", children.ToArray(),
                    "hasFollowing", actor.hasFollowing, "followingUid", following != null ? (object)following.UID : null,
                    "followDistance", root is GoalFollow ? Read(root, "followDistance") : null,
                    "distanceToFollowed", following?.tr != null ? (object)Vector2.Distance(actor.tr.position, following.tr.position) : null,
                    "relationshipToFollowed", following != null ? actor.relationships?.GetRel(following) : null,
                    "velocity", actor.rigidBody2D != null ? new[] { actor.rigidBody2D.velocity.x, actor.rigidBody2D.velocity.y } : null,
                    "targetPosition", destination is Vector3 target ? TraceWorld.Position(target) : null,
                    "hasPath", Read(ai, "hasPath"), "pathNull", Read(ai, "pathNull"), "rePath", Read(ai, "rePath"),
                    "currentWaypoint", Read(ai, "currentWaypoint"), "waypoints", waypoints,
                    "waypointsOmitted", waypointList != null ? Math.Max(0, waypointList.Count - waypoints.Count) : 0,
                    "pathError", Read(Read(ai, "path"), "error"), "pathErrorMeaning", "null-means-unavailable",
                    "blockedByDoor", actor.blockedByDoor != null ? (object)actor.blockedByDoor.UID : null,
                    "isBlockedByDoor", actor.isBlockedByDoor, "blockedByDoorTime", actor.blockedByDoorTime,
                    "doorStuck", actor.doorStuck, "candidateDoors", candidates)
            };
            return TraceWorld.Values("actor", nav.Actor, "gameTime", nav.CapturedAt, "state", nav.State, "reachability", nav.Reachability);
        }
        /// <summary>
        /// Read tile ownership, hazards and actor-specific walking checks at one position.
        /// Local walkability and safety are useful observations, not a complete route test.
        /// </summary>
        internal static object Tile(Agent observer, Vector3 point)
        {
            var gc = observer.gc; var tile = gc.tileInfo?.GetTileData(point);
            if (tile == null) return TraceWorld.Values("available", false);
            return TraceWorld.Values("available", true, "position", TraceWorld.Position(point), "chunk", tile.chunkID,
                "owner", tile.owner, "wall", tile.wallMaterial.ToString(), "floor", tile.floorMaterial.ToString(),
                "indoor", gc.tileInfo.IsIndoors(point), "solid", tile.solidObject, "hole", tile.hole,
                "hazard", tile.dangerousToWalk, "water", tile.water, "groundFire", tile.groundFire,
                "possibleToWalk", gc.tileInfo.PossibleToWalk(observer, point), "safeToWalk", gc.tileInfo.SafeToWalk(observer, point));
        }
    }
}
