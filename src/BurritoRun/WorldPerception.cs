using System;
using System.Collections.Generic;
using UnityEngine;

namespace BurritoRun
{
    internal static class WorldPerception
    {
        internal static List<Camera> Cameras(GameController gc, out CameraViewSnapshot[] snapshots)
        {
            var cameras = new List<Camera>(); var players = new Dictionary<int, List<int>>();
            foreach (var actor in new[] { gc.playerAgent, gc.playerAgent2, gc.playerAgent3, gc.playerAgent4 })
            {
                if (actor == null || !actor.gameObject.activeInHierarchy || actor.agentCamera == null) continue;
                var camera = actor.agentCamera.originalCamera;
                if (camera == null || !camera.isActiveAndEnabled) continue;
                if (!players.TryGetValue(camera.GetInstanceID(), out var ids))
                { players.Add(camera.GetInstanceID(), ids = new List<int>()); cameras.Add(camera); }
                ids.Add(actor.UID);
            }
            var values = new List<CameraViewSnapshot>();
            foreach (var camera in cameras)
            {
                var r = camera.rect;
                values.Add(new CameraViewSnapshot
                {
                    CameraId = camera.GetInstanceID(),
                    PlayerIds = players[camera.GetInstanceID()].ToArray(),
                    Rect = new[] { r.x, r.y, r.width, r.height },
                    Enabled = true,
                    Mapping = "native-player-agentCamera; shared-camera-deduplicated"
                });
            }
            snapshots = values.ToArray(); return cameras;
        }
        internal static PerceptionEvidence Capture(Agent observer, Component target, string observerKey, string targetKey, List<Camera> cameras)
        {
            var result = new PerceptionEvidence
            {
                Observer = observerKey,
                Target = targetKey,
                CapturedAt = Time.time,
                Method = "Movement.HasLOSPosition360 (native efficient mask); independent facing/cone",
                Reason = "observer-unavailable"
            };
            var point = target.transform.position;
            if (observer?.tr != null && observer.movement != null)
            {
                var delta = point - observer.tr.position;
                result.Distance = ((Vector2)delta).magnitude;
                result.ClearLOS = observer.movement.HasLOSPosition360(point);
                result.ConeDegrees = observer.LOSCone; result.Range = observer.LOSRange;
                result.InsideCone = WorldGeometry.InsideCone(observer.curRightAngle.x, observer.curRightAngle.y, delta.x, delta.y, observer.LOSCone);
                result.InsideNativeRange = result.Distance <= observer.LOSRange;
                result.Reason = "geometry-only-not-native-perception-decision";
            }
            var views = new List<Dictionary<string, object>>();
            // Renderer enabled/culling are evidence, never a promise about final visible pixels.
            var renderers = target.GetComponentsInChildren<Renderer>(false);
            foreach (var camera in cameras)
            {
                var anchor = camera.WorldToViewportPoint(point);
                bool anyRenderer = false, intersects = false; int eligible = 0;
                foreach (var renderer in renderers)
                {
                    if (!renderer.enabled || !renderer.gameObject.activeInHierarchy ||
                        (camera.cullingMask & (1 << renderer.gameObject.layer)) == 0) continue;
                    anyRenderer = true; eligible++;
                    var b = renderer.bounds;
                    float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
                    bool front = false;
                    for (int x = 0; x < 2; x++) for (int y = 0; y < 2; y++) for (int z = 0; z < 2; z++)
                            {
                                var v = camera.WorldToViewportPoint(new Vector3(x == 0 ? b.min.x : b.max.x,
                                    y == 0 ? b.min.y : b.max.y, z == 0 ? b.min.z : b.max.z));
                                front |= v.z > 0; minX = Math.Min(minX, v.x); maxX = Math.Max(maxX, v.x);
                                minY = Math.Min(minY, v.y); maxY = Math.Max(maxY, v.y);
                            }
                    intersects |= front && WorldGeometry.IntersectsViewport(minX, minY, maxX, maxY);
                }
                views.Add(TraceWorld.Values("camera", camera.GetInstanceID(), "anchorViewport", new[] { anchor.x, anchor.y, anchor.z },
                    "anchorInside", WorldGeometry.InViewport(anchor.x, anchor.y, anchor.z),
                    "boundsIntersect", anyRenderer ? (object)intersects : null, "eligibleRenderers", eligible,
                    "renderingEvidence", "enabled-layer-and-bounds-only"));
            }
            result.Views = views.ToArray(); return result;
        }
        internal static Dictionary<string, object> Detach(PerceptionEvidence p) => TraceWorld.Values("observer", p.Observer,
            "target", p.Target, "gameTime", p.CapturedAt, "distance", p.Distance, "clearLOS", p.ClearLOS,
            "insideCone", p.InsideCone, "insideNativeRange", p.InsideNativeRange, "coneDegrees", p.ConeDegrees,
            "nativeRange", p.Range, "method", p.Method, "reason", p.Reason, "views", p.Views,
            "pixelVisibility", p.PixelVisibility, "hearing", p.Hearing);
    }
}
