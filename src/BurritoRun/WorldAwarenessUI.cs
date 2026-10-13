using System;
using System.Globalization;
using UnityEngine;

namespace BurritoRun
{
    internal sealed partial class WorldAwareness
    {
        private Vector2 resultScroll;
        private Vector2 detailScroll;
        private string radiusText = "6";
        private string minimumText = "0";
        private int cameraIndex;
        private bool busyBenchmark;
        private AwarenessMode benchmarkMode = AwarenessMode.Record;
        private string filterError;
        internal bool IsPausedDetail => DetailOpen;
        internal void ToggleDetail()
        {
            if (DetailOpen) { CloseDetail(); return; }
            var player = GameController.gameController?.playerAgent;
            if (!plugin.TestMode || !plugin.Delivery.CanOrder(player)) return;
            if (Mode == AwarenessMode.Off) SetMode(AwarenessMode.Inspect);
            plugin.Phone.Open(player); DetailOpen = true; nextDetail = 0;
        }
        internal void HideDetail() { DetailOpen = false; }
        private void CloseDetail() { DetailOpen = false; plugin.Phone.Close(); }
        internal void DrawOverlay()
        {
            if (!plugin.TestMode) return;
            if (benchmark.Running && !DetailOpen)
            {
                var progressTimer = System.Diagnostics.Stopwatch.StartNew();
                var progress = new Rect(12, Screen.height - 160, 410, 44);
                Backdrop(progress); GUI.Box(progress, (suiteIndex >= 0 ? "Trial " + (suiteIndex + 1) + "/9 — " : "") + benchmark.Status + "\nKeep playing; a completion notice will appear.");
                double progressCost = progressTimer.Elapsed.TotalMilliseconds; frameWorkMs += progressCost; overlayMs += progressCost;
            }
            if (Mode == AwarenessMode.Off || !Overlay || DetailOpen || plugin.Phone.IsOpen) return;
            var timer = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var observer = Index.Get(ObserverKey);
                if (observer == null) return;
                Backdrop(new Rect(12, 140, 345, 116));
                GUI.Box(new Rect(12, 140, 345, 116), "WORLD AWARENESS — F10 details\n" +
                    observer.Name + " | " + Value(observer, "rootGoal") + " | HP " + Value(observer, "health") + "/" + Value(observer, "healthMax") +
                    "\n" + Index.Count + " entities | " + Mode + " | quality " + Quality.Level + " (" + Quality.Interval.ToString("0.0") + "s)\n" +
                    "age " + Math.Max(0, Time.time - observer.GameTime).ToString("0.00") + "s | " + lastFrameWorkMs.ToString("0.00") + " ms\n" +
                    (error ?? journal.Failure ?? Quality.Reason));
                if (Event.current.type != EventType.Repaint || Quality.Level > 1 || cameras.Count == 0) return;
                var camera = cameras[Math.Min(cameraIndex, cameras.Count - 1)];
                Vector2 origin = ScreenPoint(camera, new Vector3(observer.X, observer.Y, 0));
                Vector2 prior = default;
                for (int i = 0; i <= 32; i++)
                {
                    float angle = i * Mathf.PI * 2 / 32;
                    Vector2 point = ScreenPoint(camera, new Vector3(observer.X + Mathf.Cos(angle) * Query.MaxDistance,
                        observer.Y + Mathf.Sin(angle) * Query.MaxDistance, 0));
                    if (i > 0) Line(prior, point, Color.yellow); prior = point;
                }
                if (Resolve(ObserverKey) is Agent actor)
                {
                    float facing = Mathf.Atan2(actor.curRightAngle.y, actor.curRightAngle.x);
                    foreach (float sign in new[] { -1f, 1f })
                    {
                        float angle = facing + sign * actor.LOSCone * Mathf.Deg2Rad * 0.5f;
                        Line(origin, ScreenPoint(camera, new Vector3(observer.X + Mathf.Cos(angle) * Query.MaxDistance,
                            observer.Y + Mathf.Sin(angle) * Query.MaxDistance, 0)), Color.cyan);
                    }
                }
                int drawn = 0;
                foreach (var e in Results.Entities)
                {
                    if (++drawn > 12) break;
                    var viewport = camera.WorldToViewportPoint(new Vector3(e.X, e.Y, 0));
                    if (!WorldGeometry.InViewport(viewport.x, viewport.y, viewport.z)) continue;
                    Vector2 point = ScreenPoint(camera, new Vector3(e.X, e.Y, 0));
                    GUI.Label(new Rect(point.x + 5, point.y - 15, 160, 22), e.Name + " " +
                        Math.Sqrt(WorldGeometry.DistanceSquared(observer.X, observer.Y, e)).ToString("0.0"));
                }
                var target = Index.Get(TargetKey);
                if (target != null)
                {
                    bool? clear = perception.TryGetValue(TargetKey, out var p) ? p.ClearLOS : null;
                    Line(origin, ScreenPoint(camera, new Vector3(target.X, target.Y, 0)), clear == true ? Color.green : clear == false ? Color.red : Color.gray);
                }
                if (Navigation != null && Navigation.TryGetValue("state", out var s) && s is System.Collections.Generic.Dictionary<string, object> state &&
                    state.TryGetValue("waypoints", out var path) && path is System.Collections.Generic.List<object> points)
                {
                    Vector2 from = origin;
                    foreach (var raw in points)
                        if (raw is float[] xyz)
                        { var to = ScreenPoint(camera, new Vector3(xyz[0], xyz[1], xyz[2])); Line(from, to, Color.magenta); from = to; }
                }
            }
            finally { double cost = timer.Elapsed.TotalMilliseconds; frameWorkMs += cost; overlayMs += cost; }
        }
        private static void Backdrop(Rect rect)
        {
            var previous = GUI.color; GUI.color = new Color(0.055f, 0.06f, 0.075f, 1);
            GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = previous;
        }
        private static Vector2 ScreenPoint(Camera camera, Vector3 point)
        { var p = camera.WorldToScreenPoint(point); return new Vector2(p.x, Screen.height - p.y); }
        private static void Line(Vector2 from, Vector2 to, Color color)
        {
            var matrix = GUI.matrix; var previous = GUI.color;
            Vector2 delta = to - from;
            GUI.color = color; GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, from);
            GUI.DrawTexture(new Rect(from.x, from.y, delta.magnitude, 1), Texture2D.whiteTexture);
            GUI.matrix = matrix; GUI.color = previous;
        }
        private static string Value(WorldEntitySnapshot e, string key) => e?.Data != null && e.Data.TryGetValue(key, out var value) ? value?.ToString() ?? "unknown" : "unknown";
        internal void DrawDetail()
        {
            var width = Math.Min(1100, Screen.width - 24); var height = Math.Max(250, Screen.height - 36);
            Backdrop(new Rect(12, 18, width, height));
            GUILayout.BeginArea(new Rect(12, 18, width, height), "WORLD INSPECTOR — paused; F10/Esc closes", GUI.skin.window);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Close / resume", GUILayout.Width(125))) CloseDetail();
            if (GUILayout.Button("Capture F5", GUILayout.Width(110))) plugin.Diagnostics.MarkIncident();
            if (GUILayout.Button("Inspection: " + Mode, GUILayout.Width(130))) SetMode(Mode == AwarenessMode.Record ? AwarenessMode.Inspect : AwarenessMode.Record);
            Overlay = GUILayout.Toggle(Overlay, "Live overlay", GUILayout.Width(115));
            if (GUILayout.Button("Refresh snapshot", GUILayout.Width(140))) { nextScan = 0; RefreshPaused(); }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            busyBenchmark = GUILayout.Toggle(busyBenchmark, "Busy encounter", GUILayout.Width(140));
            if (GUILayout.Button("Next trial: " + benchmarkMode, GUILayout.Width(150))) benchmarkMode = (AwarenessMode)(((int)benchmarkMode + 1) % 3);
            if (GUILayout.Button("Start " + benchmarkMode + " trial / resume", GUILayout.Width(180))) StartBenchmark(busyBenchmark ? "busy" : "ordinary", benchmarkMode);
            if (GUILayout.Button("Run 9 trials", GUILayout.Width(110))) StartBenchmarkSuite(busyBenchmark ? "busy" : "ordinary");
            GUILayout.Label(benchmark.Status);
            GUILayout.EndHorizontal();
            var observer = Index.Get(ObserverKey);
            GUILayout.Label("Observer: " + (observer != null ? observer.Name + " " + ObserverKey : "pending") +
                " | actual goal " + Value(observer, "rootGoal") + " | HP " + Value(observer, "health") + "/" + Value(observer, "healthMax"));
            GUILayout.Label(Index.Count + " registered | census " + generation + (complete ? " sampled" : " updating") +
                " | queue " + plugin.CaptureWriter.Depth + " | world lost " + journal.Dropped + " | worker lost " + plugin.CaptureWriter.Dropped +
                " | quality " + Quality.Level + " " + Quality.Reason + " | pending perception " + pendingChecks);
            if (error != null || filterError != null) GUILayout.Label("Diagnostic: " + (filterError ?? error));
            GUILayout.Label("Last quality window: " + (Quality.WindowFrames == 0 ? "pending" :
                Quality.WindowMeanMs.ToString("0.00") + " ms mean | " + Quality.WindowOverBudget + "/" + Quality.WindowFrames + " frames >2ms") +
                " | lifecycle detail deferred " + deferredLifecycleDetails + " | hook peak " + hookMaxMs.ToString("0.00") + "ms");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Query.Range.ToString(), GUILayout.Width(85))) { Query.Range = (WorldRange)(((int)Query.Range + 1) % 4); Query.Offset = 0; nextDetail = 0; }
            GUILayout.Label("Min", GUILayout.Width(28)); minimumText = GUILayout.TextField(minimumText, GUILayout.Width(48));
            GUILayout.Label("Radius/max", GUILayout.Width(80)); radiusText = GUILayout.TextField(radiusText, GUILayout.Width(48));
            GUILayout.Label("Search", GUILayout.Width(45)); Query.Search = GUILayout.TextField(Query.Search, GUILayout.Width(145));
            if (GUILayout.Button(Query.Kind?.ToString() ?? "All kinds", GUILayout.Width(105)))
            { Query.Kind = !Query.Kind.HasValue ? WorldKind.Actor : (int)Query.Kind.Value == 11 ? (WorldKind?)null : Query.Kind.Value + 1; Query.Offset = 0; nextDetail = 0; }
            GUILayout.Label("Tag", GUILayout.Width(25)); Query.Tag = GUILayout.TextField(Query.Tag, GUILayout.Width(80));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            Query.IncludeInactive = GUILayout.Toggle(Query.IncludeInactive, "Inactive/dead", GUILayout.Width(125));
            Query.IncludeHelpers = GUILayout.Toggle(Query.IncludeHelpers, "Helpers", GUILayout.Width(85));
            if (GUILayout.Button("Camera " + (cameras.Count > 0 ? cameras[Math.Min(cameraIndex, cameras.Count - 1)].GetInstanceID().ToString() : "unavailable"), GUILayout.Width(155)))
                cameraIndex = cameras.Count == 0 ? 0 : (cameraIndex + 1) % cameras.Count;
            if (GUILayout.Button(Query.AnyViewport ? "Any viewport" : Query.CameraId.HasValue ? "This viewport" : "All positions", GUILayout.Width(105)))
            {
                if (!Query.AnyViewport && !Query.CameraId.HasValue) Query.AnyViewport = true;
                else if (Query.AnyViewport && cameras.Count > 0) { Query.AnyViewport = false; Query.CameraId = cameras[Math.Min(cameraIndex, cameras.Count - 1)].GetInstanceID(); }
                else { Query.AnyViewport = false; Query.CameraId = null; }
                Query.Offset = 0; nextDetail = 0;
            }
            if (GUILayout.Button("Previous", GUILayout.Width(80))) { Query.Offset = Math.Max(0, Query.Offset - Query.Limit); nextDetail = 0; }
            if (GUILayout.Button("Next", GUILayout.Width(65))) { if (Query.Offset + Query.Limit < Results.TotalMatched) Query.Offset += Query.Limit; nextDetail = 0; }
            GUILayout.Label(Results.TotalMatched + " matches; page " + (Query.Offset / Query.Limit + 1));
            GUILayout.EndHorizontal();
            if (float.TryParse(radiusText, NumberStyles.Float, CultureInfo.InvariantCulture, out float radius) && radius >= 0 && radius <= 100000 &&
                float.TryParse(minimumText, NumberStyles.Float, CultureInfo.InvariantCulture, out float minimum) && minimum >= 0 && minimum <= radius)
            { Query.MaxDistance = radius; Query.MinDistance = minimum; filterError = null; }
            else filterError = "Enter finite distances with 0 <= min <= max <= 100000.";
            GUILayout.BeginHorizontal();
            resultScroll = GUILayout.BeginScrollView(resultScroll, GUILayout.Width(width * 0.52f), GUILayout.Height(Math.Max(1, height - 272)));
            foreach (var e in Results.Entities)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(e.Name + " [" + e.Kind + "] " + Math.Sqrt(WorldGeometry.DistanceSquared(Query.X, Query.Y, e)).ToString("0.00") + "u", GUILayout.Width(width * 0.37f))) SelectTarget(e.Key);
                if ((e.Kind == WorldKind.Actor || e.Kind == WorldKind.Player) && GUILayout.Button("Observe", GUILayout.Width(75))) SelectObserver(e.Key);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            detailScroll = GUILayout.BeginScrollView(detailScroll, GUILayout.Height(Math.Max(1, height - 272)));
            plugin.NpcDesires.DrawInspection();
            var selected = Index.Get(TargetKey);
            if (selected != null)
            {
                GUILayout.Label(selected.Name + "\n" + selected.Key + "\n" + selected.NativeType + " | " + selected.ClassificationSource);
                GUILayout.Label("Position " + selected.X.ToString("0.00") + ", " + selected.Y.ToString("0.00") +
                    " | snapshot age " + Math.Max(0, Time.time - selected.GameTime).ToString("0.00") + "s");
                foreach (var pair in selected.Data) GUILayout.Label(pair.Key + ": " + (pair.Value is string[] strings ? string.Join(", ", strings) : pair.Value?.ToString() ?? "unknown"));
                if (perception.TryGetValue(TargetKey, out var evidence))
                {
                    GUILayout.Label("Distance " + evidence.Distance.ToString("0.00") + "u / " + (evidence.Distance / 0.64f).ToString("0.0") + " tiles\n" +
                        "Obstruction LOS: " + (evidence.ClearLOS?.ToString() ?? "unknown") + "\nInside cone: " + evidence.InsideCone +
                        " | native range: " + evidence.InsideNativeRange + "\n" + evidence.Method + "\nAge " + Math.Max(0, Time.time - evidence.CapturedAt).ToString("0.00") + "s");
                    foreach (var view in evidence.Views) GUILayout.Label("Camera " + view["camera"] + ": anchor=" + view["anchorInside"] + ", bounds=" + (view["boundsIntersect"] ?? "unknown"));
                    GUILayout.Label("Pixel visibility and hearing: unknown. Geometry is not native perception.");
                }
                else GUILayout.Label("Perception pending / unavailable");
            }
            else GUILayout.Label("Select a target. Observe selects an actor as the query origin.");
            if (Navigation != null && Navigation["state"] is System.Collections.Generic.Dictionary<string, object> nav)
            {
                GUILayout.Space(8); GUILayout.Label("OBSERVER NAVIGATION");
                foreach (string name in new[] { "rootGoal", "followingUid", "followDistance", "distanceToFollowed", "relationshipToFollowed", "hasPath", "pathNull", "currentWaypoint", "blockedByDoor", "isBlockedByDoor" })
                    GUILayout.Label(name + ": " + (nav[name]?.ToString() ?? "unknown"));
                GUILayout.Label("Stationary " + Navigation["stationarySeconds"] + "s (not a stuck verdict). Door candidates are not proven routes.");
            }
            GUILayout.EndScrollView(); GUILayout.EndHorizontal(); GUILayout.EndArea();
        }
        private void RefreshPaused()
        {
            var gc = GameController.gameController;
            if (!Ready(gc)) return;
            var timer = System.Diagnostics.Stopwatch.StartNew();
            generation++; collection = cursor = 0; counts.Clear(); scanning = true; complete = false;
            // Explicit paused census: no live-frame latency claim; native reads remain on main thread.
            while (scanning) ScanOne(gc);
            EnsureObserver(gc); RefreshDetails(gc);
            Write("paused-capture", TraceWorld.Values("captureMs", timer.Elapsed.TotalMilliseconds, "entities", Index.Count, "generation", generation), true);
        }
    }
}
