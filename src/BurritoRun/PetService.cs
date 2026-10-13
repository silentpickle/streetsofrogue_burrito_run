using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        private PetLevelScope levelScope;
        private Agent pet;
        private Agent owner;
        private GameObject dogVisual;
        private Sprite dogSprite;
        private Sprite markingSprite;
        private Sprite dropsSprite;
        private GameObject markingVisual;
        private PetIntent intent = PetIntent.Wander;
        private Item targetFood;
        private string targetFoodKey;
        private float foodCooldownUntil;
        private int petUid;
        private int petInstance;
        private int ownerUid;
        internal PetIntent Intent => intent;
        internal string FoodTargetKey => targetFoodKey;
        internal Dictionary<string, object> LastFoodAction { get; private set; }

        private string ignoredFoodKey;
        private int targetFoodUid;
        private float ignoreFoodUntil;
        private float chaseUntil;
        private float nextCheck;
        private bool spawnedThisLevel;
        private bool snackDropped;
        private bool hydrantPlaced;
        private bool released;
        private bool deathLogged;
        private string status = "No test pet on this level.";

        internal PetService(Plugin plugin)
        { this.plugin = plugin; Barks = new PetBarkService(plugin); Hydrants = new PetHydrantService(plugin, this); }
        internal PetBarkService Barks { get; }
        internal PetHydrantService Hydrants { get; }
        internal bool HydrantVisualActive => markingVisual != null && markingVisual.activeSelf;
        internal Agent Actor => pet;
        internal Agent Owner => owner;
        internal string Status => status;

        /// <summary>
        /// Spawn one native Courier proxy for this level, attach the dog visual and begin native
        /// following.
        /// Reusing a native actor supplies movement and relationships; the drawn dog is not a new
        /// native species.
        /// </summary>
        internal void SpawnTestPet(Agent player)
        {
            if (!plugin.TestMode || !plugin.Delivery.CanOrder(player))
            { plugin.Notice("Start a quick-test single-player level first."); return; }
            ReconcileLevel(player.gc);
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
                levelScope = CaptureScope(gc);
                pet = spawned;
                petUid = pet.UID; petInstance = pet.GetInstanceID();
                owner = player; ownerUid = player.UID;
                spawnedThisLevel = true;
                released = false;
                deathLogged = false;
                Barks.Reset();
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

        /// <summary>
        /// Release a following test dog back to native wandering.
        /// The explicit release path also ends owned interests rather than leaving an abandoned
        /// detour running.
        /// </summary>
        internal void ReleaseTestPet()
        {
            if (!plugin.TestMode || pet == null || pet.dead || released)
            { plugin.Notice("Spawn a following test pet first."); return; }
            Release("test-release");
            plugin.Notice("Test dog lost its owner and is wandering on native AI.");
        }

        internal void BarkTestPet()
        {
            ReconcileLevel(GameController.gameController);
            Barks.TryManual(pet, owner, !released && intent == PetIntent.Follow);
        }

        internal void DistractGuard()
        {
            ReconcileLevel(GameController.gameController);
            Barks.TryGuard(pet, owner, !released && intent == PetIntent.Follow);
        }

        /// <summary>
        /// Provide and drop one labeled developer snack for this level's following dog.
        /// The one-snack limit makes the interaction repeatable without becoming an unbounded
        /// free-item source.
        /// </summary>
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
                plugin.Notice("Free test burrito dropped. Healthy dogs sniff; injured dogs eat one burrito.");
            }
            catch (Exception error) { plugin.Error(error); }
        }

        internal void PlaceTestHydrant(Agent player)
        {
            if (!plugin.TestMode || player != owner || pet == null || pet.dead || released ||
                intent != PetIntent.Follow || !plugin.Delivery.CanOrder(player))
            { plugin.Notice("Spawn a following dog in an open area first."); return; }
            if (hydrantPlaced) { plugin.Notice("One test hydrant per level; ordinary hydrants also work."); return; }
            try
            {
                var hydrant = PetHydrantFixture.Place(player, pet);
                if (hydrant == null) { plugin.Notice("No native test hydrant could spawn."); return; }
                hydrantPlaced = true;
                plugin.Log("BURRITO_PET_HYDRANT_FIXTURE uid=" + hydrant.UID + " at=" + hydrant.tr.position);
                plugin.Notice("Test hydrant placed. The dog will notice it through World Awareness.");
            }
            catch (Exception error) { plugin.Error(error); plugin.Notice("No safe hydrant spot nearby; move to an open area."); }
        }

        internal void Tick()
        {
            if (spawnedThisLevel && ReconcileLevel(GameController.gameController)) return;
            if (pet == null) return;
            if (!WorldNative.Active(pet) || pet.UID != petUid || pet.GetInstanceID() != petInstance)
            { RetireLostPet(); return; }
            if (pet.dead)
            {
                if (!deathLogged)
                {
                    deathLogged = true;
                    Hydrants.Finish("pet-dead", false);
                    Barks.Reset("pet-dead");
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
            if (Time.timeScale <= 0 || plugin.BlocksWorldInput) return;
            if (!released && (owner == null || owner.dead || owner.gc != controller || owner.UID != ownerUid ||
                !WorldNative.Active(owner) || controller.playerAgent != owner))
                Release("owner-lost");
            if (released) return;

            if (Hydrants.Active) { Hydrants.Tick(pet, owner); return; }

            if (pet.inCombat || owner.inCombat)
            {
                if (intent == PetIntent.ChaseFood) EndFood("combat", float.MaxValue);
                return;
            }
            if (intent == PetIntent.ChaseFood)
            {
                if (plugin.Awareness.ResolveLive(targetFoodKey) != targetFood || !FoodEligible(targetFood))
                { EndFood("food-gone-or-ineligible", float.MaxValue); return; }
                float distance = Vector2.Distance(pet.tr.position, targetFood.tr.position);
                if (distance <= PetPolicy.FoodArrivalRadius)
                {
                    if (!PetPolicy.CanEat(pet.health, pet.healthMax)) EndFood("sniff-full-health", distance);
                    else EatFood(distance);
                    return;
                }
                if (Time.time >= chaseUntil) EndFood("timed-out", distance);
                return;
            }
            if (Time.time < nextCheck) return;
            nextCheck = Time.time + 0.4f;
            Barks.Tick(pet, owner, intent == PetIntent.Follow);
            if (Time.time < foodCooldownUntil) return;
            var food = FindNearbyFood(out float foodDistance, out string key);
            if (PetPolicy.Choose(true, food != null, foodDistance, false) == PetIntent.ChaseFood)
                Chase(food, foodDistance, key);
            else Hydrants.TryNotice(pet, owner);
        }

        private static PetLevelScope CaptureScope(GameController gc) => new PetLevelScope(
            gc.GetInstanceID(), SceneManager.GetActiveScene().handle,
            gc.agentsNest != null ? gc.agentsNest.GetInstanceID() : 0,
            gc.seedNum, gc.floorNameNum, gc.timeSinceLoad);

        private bool ReconcileLevel(GameController gc)
        {
            if (!spawnedThisLevel) return false;
            var live = gc != null ? CaptureScope(gc) : default;
            var reason = gc == null ? "controller-gone" : PetLevelScope.ResetReason(
                levelScope, live, gc.loadComplete, gc.returningToMainMenu,
                gc.levelTransitioning,
                gc.levelType != "HomeBase" && gc.levelType != "Attract" &&
                gc.levelType != "Tutorial");
            if (reason != null)
            {
                plugin.Log("BURRITO_PET_LEVEL_RESET reason=" + reason +
                    " oldController=" + levelScope.ControllerId + " newController=" + live.ControllerId +
                    " oldScene=" + levelScope.SceneHandle + " newScene=" + live.SceneHandle +
                    " oldNest=" + levelScope.AgentsNestId + " newNest=" + live.AgentsNestId +
                    " oldSeed=" + levelScope.Seed + " newSeed=" + live.Seed +
                    " oldFloor=" + levelScope.Floor + " newFloor=" + live.Floor +
                    " oldLoadClock=" + levelScope.TimeSinceLoad.ToString("F2") +
                    " newLoadClock=" + live.TimeSinceLoad.ToString("F2"));
                ClearForLevelChange();
                return true;
            }
            levelScope = live;
            return false;
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

        private static bool FoodEligible(Item item) => item != null && WorldNative.Active(item) &&
            item.tr != null && item.invItem != null && !item.destroying && !item.pickingUp &&
            PetPolicy.EligibleFood(item.invItem.invItemName, item.invItem.invItemCount,
                item.startingOwner != 0 || item.invItem.stealable || item.invItem.questItem,
                item.cantPickUp || item.airborne || item.beingThrown);

        private Item FindNearbyFood(out float nearestDistance, out string key)
        {
            nearestDistance = float.MaxValue; key = null;
            var query = plugin.Awareness.Index.Query(new WorldQuery
            {
                X = pet.tr.position.x,
                Y = pet.tr.position.y,
                Kind = WorldKind.Item,
                Tag = "food",
                MaxDistance = PetPolicy.FoodNoticeRadius,
                Limit = 16
            });
            foreach (var snapshot in query.Entities)
            {
                if (snapshot.Collection.StartsWith("lifecycle:", StringComparison.Ordinal) || Time.time - snapshot.GameTime > 2 || !(plugin.Awareness.ResolveLive(snapshot.Key) is Item item) ||
                    !FoodEligible(item) || pet.inventory.FindItem(item.invItem.invItemName) != null) continue;
                if (snapshot.Key == ignoredFoodKey && Time.time < ignoreFoodUntil) continue;
                float distance = Vector2.Distance(pet.tr.position, item.tr.position);
                if (distance > PetPolicy.FoodNoticeRadius || !pet.movement.HasLOSPosition360(item.tr.position)) continue;
                nearestDistance = distance; key = snapshot.Key; return item;
            }
            return null;
        }

        private void Chase(Item food, float distance, string key)
        {
            targetFood = food; targetFoodKey = key; targetFoodUid = food.UID;
            chaseUntil = Time.time + 8f;
            pet.agentInteractions.GoHere(pet, owner, food.tr.position);
            intent = PetIntent.ChaseFood;
            status = "Test dog approaching dropped food " + distance.ToString("F1") + " units away.";
            plugin.Log("BURRITO_PET_STATE pet=" + pet.UID + " state=chase-food food=" +
                food.UID + " key=" + key + " distance=" + distance.ToString("F2") + " target=" + food.tr.position);
            if (plugin.Harness.Enabled) plugin.Awareness.HarnessRecord("pet-food-noticed", TraceWorld.Values(
                "pet", pet.UID, "key", key, "snapshot", WorldNative.Detach(plugin.Awareness.Index.Get(key)),
                "distance", distance, "petPosition", TraceWorld.Position(pet.tr.position)));
            plugin.Notice("The test dog spotted dropped food!");
        }

        private void EatFood(float distance)
        {
            var food = targetFood;
            string name = food.invItem.invItemName;
            float beforeHealth = pet.health;
            if (!pet.movement.HasLOSPosition360(food.tr.position) || pet.inventory.FindItem(name) != null)
            { EndFood("pickup-blocked", distance); return; }
            try
            {
                food.Interact(pet);
                var meal = pet.inventory.FindItem(name);
                int beforeCount = meal?.invItemCount ?? 0;
                bool pickedUp = food.pickingUp && beforeCount == 1;
                if (pickedUp) meal.itemFunctions.UseItem(meal, pet);
                int afterCount = meal?.invItemCount ?? 0;
                string outcome = pickedUp && afterCount == 0 && pet.health > beforeHealth ? "ate" : "native-refused";
                // If native dietary/state checks refuse, put the original single meal back using native drop.
                if (pickedUp && afterCount == 1) pet.inventory.DropItem(meal);
                EndFood(outcome, distance, beforeCount, afterCount, beforeHealth);
            }
            catch (Exception error)
            {
                plugin.Error(error);
                var remaining = pet.inventory.FindItem(name);
                if (remaining != null && remaining.invItemCount == 1) pet.inventory.DropItem(remaining);
                EndFood("native-error", distance, beforeHealth: beforeHealth);
            }
        }

        private void EndFood(string outcome, float distance, int beforeCount = 0, int afterCount = 0, float? beforeHealth = null)
        {
            ignoredFoodKey = targetFoodKey;
            ignoreFoodUntil = foodCooldownUntil = Time.time + 8f;
            LastFoodAction = TraceWorld.Values("pet", pet.UID, "key", targetFoodKey, "foodUid", targetFoodUid,
                "outcome", outcome, "distance", distance, "inventoryBefore", beforeCount, "inventoryAfter", afterCount,
                "healthBefore", beforeHealth ?? pet.health, "healthAfter", pet.health,
                "petPosition", TraceWorld.Position(pet.tr.position), "gameTime", Time.time);
            plugin.Log("BURRITO_PET_FOOD " + Newtonsoft.Json.JsonConvert.SerializeObject(LastFoodAction));
            if (plugin.Harness.Enabled) plugin.Awareness.HarnessRecord("pet-food-action", LastFoodAction);
            SetFollow("food-" + outcome);
            if (outcome == "ate") plugin.Notice("The test dog ate the burrito and is following again.");
        }

        internal void SetFollow(string reason)
        {
            if (pet == null || owner == null) return;
            pet.job = "Follow";
            pet.jobCode = jobType.Follow;
            pet.jobBig = "";
            pet.jobBigCode = jobType.None;
            pet.SetFollowing(owner);
            pet.SetDefaultGoal("WanderFar");
            intent = PetIntent.Follow;
            targetFood = null; targetFoodKey = null;
            status = "Test dog following player " + owner.UID + ".";
            plugin.Log("BURRITO_PET_STATE pet=" + pet.UID + " state=follow owner=" +
                owner.UID + " reason=" + reason);
        }

        private void Release(string reason)
        {
            Hydrants.Finish(reason, false);
            Barks.Reset(reason);
            released = true;
            targetFood = null; targetFoodKey = null;
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

        private void RetireLostPet()
        {
            // Keep the one-per-level latch until a real scope change, even after native removal/reuse.
            Hydrants.Finish("pet-activation-lost", false);
            Barks.Reset("pet-activation-lost"); ClearVisual();
            plugin.Log("BURRITO_PET_LOST pet=" + petUid + " reason=native-activation-lost");
            pet = null; owner = null; targetFood = null; targetFoodKey = null;
            released = true; intent = PetIntent.Wander;
            status = "Test pet is no longer active. Start a new level for another.";
        }

        private void ClearVisual()
        {
            if (markingVisual != null) UnityEngine.Object.Destroy(markingVisual);
            markingVisual = null;
            foreach (var renderer in hiddenRenderers)
                if (renderer != null) renderer.enabled = true;
            hiddenRenderers.Clear();
            if (dogVisual != null) UnityEngine.Object.Destroy(dogVisual);
            dogVisual = null;
        }

        private void ClearForLevelChange()
        {
            Hydrants.Reset();
            Barks.Reset("level-change"); ClearVisual();
            controller = null;
            levelScope = default;
            pet = null;
            owner = null;
            spawnedThisLevel = false;
            snackDropped = false;
            hydrantPlaced = false;
            foodCooldownUntil = ignoreFoodUntil = nextCheck = 0; LastFoodAction = null;
            released = false;
            targetFood = null; targetFoodKey = null;
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

        internal void BeginHydrant(bool marking)
        {
            intent = marking ? PetIntent.MarkHydrant : PetIntent.ChaseHydrant;
            status = marking ? "Test dog marking a hydrant." : "Test dog approaching a hydrant.";
        }

        internal void HydrantPose(bool marking, Vector3 target)
        {
            var renderer = dogVisual != null ? dogVisual.GetComponent<SpriteRenderer>() : null;
            if (renderer == null) return;
            if (marking)
            {
                markingSprite ??= MakeDogSprite(true);
                renderer.sprite = markingSprite;
                renderer.flipX = target.x < pet.tr.position.x;
                if (markingVisual == null)
                {
                    dropsSprite ??= MakeDropsSprite();
                    markingVisual = new GameObject("BurritoRun cosmetic dog marking");
                    markingVisual.transform.SetParent(pet.tr, false);
                    var drops = markingVisual.AddComponent<SpriteRenderer>();
                    drops.sprite = dropsSprite; drops.sortingLayerID = renderer.sortingLayerID;
                    drops.sortingOrder = renderer.sortingOrder + 1;
                }
                var direction = (target - pet.tr.position).normalized;
                markingVisual.transform.localPosition = direction * .42f + new Vector3(0, .04f, -.03f);
            }
            else renderer.sprite = dogSprite;
            if (markingVisual != null) markingVisual.SetActive(marking);
        }

        private static Sprite MakeDropsSprite()
        {
            var pixels = new Color32[64];
            var yellow = new Color32(240, 212, 86, 230);
            foreach (int index in new[] { 50, 42, 35, 27, 20, 12 }) pixels[index] = yellow;
            var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false)
            { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels); texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 8, 8), new Vector2(.5f, .5f), 24f);
        }

        private static Sprite MakeDogSprite(bool marking = false)
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
                for (int y = marking ? 5 : 2; y <= (marking ? 7 : 5); y++) pixels[y * width + x] = dark;
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
