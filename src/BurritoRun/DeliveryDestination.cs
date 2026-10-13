using UnityEngine;

namespace BurritoRun
{
    // Address selection only. Door state, movement, payment, and recovery stay separate.
    internal static class DeliveryDestination
    {
        private static readonly Vector3[] Sides = { Vector3.up, Vector3.down, Vector3.left, Vector3.right };
        private static readonly float[] SampleOffsets = { 0.64f, 1.28f, 1.92f };
        private static readonly float[] MeetingOffsets = { 1.92f, 2.56f, 1.28f };

        /// <summary>
        /// Try to select a safe exterior handoff point belonging to an indoor delivery address.
        /// The nearest door can be internal or belong elsewhere; inspect building sides and clear
        /// approach before routing to it.
        /// </summary>
        internal static bool TryFindEntrance(GameController gc, Vector3 address, out Vector3 meetingPoint, out Door entrance)
        {
            meetingPoint = address;
            entrance = null;
            bool indoors = gc.tileInfo.IsIndoors(address);
            var addressTile = gc.tileInfo.GetTileData(address);
            Plugin.Instance.Log($"BURRITO_ADDRESS at={address} indoors={indoors} chunk={addressTile?.chunkID} floor={addressTile?.floorMaterial} objects={gc.objectRealList.Count}");
            if (!indoors || addressTile == null) return false;
            float bestDistance = 12f;
            int inspected = 0;
            foreach (var obj in gc.objectRealList)
            {
                var door = obj as Door;
                if (door == null || door.destroyed || door.tr == null ||
                    !door.gameObject.activeInHierarchy) continue;
                float distance = Vector2.Distance(address, door.tr.position);
                if (distance >= bestDistance) continue;
                bool diagnostic = inspected++ < 8;
                var rejection = new System.Text.StringBuilder();
                foreach (var side in Sides)
                {
                    // Probe past the threshold: a single adjacent tile can be wall
                    // or doorway data rather than the building's actual floor.
                    bool sameBuilding = false;
                    foreach (float offset in SampleOffsets)
                    {
                        var innerPosition = door.tr.position - side * offset;
                        var inner = gc.tileInfo.GetTileData(innerPosition);
                        if (inner != null && inner.chunkID == addressTile.chunkID && gc.tileInfo.IsIndoors(innerPosition))
                            sameBuilding = true;
                    }
                    if (!sameBuilding) { rejection.Append($" side={side}:no-inner-match;"); continue; }
                    foreach (float offset in MeetingOffsets)
                    {
                        var candidate = door.tr.position + side * offset;
                        var tile = gc.tileInfo.GetTileData(candidate);
                        bool outside = tile != null && !gc.tileInfo.IsIndoors(candidate);
                        bool safe = outside && !gc.tileInfo.WallExist(tile) && !tile.solidObject &&
                            !tile.hole && !tile.dangerousToWalk && !tile.water;
                        if (!safe)
                        {
                            rejection.Append($" side={side} offset={offset}:out={outside},chunk={tile?.chunkID},wall={tile?.wallMaterial},solid={tile?.solidObject},hazard={tile?.dangerousToWalk};");
                            continue;
                        }
                        // Validate the corridor back toward the doorstep as well;
                        // do not choose a tile on the other side of a second wall.
                        bool clearApproach = true;
                        for (float step = 0.64f; step < offset; step += 0.64f)
                        {
                            var approach = gc.tileInfo.GetTileData(door.tr.position + side * step);
                            if (approach == null || gc.tileInfo.WallExist(approach) || approach.solidObject || approach.hole || approach.dangerousToWalk || approach.water)
                                clearApproach = false;
                        }
                        if (!clearApproach) { rejection.Append($" side={side}:blocked-approach;"); continue; }
                        meetingPoint = candidate;
                        entrance = door;
                        bestDistance = distance;
                        break;
                    }
                    if (entrance == door) break;
                }
                if (diagnostic)
                    Plugin.Instance.Log($"BURRITO_ENTRANCE_SCAN door={door.UID} at={door.tr.position} outsideFlag={door.outsideDoor} open={door.open} selected={entrance == door} checks={rejection}");
            }
            Plugin.Instance.Log($"BURRITO_ADDRESS_RESULT entrance={(entrance != null ? entrance.UID.ToString() : "none")} inspected={inspected} target={meetingPoint}");
            return entrance != null;
        }
    }
}
