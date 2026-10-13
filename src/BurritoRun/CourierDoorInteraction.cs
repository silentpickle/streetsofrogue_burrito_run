using UnityEngine;

namespace BurritoRun
{
    // Optional delivery flavor, independent of route recovery and inventory.
    // Never open/unlock a door or synthesize NPC reactions.
    internal sealed class CourierDoorInteraction
    {
        private bool knocked;
        /// <summary>
        /// Remember the first native knock for this delivery.
        /// Sharing this state with the optional knock prevents our flavor behavior from duplicating
        /// an observed knock.
        /// </summary>
        internal bool ObserveNativeKnock()
        {
            if (knocked) return false;
            knocked = true;
            return true;
        }

        /// <summary>
        /// Try one native knock at a nearby closed door belonging to the delivery destination.
        /// The native call uses interactingAgent for attribution; restore its prior value and leave
        /// opening or responses to the game.
        /// </summary>
        internal void TryKnockAtDestination(Agent courier, Vector3 destination)
        {
            if (knocked || courier == null || courier.gc.objectRealList == null) return;
            Door nearest = null;
            float bestDistance = 1.25f;
            foreach (var obj in courier.gc.objectRealList)
            {
                var door = obj as Door;
                if (door == null || door.destroyed || door.open || door.tr == null ||
                    door.doorHelper == null || !door.gameObject.activeInHierarchy) continue;
                // Only doors at the addressed spot, not arbitrary doors along the route.
                if (Vector2.Distance(door.tr.position, destination) > 1.5f) continue;
                float distance = Vector2.Distance(door.tr.position, courier.tr.position);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                nearest = door;
            }
            if (nearest == null) return;
            // Native KnockOnDoor uses interactingAgent for noise attribution even
            // though it accepts an Agent argument. Restore any pre-existing owner.
            var previous = nearest.interactingAgent;
            try
            {
                nearest.interactingAgent = courier;
                nearest.KnockOnDoor(courier);
            }
            catch (System.Exception error)
            {
                knocked = true; // Flavor failure must not abort or repeatedly retry a delivery.
                Plugin.Instance.Error(error);
            }
            finally { nearest.interactingAgent = previous; }
        }
    }
}
