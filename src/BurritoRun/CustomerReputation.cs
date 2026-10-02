namespace BurritoRun
{
    // Pure run-scoped policy; no UI, actors, money, or save-file dependencies.
    internal sealed class CustomerReputation
    {
        internal int Reports { get; private set; }
        internal int PeacefulHandoffs { get; private set; }
        internal double ArmedChance => Reports == 0 ? 0 : Reports == 1 ? 0.5 : Reports == 2 ? 0.75 : 1;

        internal void RecordAssault()
        {
            if (Reports < 3) Reports++;
            PeacefulHandoffs = 0;
        }

        internal void RecordPeacefulHandoff()
        {
            if (Reports == 0) return;
            if (++PeacefulHandoffs < 2) return;
            Reports--;
            PeacefulHandoffs = 0;
        }

        internal bool ShouldArm(bool quickTest, double roll) => Reports > 0 && (quickTest || roll < ArmedChance);
        internal void Reset() { Reports = 0; PeacefulHandoffs = 0; }
    }
}
