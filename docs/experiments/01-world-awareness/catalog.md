# World Awareness catalog

> **What this is:** An index of World Awareness builds, test runs and the files that implement each part.  
> **Read it if:** You are tracing a result back to its build and source files.  
> **Status:** Dated working notes, kept as history. Build IDs, "current" and "next" refer to the date written, not today.

October 7 installed hydrant checkpoint: `0.6.0+20261008-052320`, `238d7320`, **67 native gates PASS**, SHA256 `89f90828578061f48c3f14229123c8372dd6ebbe2e00ed9344e75534ed798292`, `.runtime/world-harness/20261007-232321-7312/report.md`. Closed clean stream; installed binary matches. [Hydrant plan](../02-dog/hydrant.md) retains failed pilots, source scope and limits; [handoff](../../history/pause-handoff.md) owns the separate current playable run.

Earlier native-validated food build: `0.6.0+20261007-051237`, `2cf1bc70`, 66 gates, SHA256 `dbcd7d81946223fdb987ba73a93dca826b6321448852b5c42717450af0437d6a`; `.runtime/world-harness/20261006-231238-67676/`; game closed. Five food cases, native eating/Follow and retained one-per-level limit pass. See [food details](../02-dog/food.md) and [operational handoff](../../history/pause-handoff.md). Older readiness/busy captures below remain separately attributed.

Updated October 6, 2026. [pause-handoff.md](../../history/pause-handoff.md) is the operational authority. The prior readiness install is `0.6.0+20261007-035337` / `541591f7`; the final base harness passed 29 ownership/discovery/read/retirement gates and quit. Advanced readiness passed 49 gates, native correctness 45, and the controlled busy comparison 67 plus numeric targets; each retains its separate binary/receipt. Earlier evidence remains historical. Current readiness/food source and documentation changes are uncommitted.

Native harness: [workflow](harness.md), `make world-harness`, `src/BurritoRun/{WorldFixture,WorldHarness,WorldAwarenessHarness}.cs`, `scripts/{world-harness.sh,report-world-harness.py}`, `tests/test_world_harness_report.py`. Evidence/DLL/hash receipts: `.runtime/world-harness/20261004-145335-71846/` and `.runtime/world-harness/20261004-145512-72124/`; both `report.md` files PASS 29 checks. Earlier startup/interruption/timeout captures remain in sibling directories as incomplete evidence.

Stationary comparison: `.runtime/world-harness/20261004-151707-75949/report.md` (b7949371, 60 capture checks plus numeric targets). Pressure/recovery: `.runtime/world-harness/20261004-154025-78868/report.md` (a9a50717, 44 checks, 640 effects / 100 reused native instances). Plans: [stationary](performance-plan.md), [pressure](pressure-plan.md). Commands: `make world-performance`, `make world-pressure`. Each result applies to its preserved build.

## Implementation map

| Area | Files under src/BurritoRun unless noted |
|---|---|
| Detached contracts and queries | WorldModels.cs, WorldIndex.cs |
| Native census and observational lifecycle | WorldNative.cs, WorldLifecycleHooks.cs |
| Cameras and geometric perception | WorldPerception.cs |
| Registry, navigation, history, budget | WorldAwareness.cs |
| Overlay and paused inspector | WorldAwarenessUI.cs |
| Bounded recording and legacy compatibility | CaptureWorker.cs, WorldJournal.cs, TraceStorage.cs, BarkTrace.cs |
| Warmed trials and native freeze handling | WorldBenchmark.cs |
| Reports | scripts/report-world.py, scripts/report-trace.py |
| Checks | tests/DeliveryRoute.Tests/WorldChecks.cs, TraceChecks.cs; tests/test_world_report.py, test_trace_report.py |

## Evidence and preservation

Paths below are local artifacts; `.runtime` is not a published package. Backups are file snapshots, not Git commits. Do not replace evidence when a new build changes a conclusion.

| Artifact | Scope |
|---|---|
| `.runtime/checkpoints/0.5.5-before-world-awareness/` | Original source/DLL baseline |
| `.runtime/checkpoints/20261004-model-handoff/source-and-evidence.tar.gz` | Earlier 82-file catalog; predates the latest fixes |
| `.runtime/checkpoints/0.6.0-before-freeze-frame-fix/BurritoRun.dll` | Previous installed DLL preserved before restart |
| `.runtime/checkpoints/0.6.0-before-burst-fix/install-receipt.json` | Preserved prior DLL/manifest; exact installed hash and boot identity |
| `.runtime/checkpoints/0.6.0-burst-fix-prepared/manifest.json` | Tested source/test/DLL hashes; this previously prepared DLL is now installed |
| `.runtime/world-build-check-burst-fix.txt` | Build/analyzers/format passed, zero warnings/errors |
| `.runtime/world-checks-burst-fix.txt` | 242 C# assertions; summary-matched synthetic burst sequences at 30/60/120 FPS |
| `.runtime/world-bb128da0-report.md` | Enum-safe F5 export and clean terminal; no full benchmark suite |
| `.runtime/visibility-8fc5165e-*.json` | Selected clear/facing/wall/plant-clear/damaged-window numeric examples |
| `.runtime/visibility-8fc5165e-damaged-window-setup.png` | User's semi-intact-window setup image |
| `.runtime/benchmark-8fc5165e-ordinary.{md,json}` | Nine valid older-build stationary ordinary trials |
| `.runtime/benchmark-8fc5165e-busy-interrupted.json` | Invalid Off attempt, paused at 13.2329 measured seconds |
| `.runtime/benchmark-8fc5165e-busy-record-partial.json` | Invalid old Record attempt, paused at 20.3674 measured seconds |
| `.runtime/world-8fc5165e-report.md` | Final closed-session integrity passes; earlier benchmark report's pending-closure note is superseded |
| `.runtime/benchmark-5aa527ab-busy-record.{md,json}` | Valid busy Record, freeze fix exercised; budget bursts and hitches remain open |
| `.runtime/logs/world-5aa527ab-????.jsonl` | Closed stream; zero terminal errors/drops; 38 early segments retired, full-session completeness rejected |
| `.runtime/awareness-timescale-set-native.txt` | Native clock/freeze audit |
| `.runtime/visibility-los-*-native.txt` | Native LOS/mask/object audit; does not establish every barrier case |

Historical October 4 checks: 242 C# assertions, 64 Python report tests, build/analyzers/format and whitespace checks passed. These are source/report checks; installed burst changes have one lighter busy Record native sample; bounded frame-budget reduction/recovery is verified in a9a50717; sustained window/backpressure paths remain open. Synthetic sequences matched summary statistics, not recorded frame ordering.

## Session ledger

| Session | Result and limit |
|---|---|
| 9514e7bd | Clean startup smoke; no populated-world proof |
| ab4909dd | 379-entity census; shutdown-interrupted Off trial; legacy loss retained |
| fc7f7409 | Dark inspector verified; pause-interrupted Off trial; enum rejection and five worker drops |
| bb128da0 | Enum repair verified with world/legacy F5 pages; 384 latest entities; zero terminal errors/drops |
| 8fc5165e | Selected geometry tests; 390 entities in stationary suite; nine valid ordinary trials; invalid busy attempts retained; final capture integrity clean |
| 5aa527ab | Freeze-fixed build; one valid busy Record trial, nine recognized freezes; zero observed loss, closed cleanly but retained stream lacks 38 early segments; burst correction motivated by its short-window overruns |

| e5a30132 | Burst fix `0.6.0+20261004-170516` boot confirmed; populated busy Record result 6582 valid; ordinary suite interrupted after six valid trials |

## Corrected-build retest artifacts

`.runtime/benchmark-e5a30132-busy-record.{md,json}` preserves one valid sample: 0.738 ms diagnostic mean / 1.495 ms p95, no observed errors/drops, explicit lifecycle-core coverage and one >1 ms hook call. Quality stayed 0; encounter was lighter/different than prior stress. `.runtime/benchmark-e5a30132-ordinary-start.json` records the next suite start, not its completion. Read the reports for limits.

## Continuation

Use [requirements](requirements.md) for remaining gates and [handoff](../../history/pause-handoff.md) for next actions. Start local collection once and read completed evidence after the user returns; avoid quota-consuming active polling. F10 controls avoid macOS F11 conflicts. Do not count an interrupted trial or discard a valid poor result.

Earlier catalog entries are [preserved](../../history/2026-10-04-before-consolidation-WORLD_AWARENESS_CHECKPOINT.md). Historical “active/current/pending” wording is superseded by this catalog and the handoff.

## Corrected ordinary suite interruption

Six valid trials, two per mode; seventh Off trial invalid at 42.6606 measured seconds on a native clock pause (mainScale=0, nativeFreeze=false, inspector/phone closed; initiator unknown). No full suite completion. The valid six have zero >50 ms frames or losses; partial median frame p95 changes +0.76% Inspect/+0.81% Record, with active diagnostics within <1 ms mean/<2 ms p95. Completion focus mixed (five unfocused, one focused); foreground performance is not established. Saved `.runtime/benchmark-e5a30132-ordinary-partial.{md,json}`. Preserve valid and invalid evidence; collect one remaining trial per mode, not a repeat of the whole suite.


## October 6 roadmap to step 4

See [execution ledger](../02-dog/road-to-the-dog.md) for the exact profiles/builds/hashes, successful native slices and preserved non-passing pilots. Source additions are `src/BurritoRun/WorldReadiness.cs`, native fixture helpers and read-only harness evidence access; `report_world_readiness.py` independently asserts the extensions. Commands are `make world-advanced`, `make world-native` and `make world-busy`. Recovery policy correction is in `WorldIndex.cs`. Latest source checks: 244 C# assertions, 82 Python report tests, zero compiler/analyzer/format warnings/errors. Busy comparison passes; dog behavior remains unchanged.

Prior readiness install, before the food follow-on: `0.6.0+20261007-035337`, session `541591f7`, `.runtime/world-harness/20261006-215337-37447/`. The original two-repeat native harness passed 29 gates with the new instance/UID ownership guard; closed clean capture, preserved DLL/receipt SHA256 `627906cfd7f3135852642d69796fc49292c2e2540c63d269b40046c9879d05da`, matching the installed loader DLL at that checkpoint. That run closed. Its build/analyzer/format, 244 C# assertions, 82 Python report tests, shell syntax and whitespace checks pass. Source and documentation edits remain uncommitted. That readiness build left dog behavior unchanged; the subsequent food slice is documented above.
Food implementation: `PetPolicy.cs`/`PetService.cs` consume read-only `WorldAwareness.ResolveLive` query results; `PetFoodFixture.cs` owns test writes; `PetFoodHarness.cs` orchestrates/observes; `scripts/report_pet_food.py` and `tests/test_pet_food_report.py` recompute/reject unsupported outcomes.

October 6 requested visible rerun: `2cf1bc70` / `0.6.0+20261007-051237` passed 66 gates in `.runtime/world-harness/20261006-231238-67676/`. Source behavior unchanged; installed DLL matches the saved receipt. A preceding attempt in `.runtime/world-harness/20261006-231020-66891/` remains INTERRUPTED after focus changed (healthy sniff, eating/Follow and removal had completed). The rerun started with the game focused and closed cleanly. Original implementation checkpoint `249a0ae7` / `0.6.0+20261007-044122` remains preserved separately.
