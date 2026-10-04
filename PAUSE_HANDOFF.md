# Burrito Run — current handoff

Updated October 4, 2026. Read this before rebuilding, installing or launching. This is the operational state; [knowledgebase](KNOWLEDGEBASE.md) explains the project and [validation](WORLD_AWARENESS_VALIDATION.md) holds the evidence.

## Loaded versus prepared

| State | Identity | Meaning |
|---|---|---|
| Latest installed/native-validated | `0.6.0+20261004-214024`, session `a9a50717` | Functional fixtures + controlled pressure/detail/recovery/Noise pooling passed; game is closed |
| Earlier benchmark-loaded | `0.6.0+20261004-170516`, session `e5a30132` | Burst correction; one busy Record retest and six valid ordinary trials retained |
| Earlier tested burst checkpoint | `.runtime/checkpoints/0.6.0-burst-fix-prepared/` | Original tested DLL retained; latest install also includes the native harness |

Latest installed DLL SHA256 is `adf11b5fd43fba255711e2f38f74d64e03239917cdadba668433d253fa9924db`, matching the preserved DLL/receipt in `.runtime/world-harness/20261004-154025-78868/`. Build checks may create a newer timestamped output DLL without installing it. Working tree contains substantial uncommitted work; preserve it. Neither the existing prototype profile nor original saves were reset.

The earlier burst checkpoint SHA256 was `9f57cb6f7e7164e454a2251c01b40c705a4608e359ad33ccef230d615ac759b4`. Its busy retest and six valid ordinary trials remain saved. The seventh Off attempt paused and aborted that suite; one valid trial per mode remains. Earlier main log: `.runtime/logs/game-20261004-121500-40400.log`. Earlier installed DLLs/receipt: `.runtime/checkpoints/0.6.0-before-burst-fix/`.

## Repeatable native harness checkpoint

`make world-harness` builds/installs, copies only settings/unlocks (including separate `TechSettings.dat`) into a fresh profile, follows native Quick Start/CharacterSelect, waits for a stable loaded player/world, and runs two native Courier fixtures. Native writes are confined to `WorldFixture`; inspection reads the ordinary awareness pipeline. Recognized native freezes are retained; true pauses/world or player changes interrupt the run. Each result includes actual clock/load/player context. A 180-second process watchdog handles an engine loop that stops responding.

Two fully automatic launches passed 29 offline checks each: `cff3e835` / `0.6.0+20261004-205334` in `.runtime/world-harness/20261004-145335-71846/` and `ade52b5a` / `0.6.0+20261004-205511` in `.runtime/world-harness/20261004-145512-72124/`. Each captured two fixture-owned actors with fresh keys, detailed native metadata, four query memberships, perception/path/tile evidence, same-frame sampled player/actor state invariance and actual retirement. Collection took about 34 wall seconds from harness start to completion in each run. Closed streams have zero drops/rotation/failure; no engine exceptions were found. Native default `DoNothing` produced actual `GoalIdle`, zero velocity/no path in these samples. Actual pool reuse was not observed. Reports/DLLs/receipts/logs are preserved together.

Startup attempts, a real 120-second startup timeout and a world/clock interruption are retained as incomplete evidence in the earlier directories. The first complete capture's initially overstrict `agentList` provenance check is preserved in `report-before-provenance-correction.md`; ordinary detail refreshes legitimately label snapshots `detail`. The corrected reader accepts either ordinary source and rejects test-seeded discovery. One intermediate launcher report step was interrupted by editing the running shell script; its report was regenerated from preserved logs. No dog behavior was added. See [workflow and limits](WORLD_AWARENESS_HARNESS.md).

## Automated stationary performance checkpoint

`make world-performance` adds nine automatic trials after the two functional fixtures retire: Off / Inspect / Record three times, each 10 seconds warm-up + 60 seconds measurement. Run `b7949371` passed 60 capture checks and both numeric comparisons. Median trial frame p95 was 16.667 ms for all modes, active diagnostic mean 0.572–0.608 ms / p95 1.340–1.365 ms, zero >50 ms hitches/loss/rotation, quality 0. Player state and background focus stayed stable; 31 actors at each boundary; camera mapping matched. Same build/level, no manual setup or clicks. The report, DLL/receipt and clean closed logs are together in `.runtime/world-harness/20261004-151707-75949/`. This establishes stationary background numeric acceptance on this Mac, not foreground/busy load or allocations. Two earlier failed probes remain saved separately. See [plan and execution](WORLD_AWARENESS_PERFORMANCE_PLAN.md).

## Controlled native pressure checkpoint

`make world-pressure` passed 44 independently computed functional/pressure checks in `a9a50717` / `0.6.0+20261004-214024`. Ten batches / 640 attraction-disabled Noise fixtures triggered quality 0→1 (`frame-budget`), then natural recovery to 0 after 31.584 seconds. Existing inspector hook work, excluding fixture emission, measured 4.819 ms in the observed prior frame; aggregate burst hook work was 50.650 ms. The owned Courier progressed from lifecycle-core to ordinary detailed metadata and retired after recovery. One hundred native Noise instances were actually reused with fresh IDs/keys; every effect was recycled and read inactive. Nearby detail policy changed 8→2→8; the quiet scene only had up to two nearby actors, so visible omission under a larger crowd is not proven. Foreground player/focus/level/camera context stayed stable. Closed capture: 4,516 records / 4,112,759 bytes, no loss/rotation/failure or observed engine exception. Saved `.runtime/world-harness/20261004-154025-78868/report.md`; [pressure plan](WORLD_AWARENESS_PRESSURE_PLAN.md) records implementation and limits. Sustained window-budget/backpressure, actor pooling, other effects, cross-level histories and paired busy comparisons remain open. Dog behavior is unchanged.

## Earlier corrected-build result

`e5a30132`, busy Record result 6582: valid 60.0173 seconds / 3584 frames, six native freezes, diagnostic mean/p95 0.738/1.495 ms, frame p95 17.477 ms, two >50 ms frames. No observed errors/drops. 99 Bullet, 25 Noise, one Agent creation observations including warm-up; 125 lifecycle-core records mark details pending/viewport unknown. Hook peak 1.345 ms (one measured >1 ms hook call); quality remained 0 with captured overhead windows within targets. This is a lighter/different encounter than the prior stress sample: it does not verify native pressure reduction or causal speed improvement. Saved `.runtime/benchmark-e5a30132-busy-record.{md,json}`. Ordinary suite stopped: six valid trials (two each Off/Inspect/Record), zero measured >50 ms frames or losses in those valid trials. Seventh Off attempt invalid at 42.6606 measured seconds on native mainScale=0/timeScale=0, no recognized freeze/inspector/phone pause. Pause initiator unknown. Partial medians: frame p95 Off 17.232 ms, Inspect 17.363 (+0.76%), Record 17.372 (+0.81%); active diagnostic mean about 0.675/0.691 ms, p95 about 1.401/1.403 ms. Five valid-trial completion records were unfocused, one focused; preserve that setup limitation. Saved `.runtime/benchmark-e5a30132-ordinary-partial.{md,json}`; not three-per-mode acceptance. Preserve the six valid trials and invalid attempt; only one valid Off, Inspect and Record trial remains.


## What we know

- Native F5 world/legacy capture works after the enum repair. Session `bb128da0` ended with zero capture errors/drops.
- Selected-target tests separately confirmed range, cone, viewport and native geometry LOS: one plant was blocked behind a wall and clear from another position. A semi-intact window case was blocked; exact blocker/damage was not captured. These are examples, not universal visibility proof.
- Stationary ordinary suite `8fc5165e`: three valid trials each Off/Inspect/Record; frame p95 changes +1.59% / -0.19%; active diagnostic mean about 0.66 ms and p95 about 1.46 ms; zero measured frames >50 ms or trial losses. That session subsequently ended with clean capture integrity. Evidence applies to its older build and this development host.
- Busy Record `5aa527ab`: valid 60.0163 measured seconds, 3297 frames, nine native freeze frames retained. Diagnostic mean/p95 0.867/1.704 ms; frame p95 33.345 ms and eight frames >50 ms. Short windows exceeded budget while quality stayed at 0. This prompted the prepared correction; it is one stress sample, not paired busy acceptance. Session subsequently closed with terminal errors/drops zero. Retention retired 38 early segments: final report exits 2 for missing/rotated evidence, so full-session completeness is not claimed. The separately saved trial remains preserved.

Latest saved source checks: build/analyzers/format zero warnings/errors, 242 C# assertions and 64 Python report tests passed. Synthetic burst sequences validate reduction logic, not native performance. See [catalog](WORLD_AWARENESS_CHECKPOINT.md) for artifacts.

## Next work, in order

The earlier harness build `b7949371` has a complete automated stationary background comparison; the latest pressure build `a9a50717` independently passed its bounded pressure slice. Evidence stays attributed to each build. The six earlier partial ordinary trials belong to a different burst build and remain separate; completing those would require its preserved DLL and comparable setup. Do not combine them or ask the user to repeat a manual suite for current-build acceptance.

1. The bounded frame-budget pressure/recovery and deferred-detail case is complete. Extend only the remaining native sustained window-budget/backpressure or crowded-detail case as needed; retain automatic setup/evidence and continuous scenario guards.
2. Collect comparable busy Off/Inspect/Record measurements, three warmed 60-second trials per mode. Investigate recurring hitches; keep the previous stress sample and lighter Record sample as separate historical evidence.
3. Finish the remaining native functional gates in [requirements](WORLD_AWARENESS_REQUIREMENTS.md), including lifecycle transitions and input isolation, before relevant dog expansion.
4. Complete allocation profiling and modest second-machine validation before making broader performance claims.

Do not request another old-build busy retry or discard the valid stress sample. Preserve the completed stationary suite as evidence for its build. Windows/collider matrices are deferred. Future bars policy is movement-blocking and sight-permitting, but is **not implemented**. Dog food/hydrant decisions and route recovery are separate follow-ons; pet persistence across levels remains “probably no.”

## Commands and collection

```bash
bash scripts/check.sh                 # build/analyzers/format; does not install
make world-harness                    # automatic targeted native fixtures; game must be closed
make world-performance                # fixtures + nine automatic stationary trials; about 11 minutes
make world-pressure                   # capped native lifecycle burst, pooling/detail/recovery report
bash scripts/test-routing.sh
python3 -m unittest discover -s tests -p 'test_*report.py'
git diff --check
python3 scripts/report-world.py .runtime/logs/world-SESSION-????.jsonl --output .runtime/world-SESSION-report.md
```

`bash scripts/build.sh` installs: use only after the game is closed. `bash scripts/launch.sh --burrito-test` launches the separate test profile. Do not delete/reset profiles. F10 opens paused inspection and benchmark controls; F5 captures an incident. Prefer the F10 button over F11 if macOS intercepts the key. A trial takes 10 seconds warm-up plus 60 seconds measurement; the nine-trial sequence takes about 10.5 minutes. Start once, finish the chat turn, then read completed local evidence when the user returns. Avoid long active polling. A click/start notice is not completion; capture loss or missing closure prevents completeness claims.

Earlier handoffs are preserved in [dated history](docs/history/2026-10-04-before-consolidation-PAUSE_HANDOFF.md); their “current” labels describe their original date, not today's state.
