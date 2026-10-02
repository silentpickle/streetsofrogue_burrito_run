using System;
using System.Text;
using UnityEngine;

namespace BurritoRun
{
    // Read-only view of the nearest native actor or object. No selection ray,
    // AI command, damage call or inventory mutation is issued by inspection.
    internal sealed class WorldInspector
    {
        private readonly Plugin plugin;
        private string report;
        internal bool Visible { get; private set; }

        internal WorldInspector(Plugin plugin) { this.plugin = plugin; }
        internal void Hide() => Visible = false;

        internal void Capture(Agent player)
        {
            try { CaptureCore(player); }
            catch (Exception error) { plugin.Error(error); plugin.Notice("World inspection failed; see the event log."); }
        }

        private void CaptureCore(Agent player)
        {
            if (!plugin.Delivery.CanOrder(player)) return;
            var gc = player.gc;
            var origin = player.tr.position;
            Agent nearestAgent = null;
            ObjectReal nearestObject = null;
            float distance = 5f;
            int skippedDoorHelpers = 0;
            foreach (var candidate in gc.agentList)
            {
                if (candidate == null || candidate == player || candidate.tr == null || candidate.dead) continue;
                var next = Vector2.Distance(origin, candidate.tr.position);
                if (next >= distance) continue;
                nearestAgent = candidate;
                nearestObject = null;
                distance = next;
            }
            foreach (var candidate in gc.objectRealList)
            {
                if (candidate == null || candidate.destroyed || candidate.tr == null) continue;
                // DoorHelper is a collision/noise proxy, not the visible door the
                // player intended to inspect. Keep F3 for door-specific geometry.
                if (candidate.GetType().Name == "DoorHelper") { skippedDoorHelpers++; continue; }
                var next = Vector2.Distance(origin, candidate.tr.position);
                if (next >= distance) continue;
                nearestObject = candidate;
                nearestAgent = null;
                distance = next;
            }
            if (nearestAgent == null && nearestObject == null)
            { plugin.Notice("No NPC or visible object within five units. Move closer and press F4."); return; }

            plugin.Log("BURRITO_WORLD_SELECTION skippedDoorHelpers=" + skippedDoorHelpers);

            var target = nearestAgent != null ? nearestAgent.tr.position : nearestObject.tr.position;
            var tile = gc.tileInfo.GetTileData(target);
            var text = new StringBuilder();
            if (nearestAgent != null)
            {
                text.AppendLine("NPC " + nearestAgent.UID + " (" + nearestAgent.agentName + ")");
                text.AppendLine("Health: " + nearestAgent.health.ToString("0") + "/" + nearestAgent.healthMax.ToString("0") +
                    "   Knocked out: " + nearestAgent.knockedOut);
                text.AppendLine("Job: " + nearestAgent.jobCode + "   Default goal: " + nearestAgent.defaultGoalCode);
                text.AppendLine("Owner ID: " + nearestAgent.ownerID + "   Flee: " + nearestAgent.mustFlee);
                text.AppendLine("Inventory: " + (nearestAgent.inventory?.InvItemList.Count ?? 0) + " items");
                if (nearestAgent.inventory != null)
                    for (int index = 0; index < Math.Min(6, nearestAgent.inventory.InvItemList.Count); index++)
                    {
                        var item = nearestAgent.inventory.InvItemList[index];
                        text.AppendLine("  " + item.invItemName + " x" + item.invItemCount);
                    }
                plugin.Log("BURRITO_WORLD_AGENT uid=" + nearestAgent.UID + " name=" + nearestAgent.agentName +
                    " at=" + target + " health=" + nearestAgent.health + " job=" + nearestAgent.jobCode +
                    " owner=" + nearestAgent.ownerID + " items=" + (nearestAgent.inventory?.InvItemList.Count ?? 0));
            }
            else
            {
                text.AppendLine("Object " + nearestObject.UID + " (" + nearestObject.GetType().Name + ")");
                text.AppendLine("Native type: " + nearestObject.objectType + "   Prefab: " + nearestObject.prefabName);
                text.AppendLine("Destroyed: " + nearestObject.destroyed + "   Electronic: " + nearestObject.electronic);
                plugin.Log("BURRITO_WORLD_OBJECT uid=" + nearestObject.UID + " type=" + nearestObject.GetType().Name +
                    " objectType=" + nearestObject.objectType + " at=" + target +
                    " destroyed=" + nearestObject.destroyed + " electronic=" + nearestObject.electronic);
            }
            text.AppendLine("Position: " + target + "   Distance: " + distance.ToString("0.0"));
            text.AppendLine("Indoor: " + gc.tileInfo.IsIndoors(target) + "   Chunk: " + (tile?.chunkID.ToString() ?? "none"));
            if (tile != null)
            {
                text.AppendLine("Tile owner: " + tile.owner + "   Wall: " + tile.wallMaterial +
                    "   Solid: " + tile.solidObject);
                text.AppendLine("Hazard: " + tile.dangerousToWalk + "   Hole: " + tile.hole);
            }
            plugin.Log("BURRITO_WORLD_TILE at=" + target + " indoor=" + gc.tileInfo.IsIndoors(target) +
                " chunk=" + tile?.chunkID + " owner=" + tile?.owner + " wall=" + tile?.wallMaterial +
                " solid=" + tile?.solidObject + " hazard=" + tile?.dangerousToWalk);
            report = text.ToString();
            plugin.Phone.Open(player);
            plugin.Inspector.Hide();
            Visible = true;
        }

        internal void Draw()
        {
            GUILayout.BeginArea(new Rect((Screen.width - 460) / 2, 35, 460, Math.Min(500, Screen.height - 70)),
                "WORLD SNAPSHOT - " + Plugin.BuildLabel, GUI.skin.window);
            GUILayout.Label(report, new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true });
            if (GUILayout.Button("Refresh nearest (F4)", GUILayout.Height(30))) Capture(GameController.gameController?.playerAgent);
            if (GUILayout.Button("Back to Services", GUILayout.Height(30))) Hide();
            if (GUILayout.Button("Close / resume (F2 or Esc)", GUILayout.Height(30))) plugin.Phone.Close();
            GUILayout.EndArea();
        }
    }
}
