using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BurritoRun
{
    // Production content setup. No fixture code or changes to native quest/exit state.
    internal sealed class BananaEncounter
    {
        private readonly GameController controller;
        private readonly int scene, nest;
        private readonly string floor;
        private readonly int[] instances = new int[2], ids = new int[2];
        private int sourceId, sourceInstance;
        private float loadClock;
        internal Agent[] Recipients { get; } = new Agent[2];
        internal Item Source { get; private set; }
        internal Agent Player { get; }
        internal bool SameLevel => controller != null && GameController.gameController == controller && controller.loadComplete &&
            controller.loadCompleteReally && !controller.levelTransitioning && !controller.returningToMainMenu &&
            SceneManager.GetActiveScene().handle == scene && controller.agentsNest != null && controller.agentsNest.GetInstanceID() == nest &&
            controller.floorNameNum == floor && controller.timeSinceLoad + .25f >= loadClock;
        internal bool RecipientLive(int index) => index >= 0 && index < 2 && Recipients[index] != null &&
            Recipients[index].GetInstanceID() == instances[index] && Recipients[index].UID == ids[index] &&
            WorldNative.Active(Recipients[index]) && !Recipients[index].dead;
        internal bool SourceLive => Source != null && Source.GetInstanceID() == sourceInstance && Source.UID == sourceId &&
            WorldNative.Active(Source) && Source.invItem?.invItemName == QuestPolicy.RequiredItem && Source.invItem.invItemCount == 1;

        internal BananaEncounter(Agent player)
        {
            Player = player; controller = player.gc;
            scene = SceneManager.GetActiveScene().handle; nest = controller.agentsNest.GetInstanceID();
            floor = controller.floorNameNum; loadClock = controller.timeSinceLoad;
        }
        internal void ObserveClock() { if (SameLevel) loadClock = controller.timeSinceLoad; }
        /// <summary>
        /// Find three separated nearby positions, then spawn two canonical Hobo recipients and one
        /// banana.
        /// Validating positions before spawning avoids partial clutter; failed setup removes only
        /// this encounter's owned entities.
        /// </summary>
        internal bool Place()
        {
            // All positions are validated before the first spawn. Sample the straight approach as well as endpoints.
            var spots = new List<Vector3>();
            for (int ring = 2; ring <= 5 && spots.Count < 3; ring++)
                for (int direction = 0; direction < 16 && spots.Count < 3; direction++)
                {
                    float angle = direction * Mathf.PI / 8;
                    var spot = Player.tr.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * ring;
                    if (!ClearApproach(Player, spot)) continue;
                    bool crowded = false;
                    foreach (var prior in spots) if (Vector2.Distance(prior, spot) < 1.5f) crowded = true;
                    foreach (var actor in controller.agentList)
                        if (actor != null && WorldNative.Active(actor) && Vector2.Distance(actor.tr.position, spot) < 1.5f) crowded = true;
                    if (!crowded) spots.Add(spot);
                }
            if (spots.Count != 3) return false;
            try
            {
                for (int i = 0; i < 2; i++)
                {
                    // Slum Dweller is the display name; native stats/dialogue use Hobo.
                    var actor = controller.spawnerMain.SpawnAgent(spots[i], (PlayfieldObject)null, "Hobo");
                    if (actor == null) throw new InvalidOperationException("Recipient spawn refused");
                    Recipients[i] = actor; ids[i] = actor.UID; instances[i] = actor.GetInstanceID();
                    actor.SetDefaultGoal("DoNothing");
                }
                Source = controller.spawnerMain.SpawnItem(spots[2], QuestPolicy.RequiredItem);
                if (Source == null) throw new InvalidOperationException("Banana spawn refused");
                sourceId = Source.UID; sourceInstance = Source.GetInstanceID();
                if (!SourceLive || Source.startingOwner != 0 || Source.invItem.questItem || Source.invItem.stealable ||
                    !ClearApproach(Player, Source.tr.position, .64f)) throw new InvalidOperationException("Spawned banana unavailable: live=" + SourceLive +
                        " owner=" + Source.startingOwner + " quest=" + Source.invItem.questItem + " stealable=" + Source.invItem.stealable +
                        " approach=" + ClearApproach(Player, Source.tr.position, .64f) + " count=" + Source.invItem.invItemCount);
                return true;
            }
            catch { RemoveOwned(true); throw; }
        }
        /// <summary>
        /// Check LOS and sampled walking safety along the straight approach to a candidate
        /// position.
        /// After spawning an item, stopping short avoids rejecting its own ItemImage collider; this
        /// is not a general path solver.
        /// </summary>
        internal static bool ClearApproach(Agent player, Vector3 spot, float standOff = 0)
        {
            if (!player.movement.HasLOSPosition360(spot) || !player.gc.tileInfo.PossibleToWalk(player, spot)) return false;
            float distance = Vector2.Distance(player.tr.position, spot), approach = Mathf.Max(0, distance - standOff);
            int steps = Mathf.CeilToInt(approach / .32f);
            for (int i = 1; i <= steps; i++)
            {
                // SafeToWalk rejects any ItemImage, including our banana. Stop at a native pickup distance after spawning it.
                var cell = Vector3.Lerp(player.tr.position, spot, (float)i / steps * approach / distance);
                if (!player.gc.tileInfo.PossibleToWalk(player, cell) || !player.gc.tileInfo.SafeToWalk(player, cell)) return false;
            }
            return true;
        }
        /// <summary>
        /// Let surviving recipients wander again if they still have the encounter's DoNothing
        /// default.
        /// Only the default still owned by this setup may be replaced; a newer behavior should
        /// survive.
        /// </summary>
        internal void Release()
        {
            for (int i = 0; i < 2; i++)
                if (RecipientLive(i) && Recipients[i].defaultGoal == "DoNothing") Recipients[i].SetDefaultGoal("WanderFar");
        }
        // Used only for failed setup or disposable harness-owned encounters; never removes a held banana.
        /// <summary>
        /// Remove owned recipients and, optionally, the still-grounded source during failed setup
        /// or fixture cleanup.
        /// Identity checks avoid touching replacements, and a banana already taken into inventory
        /// is preserved.
        /// </summary>
        internal void RemoveOwned(bool removeSource)
        {
            for (int i = 0; i < 2; i++) if (RecipientLive(i)) Recipients[i].DestroyMe();
            if (removeSource && SourceLive) Source.DestroyMe();
        }
    }
}
