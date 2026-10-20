# Burrito Run — less manual testing, better evidence

> **What this is:** How testing moved from manual playtesting to automated in-game harnesses.  
> **Read it if:** You want the reasoning behind the test tooling.  
> **Status:** Dated working notes, kept as history. Build IDs, "current" and "next" refer to the date written, not today.

## Current workflow — October 4

Numeric bark attempts/environment history were implemented in 0.5.5; world registry/perception/navigation/incident capture and benchmark controls now exist in 0.6.0. A smaller [native actor harness](../experiments/01-world-awareness/harness.md) now runs through `make world-harness`: two fresh isolated launches passed four fixture repetitions with automatic setup/capture/removal. The historical dog/guard room proposal below remains **unbuilt**. [Requirements](../experiments/01-world-awareness/requirements.md) is the acceptance ledger; [handoff](pause-handoff.md) identifies the installed build.

The developer reads numeric records and reports; the user primarily starts play and judges readability/fun. Collection runs locally while the chat is idle. Confirm a trial start once, end the turn, then read completed evidence. Preserve invalid/poor trials and their reasons. Do not spend an active conversation polling the nine-trial sequence.

Selected visibility examples and all nine stationary ordinary trials have evidence. A completed busy Record trial retained native freeze frames and exposed burst costs/hitches; the correction is now installed with one lighter busy Record retest captured; next review the partial ordinary suite and test sustained pressure/recovery, collect relevant ordinary regression and paired busy modes, then finish functional/allocations/hardware gates. Avoid another old-build busy retry or detailed window matrix. F10 controls avoid macOS F11 conflicts. Focus switches can pause the game; the installed freeze distinction does not excuse actual or unknown pauses.

Documentation-only work does not require another build or native test. Record exact build, setup, scenario activity, coverage/quality/loss and measured time for code-affecting checks. Read [validation](../experiments/01-world-awareness/validation.md) for the full comparison procedure.

## Historical October 2 audit and fixture proposal

The dated status, gaps and proposed slices below are preserved as the original audit, not today's implementation state.

Reviewed October 2, 2026. Slice 1 is implemented in installed `0.5.5+20261002-231913`; see [capture-and-reports.md](../reference/capture-and-reports.md) for fields, limits and verification. Startup, rejected requests and incident capture are verified live; the emitted/NPC/environment path remains unverified. The fixture below is not implemented. Keep that distinction when resuming.

## Recommendation

Spend the next two slices on observability and one repeatable native-world scenario. Pause new NPC behaviors and the manually arranged guard test. The developer should arrange and inspect mechanical tests; the user should primarily judge whether a short, already working encounter is readable and fun. Keep pet persistence across levels at **probably no**.

The original brief asks for stories from existing Streets of Rogue systems, and explicitly rejects a sprawling sequel. The mod still fits that aim. Our current workflow has made the user search for an NPC, position a following dog precisely, wait through cooldowns, watch a fleeting reaction and relay the outcome. That is a test-design problem we can improve; it is not an inherent requirement of every mod experiment.

## What the evidence currently establishes

| Evidence | Established | Still unknown |
| --- | --- | --- |
| User's 0.5.2 observations | Panel spawning without an accidental punch, following, snack approach, independent wandering after release | Broad reliability across maps and interruptions |
| `events-20261002-185132-1ff4e80c.log` | Two automatic barks created native noise; Hobo 1308 and Hobo 1240 each activated the matching native noise reaction | Their subsequent motion, useful distraction, successful manual emission, audible output, entertaining consequences |
| User's bark observation | No visible consequences were noticed | Why that particular encounter lacked a visible payoff |
| 0.5.4 policy/input checks and startup smoke | Checked policies and plugin initialization passed | Live guard displacement; ordinary-run success rate |
| `events-20261002-191004-013bc56a.log`, inspected at this review | Boot and phone provisioning | No bark or guard attempt is recorded in that file at this checkpoint |

Existing logs are in `.runtime/logs/`. Missing evidence is not a pass, and it does not establish that the game ignored an action. Native noise reaction activation is a useful intermediate observation, not the final gameplay outcome.

## Gaps found in the current code

| Area | Current behavior | Change needed |
| --- | --- | --- |
| Attempt capture (audit of 0.5.4) | Rejection notices were saved in text logs, without structured attempt IDs or associated snapshots | Every explicit request gets an attempt ID and a structured reason, even if no noise is emitted |
| NPC selection | Guard scan records only the nearest otherwise eligible Guard/Patrol-goal actor; stranger scan has no candidate report | Record bounded candidate snapshots and each failed condition; show native actor name separately from default/current goal |
| NPC understanding | F4 selects one nearest actor/object; NPC logs omit some fields shown on screen, including default goal | Automatic neighborhood snapshot: actor IDs, goals, jobs, relationships, combat/follow/investigation/hearing state, position and ownership |
| Environment understanding | F3 has detailed doorway geometry; F4 captures one tile; both require manual invocation | Reuse read-only inspection for a small map around source and target: walls, doors, walkability, hazards, indoor/chunk/ownership and line of sight |
| Outcome observation | Guard movement is sampled about every 0.4 seconds, only during the matching root noise goal and while several validity checks hold | Capture a short trajectory and goal transitions; record why attribution or sampling stops, including pooled noise reuse, actor loss, combat and level change |
| Incident marker | F5 records player/courier/order, without dog, surrounding NPCs or preceding history | Export a bounded recent history plus neighborhood snapshot without needing the user to reproduce the event |
| Identity | Generic context has session/build/type/seed/time; observed seeds are zero | Add scenario attempt and level generation IDs; include available controller/scene/floor/load markers, and scope actor IDs to that level |
| Interpretation | Intent, native activation and partial movement are spread across event lines | Automatically produce one readable report with explicit evidence and missing evidence |

Code anchors: `src/BurritoRun/TestDiagnostics.cs`, `WorldInspector.cs`, `DoorwayInspector.cs`, `PetBarkService.cs`, `PetService.cs`, `PetLevelScope.cs`, and `TestScenarios.cs`. Existing delivery scenarios already show that real-world inventory/payment assertions and timeouts are possible; extend that pattern rather than introduce a second simulation framework.

Installed-assembly inspection shows that native noise arbitration considers more than distance, including hearing, following and investigation state. `GoalNoiseReact.Process` can finish its Curious subgoal within four units with line of sight in ordinary cases. These are explanations to investigate, not confirmed causes of the two weak bark encounters. Clear sight and walkable endpoints also do not establish a traversable route.

## Slice 1 — capture a complete attempt

Keep the existing immediate event log and full engine log. Add versioned JSON Lines for the bounded test trace, plus a small offline report command. No dashboard, service or model inference is needed.

1. Assign one attempt ID before any explicit bark/guard eligibility check. Record `requested`, then either `rejected` with reasons or `emitted` with source, target, volume and native noise identity. Record relevant policy state: spawn grace, cooldown, prior target and level cap. For automatic attempts, record emissions and changes in rejection state rather than identical failures every tick.
2. Capture nearby NPCs and relevant objects automatically. Preserve the actual native name/job/default goal/current goal; do not infer willingness to investigate from the word “Guard.” Candidate reasons describe our selection rule. Where the native decision itself is not instrumented, say `native-decision-unknown` rather than invent a rejection reason.
3. Keep an in-memory history for up to eight tracked nearby actors, initially at five samples per game second for twenty seconds. Capture positions, health/combat state and goal summaries. Flush it around an attempt, F5 mark, exception or interruption. Cap neighborhood detail and file size; record truncation and dropped samples. Measure overhead before increasing coverage. Do not log every NPC every frame.
4. Snapshot geometry at attempt start/end, with explicit source/target coordinates and missing-field markers. Record sampled state as sampled state; do not imply it captures every transient goal, witness or collision. Use a narrow native event hook only when a specific gap warrants it.
5. Separate `native reaction activated`, `attributed movement`, `outcome asserted`, and `visually assessed`. Preserve the noise identity at emission; if pooling invalidates attribution, retain prior evidence and terminate that measurement with a reason. Level change, actor loss, pause, combat and game exit need explicit incomplete/interrupted handling.
6. Write a terminal result for every attempt when possible. The offline report must also recognize a request without a terminal record after a crash. Include both game time and wall time so pauses and hangs are distinguishable. Keep records local in the existing runtime log directory.

Proposed result vocabulary:

| Result | Meaning |
| --- | --- |
| PASS | The named assertion was observed; list exactly which assertion |
| FAIL | A valid setup completed observation and did not meet that assertion |
| SKIPPED | Required setup was unavailable or selection rejected the attempt |
| INTERRUPTED | Observation ended because of transition, actor loss, combat, shutdown or another recorded condition |
| INCONCLUSIVE | Capture or attribution was insufficient to decide |

A report can have a reaction PASS and a useful-distraction INCONCLUSIVE. `audio.Play()` returning successfully establishes that playback was requested, not that a person heard it. A policy PASS does not become a gameplay PASS.

Illustrative future report format — not an actual test result:

```text
Guard bark / attempt 17 / fixture revision 1 / build ...
Setup: valid; native target ..., source ..., distance ..., LOS ...
Emission: observed; noise ...
Native noise reaction: PASS, matching activation recorded
Approach: PASS, ... units during matching goal
Vacated starting position: PASS, ... seconds outside start area
Return/settling: observed / not observed / observation interrupted
Useful opening and fun: not assessed
Other nearby stimuli: ... / unknown
Evidence: trace path + corresponding engine log
```

Completion gate: the developer can explain a successful attempt, a cooldown rejection and a missing-target rejection from saved files without asking the user what buttons they pressed. Check report handling against captured records, malformed/truncated records and interrupted attempts. Preserve existing build checks; smoke tests remain startup checks.

## Slice 2 — one repeatable native encounter

Build a tiny local test room: player, dog, one stationary guarding actor, a marked starting location, clear floor and one controllable doorway. Prefer an authored in-game editor fixture because it makes geometry and ownership inspectable and can later support the Pet Store. The developer handles creation, loading and mechanical testing. The game's editor/Workshop existence is verified; our specific fixture loading, actor configuration and reset workflow still need verification.

If editor access/loading becomes the main obstacle, evaluate a bounded test-only native spawn fixture instead. Verify actual prefab registration and native initialization first. Label forced fixture setup distinctly from ordinary game behavior, and remove only actors/props created by that fixture. Do not silently teleport unrelated NPCs, change factions or reset the user's saves.

The scenario should validate its setup, perform the bark once, observe and summarize automatically. It should use fixture coordinates or bounded native movement to arrange the source; if setup cannot be achieved, stop with a clear SKIPPED/timeout result. Precise dog spacing must not become another user task. Existing cooldowns remain normal-play behavior; the fixture begins each case with fresh explicitly labeled test state.

Start with these cases, in order:

1. **Quiet control:** same fixture and observation interval without a bark. Record normal movement and goal changes.
2. **Clear-floor bark:** emit once at a known separation and observe native reaction, movement toward the source, time away from the starting area, and whether/when the actor resumes normal behavior.
3. **Interruption:** change level or remove the fixture actor during observation; require a clean terminal result and no stale next-level tracking.
4. **One environmental variation:** doorway open/closed or distance varied, only after the clear-floor case is understood. Do not add a broad combination matrix yet.

Use a few fresh repeats and report all trials, including skips and interruptions. A quiet control and matching-goal attribution strengthen the explanation; native AI is nondeterministic and a patrol moving toward a sound by coincidence remains a possible confound. Avoid claiming exact deterministic replay from a seed alone. Distinguish controlled-room evidence from a short ordinary-run check with competing stimuli.

Completion gate: one control and one bark case can be rerun by the developer and produce a concise mechanical result without user choreography. Then show the user a short live encounter or captured clip and ask about readability/fun. Recording is optional; the trace must work without it. Broaden into normal play only after this gate.

## Scope and decision point

| Route | Payoff | Cost / uncertainty | Recommendation |
| --- | --- | --- | --- |
| Better traces and reports in this mod | Explains attempts and failures; reusable for couriers, props and other NPCs | Small to medium engineering slice; native decisions still require observation | Do first |
| One native fixture and scenario | Removes NPC hunting and exact manual positioning; retains the city's real systems | Medium slice; fixture initialization/reset is the largest immediate uncertainty | Do second |
| Study existing mod examples | Helps discover wrappers and established integration patterns | Must check exact installed-game compatibility; does not supply outcome proof | Targeted reference research when needed |
| Independent one-room prototype using engine samples | Own the state machine, resets, stepping and debug views | Manageable experiment if kept tiny; still needs real movement/collision and play assessment | A fallback experiment, not a migration commitment |
| Build our own broad version of Streets of Rogue | Full system control | Large project: NPC priorities, factions, witnesses/crime, ownership, doors, inventory, hazards, combat, saves, content and assets become our responsibility | Not justified by the current evidence |

Reconsider after the two slices. Continue the mod if a native interaction has an understandable observed outcome and the next trial no longer needs a sequence of user setup instructions. If a controlled fixture still cannot be repeated or the needed native behavior demands brittle global AI overrides, stop that feature and compare a tiny independent prototype. An uninteresting bark is also a reason to park barking and choose another existing system; it does not by itself justify rewriting the game.

An independent comparison would contain only one room, a player, dog, guard, snack and door; explicit idle/patrol/investigate/return states; reset/step controls; and a visible event timeline. Its purpose would be to judge interaction clarity and iteration cost. It would not establish the cost of recreating the city's emergent systems. Use simple original placeholder assets and keep it separate from the mod.

Some human playtesting remains necessary for comic timing, sound, visual clarity, control feel and whether surprises are enjoyable. Mechanics, setup, capture and initial regression checking can move substantially to the developer and harness. We should aim for “watch this working interaction” rather than repeated instructions to find and position the right NPC.

## Verified references for sample/library choices

- [Official Godot demo projects](https://github.com/godotengine/godot-demo-projects) contain standalone example projects; use a branch matching the chosen stable engine version, since `master` targets engine development. No samples have been installed for this review.
- [Official 2D navigation overview](https://docs.godotengine.org/en/stable/tutorials/navigation/navigation_introduction_2d.html) links navigation polygon and grid pathfinding samples. These provide movement building blocks, not the social/consequence systems in our brief.
- [Navigation Polygon 2D example](https://github.com/godotengine/godot-demo-projects/tree/master/2d/navigation) demonstrates `NavigationRegion2D`, `NavigationPolygon` and `NavigationAgent2D`; [grid navigation example](https://github.com/godotengine/godot-demo-projects/tree/master/2d/navigation_astar) is another small starting point. Development-branch links are references; pin a matching stable branch before building.
- [RogueLibs maintainer repository](https://github.com/Chasmical/RogueLibs) offers custom item/trait/status/menu/audio/sprite helpers but explicitly says the library and documentation are discontinued. Treat it as source/reference material until compatibility is demonstrated, not a default new dependency.
- [Developer's Version 87 announcement](https://steamcommunity.com/games/512900/announcements/detail/1711864755138648127) documents the native level editor and Workshop. Verify our fixture in the installed Mac game before relying on a particular editor capability.

## Next implementation task

Exercise the unverified native trace paths with the smallest repeatable fixture. Verify a native menu/setup mechanism first: automated start input did not advance in the first instrumentation check. Preserve the existing prototype save profile and clearly label controlled setup. Validate populated NPC/environment numbers, emitted noise, actual cooldown rejection and interruption before claiming the full capture path works. Then run the quiet control/bark comparison. Do not resume manual guard spacing as the default next step.
