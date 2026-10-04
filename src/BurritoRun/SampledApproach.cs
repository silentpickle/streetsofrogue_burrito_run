using System;

namespace BurritoRun
{
    // A position reached before the first matching sample must not count as response movement.
    internal sealed class SampledApproach
    {
        internal const float MaxGap = 0.6f;
        private bool tracking;
        private float lastTime;
        private float startX;
        private float startY;
        internal int Segment { get; private set; }
        internal int MatchingSamples { get; private set; }
        internal float Displacement { get; private set; }
        internal float Approach { get; private set; }
        internal void Sample(bool matching, float time, float x, float y, float sourceX, float sourceY)
        {
            if (!matching) { tracking = false; return; }
            MatchingSamples++;
            if (!tracking || time - lastTime > MaxGap || time < lastTime)
            {
                tracking = true;
                startX = x;
                startY = y;
                Segment++;
            }
            lastTime = time;
            Displacement = Math.Max(Displacement, Distance(startX, startY, x, y));
            Approach = Math.Max(Approach, Distance(startX, startY, sourceX, sourceY) - Distance(x, y, sourceX, sourceY));
        }
        private static float Distance(float x1, float y1, float x2, float y2) =>
            (float)Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
    }
}
