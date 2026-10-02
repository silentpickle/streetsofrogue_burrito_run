using System;
using System.Collections.Generic;
using UnityEngine;

namespace BurritoRun
{
    // One quick-test dog-like actor per level. A native Courier supplies collision,
    // damage and goal/pathfinding behavior; the dog art and snack interest are ours.
    internal sealed class PetService
    {
        private const float MinSpawnDistance = 1.7f; // Outside an accidental melee swing.
        private readonly Plugin plugin;
        private readonly List<Renderer> hiddenRenderers = new List<Renderer>();
        private GameController controller;
        private Agent pet;
        private Agent owner;
        private GameObject dogVisual;
        private Sprite dogSprite;
        private PetIntent intent = PetIntent.Wander;
        private Item targetFood;
        private int ignoredFoodId;
        private float ignoreFoodUntil;
        private float chaseUntil;
        private float nextCheck;
        private bool spawnedThisLevel;
        private bool snackDropped;
        private bool released;
        private bool deathLogged;
        private string status = "No test pet on this level.";

        internal PetService(Plugin plugin) { this.plugin = plugin; }
        internal string Status => status;

        internal void SpawnTestPet(Agent player)
        {
            if (!plugin.TestMode || !plugin.Delivery.CanOrder(player))
            { plugin.Notice("Start a quick-test single-player level first."); return; }
            if (spawnedThisLevel)
            { plugin.Notice("Only one test pet per level. Try a new level to reset."); return; }

            try
            {
                var gc = player.gc;
                if (!TryFindSpot(player, out var spot))
                {
                    plugin.Notice("No clear test-pet spot nearby; move to an open area.");
                    return;
                }
                var spawned = gc.spawnerMain.SpawnAgent(spot, (PlayfieldObject)null, "Courier");
                if (spawned == null)
                { plugin.Notice("Native test-pet actor could not spawn; see log."); return; }

                controller = gc;
                pet = spawned;
                owner = player;
                spawnedThisLevel = true;
                released = false;
                deathLogged = false;
                // A follower should begin as an ally, not merely a neutral Courier
                // whose first incidental shove can turn into a lethal feud.
                try
                {
                    pet.relationships.SetRel(owner, "Friendly");
                    owner.relationships.SetRel(pet, "Friendly");
                }
                catch (Exception relationshipError) { plugin.Error(relationshipError); }
                try { CreateDogVisual(spawned); }
                catch (Exception visualError)
                {
                    plugin.Error(visualError);
                    foreach (var renderer in hiddenRenderers)
                        if (renderer != null) renderer.enabled = true;
                    hiddenRenderers.Clear();
                    if (dogVisual != null) UnityEngine.Object.Destroy(dogVisual);
                    dogVisual = null;
                }
                SetFollow("spawn");
                plugin.Log("BURRITO_PET_SPAWN pet=" + pet.UID + " native=Courier owner=" +
                    owner.UID + " at=" + pet.tr.position + " requested=" + spot +
                    " visual=" + (dogVisual != null) + " hiddenNativeSprites=" + hiddenRenderers.Count +
                    " relationship=" + (pet.relationships != null ? pet.relationships.GetRel(owner) : "none"));
                plugin.Notice("Test street dog is following you. Drop a Delivery Burrito to distract it.");
            }
            catch (Exception error)
            {
                plugin.Error(error);
                plugin.Notice("Test-pet spawn failed; see the event log.");
            }
        }

        internal void ReleaseTestPet()
        {
            if (!plugin.TestMode || pet == null || pet.dead || released)
            { plugin.Notice("Spawn a following test pet first."); return; }
            Release("test-release");
            plugin.Notice("Test dog lost its owner and is wandering on native AI.");
        }

        internal void DropTestSnack(Agent player)
        {
            if (!plugin.TestMode || player == null || player != owner || pet == null ||
                pet.dead || released)
            { plugin.Notice("Spawn a following test dog first."); return; }
            if (snackDropped)
            { plugin.Notice("One free test snack per level; other dropped burritos also work."); return; }
            if (player.inventory.FindItem(Names.Food) != null)
            { plugin.Notice("Drop your existing Delivery Burrito to test the dog without stacking items."); return; }
            try
            {
                var snack = player.inventory.AddItem(Names.Food, 1);
                if (snack == null)
                { plugin.Notice("No inventory room for a test snack."); return; }
                player.inventory.DropItem(snack);
                snackDropped = true;
                plugin.Log("BURRITO_PET_SNACK action=drop player=" + player.UID +
                    " pet=" + pet.UID + " at=" + player.tr.position);
                plugin.Notice("Free test burrito dropped. Watch whether the dog investigates.");
            }
            catch (Exception error) { plugin.Error(error); }
        }

        internal void Tick()
        {
            if (controller != null && controller != GameController.gameController)
            { ClearForLevelChange(); return; }
            if (pet == null) return;
            if (pet.dead)
            {
                if (!deathLogged)
                {
                    deathLogged = true;
                    status = "Test pet died. Start a new level for another.";
                    plugin.Log("BURRITO_PET_DEAD pet=" + pet.UID + " at=" + pet.tr.position);
                }
                return;
            }
            if (dogVisual != null)
            {
                var renderer = dogVisual.GetComponent<SpriteRenderer>();
                if (renderer != null && pet.rigidBody2D != null &&
                    Mathf.Abs(pet.rigidBody2D.velocity.x) > 0.1f)
                    renderer.flipX = pet.rigidBody2D.velocity.x < 0;
            }
            if (Time.time < nextCheck) return;
            nextCheck = Time.time + 0.4f;
            if (!released && (owner == null || owner.dead || owner.gc != controller))
                Release("owner-lost");
            if (released) return;

            if (intent == PetIntent.ChaseFood)
            {
                if (targetFood == null || targetFood.tr == null)
                { SetFollow("food-gone"); return; }
                float distance = Vector2.Distance(pet.tr.position, targetFood.tr.position);
                if (distance <= PetPolicy.FoodArrivalRadius || Time.time >= chaseUntil)
                {
                    ignoredFoodId = targetFood.UID;
                    ignoreFoodUntil = Time.time + 8f;
                    plugin.Log("BURRITO_PET_SNIFF pet=" + pet.UID + " food=" + targetFood.UID +
                        " distance=" + distance.ToString("F2") +
                        " outcome=" + (distance <= PetPolicy.FoodArrivalRadius ? "reached" : "timed-out"));
                    SetFollow("snack-investigated");
                    return;
                }
                return;
            }

            var food = FindNearbyFood(out float foodDistance);
            if (PetPolicy.Choose(true, food != null, foodDistance, false) == PetIntent.ChaseFood)
                Chase(food, foodDistance);
        }

        private bool TryFindSpot(Agent player, out Vector2 spot)
        {
            var origin = (Vector2)player.tr.position;
            var tiles = player.gc.tileInfo;
            var native = tiles.FindLocationNearLocation(origin, null, 1.8f, 3f, true, true);
            float nativeDistance = Vector2.Distance(origin, native);
            if (native != Vector2.zero && nativeDistance >= MinSpawnDistance && nativeDistance <= 3.5f &&
                tiles.PossibleToWalk(player, native))
            {
                spot = native;
                plugin.Log("BURRITO_PET_PLACE origin=" + origin + " native=" + native +
                    " distance=" + nativeDistance.ToString("F2") + " source=native");
                return true;
            }
            var center = new Vector2(
                Mathf.Floor((origin.x + 0.32f) / 0.64f) * 0.64f,
                Mathf.Floor((origin.y + 0.32f) / 0.64f) * 0.64f);
            var directions = new[]
            {
                Vector2.right, Vector2.left, Vector2.up, Vector2.down,
                new Vector2(1, 1), new Vector2(-1, 1),
                new Vector2(1, -1), new Vector2(-1, -1)
            };
            int tested = 0;
            foreach (int radius in new[] { 2, 3, 4, 5 })
                foreach (var direction in directions)
                {
                    var candidate = center + direction * (radius * 0.64f);
                    float distance = Vector2.Distance(origin, candidate);
                    if (distance < MinSpawnDistance || distance > 3.5f) continue;
                    tested++;
                    if (!tiles.PossibleToWalk(player, candidate)) continue;
                    spot = candidate;
                    plugin.Log("BURRITO_PET_PLACE origin=" + origin + " native=" + native +
                        " nativeDistance=" + nativeDistance.ToString("F2") +
                        " tested=" + tested + " chosen=" + spot + " source=grid");
                    return true;
                }
            spot = Vector2.zero;
            plugin.Log("BURRITO_PET_PLACE origin=" + origin + " native=" + native +
                " nativeDistance=" + nativeDistance.ToString("F2") +
                " tested=" + tested + " chosen=none");
            return false;
        }

        private Item FindNearbyFood(out float nearestDistance)
        {
            nearestDistance = float.MaxValue;
            Item nearest = null;
            if (controller?.itemList == null) return null;
            foreach (var item in controller.itemList)
            {
                if (item == null || item.tr == null || item.invItem == null) continue;
                string name = item.invItem.invItemName;
                if (name != Names.Food && name != "HotFud") continue;
                if (item.UID == ignoredFoodId && Time.time < ignoreFoodUntil) continue;
                float distance = Vector2.Distance(pet.tr.position, item.tr.position);
                if (distance > PetPolicy.FoodNoticeRadius || distance >= nearestDistance) continue;
                nearest = item;
                nearestDistance = distance;
            }
            return nearest;
        }

        private void Chase(Item food, float distance)
        {
            targetFood = food;
            chaseUntil = Time.time + 8f;
            pet.agentInteractions.GoHere(pet, owner, food.tr.position);
            intent = PetIntent.ChaseFood;
            status = "Test dog chasing dropped food " + distance.ToString("F1") + " units away.";
            plugin.Log("BURRITO_PET_STATE pet=" + pet.UID + " state=chase-food food=" +
                food.UID + " distance=" + distance.ToString("F2") + " target=" + food.tr.position);
            plugin.Notice("The test dog spotted dropped food!");
        }

        private void SetFollow(string reason)
        {
            if (pet == null || owner == null) return;
            pet.job = "Follow";
            pet.jobCode = jobType.Follow;
            pet.jobBig = "";
            pet.jobBigCode = jobType.None;
            pet.SetFollowing(owner);
            pet.SetDefaultGoal("WanderFar");
            intent = PetIntent.Follow;
            targetFood = null;
            status = "Test dog following player " + owner.UID + ".";
            plugin.Log("BURRITO_PET_STATE pet=" + pet.UID + " state=follow owner=" +
                owner.UID + " reason=" + reason);
        }

        private void Release(string reason)
        {
            released = true;
            targetFood = null;
            if (pet != null && !pet.dead)
            {
                pet.SetFollowing(null);
                pet.job = "";
                pet.jobCode = jobType.None;
                pet.jobBig = "";
                pet.jobBigCode = jobType.None;
                pet.SetDefaultGoal("WanderFar");
            }
            intent = PetIntent.Wander;
            status = "Test dog wandering after owner loss.";
            plugin.Log("BURRITO_PET_STATE pet=" + (pet != null ? pet.UID.ToString() : "none") +
                " state=wander reason=" + reason);
        }

        private void ClearForLevelChange()
        {
            foreach (var renderer in hiddenRenderers)
                if (renderer != null) renderer.enabled = true;
            hiddenRenderers.Clear();
            if (dogVisual != null) UnityEngine.Object.Destroy(dogVisual);
            dogVisual = null;
            controller = null;
            pet = null;
            owner = null;
            spawnedThisLevel = false;
            snackDropped = false;
            released = false;
            targetFood = null;
            intent = PetIntent.Wander;
            status = "No test pet on this level.";
        }

        private void CreateDogVisual(Agent agent)
        {
            dogSprite ??= MakeDogSprite();
            var nativeRenderer = agent.spr != null ? agent.spr.GetComponent<Renderer>() : null;
            var visual = new GameObject("BurritoRun Test Street Dog");
            visual.transform.SetParent(agent.tr, false);
            visual.transform.localPosition = new Vector3(0, 0.1f, -0.02f);
            var dogRenderer = visual.AddComponent<SpriteRenderer>();
            dogRenderer.sprite = dogSprite;
            if (nativeRenderer != null)
            {
                dogRenderer.sortingLayerID = nativeRenderer.sortingLayerID;
                dogRenderer.sortingOrder = nativeRenderer.sortingOrder + 2;
            }
            else dogRenderer.sortingOrder = 1000;
            dogVisual = visual;
            foreach (var renderer in agent.tr.GetComponentsInChildren<Renderer>(true))
                if (renderer != null && renderer != dogRenderer)
                { hiddenRenderers.Add(renderer); renderer.enabled = false; }
        }

        private static Sprite MakeDogSprite()
        {
            const int width = 24, height = 16;
            var pixels = new Color32[width * height];
            var coat = new Color32(173, 106, 57, 255);
            var light = new Color32(228, 174, 100, 255);
            var dark = new Color32(69, 47, 43, 255);
            for (int y = 5; y <= 10; y++)
                for (int x = 5; x <= 17; x++) pixels[y * width + x] = coat;
            for (int y = 9; y <= 13; y++)
                for (int x = 16; x <= 21; x++) pixels[y * width + x] = light;
            for (int x = 7; x <= 9; x++)
                for (int y = 2; y <= 5; y++) pixels[y * width + x] = dark;
            for (int x = 15; x <= 17; x++)
                for (int y = 2; y <= 5; y++) pixels[y * width + x] = dark;
            pixels[11 * width + 4] = coat;
            pixels[12 * width + 3] = coat;
            pixels[13 * width + 2] = coat;
            pixels[14 * width + 17] = dark;
            pixels[13 * width + 17] = dark;
            pixels[12 * width + 19] = dark;
            pixels[10 * width + 22] = dark;
            pixels[9 * width + 16] = new Color32(221, 70, 61, 255);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.1f), 24f);
        }
    }
}
