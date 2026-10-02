using System;
using System.Collections.Generic;
using UnityEngine;

namespace BurritoRun
{
    // First aquarium probe: one native tank at a time, a five-tile native water
    // spill, and at most nine short-lived code-drawn fish visuals per level.
    // No ownership, electrical, AI or pickup rules are invented here.
    internal sealed class AquariumService
    {
        private readonly Plugin plugin;
        private readonly List<LooseFish> looseFish = new List<LooseFish>();
        private GameController controller;
        private ObjectReal testTank;
        private int testTankId;
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

            var origin = player.tr.position;
            var spot = player.gc.tileInfo.FindLocationNearLocation(origin, null, 1.2f, 2.5f, true, true);
            float distance = Vector2.Distance(origin, spot);
            if (spot == Vector2.zero || distance < 0.8f || distance > 2.6f ||
                !player.gc.tileInfo.PossibleToWalk(player, spot))
            { plugin.Notice("No clear nearby tank spot. Move to a more open area."); return; }

            try
            {
                var tank = player.gc.spawnerMain.spawnObjectReal(new Vector3(spot.x, spot.y, 0), player, "FishTank");
                if (tank == null) { plugin.Notice("Native FishTank prefab was unavailable; no tank spawned."); return; }
                controller = player.gc;
                testTank = tank;
                testTankId = tank.UID;
                status = "Tank " + testTankId + " placed. Hit it to test the spill.";
                plugin.Log("BURRITO_AQUARIUM_SPAWN tank=" + testTankId + " prefab=" + tank.prefabName +
                    " type=" + tank.GetType().Name + " at=" + tank.tr.position +
                    " owner=" + tank.owner);
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
            for (int index = 0; index < 3 && looseFish.Count < 9; index++)
            {
                if (SpawnLooseFish(new Vector3(center.x + (index - 1) * 0.28f,
                    center.y - 0.35f, -1.5f), index == 1, index)) fishCount++;
            }
            status = "Tank broke: " + waterTiles + " wet tiles, " + fishCount + " loose critters.";
            plugin.Log("BURRITO_AQUARIUM_BREAK tank=" + testTankId + " damager=" +
                (damager != null ? damager.UID.ToString() : "none") + " at=" + center +
                " wetTiles=" + waterTiles + " looseCritters=" + fishCount);
            plugin.Notice(status);
        }

        internal void Tick()
        {
            var current = GameController.gameController;
            if (controller != null && current != controller)
            {
                foreach (var fish in looseFish) if (fish.Object != null) UnityEngine.Object.Destroy(fish.Object);
                looseFish.Clear();
                testTank = null;
                controller = null;
                status = "No test tank. Spawn one in an open area.";
                return;
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
                fish.Object.transform.position = fish.Origin + new Vector3(
                    Mathf.Sin(age * (4.7f + fish.Index)) * 0.18f,
                    Mathf.Abs(Mathf.Sin(age * 7f + fish.Index)) * 0.10f, 0);
                fish.Object.transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(age * 9f + fish.Index) * 28f);
            }
        }

        private bool SpawnLooseFish(Vector3 position, bool shrimp, int index)
        {
            try
            {
                var sprite = shrimp ? shrimpSprite ??= MakeSprite(new Color32(255, 112, 108, 255)) :
                    fishSprite ??= MakeSprite(new Color32(77, 219, 211, 255));
                var fish = new GameObject(shrimp ? "BurritoRun Loose Shrimp" : "BurritoRun Loose Fish");
                fish.transform.position = position;
                var renderer = fish.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = 1000;
                looseFish.Add(new LooseFish(fish, position, Time.time, Time.time + 25f, index));
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
            internal readonly float Born, Expires;
            internal readonly int Index;
            internal LooseFish(GameObject obj, Vector3 origin, float born, float expires, int index)
            { Object = obj; Origin = origin; Born = born; Expires = expires; Index = index; }
        }
    }
}
