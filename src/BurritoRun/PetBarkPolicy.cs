using System.Collections.Generic;

namespace BurritoRun
{
    // Game-time limits shared by automatic and manual barking.
    internal sealed class PetBarkPolicy
    {
        internal const float StrangerRadius = 3.5f;
        internal const float GuardMinDistance = 5f;
        internal const float GuardMaxDistance = 6.5f;
        internal const float Cooldown = 20f;
        internal const int LevelLimit = 6;
        private readonly HashSet<int> strangers = new HashSet<int>();
        private float nextBark;
        internal int Count { get; private set; }
        internal float Remaining(float now) => System.Math.Max(0f, nextBark - now);
        internal string RestReason(float now) => Count >= LevelLimit ? "level-cap" :
            now < nextBark ? (Count == 0 ? "spawn-grace" : "cooldown") : null;

        internal void Reset(float now)
        {
            strangers.Clear();
            Count = 0;
            nextBark = now + 5f;
        }

        internal bool Ready(float now) => Count < LevelLimit && now >= nextBark;
        internal bool HasBarkedAt(int uid) => strangers.Contains(uid);

        internal static bool GuardInRange(float distance) =>
            distance >= GuardMinDistance && distance <= GuardMaxDistance;

        internal static bool UsefulApproach(bool nativeReaction, float displacement, float approach) =>
            nativeReaction && displacement >= 0.64f && approach >= 0.64f;

        internal bool TryBegin(float now, int uid)
        {
            if (!Ready(now) || HasBarkedAt(uid)) return false;
            strangers.Add(uid);
            Count++;
            nextBark = now + Cooldown;
            return true;
        }
    }
}
