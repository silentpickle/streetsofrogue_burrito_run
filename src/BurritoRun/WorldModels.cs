using System;
using System.Collections.Generic;

namespace BurritoRun
{
    internal enum WorldKind { Actor, Player, Item, Object, Projectile, Noise, Explosion, Fire, Gas, Water, Danger }
    internal enum WorldRange { Within, Band, Outside, All }

    // Detached values only. Once published, snapshots are never modified.
    internal sealed class WorldEntitySnapshot
    {
        public string Key { get; set; }
        public int Level { get; set; }
        public int NativeId { get; set; }
        public int Instance { get; set; }
        public int Activation { get; set; }
        public WorldKind Kind { get; set; }
        public string Name { get; set; }
        public string NativeType { get; set; }
        public string Collection { get; set; }
        public string[] Tags { get; set; } = Array.Empty<string>();
        public string ClassificationSource { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float GameTime { get; set; }
        public double WallTime { get; set; }
        public bool Active { get; set; }
        public bool Destroyed { get; set; }
        public bool Helper { get; set; }
        public int[] ViewportCameraIds { get; set; }
        public float ViewportCapturedAt { get; set; }
        public Dictionary<string, object> Data { get; set; }
    }

    internal sealed class WorldQuery
    {
        public string ObserverKey { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public WorldRange Range { get; set; } = WorldRange.Within;
        public float MinDistance { get; set; }
        public float MaxDistance { get; set; } = 6;
        public WorldKind? Kind { get; set; }
        public string Search { get; set; } = "";
        public string Tag { get; set; } = "";
        public bool IncludeInactive { get; set; }
        public bool IncludeHelpers { get; set; }
        public int? CameraId { get; set; }
        public bool AnyViewport { get; set; }
        public int Offset { get; set; }
        public int Limit { get; set; } = 50;
    }

    internal sealed class WorldQueryResult
    {
        public WorldEntitySnapshot[] Entities { get; set; }
        public int TotalMatched { get; set; }
        public int CandidatesChecked { get; set; }
        public int Generation { get; set; }
        public bool Complete { get; set; }
        public string Coverage { get; set; }
        public float CapturedAt { get; set; }
    }

    internal sealed class CameraViewSnapshot
    {
        public int CameraId { get; set; }
        public int[] PlayerIds { get; set; }
        public float[] Rect { get; set; }
        public string Mapping { get; set; }
        public bool Enabled { get; set; }
    }

    internal sealed class PerceptionEvidence
    {
        public string Observer { get; set; }
        public string Target { get; set; }
        public float CapturedAt { get; set; }
        public float Distance { get; set; }
        public bool? ClearLOS { get; set; }
        public bool? InsideCone { get; set; }
        public bool? InsideNativeRange { get; set; }
        public float? ConeDegrees { get; set; }
        public float? Range { get; set; }
        public string Method { get; set; }
        public string Reason { get; set; }
        public Dictionary<string, object>[] Views { get; set; }
        public string PixelVisibility { get; set; } = "unknown-lighting-and-occlusion-not-render-proof";
        public string Hearing { get; set; } = "unknown-radius-is-not-hearing";
    }

    internal sealed class NavigationEvidence
    {
        public string Actor { get; set; }
        public float CapturedAt { get; set; }
        public Dictionary<string, object> State { get; set; }
        public string[] DoorCandidates { get; set; }
        public string Reachability { get; set; } = "unknown-current-path-is-not-proof-of-arrival";
    }

    internal static class WorldGeometry
    {
        internal static float DistanceSquared(float x, float y, WorldEntitySnapshot entity)
        { float dx = x - entity.X, dy = y - entity.Y; return dx * dx + dy * dy; }
        internal static bool InViewport(float x, float y, float z) => z > 0 && x >= 0 && x <= 1 && y >= 0 && y <= 1;
        internal static bool IntersectsViewport(float minX, float minY, float maxX, float maxY) =>
            maxX >= 0 && minX <= 1 && maxY >= 0 && minY <= 1;
        internal static bool InsideCone(float directionX, float directionY, float targetX, float targetY, float degrees)
        {
            double length = Math.Sqrt((directionX * directionX + directionY * directionY) *
                (targetX * targetX + targetY * targetY));
            if (length <= 0) return false;
            double cosine = Math.Max(-1, Math.Min(1, (directionX * targetX + directionY * targetY) / length));
            return Math.Acos(cosine) * 180 / Math.PI < degrees * 0.5;
        }
    }
}
