using System;
using System.Collections.Generic;
using UnityEngine;

namespace BurritoRun
{
    // Aquarium probe: one native stand at a time, a five-tile native water spill,
    // and at most nine short-lived code-drawn critter visuals per level.
    // No ownership, electrical, AI or pickup rules are invented here.
    internal sealed class AquariumService
    {
        private readonly Plugin plugin;
        private readonly List<LooseFish> looseFish = new List<LooseFish>();
        private GameController controller;
        private ObjectReal testTank;
        private int testTankId;
        private GameObject tankVisual;
        private Sprite tankSprite;
        private Sprite fishSprite;
        private Sprite shrimpSprite;
        private string status = "No test tank. Spawn one in an open area.";

        internal string Status => status;

        internal AquariumService(Plugin plugin) { this.plugin = plugin; }

        internal void SpawnTestTank(Agent player)
        {
            if (!plugin.TestMode || !plugin.Delivery.CanOrder(player))
            { plugin.Notice("Start a quick-test single-player level first."); return; }
            if (testTank != null && !testTank.destroyed && testTank.gc == player.gc)
            { plugin.Notice("Break the existing test tank before spawning another."); return; }
            if (looseFish.Count > 6)
            { plugin.Notice("Wait for the loose fish to clear before testing another tank."); return; }

            try
            {
                if (!TryFindSpot(player, out var spot))
                { plugin.Notice("No clear tank square within 3.6 units; see placement numbers in log."); return; }
                // FishTank exists only in the game's name database, not its prefab
                // dictionary. Table is registered in both prefab and object-var data.
                var tank = player.gc.spawnerMain.spawnObjectReal(new Vector3(spot.x, spot.y, 0), player, "Table");
                if (tank == null) { plugin.Notice("Native Table base could not spawn; see log."); return; }
                controller = player.gc;
                testTank = tank;
                testTankId = tank.UID;
                try { tankVisual = CreateTankVisual(tank); }
                catch (Exception visualError) { plugin.Error(visualError); }
                status = "Aquarium " + testTankId + " on a Table base. Hit it to test.";
                plugin.Log("BURRITO_AQUARIUM_SPAWN tank=" + testTankId + " prefab=" + tank.prefabName +
                    " type=" + tank.GetType().Name + " at=" + tank.tr.position +
                    " requested=" + spot + " visual=" + (tankVisual != null) + " owner=" + tank.owner);
                plugin.Notice(status);
            }
            catch (Exception error)
            {
                plugin.Error(error);
                status = "Tank spawn failed; see the event log.";
                plugin.Notice(status);
            }
        }

        internal void ForceBreak(Agent player)
        {
            if (!plugin.TestMode || player == null || !ShouldBreak(testTank) || player.gc != controller)
            { plugin.Notice("Spawn an intact test aquarium on this level first."); return; }
            plugin.Log("BURRITO_AQUARIUM_ACTION force-break tank=" + testTankId + " player=" + player.UID);
            testTank.DestroyMe(player);
        }

        internal bool ShouldBreak(ObjectReal tank) => plugin.TestMode && tank != null &&
            tank == testTank && tank.UID == testTankId && tank.gc == controller &&
            controller == GameController.gameController && controller.loadComplete &&
            controller.serverPlayer && !tank.destroying && !tank.destroyed;

        internal void OnBroken(ObjectReal tank, PlayfieldObject damager)
        {
            if (tank == null || tank != testTank || tank.UID != testTankId ||
                tank.gc != controller || controller != GameController.gameController) return;
            testTank = null; // Consume the event before any native callback can re-enter.
            if (tankVisual != null) UnityEngine.Object.Destroy(tankVisual);
            tankVisual = null;
            var center = tank.tr.position;
            int waterTiles = 0;
            var offsets = new[]
            {
                Vector2.zero, Vector2.right * 0.64f, Vector2.left * 0.64f,
                Vector2.up * 0.64f, Vector2.down * 0.64f
            };
            foreach (var offset in offsets)
                if (controller.tileInfo.SpillLiquid(new Vector2(center.x + offset.x, center.y + offset.y), "Water", false))
                    waterTiles++;

            int fishCount = 0;
            int scatterCount = 0;
            for (int index = 0; index < 3 && looseFish.Count < 9; index++)
            {
                if (SpawnLooseFish(new Vector3(center.x + (index - 1) * 0.28f,
                    center.y - 0.35f, -1.5f), index > 0, index, center, out bool scattered))
                {
                    fishCount++;
                    if (scattered) scatterCount++;
                }
            }
            status = "Tank broke: " + waterTiles + " wet tiles, " + fishCount +
                " critters, " + scatterCount + " shrimp scuttling.";
            plugin.Log("BURRITO_AQUARIUM_BREAK tank=" + testTankId + " damager=" +
                (damager != null ? damager.UID.ToString() : "none") + " at=" + center +
                " wetTiles=" + waterTiles + " looseCritters=" + fishCount +
                " scatteringShrimp=" + scatterCount);
            plugin.Notice(status);
        }

        internal void Tick()
        {
            var current = GameController.gameController;
            if (controller != null && current != controller)
            {
                foreach (var fish in looseFish) if (fish.Object != null) UnityEngine.Object.Destroy(fish.Object);
                looseFish.Clear();
                if (tankVisual != null) UnityEngine.Object.Destroy(tankVisual);
                tankVisual = null;
                testTank = null;
                controller = null;
                status = "No test tank. Spawn one in an open area.";
                return;
            }
            if (testTank != null && (testTank.destroying || testTank.destroyed))
            {
                plugin.Log("BURRITO_AQUARIUM_MISSED_HOOK tank=" + testTankId +
                    " destroying=" + testTank.destroying + " destroyed=" + testTank.destroyed);
                if (tankVisual != null) UnityEngine.Object.Destroy(tankVisual);
                tankVisual = null;
                testTank = null;
                status = "Tank destroyed without spill hook; see log.";
            }
            for (int index = looseFish.Count - 1; index >= 0; index--)
            {
                var fish = looseFish[index];
                if (fish.Object == null || Time.time >= fish.Expires)
                {
                    if (fish.Object != null) UnityEngine.Object.Destroy(fish.Object);
                    looseFish.RemoveAt(index);
                    continue;
                }
                float age = Time.time - fish.Born;
                if (fish.Scatter)
                {
                    const float delay = 0.35f, duration = 2.4f;
                    float progress = Mathf.Clamp01((age - delay) / duration);
                    float eased = Mathf.SmoothStep(0, 1, progress);
                    var position = Vector3.Lerp(fish.Origin, fish.Destination, eased);
                    float twitch = progress > 0 && progress < 1 ?
                        Mathf.Sin(age * 24f + fish.Index) * 0.045f : 0;
                    position += new Vector3(-fish.DirectionY * twitch,
                        fish.DirectionX * twitch + Mathf.Abs(Mathf.Sin(age * 11f)) * 0.025f, 0);
                    fish.Object.transform.position = position;
                    fish.Object.transform.rotation = Quaternion.Euler(0, 0,
                        Mathf.Sin(age * 14f + fish.Index) * 12f);
                }
                else
                {
                    fish.Object.transform.position = fish.Origin + new Vector3(
                        Mathf.Sin(age * (4.7f + fish.Index)) * 0.18f,
                        Mathf.Abs(Mathf.Sin(age * 7f + fish.Index)) * 0.10f, 0);
                    fish.Object.transform.rotation = Quaternion.Euler(0, 0,
                        Mathf.Sin(age * 9f + fish.Index) * 28f);
                }
            }
        }

        private bool TryFindSpot(Agent player, out Vector2 spot)
        {
            var origin = (Vector2)player.tr.position;
            var tiles = player.gc.tileInfo;
            var native = tiles.FindLocationNearLocation(origin, null, 1.2f, 2.5f, true, true);
            float nativeDistance = Vector2.Distance(origin, native);
            bool nativeWalkable = native != Vector2.zero && tiles.PossibleToWalk(player, native);
            if (nativeWalkable && nativeDistance >= 1.0f && nativeDistance <= 3.2f)
            {
                spot = native;
                plugin.Log("BURRITO_AQUARIUM_PLACE origin=" + origin + " native=" + native +
                    " distance=" + nativeDistance.ToString("F2") + " walkable=True source=native");
                return true;
            }

            var center = new Vector2(
                Mathf.Floor((origin.x + 0.32f) / 0.64f) * 0.64f,
                Mathf.Floor((origin.y + 0.32f) / 0.64f) * 0.64f);
            var directions = new[]
            {
                Vector2.right, Vector2.left, Vector2.up, Vector2.down,
                new Vector2(1, 1), new Vector2(-1, 1), new Vector2(1, -1), new Vector2(-1, -1)
            };
            int tested = 0;
            int walkable = 0;
            foreach (int radius in new[] { 3, 4, 2, 5 })
                foreach (var direction in directions)
                {
                    var candidate = center + direction * (radius * 0.64f);
                    float distance = Vector2.Distance(origin, candidate);
                    if (distance < 1.0f || distance > 3.6f) continue;
                    tested++;
                    if (!tiles.PossibleToWalk(player, candidate)) continue;
                    walkable++;
                    spot = candidate;
                    plugin.Log("BURRITO_AQUARIUM_PLACE origin=" + origin + " native=" + native +
                        " nativeDistance=" + nativeDistance.ToString("F2") +
                        " nativeWalkable=" + nativeWalkable + " fallbackTested=" + tested +
                        " fallbackWalkable=" + walkable + " chosen=" + spot +
                        " chosenDistance=" + distance.ToString("F2") + " source=grid");
                    return true;
                }
            spot = Vector2.zero;
            plugin.Log("BURRITO_AQUARIUM_PLACE origin=" + origin + " native=" + native +
                " nativeDistance=" + nativeDistance.ToString("F2") +
                " nativeWalkable=" + nativeWalkable + " fallbackTested=" + tested +
                " fallbackWalkable=" + walkable + " chosen=none");
            return false;
        }

        private GameObject CreateTankVisual(ObjectReal tank)
        {
            tankSprite ??= MakeTankSprite();
            var visual = new GameObject("BurritoRun Aquarium Glass");
            visual.transform.SetParent(tank.tr, false);
            visual.transform.localPosition = new Vector3(0, 0.55f, -0.001f);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = tankSprite;
            if (tank.spr != null)
            {
                var nativeRenderer = tank.spr.GetComponent<Renderer>();
                if (nativeRenderer != null)
                {
                    renderer.sortingLayerID = nativeRenderer.sortingLayerID;
                    renderer.sortingOrder = nativeRenderer.sortingOrder + 1;
                }
            }
            return visual;
        }

        private static Sprite MakeTankSprite()
        {
            const int width = 24, height = 18;
            var pixels = new Color32[width * height];
            var frame = new Color32(30, 43, 62, 255);
            var water = new Color32(53, 150, 192, 220);
            var glass = new Color32(157, 226, 244, 115);
            var fish = new Color32(255, 191, 69, 255);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                        pixels[y * width + x] = frame;
                    else if (y <= 10) pixels[y * width + x] = water;
                    else pixels[y * width + x] = glass;
                }
            for (int x = 2; x < width - 2; x++) pixels[11 * width + x] = new Color32(177, 233, 247, 255);
            for (int x = 7; x <= 11; x++) pixels[6 * width + x] = fish;
            pixels[5 * width + 7] = fish;
            pixels[7 * width + 7] = fish;
            pixels[6 * width + 10] = new Color32(25, 46, 65, 255);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0), 24f);
        }

        private bool SpawnLooseFish(Vector3 position, bool shrimp, int index, Vector3 center,
            out bool scattered)
        {
            scattered = false;
            try
            {
                ScatterRoute route = default;
                var player = controller.playerAgent;
                if (shrimp && player != null)
                {
                    try
                    {
                        route = AquariumScatter.Choose(center.x, center.y,
                            testTankId + (index - 1) * 2, (x, y) =>
                                controller.tileInfo.PossibleToWalk(player, new Vector2(x, y)));
                    }
                    catch (Exception routeError) { plugin.Error(routeError); }
                }
                var destination = route.Found ? new Vector3(route.X, route.Y, position.z) : position;
                var sprite = shrimp ? shrimpSprite ??= MakeSprite(new Color32(255, 112, 108, 255)) :
                    fishSprite ??= MakeSprite(new Color32(77, 219, 211, 255));
                var critter = new GameObject(shrimp ? "BurritoRun Loose Shrimp" : "BurritoRun Loose Fish");
                critter.transform.position = position;
                var renderer = critter.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = 1000;
                renderer.flipX = route.Found && destination.x < position.x;
                looseFish.Add(new LooseFish(critter, position, destination, Time.time,
                    Time.time + 25f, index, route.Found));
                scattered = route.Found;
                plugin.Log("BURRITO_AQUARIUM_CRITTER tank=" + testTankId + " kind=" +
                    (shrimp ? "shrimp" : "fish") + " index=" + index +
                    " mode=" + (route.Found ? "scatter" : "flop") +
                    " at=" + position + " target=" + destination +
                    " direction=" + (route.Found ? route.Direction.ToString() : "none") +
                    " steps=" + route.Steps);
                return true;
            }
            catch (Exception error) { plugin.Error(error); return false; }
        }

        private static Sprite MakeSprite(Color32 color)
        {
            const int width = 12, height = 7;
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    bool body = x >= 3 && x <= 9 && Math.Abs(y - 3) <= (x == 3 || x == 9 ? 1 : 2);
                    bool tail = x <= 2 && Math.Abs(y - 3) <= 2 && Math.Abs(y - 3) >= x / 2;
                    if (body || tail) pixels[y * width + x] = color;
                }
            pixels[4 * width + 8] = new Color32(17, 35, 47, 255);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 24f);
        }

        private sealed class LooseFish
        {
            internal readonly GameObject Object;
            internal readonly Vector3 Origin;
            internal readonly Vector3 Destination;
            internal readonly float Born, Expires;
            internal readonly int Index;
            internal readonly bool Scatter;
            internal readonly float DirectionX, DirectionY;
            internal LooseFish(GameObject obj, Vector3 origin, Vector3 destination, float born,
                float expires, int index, bool scatter)
            {
                Object = obj;
                Origin = origin;
                Destination = destination;
                Born = born;
                Expires = expires;
                Index = index;
                Scatter = scatter;
                var direction = (destination - origin).normalized;
                DirectionX = direction.x;
                DirectionY = direction.y;
            }
        }
    }
}
