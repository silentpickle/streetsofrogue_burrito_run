using System;
using System.Text;
using UnityEngine;

namespace BurritoRun
{
    // Read-only snapshot. Does not issue movement, touch doors, create noise, or order food.
    internal sealed class DoorwayInspector
    {
        private readonly Plugin plugin;
        private string report;
        private Font gridFont;
        internal bool Visible { get; private set; }

        internal DoorwayInspector(Plugin plugin) { this.plugin = plugin; }
        internal void Hide() => Visible = false;

        internal void Capture(Agent player)
        {
            try { CaptureCore(player); }
            catch (Exception error) { plugin.Error(error); plugin.Notice("Door inspection failed; see log. No delivery was changed."); }
        }

        private void CaptureCore(Agent player)
        {
            if (!plugin.Delivery.CanOrder(player)) return;
            Door door = null;
            float nearest = 5f;
            foreach (var obj in player.gc.objectRealList)
            {
                var candidate = obj as Door;
                if (candidate == null || candidate.destroyed || candidate.tr == null || !candidate.gameObject.activeInHierarchy) continue;
                float distance = Vector2.Distance(player.tr.position, candidate.tr.position);
                if (distance >= nearest) continue;
                door = candidate;
                nearest = distance;
            }
            if (door == null) { plugin.Notice("No intact door within 5 units. Stand nearer a doorway and press F3."); return; }
            var courier = plugin.Delivery.Courier;
            var actor = courier != null && courier.didStartReal && !courier.dead ? courier : player;
            var position = door.tr.position;
            int centerX = ToTile(position.x);
            int centerY = ToTile(position.y);
            var header = $"Door {door.UID}: direction={door.direction}, type={door.doorType}\n" +
                $"open={door.open}, locked={door.locked}, outsideFlag={door.outsideDoor}\n" +
                $"World ({position.x:0.00}, {position.y:0.00}); tile ({centerX}, {centerY})\n" +
                $"Walkability actor: {(actor == player ? "player" : "courier")} {actor.UID}";
            plugin.Log($"BURRITO_DOOR_GRID_BEGIN door={door.UID} direction={door.direction} type={door.doorType} open={door.open} locked={door.locked} outside={door.outsideDoor} position={position} player={player.tr.position} actor={actor.UID} target={plugin.Delivery.Destination}");
            LogColliders("door", door);
            if (door.doorHelper != null) LogColliders("helper1", door.doorHelper);
            if (door.doorHelper2 != null) LogColliders("helper2", door.doorHelper2);
            var grid = new StringBuilder();
            grid.AppendLine("       x offsets: -4 .. +4; north is UP");
            for (int dy = 4; dy >= -4; dy--)
            {
                grid.Append(dy.ToString("+0;-0; 0")).Append("   ");
                for (int dx = -4; dx <= 4; dx++)
                {
                    var sample = new Vector3((centerX + dx) * 0.64f, (centerY + dy) * 0.64f, player.tr.position.z);
                    var tile = player.gc.tileInfo.GetTileData(sample);
                    if (tile == null) { grid.Append("? "); continue; }
                    bool inside = player.gc.tileInfo.IsIndoors(sample);
                    bool possible = player.gc.tileInfo.PossibleToWalk(actor, sample);
                    bool safe = player.gc.tileInfo.SafeToWalk(actor, sample);
                    char symbol = player.gc.tileInfo.WallExist(tile) && !tile.destroyedWall ? '#' :
                        !possible ? 'X' : !safe ? '!' : inside ? 'i' : 'o';
                    if (IsSameCell(sample, player.tr.position)) symbol = 'P';
                    if (plugin.Delivery.Destination.HasValue && IsSameCell(sample, plugin.Delivery.Destination.Value)) symbol = 'T';
                    if (dx == 0 && dy == 0) symbol = 'D';
                    grid.Append(symbol).Append(' ');
                    plugin.Log($"BURRITO_DOOR_TILE door={door.UID} dx={dx} dy={dy} at={sample} inside={inside} chunk={tile.chunkID} floor={tile.floorMaterial} wall={tile.wallMaterial} destroyedWall={tile.destroyedWall} solid={tile.solidObject} hazard={tile.dangerousToWalk} hole={tile.hole} owner={tile.owner} possible={possible} safe={safe}");
                }
                grid.AppendLine();
            }
            report = header + "\n\n" + grid +
                "\ni=indoor  o=outdoor  #=wall  X=native blocked\n!=possible but unsafe  D=door  P=player  T=target\nMarkers override cell symbols; full data is in the log.\nLocal walkability does NOT prove a complete path.\nSnapshot only: opening a door afterward doesn't refresh it.";
            plugin.Log("BURRITO_DOOR_GRID_END door=" + door.UID);
            plugin.Phone.Open(player);
            plugin.World.Hide();
            Visible = true;
        }

        private static bool IsSameCell(Vector3 first, Vector3 second) =>
            ToTile(first.x) == ToTile(second.x) && ToTile(first.y) == ToTile(second.y);

        private static int ToTile(float coordinate) => Mathf.FloorToInt((coordinate + 0.32f) / 0.64f);

        private void LogColliders(string role, ObjectReal obj)
        {
            plugin.Log($"BURRITO_DOOR_GEOMETRY role={role} uid={obj.UID} position={obj.tr.position} scale={obj.tr.lossyScale} rotation={obj.tr.eulerAngles}");
            var colliders = obj.GetComponentsInChildren<Collider2D>(true);
            for (int index = 0; index < Math.Min(16, colliders.Length); index++)
            {
                var collider = colliders[index];
                plugin.Log($"BURRITO_DOOR_COLLIDER role={role} type={collider.GetType().Name} active={collider.gameObject.activeInHierarchy} enabled={collider.enabled} trigger={collider.isTrigger} center={collider.bounds.center} size={collider.bounds.size} offset={collider.offset}");
            }
        }

        internal void Draw()
        {
            float width = Mathf.Min(640, Screen.width - 30);
            if (gridFont == null) gridFont = Font.CreateDynamicFontFromOSFont("Menlo", 16);
            GUILayout.BeginArea(new Rect((Screen.width - width) / 2, 25, width, Mathf.Min(650, Screen.height - 50)), "DOORWAY SNAPSHOT - " + Plugin.BuildLabel, GUI.skin.window);
            GUILayout.Label(report, new GUIStyle(GUI.skin.label) { font = gridFont, fontSize = 16, wordWrap = true });
            if (GUILayout.Button("Refresh snapshot (F3)", GUILayout.Height(30))) Capture(GameController.gameController.playerAgent);
            if (GUILayout.Button("Back to Services", GUILayout.Height(30))) Hide();
            if (GUILayout.Button("Close / resume game (F2 or Esc)", GUILayout.Height(30))) plugin.Phone.Close();
            GUILayout.EndArea();
        }
    }
}
