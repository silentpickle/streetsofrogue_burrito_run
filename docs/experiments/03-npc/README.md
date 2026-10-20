# 3 · NPC desires and coordination (October 7–13, 2026)

**In one sentence:** one opted-in Slum Dweller weighs nearby food (how much it
wants it minus how far away it is), picks one up, and gives way whenever the
game's own jobs, recruitment or danger need it.

**Why it exists:** this is the first step toward the real goal of NPCs with their
own reasons to act. Before adding anything, we surveyed what the game's NPCs
already do so we would not duplicate it or break it.

**Where it ended up:** the decision logic is pure and replayable offline. The
corrected in-game suite passes 126 checks. A naturally spawned resident
successfully enrolled, but its food pickup and paid recruitment in ordinary play
were never finished. The key lesson: the game calls Slum Dwellers `Hobo`, and our
early code that used `SlumDweller` silently excluded real ones.

## Reading order

1. [interactions-audit.md](interactions-audit.md) (Oct 7): what native NPC behaviour exists.
2. [dialogue-behavior-map.md](dialogue-behavior-map.md) (Oct 12): what each dialogue option really does.
3. [desires.md](desires.md) (Oct 12): the experiment and its controls.
4. [coordination.md](coordination.md) (Oct 13): the fixes and the latest verified state. **Most current.**
5. [desires-handoff.md](desires-handoff.md): testing history, for whoever picks this up.

Source: `src/BurritoRun/Npc*.cs`. A replayable captured decision is in
[docs/examples](../../examples/README.md).
