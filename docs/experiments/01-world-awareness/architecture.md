# World awareness architecture

> **What this is:** How World Awareness keeps a live index of everything on the level without slowing the game.  
> **Read it if:** You want the design rules (identity, threading, budgets) behind the world index.  
> **Status:** Reference. Accurate for the source at publication (October 20, 2026).

Native collections and observational lifecycle hooks feed a main-thread registry and spatial hash. Detached query, camera, perception and navigation snapshots feed the panel, overlay and capture stream. A single bounded background worker serializes/writes world and existing bark/event traces; no Unity object crosses that boundary. No native movement, door, relationship or inventory command is invoked by awareness.

## Contracts and identity

`WorldEntitySnapshot`, `WorldQuery`, `WorldQueryResult`, `CameraViewSnapshot`, `PerceptionEvidence`, `NavigationEvidence` are internal detached interfaces. Published values are immutable by ownership. Consumers cannot retain native references. Queries use horizontal world distance, native tile size 0.64, stable distance/key ordering, category/tag/search filters and pagination. Spatial hashing uses 6.4-unit buckets; large queries enumerate entries instead of huge empty grids.

Entity identity includes level generation, Unity instance, native ID and activation generation. Registry scans evict handles missing from two consecutive census cycles and exclude null handles and record inactive/dead/destroyed flags; runtime IDs are revalidated before detailed reads. Scan completion is a rolling census, not an atomic world snapshot. Collection mutation, absence and stale observations must remain visible in coverage metadata. Level/controller/scene/nest/floor/load-clock changes clear handles and history.

## Native sources

Census sources: agentList, playerAgentList, deadAgentList, itemList, objectRealList, bulletList, noiseList, explosionList, firesList, gasesList, watersList, dangerList and dangerListNoCollider. Duplicate entities deduplicate by identity. Inventory categories/content counts are metadata; inventories are not separate loose world targets. Helpers are excluded by default. Semantic Food uses native Food category or explicit HotFud/BurritoRun_Food; Door and FireHydrant use exact native types. Every classification retains its source.

SpawnerMain return-value postfixes observe transient creation. Retirement hooks record native retirement **requests**, which are not proof that destruction succeeded. Sampled collection membership and active state supplement them. Postfix observers watch pool deactivation after its native state update. Actor spawns are included. Nested spawn wrappers deduplicate the same active instance/native ID. Hooks never substitute native return values. Capture does not claim coverage of unregistered decorative particles or future unseen spawn paths.

## Visibility and navigation

Camera discovery uses native player agentCamera/originalCamera mappings, deduplicates shared cameras, and stores rect/player associations. Viewport-anchor membership is separate from renderer-bounds intersection. Native onCamera includes overscan and potentially other players; it is metadata only. Renderer enabled/layer/bounds evidence is not final pixel visibility.

Perception calls audited Movement.HasLOSPosition360 using the native efficient mask, and independently reports angle versus LOSCone and distance versus LOSRange. These geometric tests are not the full native perception decision; hearing and final lighting/occlusion remain unknown. No universal visible boolean conflates these results.

Navigation reads current root/subgoals, target, following distance, path flags, waypoint index/positions, velocity and door-blocking fields. Nearby doors expose state/building context via snapshots/selected tile data. A local walkable cell, candidate door or native path does not prove arrival. Low displacement is a measured state, not a stuck verdict.

## Scheduling and capture

The burst source described here is now installed; consult the [pause handoff](../../history/pause-handoff.md) for exact runtime identity and native validation status. Default quick-test mode is Record. Command-line --awareness-off and --awareness-inspect enable paired tests. Baseline mode still writes low-rate performance/session metadata. F10 opens paused inspection with an Off/Inspect/Record benchmark trial control (10-second warm-up, 60-second measurement); F5 exports bounded history and progressive census pages. UI controls use existing Services pause/input isolation. Automatic behaviors are not changed. Warm-up/measurement use unscaled time; explicitly evidenced native freeze frames remain in frame/hitch statistics. Real or unknown pauses and clock/level/mode discontinuities invalidate trials rather than trimming samples.

Census runs cooperatively under a 1 ms frame budget with 0.2-game-second target interval. Selected details and progressive perception have separate timestamps. Maximum 32 perception pairs per 0.2 seconds, with selected target priority. Quality reduces off-target perception, nearby tracking, and refresh frequency (5/2/1 Hz), then suspends optional sample writes; recovery takes 30 seconds of good budget observations per step. Individual native calls and explicit paused scans can exceed the cooperative budget and must be measured separately.

A single CaptureWorker FIFO limits queue to 2048 records and conservative 4 MiB payload accounting, reserving 64 records/256 KiB for essential events. Detached payload measurement rejects unknown reference types. World history has independent 101-frame and 8 MiB conservative payload caps, with 20-game-second expiry and explicit evictions. Incident history is queued progressively across frames. Legacy bark history has a 1 MiB cap and exports at most eight frames per history/incident record with offsets. Legacy bark/event text uses buffered worker-owned streams with half-second idle flushes and shutdown disposal. Event and bark files have 8 MiB caps. No live drain/wait is permitted. World trace segments are 8 MiB, retaining 16 segments (128 MiB/session), with manifest and rotation counts. World stream schema 1 is independent of bark schema 1. JSON serialization/file I/O run on the worker. Abrupt exit may lose queued data; missing terminal/manifest closure is incomplete evidence.

Pure queries can replay over saved snapshots. Offline reports cannot recreate missing native LOS, state changes or routes. Resource and capture failures are reported rather than converted into false/zero/pass.

## Burst policy refinement — installed; native sustained pressure/recovery pending

Lifecycle snapshots retain identity and short-lived effect values immediately, mark detailed metadata pending and camera evidence unknown, and defer detailed reads/projection to cooperative scans. Pool lookup participates in measured hook costs. This does not expand lifecycle coverage or allow completeness claims after loss. Optional quality reduction now also observes one-second mean (>1ms) and >2ms-frame fraction (>5%), reducing after two pressured windows, in addition to consecutive overruns/backpressure. Recovery requires observed headroom; paused/unobserved gaps do not count. These source changes are installed and boot confirmed; one lighter populated busy Record sample has cost/coverage evidence; sustained native pressure/recovery remains pending.
