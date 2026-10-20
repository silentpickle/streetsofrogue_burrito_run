# Controlled native pressure and recovery

> **What this is:** The plan and outcome for the stress test that floods the game with effects and checks recovery.  
> **Read it if:** You want to know how World Awareness behaves under load.  
> **Status:** Dated plan, October 4, 2026.

The stationary comparison `b7949371` is complete. This next slice adds `make world-pressure`, retaining the fast functional and stationary commands.

1. Reuse the disposable profile, native startup and two functional fixtures. Begin only with capture clean and quality at its normal level 0.
2. Spawn one owned stationary Courier as a deferred-detail witness. Record its lifecycle-core snapshot immediately, then observe ordinary census/detail completion; never seed or refresh the index through test code.
3. Emit at most 32 batches of 64 native Noise objects (2,048 total). Use the audited six-argument native `SpawnNoise` overload with volume 0, type Normal and `dontAttract=true`. Native volume 0 still has radius 1.28; this is an attraction-disabled native effect fixture, not an audible dog/bark experiment. Recycle every owned object immediately via `Noise.DestroyMe`; do not leave an ongoing native effect population or alter residents/player/clock.
4. Stop emission on the first observed quality reduction or writer saturation. Record actual hook costs, quality window evidence, queue pressure and every fixture activation/cleanup. Count only the existing observational hooks in diagnostic cost; exclude the fixture emission/coordinator itself so test work cannot manufacture diagnostic pressure. Never inject timing samples or change quality policy.
5. Keep the actor alive while native detail completes and quality naturally returns to 0. Record periodic detached samples and require at least 30 wall seconds after pressure stops, clean capture and actual ordinary detail. Then remove the owned actor and confirm native/index retirement. Cap recovery at 150 seconds and the whole process at 300 seconds. Pause/world/player/focus/health/combat changes interrupt; no silent retries.
6. Independently report complete lifecycle-core-to-detail evidence, bounded effect identities, native pooling/recycling, observed reduced detail policy and natural recovery. If the cap never causes reduction, return INCONCLUSIVE rather than artificially increasing load or claiming a pass. This checks a controlled lifecycle burst, not paired busy performance or allocation/hardware acceptance.
7. Run build/analyzer/format, C# and report regression checks, then the native command. Preserve failed runs, update this plan/handoff/validation with actual artifacts, and leave dog behavior unchanged.

## Execution

Implemented and native-validated. Run `.runtime/world-harness/20261004-154025-78868/`, session `a9a50717`, build `0.6.0+20261004-214024`, passed all 44 functional/pressure capture checks and exited zero. Preserved DLL SHA256: `adf11b5fd43fba255711e2f38f74d64e03239917cdadba668433d253fa9924db`. All native API inspection used the installed game's assembly; no user setup was needed.

Actual pressure: 10 batches / 640 attraction-disabled native Noise effects, then automatic stop at quality 1 (`frame-budget`). Observed prior-frame diagnostic work was 4.819 ms; the recorded burst hook sum was 50.650 ms with maximum individual hook 1.483 ms. No fixture/coordinator timing was added to diagnostic cost. The reduced policy used nearby actor limit 2 instead of 8. One hundred native instances were actually reused with fresh native IDs/activation keys, and all 640 fixture effects had independent native creation and PoolRecycle evidence plus inactive cleanup reads.

The witness's partial lifecycle-core snapshot at wall 38.351 s became ordinary `detail` metadata by wall 38.631 s with the same key. Natural recovery to quality 0 (`gradual-recovery`) occurred at wall 70.198 s, 31.584 s after emission stopped, restoring actor limit 8. The witness's native/index retirement followed at wall 75.257 s. Player state, foreground focus, level and camera mapping stayed stable. The closed manifest has 4,516 records / 4,112,759 bytes, zero drops/rotation/failure and no observed engine exceptions.

Checks: zero build/analyzer/format warnings/errors, 242 C# assertions and 64 Python report tests. Earlier functional and stationary reports still pass independently. This proves bounded frame-budget reduction/recovery and Noise pooling, not sustained window-budget/backpressure paths, actor pooling, other effect types, cross-level lifecycle, paired busy performance or allocation/hardware acceptance. The quiet scene had at most two nearby actors, so the 8→2→8 policy transition is verified; visible detail omission under a larger crowd is still open. Dog behavior is unchanged. Review the saved `report.md`; reproduce with `make world-pressure`.
