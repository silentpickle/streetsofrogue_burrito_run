# Burrito Run — current handoff

> **What this is:** The handoff written when development paused: the last known runtime state and open threads.  
> **Read it if:** You are resuming development and need to know exactly where it stopped.  
> **Status:** Dated working notes, kept as history. Build IDs, "current" and "next" refer to the date written, not today.

**October 20 publication checkpoint:** behavior work is paused for a platform reassessment.
See [current state](../current-state.md) and [publication verification](../maintainers/publication-check.md).
The runtime and next-action notes below are dated history.

**Current scope — October 12 banana favors:** the user authorized one optional
banana encounter with competing $15 / First Aid Kit rewards on ordinary generated
levels. [04-banana-favors.md](../experiments/04-banana-favors.md) owns this slice, play command, source map
and validation. Phone shipping is shelved; existing services, NPC experiments, dog
and aquarium work are retained. This narrowly reopens encounter placement and
its disposable tests; broader authored levels remain paused. Earlier scope and
installed-build statements below are historical.

Updated October 7, 2026. Read this before rebuilding, installing or launching. This is the operational state; [knowledgebase](knowledgebase.md) explains the project and [validation](../experiments/01-world-awareness/validation.md) holds the evidence.

## Loaded versus prepared

**Banana checkpoint:** installed/native-tested `0.6.0+20261012-183103`,
session `99ef4488`, 94 native gates; 300 C# and 119 Python checks pass.
Normal-mode title-screen session `e057b37b` is open in the disposable profile
`.runtime/banana-playcheck/20261012-123206/profile`. Computer-control input did
not reliably advance its start menu; a human normal-input playcheck is pending.
Close that game before rebuilding/installing. Exact evidence and limits are in
[04-banana-favors.md](../experiments/04-banana-favors.md). The dated records below are historical.

**October 12 NPC tooling:** the user resumed after the quota pause; tooling and first autonomous pickup now pass the native suite (73 checks). Read [desires-handoff.md](../experiments/03-npc/desires-handoff.md) for current evidence and [desires.md](../experiments/03-npc/desires.md) for commands. This is a disabled-by-default, one-civilian experiment; level authoring remains paused.



**Scope override — October 7:** the user paused all level-design and setup work and redirected the project to all NPC interactions. Do not continue the prior hydrant playtest/setup, authored chunks or new fixtures as the next task. Start with the [NPC interaction audit](../experiments/03-npc/interactions-audit.md): inspect existing native rules, especially self-preservation and territory, before selecting behavioral changes. The dated build/session state below is retained as historical evidence, not a new live-state check.

The October 6 readiness and controlled busy checks passed and closed. The user selected food interaction, which passed its native suite. The October 7 normal playtest has closed; the user judged the dog decent and selected **just hydrant interest**. The installed hydrant build passes its isolated native fixture; [hydrant scope and evidence](../experiments/02-dog/hydrant.md) owns this continuation. The [roadmap execution](../experiments/02-dog/road-to-the-dog.md) retains the earlier step 1–4 sequence and exact evidence.

October 7 normal playtest: `c9fef7ba`, build `0.6.0+20261007-051237`, prototype profile `.runtime/profile`, log `.runtime/logs/game-20261007-204617-38861.log`. Dog `1324` spawned Friendly/following player `1332`; snack `714` was discovered through World Awareness, sniffed at full native health (33), and followed by return-to-owner events. Screenshot confirmed the playable level and following-dog panel. The user judged the dog decent; this was a subjective playcheck, not a new isolated acceptance capture. The earlier reported disappearance was the automated food fixture deliberately destroying its owned dog and quitting after validation.

Hydrant validation **PASS: 67 gates**, `238d7320`, build `0.6.0+20261008-052320`, `.runtime/world-harness/20261007-232321-7312/`; SHA256 `89f90828578061f48c3f14229123c8372dd6ebbe2e00ed9344e75534ed798292`. Native marking/Follow, original idle-anchor restoration, unchanged player/dog health and hydrant state, full cooldown/repeat suppression, removal, timeout, release and owned cleanup all pass. Closed clean stream, matching installed binary, zero observed engine exceptions. Build/analyzers/format, 255 C# assertions and 110 Python tests pass. A regular playtest has launched separately: session `74a9f300`, log `.runtime/logs/game-20261007-232752-8650.log`. It remains at the main menu: UI clicks/keys did not advance it and a Dock focus-control call timed out. No dog has spawned in this session yet. Start a single-player Quick Start, then F6 and Place one test hydrant. This regular run has no automatic cleanup/quit. Do not rebuild/install or launch another game over it.

| State | Identity | Meaning |
|---|---|---|
| Installed/native-validated hydrant | `0.6.0+20261008-052320`, `238d7320` | 67 gates: base plus bounded hydrant cases; closed capture |
| Earlier native-validated food | `0.6.0+20261007-051237`, `2cf1bc70` | 66 gates: base plus five food cases and retained one-per-level limit; closed capture |
| Prior final readiness ownership guard | `0.6.0+20261007-035337`, `541591f7` | 29 base gates; preserved separately |
| Validated controlled busy comparison | `0.6.0+20261007-034119`, `50f10862` | Nine trials, 67 gates and numeric targets; closed capture |
| Validated advanced readiness | `0.6.0+20261007-033333`, `c0f83161` | 49 gates: sustained window/crowded detail/backpressure and natural recovery; closed capture |
| Validated native correctness slice | `0.6.0+20261007-033919`, `7a7fa07d` | 45 gates: ordinary food/hydrant/door metadata, API pause/resume, owned retirement and native scope reset; closed capture |
| Prior native pressure checkpoint | `0.6.0+20261004-214024`, `a9a50717` | 44 functional/burst/detail/recovery/Noise-pooling gates; preserved separately |

All commands create fresh disposable profiles and preserve exact binary/hash receipts. Original saves and the existing prototype profile have not been reset. The repository was clean on entry; current uncommitted edits implement readiness extensions, the first food response and documentation. Food behavior now uses World Awareness and native eating.

The older burst checkpoint/partial ordinary suite remain historical evidence, not current live state. Their identities, hashes and reports are retained below and in the catalog. Build checks can create newer output timestamps without installing them.

Earlier native-validated food build: `0.6.0+20261007-051237`, `2cf1bc70`, 66 gates, SHA256 `dbcd7d81946223fdb987ba73a93dca826b6321448852b5c42717450af0437d6a`; `.runtime/world-harness/20261006-231238-67676/`. A healthy dog leaves the snack; an injured dog consumes one native inventory item and heals 18→33, then executes native `GoalFollow` with the player as owner. Stack rejection, missing target, release, cooldown, unchanged player inventory/money/health, exact activation retirement and rejection of another pet after cleanup pass. Closed clean stream, matching installed/saved DLL, no observed engine exceptions. Build/analyzer/format, 250 C# assertions and 96 Python report tests pass. Source/documentation remain uncommitted.

`make world-food` handles setup and evidence automatically. The fixture applies bounded nonlethal injury only to its owned dog; the behavior itself never writes health. [Food plan and evidence](../experiments/02-dog/food.md) records both failures and the arrival-sampling correction. Bring the game to the front during startup, then keep its focus stable while watching. The remaining human boundary is whether the small encounter reads clearly and feels useful. Eating at full health is outside native food rules; hunger simulation, retrieval, hydrant and movement recovery remain separate choices.

## October 6 readiness and recovery correction

`make world-advanced` passed 49 independently recomputed gates. Actual two-window pressure reduced quality after 848 attraction-disabled Noise lifecycles; six nearby actor details reduced to two and naturally restored to six after 30.200 seconds. A separate labeled writer fault released on its first real trigger (at most two seconds) reached 3,440,856 queued bytes after 120 emissions, then restored naturally after 30.067 seconds. Clean closed capture, stable background focus, no loss/rotation/error. Run `.runtime/world-harness/20261006-213334-31981/`.

`make world-native` passed 45 gates. Ordinary Food-category Item, FireHydrant and Door detail; three door metadata states and same-frame state invariance; API pause/input-guard/resume and preserved sampled player state; native retirement and index removal; native reload from scope 3 to 4 with all 416 old live world keys absent. Courier/item deactivate; hydrant/door remain active GameObjects but are natively destroyed. Run `.runtime/world-harness/20261006-213920-33897/`.

Native pilots exposed recovery starvation from isolated frames over 2 ms. Recovery now resets for pressured windows and actual reductions/saturation, using the unchanged budget thresholds and 30-second duration. Within-budget isolated outliers no longer prevent restoration indefinitely. Three pressure pilots and one overly strict cleanup pilot stay saved as non-passing evidence in the roadmap plan.

These slices do not prove physical key/mouse isolation, door collider traversal, actual floor progression, actor pooling, combat/hazard equivalence, allocations or other hardware. Those remain release gates. The user selected food; native proof is in the food plan and a short subjective playcheck remains; the controlled busy comparison is complete.

## Completed controlled busy comparison

`make world-busy` passed 67 capture/comparability gates and both numeric comparisons in `50f10862` / `0.6.0+20261007-034119`, `.runtime/world-harness/20261006-214120-34474/`. Nine valid trials: three each Off/Inspect/Record, 10-second warm-up and 60-second measurement. Each had 300 pulses / 1,200 immediate Noise lifecycles / 300 immediate projectile lifecycles, 18 movement rounds and actual actor travel; all four native identities matched at every boundary. Median trial frame p95: Off 17.407 ms, Inspect 17.493 ms (+0.49%), Record 17.452 ms (+0.26%). Active diagnostics mean 0.676–0.750 ms / p95 1.427–1.644 ms; quality 0 throughout, zero >50 ms frames, drops/errors/retention loss. Foreground focus, player/health/combat/world/camera guards stayed stable. Closed 30,230 records / 25,095,155 bytes. SHA256 `6ffd4f2b32437e0dc8492e25c978803151289697099b08658dd12b3f943d9a69`. This certifies the controlled motion/transient-lifecycle scenario on this Mac, not a combat/hazard encounter or other hardware.

## Repeatable native harness checkpoint

`make world-harness` builds/installs, copies only settings/unlocks (including separate `TechSettings.dat`) into a fresh profile, follows native Quick Start/CharacterSelect, waits for a stable loaded player/world, and runs two native Courier fixtures. Native writes are confined to `WorldFixture`; inspection reads the ordinary awareness pipeline. Recognized native freezes are retained; true pauses/world or player changes interrupt the run. Each result includes actual clock/load/player context. A 180-second process watchdog handles an engine loop that stops responding.

Two fully automatic launches passed 29 offline checks each: `cff3e835` / `0.6.0+20261004-205334` in `.runtime/world-harness/20261004-145335-71846/` and `ade52b5a` / `0.6.0+20261004-205511` in `.runtime/world-harness/20261004-145512-72124/`. Each captured two fixture-owned actors with fresh keys, detailed native metadata, four query memberships, perception/path/tile evidence, same-frame sampled player/actor state invariance and actual retirement. Collection took about 34 wall seconds from harness start to completion in each run. Closed streams have zero drops/rotation/failure; no engine exceptions were found. Native default `DoNothing` produced actual `GoalIdle`, zero velocity/no path in these samples. Actual pool reuse was not observed. Reports/DLLs/receipts/logs are preserved together.

Startup attempts, a real 120-second startup timeout and a world/clock interruption are retained as incomplete evidence in the earlier directories. The first complete capture's initially overstrict `agentList` provenance check is preserved in `report-before-provenance-correction.md`; ordinary detail refreshes legitimately label snapshots `detail`. The corrected reader accepts either ordinary source and rejects test-seeded discovery. One intermediate launcher report step was interrupted by editing the running shell script; its report was regenerated from preserved logs. No dog behavior was added. See [workflow and limits](../experiments/01-world-awareness/harness.md).

## Automated stationary performance checkpoint

`make world-performance` adds nine automatic trials after the two functional fixtures retire: Off / Inspect / Record three times, each 10 seconds warm-up + 60 seconds measurement. Run `b7949371` passed 60 capture checks and both numeric comparisons. Median trial frame p95 was 16.667 ms for all modes, active diagnostic mean 0.572–0.608 ms / p95 1.340–1.365 ms, zero >50 ms hitches/loss/rotation, quality 0. Player state and background focus stayed stable; 31 actors at each boundary; camera mapping matched. Same build/level, no manual setup or clicks. The report, DLL/receipt and clean closed logs are together in `.runtime/world-harness/20261004-151707-75949/`. This establishes stationary background numeric acceptance on this Mac, not foreground/busy load or allocations. Two earlier failed probes remain saved separately. See [plan and execution](../experiments/01-world-awareness/performance-plan.md).

## Controlled native pressure checkpoint

`make world-pressure` passed 44 independently computed functional/pressure checks in `a9a50717` / `0.6.0+20261004-214024`. Ten batches / 640 attraction-disabled Noise fixtures triggered quality 0→1 (`frame-budget`), then natural recovery to 0 after 31.584 seconds. Existing inspector hook work, excluding fixture emission, measured 4.819 ms in the observed prior frame; aggregate burst hook work was 50.650 ms. The owned Courier progressed from lifecycle-core to ordinary detailed metadata and retired after recovery. One hundred native Noise instances were actually reused with fresh IDs/keys; every effect was recycled and read inactive. Nearby detail policy changed 8→2→8; the quiet scene only had up to two nearby actors, so visible omission under a larger crowd is not proven. Foreground player/focus/level/camera context stayed stable. Closed capture: 4,516 records / 4,112,759 bytes, no loss/rotation/failure or observed engine exception. Saved `.runtime/world-harness/20261004-154025-78868/report.md`; [pressure plan](../experiments/01-world-awareness/pressure-plan.md) records implementation and limits. At that October 4 checkpoint, sustained window-budget/backpressure, actor pooling, other effects, cross-level histories and paired busy comparisons remained open; the October 6 status above supersedes it. Dog behavior was unchanged at that earlier checkpoint.

## Earlier corrected-build result

`e5a30132`, busy Record result 6582: valid 60.0173 seconds / 3584 frames, six native freezes, diagnostic mean/p95 0.738/1.495 ms, frame p95 17.477 ms, two >50 ms frames. No observed errors/drops. 99 Bullet, 25 Noise, one Agent creation observations including warm-up; 125 lifecycle-core records mark details pending/viewport unknown. Hook peak 1.345 ms (one measured >1 ms hook call); quality remained 0 with captured overhead windows within targets. This is a lighter/different encounter than the prior stress sample: it does not verify native pressure reduction or causal speed improvement. Saved `.runtime/benchmark-e5a30132-busy-record.{md,json}`. Ordinary suite stopped: six valid trials (two each Off/Inspect/Record), zero measured >50 ms frames or losses in those valid trials. Seventh Off attempt invalid at 42.6606 measured seconds on native mainScale=0/timeScale=0, no recognized freeze/inspector/phone pause. Pause initiator unknown. Partial medians: frame p95 Off 17.232 ms, Inspect 17.363 (+0.76%), Record 17.372 (+0.81%); active diagnostic mean about 0.675/0.691 ms, p95 about 1.401/1.403 ms. Five valid-trial completion records were unfocused, one focused; preserve that setup limitation. Saved `.runtime/benchmark-e5a30132-ordinary-partial.{md,json}`; not three-per-mode acceptance. Preserve the six valid trials and invalid attempt; only one valid Off, Inspect and Record trial remains.


## What we know

- Native F5 world/legacy capture works after the enum repair. Session `bb128da0` ended with zero capture errors/drops.
- Selected-target tests separately confirmed range, cone, viewport and native geometry LOS: one plant was blocked behind a wall and clear from another position. A semi-intact window case was blocked; exact blocker/damage was not captured. These are examples, not universal visibility proof.
- Stationary ordinary suite `8fc5165e`: three valid trials each Off/Inspect/Record; frame p95 changes +1.59% / -0.19%; active diagnostic mean about 0.66 ms and p95 about 1.46 ms; zero measured frames >50 ms or trial losses. That session subsequently ended with clean capture integrity. Evidence applies to its older build and this development host.
- Busy Record `5aa527ab`: valid 60.0163 measured seconds, 3297 frames, nine native freeze frames retained. Diagnostic mean/p95 0.867/1.704 ms; frame p95 33.345 ms and eight frames >50 ms. Short windows exceeded budget while quality stayed at 0. This prompted the prepared correction; it is one stress sample, not paired busy acceptance. Session subsequently closed with terminal errors/drops zero. Retention retired 38 early segments: final report exits 2 for missing/rotated evidence, so full-session completeness is not claimed. The separately saved trial remains preserved.

Latest saved source checks: build/analyzers/format zero warnings/errors, 244 C# assertions and 82 Python report tests passed. Synthetic burst sequences validate reduction logic, not native performance. See [catalog](../experiments/01-world-awareness/catalog.md) for artifacts.

## Next work, in order

The nine-trial controlled busy comparison, native food suite and bounded hydrant suite are complete. The user selected just hydrant interest after the food playcheck. Next is a short subjective hydrant playcheck in the normal playable run, using F6 and Place one test hydrant if needed. This run stays open; automatic fixtures remove their test dog and quit. Keep each captured build separately attributed.

Hydrant interest now uses ordinary discovery, one native detour, a bounded cosmetic pose and actual native Follow. Barking/NPC reactions, retrieval and general movement recovery were not selected. Do not expand those as part of the hydrant playcheck.

Physical input paths, actor pooling, broader effects/barriers/species, actual elevator histories and combat/hazard comparisons remain release gates. Allocation profiling and a modest second machine are needed for broader performance claims. Detailed window/collider matrices remain deferred by user choice. Future bars policy is planned, not implemented; cross-level pet persistence remains “probably no.”

## Commands and collection

```bash
bash scripts/check.sh                 # build/analyzers/format; does not install
make world-harness                    # automatic targeted native fixtures; game must be closed
make world-performance                # fixtures + nine automatic stationary trials; about 11 minutes
make world-pressure                   # capped native lifecycle burst, pooling/detail/recovery report
make world-advanced                   # sustained windows, crowded detail, bounded writer fault
make world-native                     # native classification, API pause, retirement and scope reset
make world-busy                       # nine automatic controlled native motion/effect trials
bash scripts/test-routing.sh
python3 -m unittest discover -s tests -p 'test_*.py'
git diff --check
python3 scripts/report-world.py .runtime/logs/world-SESSION-????.jsonl --output .runtime/world-SESSION-report.md
```

`bash scripts/build.sh` installs: use only after the game is closed. `bash scripts/launch.sh --burrito-test` launches the separate test profile. Do not delete/reset profiles. F10 opens paused inspection and benchmark controls; F5 captures an incident. Prefer the F10 button over F11 if macOS intercepts the key. A trial takes 10 seconds warm-up plus 60 seconds measurement; the nine-trial sequence takes about 10.5 minutes. The harness completes and reports automatically; preserved evidence can be read again without launching. Keep builds/tests and manual world input out of a collecting comparison. A click/start notice is not completion; capture loss or missing closure prevents completeness claims.

Earlier handoffs are preserved in [dated history](2026-10-04-before-consolidation-PAUSE_HANDOFF.md); their “current” labels describe their original date, not today's state.

October 6 requested visible rerun: `2cf1bc70` / `0.6.0+20261007-051237` passed 66 gates in `.runtime/world-harness/20261006-231238-67676/`. Source behavior unchanged; installed DLL matches the saved receipt. A preceding attempt in `.runtime/world-harness/20261006-231020-66891/` remains INTERRUPTED after focus changed (healthy sniff, eating/Follow and removal had completed). The rerun started with the game focused and closed cleanly. Original implementation checkpoint `249a0ae7` / `0.6.0+20261007-044122` remains preserved separately.
