# Small native World Awareness harness

> **What this is:** How the automated in-game test harness works and how to run it.  
> **Read it if:** You want to run `make world-harness` or understand what it checks.  
> **Status:** Reference. Accurate for the source at publication (October 20, 2026).

Close Streets of Rogue, then run from this repository:

```bash
make world-harness
```

For the functional fixtures followed by an automatic stationary performance comparison:

```bash
make world-performance
```

This takes about 11 minutes plus startup and needs no inspector clicks. After both fixtures retire, it runs Off / Inspect / Record three times on the same generated level and build, with 10 seconds of warm-up and 60 seconds of measurement per trial. Player movement over 0.05 units, health changes, combat, focus changes, pauses or world changes interrupt the run. Residents continue simulating; population counts and camera mapping are recorded at trial boundaries. Camera mapping stability is checked at boundaries, whereas player/focus guards run every frame. The common coordinator cost is included in diagnostic timing. Stable background focus is permitted and recorded; background captures do not establish foreground performance.

The reporter requires nine complete ordered trials, checks native summary agreement and context, then compares the median trial frame p95 against Off. Numeric acceptance requires Off below 33.333 ms, active modes within 5% of Off and below 33.333 ms, and every active trial below 1 ms diagnostic mean / 2 ms diagnostic p95. `NEEDS INVESTIGATION` means complete evidence missed a numeric target; `INCOMPLETE/FAIL` means the capture or scenario cannot support the comparison. Both exit nonzero. The performance watchdog is 900 seconds. Older partial benchmark builds are retained separately. See [the implementation plan](performance-plan.md).

The command builds/installs the plugin, copies settings and unlocks into a new isolated profile, starts single-player through the native menus, and runs two fresh fixtures. No NPC hunting, dog positioning, incident key or inspector clicks are needed. Each fixture owns one native Courier, chooses one of at most 32 nearby safe, walkable, unoccupied, clear-LOS positions and forces `DoNothing` as explicit test setup. The base command never creates a dog or changes resident goals/factions/positions. The explicit `--food` extension adds one owned dog and food fixtures. Geometry and native AI remain the game's own; this is controlled setup on a generated level, not a deterministic authored room.

The harness copies the existing test profile's separate `TechSettings.dat` to avoid first-run display setup. It waits for native menu initialization, chooses Quick Start and Soldier, confirms start, then accepts the game's final `CharacterSelect` choice before waiting for final level loading. This installed build disables the menu's parent object when play resumes; starting from Home Base requires opening the native main menu first. Collection runs in the background only for this harness process. No persistent application setting is changed in either existing profile.

`WorldFixture.cs` contains the native setup/removal calls. `WorldHarness.cs` schedules phases and records terminal reasons. `WorldAwarenessHarness.cs` only reads the existing registry and native evidence; it cannot insert the fixture into the index. Normal lifecycle hooks/census must discover it and complete deferred metadata. Cleanup checks the fixture's native instance/UID before native removal; unrelated actors are never reset or deleted.

For each repeat, saved evidence includes:

- Native/indexed identity, activation, collection, timestamps, position and actual actor fields.
- Within, band, outside and whole-level query membership for that actor.
- Numeric native geometric LOS, cone/range, camera, navigation and selected-tile evidence.
- Player/actor state immediately before and after the same-frame inspection reads.
- Owned cleanup request followed by actual native inactivity and absence from active index results.

The offline report independently checks those numbers, order, identity freshness, capture loss and closure. `COMPLETE` is a collector status, not a pass. A changed native state, stale/partial snapshot, failed query, missing retirement or interrupted run cannot pass. State invariance covers the recorded fields in one synchronous read sequence; it does not prove every inspector UI/input path or all internal native fields are immutable. LOS remains geometric evidence, not hearing, native recognition, reachable routes or final rendered pixels.

Every command creates `.runtime/world-harness/TIMESTAMP-PID/` with its own `profile/`, engine/plugin/event/world logs, build receipt and preserved DLL. `report.md` gives the short result, `report.json` the computed checks, and `world-report.md` the general capture summary. The command exits zero only when the native report passes and the launch exits cleanly. Re-read any saved run without launching:

```bash
python3 scripts/report-world-harness.py .runtime/world-harness/TIMESTAMP-PID
```

The collector allows 120 wall seconds for startup and 15 seconds per discovery/retirement phase. It records timeouts, world/player/clock changes, actor loss, exceptions and shutdown as incomplete results. A separate 180-second process watchdog handles a stopped engine loop. Failed captures and profiles stay saved; rerunning always creates a fresh profile and fixture. Original saves and the existing `.runtime/profile` are retained.

This is the first native functional slice. It does not complete all WA-01–WA-14 requirements. Pool reuse is only verified when the native instance is actually reused; distinct keys alone do not establish that. Multi-level histories, short-lived effects, door variations, full input isolation, allocation/performance comparisons and modest hardware remain separate gates. Dog behavior stays unchanged until the relevant World Awareness gates are established.

## Validation

The optional pressure slice runs with `make world-pressure`. It repeats the two functional fixtures, requires a clean normal-quality baseline, then keeps one owned Courier as a lifecycle-core-to-detail witness. At most 32 batches of 64 native Noise effects are spawned with attraction disabled and immediately recycled. Existing inspector hooks supply the measured cost; the fixture's emission loop is excluded from diagnostic accounting. Emission stops on reduction, saturation or the cap. Native player/world/clock/focus guards stay active. Recovery is observed naturally for at least 30 seconds, with a 150-second recovery limit and 300-second process watchdog. A cap that never triggers reduction is inconclusive. The report requires actual activation/recycling, fresh native pooling IDs/keys, ordinary completed actor detail, reduced actor-detail limits and return to normal quality before owned cleanup. This is a lifecycle burst check, not a busy Off/Inspect/Record comparison. See [pressure plan](pressure-plan.md).

Native validation results are recorded in [validation.md](validation.md) and [pause-handoff.md](../../history/pause-handoff.md). Automated regression checks:

```bash
bash scripts/check.sh
bash scripts/test-routing.sh
python3 -m unittest discover -s tests -p 'test_*report.py'
git diff --check
```

Report tests include positive evidence and rejection of missing observations, capture loss, interruption, state changes, stale/lifecycle-only detail, wrong query membership, retirement requests without inactivity, duplicates and malformed values. The assembly inspection tool now traverses nested native types so coroutine startup prerequisites can be audited directly.

## Bounded readiness extensions (October 6)

Each command starts with the two original functional repeats and retains an exact binary, receipt, isolated profile, closed capture and independently recomputed report. Commands run separately; reports never combine builds or manufacture missing observations.

- `make world-advanced`: four owned nearby Couriers establish ordinary crowded detail. Up to 4,800 attraction-disabled native Noise lifecycles exercise sustained inspector work. Actual one-second windows must show two consecutive pressured windows and the `window-budget` reduction. Recorded frames must show fewer actor details and natural restoration. A separate test-only background writer fault waits at most two seconds, releases on the first actual saturation trigger, and requires queue backpressure, clean capture and natural restoration. No synthetic costs or quality-state writes are permitted. The sustained probe uses eight emissions per frame and skips emission after an observed frame over 2 ms. A failed trigger/recovery bound or a different reduction reason stays inconclusive.
- `make world-busy`: the same four actor fixtures receive bounded alternating native GoHere commands every four seconds. Each of nine warmed trials receives 300 fixed measurement pulses, each four immediate native Noise lifecycles plus one immediately destroyed projectile. Actual travel and native goal/path snapshots accompany the counts. Three trials each Off/Inspect/Record must meet the existing numeric targets and stable player/world/focus/camera guards. This scenario proves controlled actor motion and transient lifecycle activity; it does not certify a combat/hazard encounter. Fixture coordination/emission time is excluded from diagnostic overhead; actual inspector lifecycle-hook work is included, and frame time includes the whole native scenario.
- `make world-native`: owned food, hydrant and door objects must be discovered/classified through ordinary awareness. Three controlled door field states must read correctly without changing sampled actor/player state. Native API inspector open/close must pause/resume and report the world-input guard. Owned entities must become natively inactive or destroyed and retire from the live index, then a native level reload must reset scope and reject old activation keys. Door collider traversal, physical F10/F2/Escape/mouse paths and actual elevator/floor progression remain separate checks.

All native writes remain in `WorldFixture`; `WorldReadiness` only schedules fixtures, reads the ordinary service and records detached evidence. `report_world_readiness.py` adds its assertions to the original report rather than treating a captured COMPLETE as success. Test failures and numeric target failures return nonzero. The commands require the game to be closed; no manual menu setup or save reset is needed.

## First dog food extension

`make world-food` runs the same two base fixtures, then the five cases in [food.md](../02-dog/food.md). `PetFoodFixture` owns setup, native test injury and cleanup; `PetFoodHarness` observes the ordinary `PetService` response. The inspector's new `ResolveLive` consumer boundary only reads activation/native identity. `report_pet_food.py` checks actual native count/health changes, ordinary discovery, actual `GoalFollow` and owner, cancellation/preservation, cooldown and native/index retirement. The base report still checks receipt/hash, closed clean capture and engine exceptions. A log saying `ate` cannot certify the experiment by itself.
