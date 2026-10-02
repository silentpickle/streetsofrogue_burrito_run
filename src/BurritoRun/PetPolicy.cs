namespace BurritoRun
{
    // Intent only. Native goals/pathfinding remain responsible for actual movement.
    internal enum PetIntent { Follow, ChaseFood, Wander }

    internal static class PetPolicy
    {
        internal const float FoodNoticeRadius = 6f;
        internal const float FoodArrivalRadius = 0.8f;

        internal static PetIntent Choose(bool ownerAvailable, bool foodAvailable,
            float distanceToFood, bool foodRecentlyIgnored)
        {
            if (!ownerAvailable) return PetIntent.Wander;
            if (foodAvailable && !foodRecentlyIgnored && distanceToFood > FoodArrivalRadius &&
                distanceToFood <= FoodNoticeRadius) return PetIntent.ChaseFood;
            return PetIntent.Follow;
        }
    }
}
