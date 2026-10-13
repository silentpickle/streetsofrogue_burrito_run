namespace BurritoRun
{
    // Pure run-scoped policy; no UI, actors, money, or save-file dependencies.
    internal sealed class CustomerReputation
    {
        internal int Reports { get; private set; }
        internal int PeacefulHandoffs { get; private set; }
        internal double ArmedChance => Reports == 0 ? 0 : Reports == 1 ? 0.5 : Reports == 2 ? 0.75 : 1;

        /// <summary>
        /// Add a bounded driver-safety report and reset peaceful recovery progress.
        /// A new assault should interrupt recovery rather than inherit credit from earlier
        /// handoffs.
        /// </summary>
        internal void RecordAssault()
        {
            if (Reports < 3) Reports++;
            PeacefulHandoffs = 0;
        }

        /// <summary>
        /// Clear one safety report after two peaceful inventory handoffs.
        /// Gradual recovery rewards completed service interactions instead of elapsed time or
        /// dropped packages.
        /// </summary>
        internal void RecordPeacefulHandoff()
        {
            if (Reports == 0) return;
            if (++PeacefulHandoffs < 2) return;
            Reports--;
            PeacefulHandoffs = 0;
        }

        /// <summary>
        /// Decide whether a replacement driver receives protection from reports and a supplied
        /// random roll.
        /// Injecting the roll keeps normal probabilities testable; developer mode makes flagged
        /// cases predictable.
        /// </summary>
        internal bool ShouldArm(bool quickTest, double roll) => Reports > 0 && (quickTest || roll < ArmedChance);
        internal void Reset() { Reports = 0; PeacefulHandoffs = 0; }
    }
}
