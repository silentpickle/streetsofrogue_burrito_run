using System;

namespace BurritoRun
{
    // Native controllers and agent UIDs can be recycled across runs. Keep several
    // independent level markers, and also observe the game's transition flags.
    internal readonly struct PetLevelScope
    {
        internal readonly int ControllerId;
        internal readonly int SceneHandle;
        internal readonly int AgentsNestId;
        internal readonly int Seed;
        internal readonly string Floor;
        internal readonly float TimeSinceLoad;

        internal PetLevelScope(int controllerId, int sceneHandle, int agentsNestId,
            int seed, string floor, float timeSinceLoad)
        {
            ControllerId = controllerId;
            SceneHandle = sceneHandle;
            AgentsNestId = agentsNestId;
            Seed = seed;
            Floor = floor;
            TimeSinceLoad = timeSinceLoad;
        }

        internal static string ResetReason(PetLevelScope oldScope, PetLevelScope live,
            bool loadComplete, bool returningToMainMenu, bool levelTransitioning, bool playable)
        {
            if (!loadComplete) return "load-incomplete";
            if (returningToMainMenu) return "returning-to-menu";
            if (levelTransitioning) return "level-transitioning";
            if (!playable) return "nonplayable-level";
            if (oldScope.ControllerId != live.ControllerId) return "controller-changed";
            if (oldScope.SceneHandle != live.SceneHandle) return "scene-changed";
            if (oldScope.AgentsNestId != 0 && live.AgentsNestId != 0 &&
                oldScope.AgentsNestId != live.AgentsNestId) return "agents-nest-changed";
            if (oldScope.Seed != live.Seed) return "seed-changed";
            if (!string.Equals(oldScope.Floor, live.Floor, StringComparison.Ordinal))
                return "floor-changed";
            if (live.TimeSinceLoad + 0.25f < oldScope.TimeSinceLoad)
                return "load-clock-restarted";
            return null;
        }
    }
}
