# World Awareness catalog — October 4, 2026

> **What this is:** An earlier copy of the World Awareness catalog, saved before consolidation.  
> **Read it if:** You need the exact October 4 wording.  
> **Status:** Dated working notes, kept as history. Build IDs, "current" and "next" refer to the date written, not today.

## State

Build 0.6.0+20261004-050727; latest reviewed session bb128da0 ended cleanly. No code changes, rebuild, installation or launch were performed for this checkpoint. Work is uncommitted; the snapshot is a file backup, not a Git commit. Model switch is a user/app setting, not a plugin change.

## Implementation map

| Area | Files | State |
|---|---|---|
| Contracts and distance queries | WorldModels.cs, WorldIndex.cs | Implemented; pure boundary/reference tests passed |
| Native census and lifecycle | WorldNative.cs, WorldLifecycleHooks.cs | Implemented; populated census and transient records observed; controlled reuse/destruction gates open |
| Cameras and perception | WorldPerception.cs | Implemented; native wall/cone/viewport cases open |
| Registry, navigation, history and budgeting | WorldAwareness.cs | Implemented; actual goals/health/path/door evidence recorded; no movement commands |
| Live overlay and paused inspector | WorldAwarenessUI.cs | Implemented; darker panel and pause/capture observed; full input-isolation gate open |
| Bounded recording | CaptureWorker.cs, WorldJournal.cs, TraceStorage.cs, BarkTrace.cs | Implemented; enum-compatible F5 capture succeeds; terminal losses zero |
| Benchmark collection | WorldBenchmark.cs, WorldAwareness.cs | Implemented; interrupted trials correctly invalid; comparative acceptance pending |
| Reports and tests | scripts/report-world.py, scripts/report-trace.py, tests/DeliveryRoute.Tests/WorldChecks.cs, tests/test_world_report.py | Last checks: 223 C# assertions, 35 Python tests passed |

C# files above are under `src/BurritoRun/`. Architecture, budgets and requirements remain in WORLD_AWARENESS_ARCHITECTURE.md, WORLD_AWARENESS_PLAN.md and WORLD_AWARENESS_REQUIREMENTS.md. WORLD_AWARENESS_VALIDATION.md records evidence and gaps.

## Evidence inventory

| Session | Result |
|---|---|
| 9514e7bd | Startup smoke passed; no populated-world proof |
| ab4909dd | 379-entity census; partial timing; first Off trial invalid at shutdown (55.507 s); legacy capture loss retained |
| fc7f7409 | Dark inspector verified; Off trial invalid at pause (52.2006 s); enum rejection diagnosed, worker loss 5 |
| bb128da0 | Corrected build; F5 world plus legacy pages succeed; clean terminal errors/drops all zero; 384-entity latest census; no completed benchmark suite |

Latest report: `.runtime/world-bb128da0-report.md`; inputs `.runtime/logs/world-bb128da0-????.jsonl`, matching manifest, `trace-*bb128da0.jsonl`, `events-*bb128da0.log`, and `.runtime/logs/game-20261003-230740-88637.log`.

The world stream has 379 effect-created and 371 entity-removed records; retirement requests can repeat and are not unique deaths. Lifecycle observations are useful evidence, not proof that all short-lived entities were captured. The report's “Latest observed state” currently shows incident-time detail (368.314 game seconds), while its census/performance sections use later records: do not treat those sections as an atomic snapshot. Selected-target perception is null.

Latest Record window: mean diagnostic 0.7317 ms, p95 1.6005 ms; frame p95 17.4528 ms. Hardware: M3 Max, 128 GiB, 16 logical CPUs. These windows do not establish 30 FPS acceptance on another computer.

## Remaining acceptance work

- Controlled native wall/cone/viewport edges, food classification, door states, follower waiting/fleeing, pooled reuse, level/destruction updates and input isolation.
- Three valid warmed 60-second trials per Off/Inspect/Record mode, for ordinary and busy scenarios. Scenario comparability and recurring hitches require review beyond numeric thresholds.
- Allocation profiling and finer native/index cost attribution; stress lifecycle hooks and census completion under busy populations.
- Modest second-machine validation. Live split-screen/network deferred. No dog-interest or movement-recovery expansion in this milestone.

## Quota-efficient continuation

Use GPT-6.1 Sol for the next scoped implementation or report-review turn. Keep collection independent of model execution: start an existing local trial, confirm its start once, then end the chat turn while the game records. The user returns after completion; read and report once. Do not spend an active conversation polling for ten minutes. Chat quota exhaustion alone does not stop an already running game's local logger, though game/app/process termination still can.

The nine-trial sequence takes about 10.5 minutes without pause or level changes. F10 offers “Run 9 trials”; individual trials allow completing missing evidence. Avoid F11 if macOS intercepts it. Each measured trial requires 10 seconds warm-up plus 60 seconds capture. Preserve invalid records and explain their invalidation; never count a button press as completion.

Resume prompt: “Read PAUSE_HANDOFF.md and WORLD_AWARENESS_CHECKPOINT.md. Review the saved bb128da0 report. Continue one outstanding validation case; keep local collection independent of chat and do not rebuild or launch until the next test is chosen.”

## October 4 scope decision: simplify barriers

User explicitly prefers practical rules over detailed window/collider diagnostics. Defer exact-blocker/window-damage instrumentation and further window matrices; they are not the next implementation slice. Preserve the existing captured clear/wall/damaged-window evidence without promoting it to complete perception validation.

For future behavior policy, bars block movement but permit sight. Implement this as an explicit policy rule when needed; retain the native geometry result separately so an override cannot be mistaken for an observed native answer. This rule does not prove a path, allow passage or establish hearing/actual NPC awareness. It is a requested policy, not an already implemented or verified feature. Ordinary walls continue using the native obstruction evidence; use native window evidence for now. Broader sensory simulation is deferred.

Next priority: ordinary Off/Inspect/Record comparison using existing local trial collection. Start once, end the chat turn during recording, then inspect finished evidence. Busy encounter and remaining selected native cases follow; do not spend another manual loop on window damage.

## October 4 ordinary stationary benchmark

Session `8fc5165e`: all nine ordinary trials completed valid, three each Off/Inspect/Record, with 10-second warm-ups and at least 60-second measurement. User reports little/no movement in a quiet non-visible part of the level; same level and 390 registered entities throughout trial summaries. Median trial frame p95: Off 16.991 ms, Inspect 17.261 ms (+1.59%), Record 16.959 ms (-0.19%). Median diagnostic mean/p95: Inspect 0.657/1.450 ms; Record 0.658/1.466 ms. Every active trial meets <1 ms mean / <2 ms p95. Zero measured frames >50 ms across nine trials, zero trial losses and completion quality 0.

Saved benchmark report `.runtime/benchmark-8fc5165e-ordinary.md` and detached raw trial snapshot `.runtime/benchmark-8fc5165e-ordinary.json`. Stationary ordinary numeric targets are satisfied on this host; no claim for busy encounters or modest hardware. Session remains active, so `.runtime/world-8fc5165e-report.md` conservatively flags missing shutdown; normal exit/final stream integrity review pending. Do not repeat this suite solely because it was stationary: keep it as the stationary baseline and add the missing busy evidence.

## October 4 busy trial and freeze-frame correction

Busy Record attempt `8fc5165e`, start 15475, trial 17493, stopped as invalid “paused” at 20.3674 measured seconds after 10-second warm-up. Recorded 269 Bullet, 21 Noise and 13 Danger creation events from start through interruption, including warm-up. Partial frame p95 17.4422 ms; diagnostic mean 0.6987 ms/p95 1.8323 ms; zero >50 ms measured frames, errors/drops zero. Saved `.runtime/benchmark-8fc5165e-busy-record-partial.json`. Do not promote this to a complete busy benchmark.

Source audit exposed an overly broad pause detector: installed build invalidates on any zero Time.timeScale, but native GameController.SetTimeScale also sets zero during freeze frames. Gameplay resumed within roughly two frames of the interruption, consistent with this mechanism; old capture lacks exact clock flags, so the individual pause cause remains unproven. IL audit `.runtime/awareness-timescale-set-native.txt`.

Prepared source correction (NOT installed in the active game): classify only explicitly evidenced native freeze frames with positive effective native clocks and no phone/inspector/screenshot pause. Unknown/UI clock stops remain invalid. All freeze-frame wall time, frame samples and slow-frame hitches stay included in the 60-second trial; no samples are trimmed to obtain a pass. Trial exports add freeze-frame counts and native clock/UI/focus state; performance windows include known freeze frames. Added regressions for normal/end-transition freezes, main/secondary/UI/unknown pauses, disabled freeze mode, complete trials retaining slow freeze frames and explicit pause precedence. 233 C# assertions and 35 Python reader tests passed; build/analyzer/format passed. Final build rechecked after screenshot-pause guard.

Active game remains on 0.6.0+20261004-050727. Reload required for correction; preserve current run until user ends it. Existing ordinary suite stays valid evidence for old build; run a relevant busy retest on corrected build and compare old/new ordinary overhead if needed before release claim. No further old-build busy retry needed.

## October 4 reload: freeze-frame fix installed

User authorized restart. Prior session `8fc5165e` ended with errors=0, workerDrops=0, worldDrops=0; final `.runtime/world-8fc5165e-report.md` passes capture integrity. Prior DLL preserved at `.runtime/checkpoints/0.6.0-before-freeze-frame-fix/BurritoRun.dll`. Build/install completed with zero warnings/errors. New loaded build **0.6.0+20261004-164018**, session **5aa527ab**, log `.runtime/logs/game-20261004-104112-27012.log`; BURRITO_BOOT confirmed, no plugin exception in inspected startup log. Source and installed DLL contain the freeze-frame detection/counter/clock-state changes. Native busy retest pending; startup is not validation of the fix in gameplay.

Next: user starts normal single-player run; F10 Busy encounter checked, Next trial Record, Start Record trial/resume. Collect 70 seconds locally while playing a busy scene; return only after completion. Verify valid=true and nativeFreezeFrames plus activity/clock/loss/quality evidence. Still need three trials per mode for full busy comparison; don't count the partial old trials. Launcher session 77463 belongs to this process.

## October 4 completed busy Record stress trial

Corrected build 0.6.0+20261004-164018, session 5aa527ab, trial sequence 3689: valid 60.0163 measured seconds / 3297 frames after warm-up; nativeFreezeFrames=9, UI closed/focused at completion. Freeze-frame correction verified in this native run. Lifecycle creations including warm-up: 400 Bullet, 34 Noise, 20 Danger, 4 Agent (observations, not unique population). Zero observed errors/drops. Trial diagnostic mean 0.8672 ms/p95 1.7044 ms; frame p95 33.3455 ms/p99 33.8286 ms, 8 frames >50 ms.

Saved `.runtime/benchmark-5aa527ab-busy-record.{md,json}`. This completes one busy stress trial, not full paired busy acceptance. One short performance window had diagnostics mean 2.0621 ms/p95 10.6038 ms with 161.26 ms aggregate hook cost; another mean 1.2008 ms/p95 2.0890 ms. Quality remained 0 in captured windows. Investigate burst costs/degradation and compare busy Off before attributing frame-time tails or claiming performance pass. Do not discard this valid sample or repeat merely to get a better number. Game is active; final shutdown integrity pending.

## October 4 prepared burst-budget correction (not installed)

The busy stress sample revealed expensive lifecycle windows while quality stayed at 0. Source now captures lifecycle identity/classification/position/active state and transient Noise/Bullet/hazard fields immediately; actor/item/object detail and camera projection are deferred to the scheduled census/selected detail. Lifecycle snapshots explicitly label pending detail and unknown viewport via data.coverage. Pool GetComponent lookup is skipped when lifecycle capture is disabled and included in hook timing when enabled. Essential event ordering and bounded queue remain; loss counters are unchanged.

Quality adds one-second numerical overhead windows: mean >1ms or >5% frames over 2ms is pressure. Two pressured windows reduce optional work; the existing consecutive-frame/saturation fast response remains. Sustained observed headroom restores one level after 30 seconds; unobserved gaps cannot count as recovery. Logs add hookMaxMs/hookOverBudget/deferredLifecycleDetails and quality window mean/frame counts/time; UI shows pressure reason and numerical evidence.

Build/analyzers/format and 242 linked C# assertions pass; 35 Python tests pass. Synthetic separated spikes matched to the bad native window summaries (mean 2.0621ms/p95 10.6038ms) kept historical quality=0 but reduced new quality to 2 at 30/60/120fps. These are synthetic sequences: actual frame ordering was not recorded, and they do not establish native performance. Evidence `.runtime/world-checks-burst-fix.txt`, `.runtime/world-build-check-burst-fix.txt`.

Current game remains loaded with 0.6.0+20261004-164018 / session 5aa527ab. SOURCE DIFFERS FROM LOADED DLL. Prepared build snapshot `.runtime/checkpoints/0.6.0-burst-fix-prepared/`. Do not install over this game. Next: after authorized normal close, finalize session integrity report, install/relaunch the prepared fix, run one relevant busy Record retest and ordinary check, then collect missing busy Off/Inspect/Record comparisons. Full busy acceptance and modest hardware remain open. Do not discard the completed prior stress capture.
