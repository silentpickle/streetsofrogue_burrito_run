# World Awareness push — architecture and 30 FPS contract

Updated October 4, 2026. Deliver the reusable read-only world service, live overlay, paused inspector, structured logs and offline reports. Dog interests and movement recovery are separate follow-ons. [Handoff](PAUSE_HANDOFF.md) owns the installed/prepared state; [requirements](WORLD_AWARENESS_REQUIREMENTS.md) owns acceptance evidence.

## Milestones and completion gates

| Milestone | Status | Evidence and remaining gate |
|---|---|---|
| M0 baseline/contracts | Implemented; baseline preservation verified | Original 0.5.5 source/DLL checkpoint, detached contracts and benchmark procedure exist; full live comparative acceptance remains M5 |
| M1 registry/capture | Implemented; partially verified | Populated censuses, transient creation records, bounded writer tests and clean closed-session captures; controlled pooling/level/destruction reconciliation remains open |
| M2 queries/perception | Implemented; partially verified | Distance/index reference checks and native selected clear/wall/cone examples; viewport edges and broader native classification/coverage remain open |
| M3 navigation evidence | Implemented; partially verified | Native goal/health/path/door fields captured; controlled waiting/fleeing/progress/door-state examples remain open |
| M4 inspector workflow | Implemented; partially verified | F10 pause, darker overlay and F5 numeric world/legacy export observed; full input-isolation/incident coverage gates remain open |
| M5 native/performance | In progress; not accepted | Stationary ordinary numeric comparison passed on older build; one valid busy Record sample exposed bursts; corrected-build comparisons, hitches, allocations and modest hardware remain open |

“Implemented” means code exists; a compile or synthetic check cannot verify the full native requirement. Logging and performance instrumentation accompany every milestone. Save a report/checkpoint at each gate.

## Resource policy

| Area | Default |
|---|---|
| Foreground scheduling | Cooperative 1 ms/frame; stop additional optional work when exhausted; measure indivisible-call overruns |
| Total measured diagnostics | Below 1 ms mean and 2 ms p95, including overlay |
| Registry | Target five updates/game second, distributed across frames |
| Expensive perception | At most 32 pairs/0.2 game seconds, also subject to time budget |
| Detailed tracking | One observer, one selected target, up to eight nearby actors |
| History | Twenty game seconds, count/size caps and explicit evictions |
| Queue/disk | Bounded count/bytes, reserved essential capacity, buffered rotating files and explicit loss |

See [architecture](WORLD_AWARENESS_ARCHITECTURE.md) for implemented caps and contracts. Native access stays on Unity's main thread; only detached data reaches the single background serializer/writer. Legacy bark logs retain format/order and share the buffered path.

Under load, defer off-target/off-screen LOS, reduce overlay/nearby detail, reduce refresh 5→2→1 Hz, and suspend optional detailed recording under writer saturation. Observer/target and lifecycle/loss records have priority. Display/export quality reason, refresh interval, age, deferred checks, queue depth and dropped records. Restore gradually with sustained observed headroom. Paused detail costs are reported separately. Lossy or stale capture never claims completeness.

The burst correction (installed; native retest pending) adds one-second pressure windows (mean >1 ms or >5% frames over 2 ms), reduction after two pressured windows, and 30 seconds of observed recovery per level. Minimal lifecycle metadata precedes scheduled detail/camera reads. One populated lighter busy Record retest exercised these changes with aggregate diagnostic targets met; native sustained pressure/recovery remains open. See catalog for numeric evidence; source checks alone do not establish native cost.

## Performance acceptance and maintenance

Benchmark Off, Inspect and Record in the same ordinary scenario and a comparable busy encounter containing NPC motion, noises, projectiles and hazards. After warm-up collect three 60-second measurements per mode per scenario. Do not impose a frame cap to hide overhead.

Record frame median/p95/p99 and >50 ms frames; native/index/query/overlay/copy/serialization/write costs; allocation/GC, retained memory, queue depth and file growth; entity/perception counts and quality reductions. Retained-memory deltas are not total allocations. Known native freeze frames remain in unscaled measurement and frame/hitch statistics; real/unknown pauses and discontinuities invalidate trials.

Where baseline sustains 30 FPS with headroom, active inspection must also sustain it. Median trial p95 must worsen by no more than 5%; investigate new recurring hitches. Diagnostics must meet their budget or visibly reduce work. Reports must also establish scenario comparability and coverage. Query/queue/scale replay tests supplement native runs and cannot prove another computer's performance.

Changes to registry, queries, overlays, hooks or logging rerun relevant checks/benchmarks. Documentation-only changes do not. Development evidence is from an Apple M3 Max, 128 GiB RAM, 16 logical CPUs. A modest second-machine run is required before claiming support for that hardware class. Live split-screen and network verification remain deferred.

## Current sequence and scope

The tested burst DLL has one completed lighter busy Record retest; finish/review the partial ordinary suite and verify a heavier native pressure/recovery case; complete paired busy comparisons and remaining functional gates; then profile allocations and validate modest hardware. The completed older stationary suite and valid busy stress sample stay in the evidence ledger.

Use practical barriers: native LOS for walls/windows now; defer exact window damage/blocker instrumentation. Future bars policy blocks movement but permits sight, retaining native evidence separately from the override. It is not implemented and does not prove hearing or reachability. Cross-level pet persistence stays “probably no.” Expand dog desires/responses only after this foundation has useful evidence.

Previous plan snapshots are [archived](docs/history/2026-10-04-before-consolidation-WORLD_AWARENESS_PLAN.md).
