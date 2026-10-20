# 1 · World Awareness (October 2–6, 2026)

**In one sentence:** a read-only service that keeps track of every actor, item,
object and effect on the level, plus a paused inspector (F10), an evidence
capture key (F5), and automated tests that run inside the real game.

**Why it exists:** the burrito courier kept doing surprising things, and there was
no reliable way to see what it could perceive, where it was trying to go, or
whether an action had actually happened. Every later experiment builds on this.

**Where it ended up:** functional checks, a stress test and a stationary
frame-rate comparison all pass on the development Mac. Full 30 FPS release
acceptance (combat, hazards, slower hardware) was never finished.

## Reading order

1. [architecture.md](architecture.md): the design rules (identity, threading, budgets). Start here.
2. [plan.md](plan.md): milestones M0–M5 and their status.
3. [requirements.md](requirements.md): each requirement, implemented and verified status.
4. [harness.md](harness.md): how to run the automated in-game tests.
5. [performance-plan.md](performance-plan.md) and [pressure-plan.md](pressure-plan.md): the two load tests.
6. [validation.md](validation.md) and [catalog.md](catalog.md): detailed evidence, run by run. Use these when tracing a specific claim.

Source lives in `src/BurritoRun/World*.cs`; see the [architecture map](../../architecture.md).
