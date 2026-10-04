# Latest source checkpoint — October 4, 2026

## October 4 prepared burst-budget correction (not installed)

The busy stress sample revealed expensive lifecycle windows while quality stayed at 0. Source now captures lifecycle identity/classification/position/active state and transient Noise/Bullet/hazard fields immediately; actor/item/object detail and camera projection are deferred to the scheduled census/selected detail. Lifecycle snapshots explicitly label pending detail and unknown viewport via data.coverage. Pool GetComponent lookup is skipped when lifecycle capture is disabled and included in hook timing when enabled. Essential event ordering and bounded queue remain; loss counters are unchanged.

Quality adds one-second numerical overhead windows: mean >1ms or >5% frames over 2ms is pressure. Two pressured windows reduce optional work; the existing consecutive-frame/saturation fast response remains. Sustained observed headroom restores one level after 30 seconds; unobserved gaps cannot count as recovery. Logs add hookMaxMs/hookOverBudget/deferredLifecycleDetails and quality window mean/frame counts/time; UI shows pressure reason and numerical evidence.

Build/analyzers/format and 242 linked C# assertions pass; 35 Python tests pass. Synthetic separated spikes matched to the bad native window summaries (mean 2.0621ms/p95 10.6038ms) kept historical quality=0 but reduced new quality to 2 at 30/60/120fps. These are synthetic sequences: actual frame ordering was not recorded, and they do not establish native performance. Evidence `.runtime/world-checks-burst-fix.txt`, `.runtime/world-build-check-burst-fix.txt`.

Current game remains loaded with 0.6.0+20261004-164018 / session 5aa527ab. SOURCE DIFFERS FROM LOADED DLL. Prepared build snapshot `.runtime/checkpoints/0.6.0-burst-fix-prepared/`. Do not install over this game. Next: after authorized normal close, finalize session integrity report, install/relaunch the prepared fix, run one relevant busy Record retest and ordinary check, then collect missing busy Off/Inspect/Record comparisons. Full busy acceptance and modest hardware remain open. Do not discard the completed prior stress capture.

---

# Latest test result — October 4, 2026

## October 4 completed busy Record stress trial

Corrected build 0.6.0+20261004-164018, session 5aa527ab, trial sequence 3689: valid 60.0163 measured seconds / 3297 frames after warm-up; nativeFreezeFrames=9, UI closed/focused at completion. Freeze-frame correction verified in this native run. Lifecycle creations including warm-up: 400 Bullet, 34 Noise, 20 Danger, 4 Agent (observations, not unique population). Zero observed errors/drops. Trial diagnostic mean 0.8672 ms/p95 1.7044 ms; frame p95 33.3455 ms/p99 33.8286 ms, 8 frames >50 ms.

Saved `.runtime/benchmark-5aa527ab-busy-record.{md,json}`. This completes one busy stress trial, not full paired busy acceptance. One short performance window had diagnostics mean 2.0621 ms/p95 10.6038 ms with 161.26 ms aggregate hook cost; another mean 1.2008 ms/p95 2.0890 ms. Quality remained 0 in captured windows. Investigate burst costs/degradation and compare busy Off before attributing frame-time tails or claiming performance pass. Do not discard this valid sample or repeat merely to get a better number. Game is active; final shutdown integrity pending.

---

# Latest operational update — October 4, 2026

## October 4 reload: freeze-frame fix installed

User authorized restart. Prior session `8fc5165e` ended with errors=0, workerDrops=0, worldDrops=0; final `.runtime/world-8fc5165e-report.md` passes capture integrity. Prior DLL preserved at `.runtime/checkpoints/0.6.0-before-freeze-frame-fix/BurritoRun.dll`. Build/install completed with zero warnings/errors. New loaded build **0.6.0+20261004-164018**, session **5aa527ab**, log `.runtime/logs/game-20261004-104112-27012.log`; BURRITO_BOOT confirmed, no plugin exception in inspected startup log. Source and installed DLL contain the freeze-frame detection/counter/clock-state changes. Native busy retest pending; startup is not validation of the fix in gameplay.

Next: user starts normal single-player run; F10 Busy encounter checked, Next trial Record, Start Record trial/resume. Collect 70 seconds locally while playing a busy scene; return only after completion. Verify valid=true and nativeFreezeFrames plus activity/clock/loss/quality evidence. Still need three trials per mode for full busy comparison; don't count the partial old trials. Launcher session 77463 belongs to this process.

---

Earlier checkpoint history follows; this operational update takes precedence.

# Current handoff — October 4, 2026: catalog before model switch

User requested a durable checkpoint before switching to GPT-6.1 Sol because chat quota repeatedly interrupted log collection. Read `WORLD_AWARENESS_CHECKPOINT.md` first, then the plan/architecture/requirements/validation documents as needed. Do not repeat completed collection or rebuild merely to resume.

Installed/last loaded build: **0.6.0+20261004-050727**. Latest reviewed session: **bb128da0**, now ended cleanly. Its terminal record confirms **errors=0, workerDrops=0, worldDrops=0**. F5 world incident (36 history frames) and six ordered legacy incident pages were captured successfully. The enum serialization repair is natively verified. Saved report: `.runtime/world-bb128da0-report.md`. Latest census: 384 entities. Latest live Record window: diagnostic mean 0.7317 ms, p95 1.6005 ms; frame p95 17.4528 ms. These are preliminary windows, not comparative benchmark acceptance.

Last recorded checks: build/analyzers/format passed; 223 C# assertions and 35 Python tests passed. This checkpoint changes documentation only; no build, install or game launch is required. Working tree includes substantial uncommitted work predating this push; preserve it.

## October 4 live visibility continuation

Installed build unchanged. Active test session is `8fc5165e`, launched via `.runtime/logs/game-20261004-094456-21362.log`; check process state before assuming it remains active. Selected Sign captured with clear LOS and cone false then true. Same Plant UID 1502/key `3:-315156:1502:237` captured behind a wall (LOS=false, cone=true, 0.967u) and from clear side (LOS=true, cone=true, ~1.71u), with zero errors/drops and matching UI/log values. Evidence snapshots: `.runtime/visibility-8fc5165e-{clear,facing,wall,plant-clear}.json`; exact numbers in WORLD_AWARENESS_VALIDATION.md. User switches game/chat focus; paused functional captures are fine, but not performance trials. Window/bar tests added; native LOS uses myLayerMaskEfficient, not movement solidity. No code rebuild/installation done. User subsequently chose to simplify barriers: defer further window/exact-collider diagnostics; bars are a future explicit sight-policy exception while remaining movement-blocking. Next priority is ordinary benchmarks with local collection and idle chat. Do not claim full WA-06 verification from these examples.

## Ordinary benchmark now completed

All nine ordinary trials in active session `8fc5165e` are valid. Stationary/quiet setup confirmed by user. Off/Inspect/Record median trial frame p95 16.991/17.261/16.959 ms; Inspect +1.59%, Record -0.19%. Every active trial satisfies diagnostic mean <1 ms and p95 <2 ms; zero >50 ms trial frames and zero trial drops. Saved `.runtime/benchmark-8fc5165e-ordinary.{md,json}`. Full stream still lacks terminal record while game is active; finalize report after normal exit. Do not rerun this completed stationary suite. Next: busy stress/paired tests and remaining functional gates, with local collection and idle chat.

## Pending source fix — freeze-frame benchmark detection

Busy Record trial 17493 stopped at 20.3674 measured seconds on zero Time.timeScale despite immediate resumed gameplay. Native freeze frames also set Time.timeScale=0: source now distinguishes them from real pauses and retains their frame-time cost. Adds trial nativeFreezeFrames/clockState, performance nativeFreeze/appFocused. Build/analyzer/format, 233 C# assertions and 35 Python checks pass; NOT installed. Active game still uses 0.6.0+20261004-050727. Do not ask for another old-build busy retry. Reload when user ends run, then verify a full busy trial with native freeze evidence; preserve ordinary results and invalid attempts. Busy partial report `.runtime/benchmark-8fc5165e-busy-record-partial.json`. Exact historical pause cause not recorded, so freeze-frame attribution remains consistent inference, not proven event identity.

## Next work

1. Review the saved report and catalog. M1–M4 have implementation and partial evidence; M5 comparative trials remain incomplete.
2. Choose one controlled native functional case or a benchmark scenario before starting a run. Do not restart/reinstall an active game without accounting for its current run.
3. Collect locally with the existing benchmark sequencer; end the chat turn while the game collects. The model does not need to remain active to record. Return once collection finishes or a problem occurs, and parse files once. Keep invalid trials as evidence; resume missing trials instead of restarting all work.
4. Native wall/cone/viewport, door states, pool reuse, destruction, input isolation, allocation profiling and modest-machine tests remain open. No selected target was present in the successful F5 capture; it does not verify selected-target LOS.

## Commands and preservation

`bash scripts/check.sh` checks without installing. `bash scripts/test-routing.sh` and `python3 -m unittest discover -s tests -p 'test_*report.py'` run checks. **`bash scripts/build.sh` installs: close the game first.** `bash scripts/launch.sh --burrito-test` starts ordinary play; smoke auto-quits and is not gameplay verification.

Report: `python3 scripts/report-world.py .runtime/logs/world-SESSION-????.jsonl --output .runtime/world-SESSION-report.md`.

Baseline: `.runtime/checkpoints/0.5.5-before-world-awareness/`. Current catalog/source/evidence snapshot: `.runtime/checkpoints/20261004-model-handoff/` (manifest includes checksums). It excludes profiles, dependencies and Git internals.

Dog behavior changes and movement recovery remain follow-ons; persistence across levels is “probably no.” Split-screen/network live tests are deferred. Do not infer reachability, visibility or blame for damage from proximity alone.

---

The following handoffs are historical; the checkpoint above takes precedence.

# Burrito Run — pause handoff (October 1, 2026, Denver time)

This is the short operational checkpoint. `PROJECT_RECORD.md` holds the detailed experiments, failures, logs and implementation history; `ROADMAP.md` holds the next feature gates. No code or installed DLL was changed for this handoff.

## October 2 continuation

**Current logging checkpoint:** installed `0.5.5+20261002-231913`; read `LOGGING.md` for numeric capture and limits. Build/format, 86 linked checks, 18 report tests and startup smoke passed. Native no-pet rejection/incident capture works; populated NPC/environment/emission/movement capture still needs fixture verification. The check was closed. Prior DLLs: `.runtime/checkpoints/0.5.4-before-tracing/`. Next verify a native setup mechanism and fixture; automated start input did not advance in this check. Earlier paragraphs below are historical.

**Latest direction:** reduce user testing burden before adding behaviors. Read `TESTING_PLAN.md` first: improve complete attempt logging and NPC/environment capture, then build one repeatable native fixture. These are proposed changes only. Do not rebuild, launch or resume manual guard positioning just to repeat the prior test; the next implementation slice is observability. The installed build below remains unchanged by this review.

This October 1 stopping state is historical. The continuation verified panel-click spawning without a punch, following, snack approach and voluntary release on 0.5.2. The user flagged pet persistence across levels as **probably no** and chose barking for interaction per NPC. Installed build is now **0.5.4+20261002-190822**; compilation/format verification, 74 policy/input checks and startup smoke passed. F9/the panel runs a focused guard-distraction probe; automatic stranger barking starts off. Guard movement remains unproven live. The old installed DLLs are in `.runtime/checkpoints/0.5.2-before-barking/`. See `README.md` for the bark test and `PROJECT_RECORD.md` for current evidence; two automatic barks triggered logged native NPC noise reactions, but the user saw no consequences. Useful visible distraction, audio and successful manual emission remain unproven.

## Exact stopping state (October 1)

- Source checkpoint before this note: commit `5c48cc4` (`Reset test pet on native level transitions`). Installed and last loaded plugin: **0.5.2+20261002-051801** (compiled UTC), DLL SHA-256 `32bb8856ccc07c8ffd960a272484f734de0977db88ab749f72e47777bb06fe33`. It is a private Mac BepInEx/Harmony prototype, not a Workshop upload or a new executable.
- `bash scripts/check.sh` built with zero warnings/errors; `bash scripts/test-routing.sh` passed 54 pure checks. The isolated smoke logged `BURRITO_SMOKE_PASS` in `.runtime/logs/game-20261001-231810-22602.log`; its window **intentionally** quits after about 25 seconds. Startup smoke does not prove gameplay.
- The later normal quick-test session `14781eeb` booted 0.5.2 in `.runtime/logs/game-20261001-232046-22803.log`. Its immediate event file is `.runtime/logs/events-20261002-052052-14781eeb.log`. This session has ended; the macOS app inventory showed the game **not running** at pause time. Do not infer anything about later process state from this note.
- That event file records dog 1244 spawning at game time 72.10, two same-level refusals at 73.44 and 74.44, `BURRITO_PET_LEVEL_RESET reason=load-incomplete` during a scene/controller/nest change at 78.29, dog 1254 spawning at 98.06, and another same-level refusal at 99.06. This is a **live log pass for the one-dog-per-level reset**, including reused player UID 1332; the user has not separately described the second dog's visual behavior. No plugin error appeared in the inspected event log. Following, dropped-food detour, voluntary release, dog-button click suppression and cross-level edge cases remain unproven.
- The earlier 0.5.1 test found no accidental punch while using F6; it did not isolate a click on **Spawn test dog**. The first 0.5.0 panel-click punch and hostile Courier consequence are historical and motivated the input guard. Do not call the dog finished: it is a native Courier with a dog visual, not a native animal archetype.

## How to resume or reset a test

1. Double-click `quick-test.command` (or run `bash scripts/launch.sh --burrito-test` from this directory). Ordinary Steam/app launch **does not** load this private loader. Confirm the persistent bottom-left `Burrito Run 0.5.2+20261002-051801` label in the *current process* before trusting observations. Keep the launcher Terminal window open.
2. Start a normal single-player run. F6 spawns the test dog; the quick-test panel has the same button. A second spawn on that same level should be refused. After going to a genuinely new level or ending and starting a new run, one new spawn should be allowed. **Clear scenario** does not reset a level, world objects, money, food or reputation; there is no synthetic world-reset button. For a clean retry use the game's menu and a fresh single-player run. Random maps/NPCs can change outcomes.
3. A plugin rebuild is a different kind of restart: **close the game first**, then `bash scripts/check.sh`, `bash scripts/test-routing.sh`, and `bash scripts/build.sh`; relaunch to load the new DLL. Never replace the plugin while a game process has it loaded. The compiled build label, not a source version or an old log, identifies what is running. `bash scripts/launch.sh --burrito-smoke` is only an isolated startup check and auto-quits; use `--burrito-test` for play.
4. Use `.runtime/logs/events-*.log` for immediate `BURRITO_*` events and the matching `game-*.log`/BepInEx log for native behavior and errors. Match by `session=`, `build=`, scenario run, level/seed and game time; filenames use host local time while build/event timestamps are UTC. For a surprising result press F5 to mark an incident, then record what was visible. F4 snapshots select the nearest eligible object by straight-line distance, even through walls; their indoor flag belongs to the selected target. F3 is the doorway inspector.
5. The launcher keeps a prototype save profile under `.runtime/profile`, seeded with settings/unlocks on its first launch. This is **save redirection, not a security sandbox or a clean-world button**. Do not delete/reset profile or save files as an ordinary retry. The original install is not patched by the launcher, but its paths are machine-specific. `scripts/launch.sh` rejects another running Streets of Rogue process.

## Our approach versus established Streets of Rogue practice

| Existing route / primary reference | What we do now | What we should do when that route matters |
| --- | --- | --- |
| The game's [official Version 87 announcement](https://steamcommunity.com/games/512900/announcements/detail/1711864755138648127) introduced the level editor and Workshop; the [Workshop](https://steamcommunity.com/app/512900/workshop/) presents characters, buildings, levels and campaigns, with Campaign/Chunk Pack/Character categories. | We spawn test actors/props in generated vanilla levels; there is no Pet Store chunk, Gym chunk or Workshop package. | Author and test a small in-game chunk/Chunk Pack for the Pet Store/Gym, then separately test it with the code plugin. Do not imply that Workshop content carries our DLL or that code distribution through Workshop is supported; this has not been established. |
| The [BepInEx 5 plugin guide](https://docs.bepinex.dev/v5.4.11/articles/dev_guide/plugin_tutorial/index.html) describes `BaseUnityPlugin`, DLLs under `BepInEx/plugins`, logging and Harmony; a [Streets of Rogue-specific practitioner guide](https://steamcommunity.com/sharedfiles/filedetails/?id=2106187116) documents the same code-modding path. | We use a local, pinned BepInEx loader, a C# plugin, narrow Harmony hooks and a small Cecil profile patcher. `tools/InspectGame` checks the *installed* assembly before relying on native names/signatures. The launcher keeps the loader out of the game install and captures per-session logs. | Keep this private workflow until game compatibility and clean installation are tested. Verify actual native registration and behavior, not just type names or a successful compile. Test on the exact target game/OS and list dependencies before any public package. |
| [RogueLibs' own repository](https://github.com/Chasmical/RogueLibs) says it is discontinued/archived; its [installation guide](https://chasmical.github.io/RogueLibs/docs/user/installation/) describes it as an optional library layered on BepInEx. | An earlier RogueLibs 4 release-candidate attempt did not load cleanly against this Mac game's Mirror-based assembly, so we use direct BepInEx/Harmony. That is an observed compatibility result for this setup, **not** a universal verdict on RogueLibs. | Reassess a maintained/forked abstraction only if it demonstrably works with this installed game and reduces lifecycle/API risk. Do not add a discontinued dependency just to look conventional. |

The practical architecture rule is to use native pathing, inventory, interactions, ownership and consequences where verified, keep our test UI and policy separate, and make every added object/actor bounded across death and level transitions. A BepInEx DLL runs inside the game process with its privileges; analyzer checks and the separate save profile are hygiene, **not** permission containment.

## Next safe slice, not a promise

Check the user-visible 0.5.2 dog after a new-level spawn and the panel button itself. Then test the existing bounded dropped-burrito chase and F7 release. For extra comedy, first probe a non-spamming bark that can trigger an ordinary NPC response; keep hydrant pee and gut-eating behind native target/setting checks. Decide whether the Pet Store fixture makes the dog fun in an ordinary run before multiplying species or pursuing a larger rewrite. Courier/doorway, phone presentation and delivery-reputation reset are separate lanes in `ROADMAP.md`.
