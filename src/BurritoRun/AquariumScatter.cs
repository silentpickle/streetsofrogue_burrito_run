using System;

namespace BurritoRun
{
    // Visual-only escape route. Every step is cardinal and separately checked;
    // this is not native NPC pathfinding or a promise that shrimp can be picked up.
    internal static class AquariumScatter
    {
        internal const float TileSize = 0.64f;

        internal static ScatterRoute Choose(float centerX, float centerY, int preferredDirection,
            Func<float, float, bool> canEnter)
        {
            if (canEnter == null) throw new ArgumentNullException(nameof(canEnter));
            int first = ((preferredDirection % 4) + 4) % 4;
            for (int attempt = 0; attempt < 4; attempt++)
            {
                int direction = (first + attempt) % 4;
                int dx = direction == 0 ? 1 : direction == 1 ? -1 : 0;
                int dy = direction == 2 ? 1 : direction == 3 ? -1 : 0;
                float oneX = centerX + dx * TileSize;
                float oneY = centerY + dy * TileSize;
                if (!canEnter(oneX, oneY)) continue;
                float twoX = oneX + dx * TileSize;
                float twoY = oneY + dy * TileSize;
                if (canEnter(twoX, twoY))
                    return new ScatterRoute(true, twoX, twoY, direction, 2);
                return new ScatterRoute(true, oneX, oneY, direction, 1);
            }
            return default;
        }
    }

    internal readonly struct ScatterRoute
    {
        internal readonly bool Found;
        internal readonly float X;
        internal readonly float Y;
        internal readonly int Direction;
        internal readonly int Steps;

        internal ScatterRoute(bool found, float x, float y, int direction, int steps)
        {
            Found = found;
            X = x;
            Y = y;
            Direction = direction;
            Steps = steps;
        }
    }
}
