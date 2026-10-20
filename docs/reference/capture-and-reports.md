# Numeric capture and evidence reports

> **What this is:** What the mod writes to its local logs, the field layout, and the scripts that turn logs into reports.  
> **Read it if:** You are reading a capture file or a generated report.  
> **Status:** Partly dated (started October 4, 2026). The field descriptions remain valid.

Updated October 4, 2026. Logs stay local in `.runtime/logs/`. [Handoff](../history/pause-handoff.md) owns exact build identity; [architecture](../experiments/01-world-awareness/architecture.md) owns caps/scheduling. The bark field reference below began with 0.5.5; it remains compatible with 0.6.0.

## World capture and buffered writing — current

`world-SESSION-????.jsonl` plus its manifest records census/lifecycle, selected details, camera/perception/navigation evidence, incidents, performance windows, trial results and coverage/loss. World schema 1 is separate from legacy bark trace schema 1. F5 queues bounded history and progressive census pages; F10 controls pause/selection/trials. JSON serialization and disk writes run on a single bounded background worker using detached values. Legacy bark/event text keeps its format and order on that same buffered path.

Queue defaults: 2048 records / 4 MiB conservative payload accounting, with 64 records / 256 KiB reserved for essential work. World history: 20 game seconds, 101 frames / 8 MiB. Legacy bark history: 1 MiB with pages of at most eight frames. World disk segments: 8 MiB, retain 16 per session. Rotation/eviction/loss is explicit; abrupt exit or missing terminal closure prevents completeness claims. Do not block gameplay waiting for a drain or serialize a large incident on the game thread.

Performance evidence includes diagnostic/phase costs, queues/drops, frame tails and quality reductions. The installed burst correction adds hookMaxMs, hookOverBudget, deferredLifecycleDetails and one-second quality-window values; lifecycle core marks detail pending and viewport unknown. The corrected-build busy Record retest exercised these new fields; sustained native pressure/recovery remains pending. See `.runtime/benchmark-e5a30132-busy-record.md`. Known native freezes stay in measured unscaled time/hitch statistics; actual/unknown pauses remain invalid.

```bash
python3 scripts/report-world.py .runtime/logs/world-SESSION-????.jsonl --output .runtime/world-SESSION-report.md
```

Report exit zero establishes readable/structurally accepted evidence, not gameplay, scenario comparability or 30 FPS acceptance. Loss/malformed/missing closure stays conservative. Native observations and pure/report checks are cataloged in [validation](../experiments/01-world-awareness/validation.md).

## Legacy bark trace reference

Every explicit F8/bark or F9/guard request receives a session-scoped attempt ID before eligibility checks. Existing text/engine logs remain; `trace-<UTC>-<session>.jsonl` adds schema-versioned numeric records and explicit missing values. Boot prints the exact trace path. Automatic emissions and changes in automatic availability are captured too.

## Captured values

| Area | Fields |
| --- | --- |
| Identity/timing | Build, session, attempt, sequence, level generation, scene/controller/nest IDs, observed seed, floor, load clock, UTC, game time, wall seconds, time scale |
| Policy | Count, level cap, remaining game seconds, spawn-grace/cooldown/cap reason, following state, distance limits |
| Actors | Native name, UID/Unity instance ID, coordinates, current/max health, jobs/default/root goals, goal/subgoal lists, hearing range, investigation, owner ID, source relationship, combat/follow/death/KO/pool state, distance from source, root noise ID/instance |
| Selection | Distance, line of sight or unknown, eligibility/reasons, scanned/omitted counts, reason counts; selected target separately before emission |
| Environment | Source/target tile grids and coordinates, 0.64-unit cells, indoor/chunk/ownership, wall/floor/solid/hazard/hole data, native walkability/safety, nearby objects and door open/locked/direction state |
| Emission | Volume, native radius/ID/instance, noise lifetime/initial pause, starting separation, source/target snapshots, audio playback requested (audibility unverified) |
| Observation | Coordinates, root goal/noise identity, native reaction activation, displacement/approach since emission, matching-sample segments, maximum measured matching displacement/approach, counts/gaps, snapshot cost, noise state/lifetime/heard count, interruptions and dropped records |

Missing context is `null` or has an unavailable reason; it never becomes an observed zero. Native seed zero is retained, without claiming replayability.

## Accuracy and bounds

Native activation, movement and useful gameplay consequences are separate evidence. Movement requires a matching target reaction and consecutive samples with the matching root noise goal/identity. The first matching sample starts a baseline; earlier movement is excluded. Another goal or a gap over 0.6 game seconds starts a new segment. Sampling cannot prove continuous goal execution or isolate all competing stimuli.

The offline report independently recomputes movement from coordinates using Unity float32 precision. It checks flags/values, counts, actor/noise identities, observation duration and level identity. Missing, malformed, truncated, conflicting or dropped records yield conservative results. Absence of observed reaction/movement does not establish native nonresponse. Brief reactions and propagated window noise can escape this capture. Plugin selection reasons are distinct from unknown native decision reasons.

Actor loss, death/KO, combat, pause, release/reset, loading, shutdown and pooled noise reuse terminate active observation explicitly. Abrupt exit may prevent a final write; missing terminals remain inconclusive. Audio requested is not audibility; local walkability is not a route; mechanics PASS is not fun or useful distraction.

Test-mode capture targets 0.2 game seconds per sample, retaining at most 101 frames/twenty game seconds. Each frame has at most eight actors within twelve units, prioritizing source/owner/target. Requests detail at most 32 candidates in native list order with total/omitted counts. Environment captures at most two 5×5 grids and 24 nearby objects. Goal lists are bounded. Larger neighborhoods are partial evidence.

Observations run for at most eight game seconds. F5 exports history/environment even without an active bark. Exceptions export an incident and interrupt observations. Trace files have an 8 MiB cap and reserved terminal capacity; failures warn in text logs and record omissions where possible. Snapshot cost excludes serialization/file I/O and is not a full performance benchmark.

## Read and verify

```bash
python3 scripts/report-trace.py .runtime/logs/trace-YYYYMMDD-HHMMSS-session.jsonl
python3 scripts/report-trace.py .runtime/logs/trace-YYYYMMDD-HHMMSS-session.jsonl --output .runtime/logs/report-session.md
python3 -m unittest discover -s tests -p 'test_trace_report.py'
bash scripts/test-routing.sh
bash scripts/check.sh
```

Reader exit `2` means corrupt/incomplete evidence; `0` means readable evidence, including honest SKIPPED/INCONCLUSIVE outcomes. Exit zero is not gameplay PASS. The reader refuses to overwrite the source and does not infer new measurements from old text logs. The developer reads the reports; the user does not need to transcribe test history.

## Historical 0.5.5 verification — October 2

Analyzer/format build passed with zero warnings/errors; 86 linked checks and 18 report tests passed. Cases include numeric/null/escaping/culture handling, cap/write failure, history aging, pre-reaction movement exclusion, broken sample segments, float32 precision, sideways motion, reused actors/noises, interruptions and missing/conflicting/corrupt records. Synthetic tests validate reporting rules, not native gameplay.

Built/installed DLL SHA-256: `5b312b15f7e466a04f153344ae23bfcb12846f3ad6b0cf029b5f92b64e054387`. Previous DLLs: `.runtime/checkpoints/0.5.4-before-tracing/`.

Final startup smoke session `c1dab250` in `.runtime/logs/game-20261002-171945-45975.log` booted this build, created a trace and logged `BURRITO_SMOKE_PASS`. The prior 0.5.5 build (`0.5.5+20261002-225945`) also passed smoke in session `67122dec`; its native check session `757fd4a8` in `.runtime/logs/game-20261002-170146-45019.log` captured F8/F9 no-pet requests as SKIPPED and an F5 incident. Report: `.runtime/logs/report-757fd4a8.md`. No plugin/capture error was found in the inspected engine logs. The final build adds only a safeguard against labelling a positive measurement PASS after trace writes are dropped. Both checks exited.

The native start menu did not advance under automated input; plugin hotkeys responded. This run did **not** verify populated live NPC/environment snapshots, cooldown with an actual dog, emission, reaction, movement or interrupted native observation. The menu automation cause is not established. Those were the October 2 gaps. Subsequent world capture/native evidence is recorded in the [World Awareness validation ledger](../experiments/01-world-awareness/validation.md); the proposed authored fixture still is not built.
