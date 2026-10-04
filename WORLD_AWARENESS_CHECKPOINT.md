# World Awareness catalog

Updated October 4, 2026. [PAUSE_HANDOFF.md](PAUSE_HANDOFF.md) is the operational authority. Latest native-validated install is `0.6.0+20261004-214024` / `a9a50717`; the automatic pressure harness passed 44 capture checks and quit. Earlier burst comparisons in `e5a30132` remain preserved. All working-tree work remains uncommitted.

Native harness: [workflow](WORLD_AWARENESS_HARNESS.md), `make world-harness`, `src/BurritoRun/{WorldFixture,WorldHarness,WorldAwarenessHarness}.cs`, `scripts/{world-harness.sh,report-world-harness.py}`, `tests/test_world_harness_report.py`. Evidence/DLL/hash receipts: `.runtime/world-harness/20261004-145335-71846/` and `.runtime/world-harness/20261004-145512-72124/`; both `report.md` files PASS 29 checks. Earlier startup/interruption/timeout captures remain in sibling directories as incomplete evidence.

Stationary comparison: `.runtime/world-harness/20261004-151707-75949/report.md` (b7949371, 60 capture checks plus numeric targets). Pressure/recovery: `.runtime/world-harness/20261004-154025-78868/report.md` (a9a50717, 44 checks, 640 effects / 100 reused native instances). Plans: [stationary](WORLD_AWARENESS_PERFORMANCE_PLAN.md), [pressure](WORLD_AWARENESS_PRESSURE_PLAN.md). Commands: `make world-performance`, `make world-pressure`. Each result applies to its preserved build.

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

Latest saved checks: 242 C# assertions, 64 Python report tests, build/analyzers/format and whitespace checks passed. These are source/report checks; installed burst changes have one lighter busy Record native sample; bounded frame-budget reduction/recovery is verified in a9a50717; sustained window/backpressure paths remain open. Synthetic sequences matched summary statistics, not recorded frame ordering.

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

Use [requirements](WORLD_AWARENESS_REQUIREMENTS.md) for remaining gates and [handoff](PAUSE_HANDOFF.md) for next actions. Start local collection once and read completed evidence after the user returns; avoid quota-consuming active polling. F10 controls avoid macOS F11 conflicts. Do not count an interrupted trial or discard a valid poor result.

Earlier catalog entries are [preserved](docs/history/2026-10-04-before-consolidation-WORLD_AWARENESS_CHECKPOINT.md). Historical “active/current/pending” wording is superseded by this catalog and the handoff.

## Corrected ordinary suite interruption

Six valid trials, two per mode; seventh Off trial invalid at 42.6606 measured seconds on a native clock pause (mainScale=0, nativeFreeze=false, inspector/phone closed; initiator unknown). No full suite completion. The valid six have zero >50 ms frames or losses; partial median frame p95 changes +0.76% Inspect/+0.81% Record, with active diagnostics within <1 ms mean/<2 ms p95. Completion focus mixed (five unfocused, one focused); foreground performance is not established. Saved `.runtime/benchmark-e5a30132-ordinary-partial.{md,json}`. Preserve valid and invalid evidence; collect one remaining trial per mode, not a repeat of the whole suite.
