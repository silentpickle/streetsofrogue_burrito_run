# World Awareness push — 30 FPS contract

> **What this is:** An earlier copy of the World Awareness plan, saved before consolidation.  
> **Read it if:** You need the exact October 4 wording.  
> **Status:** Dated working notes, kept as history. Build IDs, "current" and "next" refer to the date written, not today.

The first deliverable is the read-only inspector and reusable query/capture service. Food/hydrant interests and movement recovery follow separately. No new dog behavior is part of this push.

## Resumed implementation — October 3, 2026

The correctness audit fixed stale registry retention, actor activity filtering, effect-specific IDs, selected-target perception budgeting, mode-window mixing, writer failure loss accounting, bounded history and progressive incident export. Warmed 60-second benchmark trials now have a runtime producer and invalidation rules. Build/analyzers/format, linked C# checks and 35 Python tests pass. A native startup smoke passed; a native populated census and preliminary timing windows were captured. The enum-safe payload correction is installed as 0.6.0+20261004-050727; its populated F5 capture retest passed with zero terminal capture errors or drops in session bb128da0. The latest build adds darker panels and automatic nine-trial sequencing; comparative acceptance evidence remains pending. See [validation.md](../experiments/01-world-awareness/validation.md) for exact builds, evidence and remaining gates. M1–M4 remain provisional until native validation; M5 is pending.

## Milestones

| Milestone | Status | Gate |
|---|---|---|
| M0 baseline/contracts | Implemented | Source including uncommitted work and installed 0.5.5 DLL preserved under `.runtime/checkpoints/0.5.5-before-world-awareness`; baseline manifest includes SHA and Git HEAD |
| M1 registry/capture | Implemented; verification ongoing | Reconcile source counts, pooled reuse, short-lived effects and queue saturation |
| M2 spatial/perception | Implemented; verification ongoing | Reference query parity, native wall/viewport/cone examples |
| M3 navigation evidence | Implemented; verification ongoing | Explain following/waiting/fleeing/path evidence without movement commands |
| M4 inspector workflow | Implemented; verification ongoing | Panel, overlay, reports and saved numbers agree; input isolation |
| M5 native/performance | Pending | Paired ordinary/busy benchmarks and requirement evidence |

See WORLD_AWARENESS_ARCHITECTURE.md for contracts and WORLD_AWARENESS_REQUIREMENTS.md for requirement evidence. Never change an implemented status to verified merely because the assembly compiled.

## Performance acceptance

Preserve 30 FPS where the baseline sustains it with headroom; do not impose a 30 FPS cap. Cooperative 1 ms foreground scheduling budget; goal below 1 ms mean and 2 ms p95 total diagnostic work. Compare three warmed-up 60-second trials for each of Off, Inspect and Record modes, in ordinary and busy encounters. Median trial p95 frame time should worsen by no more than 5%. Investigate new recurring frames over 50 ms. Main-thread native calls cannot be preempted; log individual overruns and stop scheduling additional optional work.

Development host: Apple M3 Max, 128 GiB RAM, 16 logical CPUs. This is not modest-hardware validation. Live split-screen and network play are not verified. A modest second-machine benchmark remains a release gate for claims about that hardware class.

## Current evidence

Prior 0.5.5 run: 120 bark snapshots, median collection 0.2452 ms, p95 0.3088 ms, maximum 2.3878 ms. These numbers exclude serialization/file I/O and do not establish overall framerate impact. World awareness final checkpoint build compiles with zero warnings/errors; C# checks and 30 Python report tests pass. Native installation and functional/performance validation are pending.

## Workflow

1. Preserve baseline and implement/test each milestone with logging in place.
2. Build with analyzers and verify formatting; do not overwrite the installed DLL while the current user session is running.
3. Validate using the separate prototype profile. Save captures, reports, failures and exact build identity.
4. Update requirement evidence and this milestone table. Publish no hardware-wide performance claim from this development machine alone.

## October 4 scope decision: simplify barriers

User explicitly prefers practical rules over detailed window/collider diagnostics. Defer exact-blocker/window-damage instrumentation and further window matrices; they are not the next implementation slice. Preserve the existing captured clear/wall/damaged-window evidence without promoting it to complete perception validation.

For future behavior policy, bars block movement but permit sight. Implement this as an explicit policy rule when needed; retain the native geometry result separately so an override cannot be mistaken for an observed native answer. This rule does not prove a path, allow passage or establish hearing/actual NPC awareness. It is a requested policy, not an already implemented or verified feature. Ordinary walls continue using the native obstruction evidence; use native window evidence for now. Broader sensory simulation is deferred.

Next priority: ordinary Off/Inspect/Record comparison using existing local trial collection. Start once, end the chat turn during recording, then inspect finished evidence. Busy encounter and remaining selected native cases follow; do not spend another manual loop on window damage.

## October 4 prepared burst-budget correction (not installed)

The busy stress sample revealed expensive lifecycle windows while quality stayed at 0. Source now captures lifecycle identity/classification/position/active state and transient Noise/Bullet/hazard fields immediately; actor/item/object detail and camera projection are deferred to the scheduled census/selected detail. Lifecycle snapshots explicitly label pending detail and unknown viewport via data.coverage. Pool GetComponent lookup is skipped when lifecycle capture is disabled and included in hook timing when enabled. Essential event ordering and bounded queue remain; loss counters are unchanged.

Quality adds one-second numerical overhead windows: mean >1ms or >5% frames over 2ms is pressure. Two pressured windows reduce optional work; the existing consecutive-frame/saturation fast response remains. Sustained observed headroom restores one level after 30 seconds; unobserved gaps cannot count as recovery. Logs add hookMaxMs/hookOverBudget/deferredLifecycleDetails and quality window mean/frame counts/time; UI shows pressure reason and numerical evidence.

Build/analyzers/format and 242 linked C# assertions pass; 35 Python tests pass. Synthetic separated spikes matched to the bad native window summaries (mean 2.0621ms/p95 10.6038ms) kept historical quality=0 but reduced new quality to 2 at 30/60/120fps. These are synthetic sequences: actual frame ordering was not recorded, and they do not establish native performance. Evidence `.runtime/world-checks-burst-fix.txt`, `.runtime/world-build-check-burst-fix.txt`.

Current game remains loaded with 0.6.0+20261004-164018 / session 5aa527ab. SOURCE DIFFERS FROM LOADED DLL. Prepared build snapshot `.runtime/checkpoints/0.6.0-burst-fix-prepared/`. Do not install over this game. Next: after authorized normal close, finalize session integrity report, install/relaunch the prepared fix, run one relevant busy Record retest and ordinary check, then collect missing busy Off/Inspect/Record comparisons. Full busy acceptance and modest hardware remain open. Do not discard the completed prior stress capture.
