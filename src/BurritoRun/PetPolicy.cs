namespace BurritoRun
{
    // Intent only. Native goals/pathfinding remain responsible for actual movement.
    internal enum PetIntent { Follow, ChaseFood, ChaseHydrant, MarkHydrant, Wander }

    internal static class PetPolicy
    {
        internal const float FoodNoticeRadius = 6f;
        internal const float FoodArrivalRadius = 0.8f;

        internal static bool EligibleFood(string name, int count, bool protectedItem, bool blocked) =>
            (name == "BurritoRun_Food" || name == "HotFud") && count == 1 && !protectedItem && !blocked;

        internal static bool CanEat(float health, float maximum) => health > 0 && health < maximum;

        internal static PetIntent Choose(bool ownerAvailable, bool foodAvailable,
            float distanceToFood, bool foodRecentlyIgnored)
        {
            if (!ownerAvailable) return PetIntent.Wander;
            if (foodAvailable && !foodRecentlyIgnored && distanceToFood >= 0 &&
                distanceToFood <= FoodNoticeRadius) return PetIntent.ChaseFood;
            return PetIntent.Follow;
        }
    }
}
