# Burrito Run knowledgebase

Updated October 4, 2026. This private BepInEx prototype uses Streets of Rogue's native world/NPC systems to explore small interactions. The current push is reusable **read-only world awareness and trustworthy evidence**, before expanding dog desires or movement recovery.

## Start here

| Need | Document |
|---|---|
| Exact stopping state, installed versus prepared build, next action | [PAUSE_HANDOFF.md](PAUSE_HANDOFF.md) |
| Milestones, resource budgets and 30 FPS acceptance | [WORLD_AWARENESS_PLAN.md](WORLD_AWARENESS_PLAN.md) |
| Data contracts, native sources, scheduling and writer | [WORLD_AWARENESS_ARCHITECTURE.md](WORLD_AWARENESS_ARCHITECTURE.md) |
| Requirement status, evidence and open gates | [WORLD_AWARENESS_REQUIREMENTS.md](WORLD_AWARENESS_REQUIREMENTS.md) |
| Source map, sessions, reports and backups | [WORLD_AWARENESS_CHECKPOINT.md](WORLD_AWARENESS_CHECKPOINT.md) |
| Detailed native observations and benchmark procedure | [WORLD_AWARENESS_VALIDATION.md](WORLD_AWARENESS_VALIDATION.md) |
| Capture fields, bounds and report commands | [LOGGING.md](LOGGING.md) |
| Test workflow and original fixture proposal | [TESTING_PLAN.md](TESTING_PLAN.md) |
| Controls/build commands | [README.md](README.md) |
| Larger experiments and preserved project history | [ROADMAP.md](ROADMAP.md), [PROJECT_RECORD.md](PROJECT_RECORD.md) |

The handoff owns runtime identity; the requirements ledger owns verification status. Dated records preserve what was known then. Read current summaries before treating an old “next/current/unverified” statement as today's task.

## What exists and what we are doing

Delivery/Services, the parked aquarium effect, a one-per-level dog proxy and bounded native-noise barking precede world awareness. The dog is a drawn visual over a native Courier actor. Following, dropped-burrito attention and release/wandering have user observations; a logged bark or NoiseReact activation alone does not establish a useful distraction.

World awareness now provides an entity registry/spatial index, range queries, camera/geometry evidence, goal/path/door detail, live/paused inspection, bounded background capture and offline reports. Native census counts and selected visibility examples are recorded. The older stationary comparison passed numeric targets; the first completed busy sample found burst costs and hitches. We installed the tested narrower lifecycle capture and better quality reduction; one lighter populated busy Record retest is captured, and the ordinary comparison suite stopped after six valid trials. **This is validation work, not completed 30 FPS release acceptance.** Exact builds and next steps belong in the handoff.

## Native behavior and interpretation

| Native primitive/evidence | Useful meaning | Limit |
|---|---|---|
| Follow, GoHere, WanderFar | Native movement goals used by the dog/delivery probes | They do not create ownership, custody, desires or guaranteed arrival |
| FleeDanger, NoiseReact, GoGet | Native reaction/goal candidates; use depends on actor/context | Actor-specific use needs native evidence; an activation is not a visible outcome |
| Movement.HasLOSPosition360 | Native geometry linecast using its efficient mask | Separate from cone/range, hearing, lighting, movement and full NPC perception |
| Viewport anchor/renderer bounds/onCamera | Separate camera/rendering observations | Not proof of final pixels or which player actually perceives a target |
| Goal/path/waypoint/velocity/door metadata | Actual state and measured progress/candidates | Intent, proximity or local walkability cannot prove reachability or a stall |

Native collections update intelligence through rolling census and lifecycle observations. Deaths/removals/level/pool identities are tracked; camera/perception/detail observations have their own ages. Destruction can change later LOS answers, but the registry is not an atomic complete frame and sampled evidence is not continuous knowledge. Lifecycle retirement requests are not proof of successful destruction. Prepared lifecycle core records explicitly defer actor/item/object detail and camera evidence to scheduled scans.

## Native window investigation — inspected October 4

Read-only inspection of the installed Assembly-CSharp.dll confirms a dedicated chain: Window.DamagedObject can emit both a volume-2 `WindowHelper` noise tied to the window helper and an ordinary volume-1 break noise. Noise.SetupNoise chooses a windowHelperAgent from active brain actors using starting-chunk/owner/prison matching, death/state and distance/helper-side checks; it marks hasWindow and prompts AI processing. BrainUpdate.GoalArbitrate checks the designated helper agent alongside its other selection rules. GoalNoiseReact.Activate adds GoalPathTo toward the window helper position for that selected agent, with a noise-position fallback. Generic reactions can instead use GoalCurious. GoalNoiseReact.Process exempts WindowHelper/DoorHelper from the ordinary nearby/LOS Curious-removal branch.

This is native event metadata plus actor eligibility and goal/path logic, not evidence that an NPC universally sees through walls. Going to the window does not by itself prove it identified the player as culprit; full witness/crime attribution and a controlled runtime example were not traced here. Inspecting these files did not change the running game's behavior or interrupt its benchmark.

Audit files: `.runtime/window-npc-members-native.txt`, `.runtime/window-npc-reaction-native.txt`, `.runtime/window-npc-noise-native.txt`, `.runtime/window-npc-arbitration-native.txt`. These establish code paths, not which path a particular NPC took in the user's encounter. Retain native helper propagation and dedicated window responses separately from generic bark reaction coverage.

Useful reuse pattern: an incident carries a target identity/location, eligibility selects a responder, and native goals handle the response. Use this as a candidate for later object interactions and dog interest decisions. Quiet food/hydrant discovery still comes from world queries; a disturbance can arrive through an event. Preserve the distinction between noticing a location, investigating it and identifying a culprit. WindowHelper has window/building-specific rules, so reusing its exact noise type for arbitrary dog interests is not established. No new response behavior is implemented by this finding.

## Decisions to retain

- Prioritize interaction per NPC and reuse over more species or deep simulation. Dog food/hydrant interest is a follow-on to the world service; a Pet Store fixture is not built.
- Cross-level pet persistence: **probably no**. Existing delivery waiting preserves native wandering by user choice.
- Use practical barrier rules: native wall/window geometry now; future bars block movement but permit sight. The bars override is planned, not implemented; exact window-damage/collider matrices are deferred.
- Inspector reads do not command movement, doors, relationships or inventories. Pause and input handling are explicit UI actions.
- Logs retain numbers, missingness, observation age, coverage, quality reductions and losses. Never turn an unknown or missing record into zero/false/pass.
- Keep native access on Unity's main thread; serialize/write detached data on one bounded worker. Build success, reader exit zero and synthetic scale checks have distinct meanings from gameplay/performance acceptance.

## Practical testing and continuity

Use local trials rather than asking the user to narrate fleeting mechanics. Start/confirm once, let the game collect without active chat polling, and inspect completed reports after the user returns. Focus changes matter; pause, level/mode changes and capture loss invalidate trials. Native combat freeze frames also set Time.timeScale to zero: the installed correction distinguishes evidenced freezes and retains their unscaled time/hitch costs. Old invalid attempts remain evidence; their exact historical pause cause was not captured.

Do not install over a running game. `scripts/check.sh` checks/builds without installation; `scripts/build.sh` installs. Keep exact build/session plus source/DLL checkpoints. Documentation-only changes need consistency/link checks, not another game rebuild. F10 offers inspection/benchmark buttons; F5 captures incidents; macOS may intercept F11. Main-menu automation was unreliable, so use user-started runs when necessary.

Benchmark claims currently apply only to the measured Apple M3 Max development machine. Busy paired comparisons, recurring hitches, full allocation profiling, remaining native cases and modest second-machine validation are open. No Workshop/public distribution, independent-game rewrite or additional framework migration is committed.
