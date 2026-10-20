# Burrito Run knowledgebase

> **What this is:** The running index of decisions and documents kept during development.  
> **Read it if:** You are researching why something was decided.  
> **Status:** Dated working notes, kept as history. Build IDs, "current" and "next" refer to the date written, not today.

**October 20 publication checkpoint:** behavior work is paused for a platform
reassessment. Begin with [current state](../current-state.md) and the
[public documentation map](../README.md). Dated scope/next-action notes below
are preserved history.

**Native NPC coordination:** [coordination.md](../experiments/03-npc/coordination.md) tracks the
canonical Hobo enrollment fix, native recruitment/employment/job precedence and
v1-compatible replay. It owns current verification status; earlier checkpoints
below remain historical.

**October 12 dialogue/behavior overlap review:** [NPC dialogue and behavior map](../experiments/03-npc/dialogue-behavior-map.md)
connects the observed menus and our favor lines to native recruitment, role jobs,
bribery/threats, relationships, gifts, money, quests and planned fears/desires.
It also records the older food experiment's `SlumDweller` / native `Hobo` identity
gap; its fixture pass is not canonical Slum Dweller compatibility proof.

**Current scope — October 12 banana favors:** the user authorized one optional
banana encounter with competing $15 / First Aid Kit rewards on ordinary generated
levels. [04-banana-favors.md](../experiments/04-banana-favors.md) owns this slice, play command, source map
and validation. Phone shipping is shelved; existing services, NPC experiments, dog
and aquarium work are retained. This narrowly reopens encounter placement and
its disposable tests; broader authored levels remain paused. Earlier scope and
installed-build statements below are historical.

Updated October 7, 2026. This private BepInEx prototype uses Streets of Rogue's native world/NPC systems to explore small interactions. Reusable **read-only world awareness and trustworthy evidence** now support bounded native food and hydrant interests. [project-record.md](project-record.md) records integration surprises and emerging scope; the handoff owns the next action.

## Start here

**October 12 NPC tooling:** the user resumed after the quota pause; tooling and first autonomous pickup now pass the native suite (73 checks). Read [desires-handoff.md](../experiments/03-npc/desires-handoff.md) for current evidence and [desires.md](../experiments/03-npc/desires.md) for commands. This is a disabled-by-default, one-civilian experiment; level authoring remains paused.



**Current scope — October 7:** all level-design and setup work is paused. Focus is now all NPC interactions, starting with existing self-preservation and territory rules. The [NPC interaction audit](../experiments/03-npc/interactions-audit.md) supersedes earlier next-action recommendations; existing implementation and evidence remain preserved.

| Need | Document |
|---|---|
| First food interaction, native rules and automated proof | [food.md](../experiments/02-dog/food.md) |
| Bounded hydrant detour, cosmetic marking and automated proof | [hydrant.md](../experiments/02-dog/hydrant.md) |
| Roadmap to the first dog decision, bounded checks and user involvement | [road-to-the-dog.md](../experiments/02-dog/road-to-the-dog.md) |
| Exact stopping state, installed versus prepared build, next action | [pause-handoff.md](pause-handoff.md) |
| Milestones, resource budgets and 30 FPS acceptance | [plan.md](../experiments/01-world-awareness/plan.md) |
| Data contracts, native sources, scheduling and writer | [architecture.md](../experiments/01-world-awareness/architecture.md) |
| Requirement status, evidence and open gates | [requirements.md](../experiments/01-world-awareness/requirements.md) |
| Source map, sessions, reports and backups | [catalog.md](../experiments/01-world-awareness/catalog.md) |
| Detailed native observations and benchmark procedure | [validation.md](../experiments/01-world-awareness/validation.md) |
| Capture fields, bounds and report commands | [capture-and-reports.md](../reference/capture-and-reports.md) |
| Test workflow and original fixture proposal | [testing-plan.md](testing-plan.md) |
| Controls/build commands | [README.md](../../README.md) |
| Larger experiments and preserved project history | [roadmap.md](roadmap.md), [project-record.md](project-record.md) |

The handoff owns runtime identity; the requirements ledger owns verification status. Dated records preserve what was known then. Read current summaries before treating an old “next/current/unverified” statement as today's task.

## What exists and what we are doing

Delivery/Services, the parked aquarium effect, a one-per-level dog proxy and bounded native-noise barking precede world awareness. The dog is a drawn visual over a native Courier actor. Following, dropped-burrito attention and release/wandering have user observations; a logged bark or NoiseReact activation alone does not establish a useful distraction.

World awareness provides an entity registry/spatial index, range queries, camera/geometry evidence, goal/path/door detail, live/paused inspection, bounded background capture and offline reports. The earlier stationary comparison and pressure sample remain separately attributed. New native readiness slices and a complete controlled busy comparison pass on the development host. **Full 30 FPS release acceptance remains open**, including combat/hazard equivalence, allocations and other hardware. Exact builds and limits belong in the handoff.

The October 6 readiness extensions now provide automatic sustained-window/crowded-detail/backpressure validation and controlled food/hydrant/door, API pause/resume and native scope-reload checks. These bounded slices pass; the nine-trial controlled busy comparison passes. The recovery policy was corrected so isolated within-budget outliers do not prevent natural restoration. See the [roadmap execution](../experiments/02-dog/road-to-the-dog.md) for exact reports and limits. The user selected food. The first native eating/Follow slice now passes its automatic fixture; healthy dogs sniff and leave food, injured dogs eat one. World Awareness supplies read-only discovery, while fixture injury/setup stays separate. The next user boundary is subjective readability/usefulness in a short visible test (`make world-food`). Broad release matrices remain open.

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

October 7: the user judged the live dog decent and chose **just hydrant interest**. The new bounded cosmetic marking slice and its automatic fixture are documented in [the hydrant plan](../experiments/02-dog/hydrant.md). Barking/NPC response changes, retrieval, repeated-food polish and general movement recovery were not selected. The normal visible playtest has no automatic cleanup; full native fixtures deliberately remove their owned dog and quit.

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
