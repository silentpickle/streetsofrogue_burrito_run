using System.Collections.Generic;

namespace BurritoRun
{
    internal sealed class PetHydrantPolicy
    {
        internal const float NoticeRadius = 4f;
        internal const float OwnerRadius = 6f;
        internal const float ArrivalRadius = 1.2f;
        internal const float HydrantRadius = 1.6f;
        internal const float ChaseSeconds = 8f;
        internal const float MarkSeconds = 1.25f;
        internal const float Cooldown = 45f;
        private readonly HashSet<string> visited = new HashSet<string>();
        private float nextInterest;
        internal bool Ready(float now) => now >= nextInterest && visited.Count < 32;
        internal bool CanNotice(string key, float now) => key != null && Ready(now) && !visited.Contains(key);
        internal void Finish(string key, float now)
        { if (key != null && visited.Count < 32) visited.Add(key); nextInterest = now + Cooldown; }
        internal void Reset() { visited.Clear(); nextInterest = 0; }
    }
}
