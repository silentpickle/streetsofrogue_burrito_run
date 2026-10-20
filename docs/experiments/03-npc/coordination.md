# Native NPC coordination

> **What this is:** How our NPC actions coexist with the game's own NPC jobs, recruitment and danger responses.  
> **Read it if:** You want the latest verified state of the NPC experiment.  
> **Status:** Most recent NPC record, October 13, 2026.

October 12 implementation request. Branch `codex/native-npc-coordination`;
baseline commit `e4607dc` preserves the earlier banana favors, NPC experiments,
dog/aquarium work and documentation. Nothing is pushed or published. Original
changed files and hashes are also saved under
`.runtime/npc-coordination/20261013T053844Z/`.


## October 13 ordinary-play correction

The manual playcheck exposed a second eligibility gap. The selected naturally
spawned Hobo, UID `1316`, was healthy, not fighting and not following anyone.
At game time `655.2385` it was using native `GoalWanderFar`. The runtime guard
allowed only `GoalIdle` or `GoalWander`, so the panel correctly showed a refusal
and no successful enrollment event was recorded. The earlier fixture used
`DoNothing`, which hid this normal wandering state.

The corrected guard also permits exact `GoalWanderFar`. Patrol, specialist,
curious-object, unknown/uninitialized and emergency roots remain protected;
native obligations and danger still take precedence. The classification is a
pure predicate used by the live adapter. Its new regression failed on the old
predicate and passed after the correction. Build/analyzers/format are clean;
**329 C# assertions and 131 Python tests** pass.

A revision-3 native suite adds `native-wander`: actual WanderFar enrollment,
manual native food approach/pickup, exact transfer, unchanged WanderFar default,
and observed native behavior after release. This controlled fixture does not
substitute for the remaining naturally generated resident playcheck. Native
verification of the correction passes **126 gates**, build
`0.6.0+20261013-063045`, session `e39185fd`, run
`.runtime/world-harness/20261013-003046-14643/`. The native wandering case enrolled from `GoalWanderFar`,
collected exactly one Fud, retired the ground item and preserved `WanderFar`.
Ordinary-play retry confirms enrollment of a naturally generated Hobo, UID
`1226`, from `GoalWanderFar` at game time `218.133972`, session `37d6a709`.
The log records no employer, follower, native job or obligation at enrollment.
Food pickup and paid recruitment remain pending; full acceptance is incomplete.
Previous run results below are pre-correction evidence.

The original live failure and the red/green test logs are retained in
`.runtime/npc-coordination/20261013T053844Z/`: `wander-enrollment-failure.json`,
`wander-red.log`, `wander-green.log`, `wander-check.log` and `wander-python.log`.
The failed-playcheck profile is copied to
`playcheck-profile-after-failed-enrollment`; the original profile is retained.

## Behavior and ownership

One explicitly enrolled native `Hobo` (displayed as Slum Dweller) can collect and
retain Fud or Banana. Autonomy remains off by default. The existing inspector and
quick-test panel select the resident; there is no automatic enrollment.

`NpcDesires` collects at most 16 nearby food observations every 0.4 game seconds.
`NpcDecision` scores the existing preference-minus-distance choices.
`NpcPickupProbe` uses native movement and `Item.Interact`, owns one exact pickup
goal, and verifies inventory gain and ground retirement. Property, LOS,
item-state, pause, timeout and cooldown rules are retained.

Precedence is danger, native obligation, current valid pickup, cooldown, then a
new food choice. Obligations include the native `joiningParty` recruitment phase,
employer/follower flags or references, foreign jobs and another busy native root.
Native state is checked before commanding and on every active tick. Enrollment
and continued control reject multiplayer, co-op and four-player modes. Our own exact
GoHere goal and fields are exempt from the foreign-job check.

On release, only the owned root can be terminated. Termination clears remaining
owned GoHere fields individually, preserving newer job names, both job codes,
jobBig and assigned destination. This handles a native Ruckus or Follow command
that replaces only part of the job before the root changes. A newer identical
GoHere command is caught before it writes its fields. Runtime pickup code does
not reset native allegiance, employer, default behavior or a newer root.

Policy v2 adds nullable `NativeObligation` to the detached input and reports its
reason. Replay accepts v1 with its original precedence and v2 with the new rule;
unknown versions and malformed v2 inputs are rejected. All 24 existing saved v1
replays passed the new evaluator, with the captures retained unchanged.

## Pre-correction verification

- Baseline build: zero warnings/errors; existing files checkpointed unchanged.
- Updated build/analyzers/format: zero warnings/errors; **319 C# assertions** and
  **129 Python tests** pass.
- Final canonical Hobo pickup/cancel/flee-signal suite: **PASS, 38 gates**, build
  `0.6.0+20261013-055700`, session `943efc7a`, run
  `.runtime/world-harness/20261012-235701-98302/`.
- Final expanded coordination suite: **PASS, 119 gates**, build
  `0.6.0+20261013-055842`, session `497d584d`, run
  `.runtime/world-harness/20261012-235843-99671/`. All eight v2 captures replay.
  Recruitment yields before transfer, Follow resumes, and Ruckus job fields and
  the exact replacement root survive cleanup. Autonomy stays inhibited beyond
  the cooldown while the actor remains employed.
- Banana-favor regression: **PASS, 96 gates**, build
  `0.6.0+20261013-055505`, session `ecf5b358`, run
  `.runtime/world-harness/20261012-235506-96845/`.
- Naturally generated resident, inspector enrollment, ordinary menu and paid
  recruitment playcheck: **pending; full completion is not established**.

Controlled tests use disposable native Hobo actors. Additional cases cover
employment-only and following-only eligibility, native AssistMe recruitment
before and during pickup, employment during pickup, native CauseRuckus job
replacement, and preservation of an exact replacement root. Recruitment tests
invoke the native handler; they do not claim a human clicked or paid through its
menu. Signal-based danger tests do not establish every live combat scenario.

The expanded reader requires canonical actor identity, revision-3 evidence (including the new wandering case),
exact transfers, rejection/interruption results, preserved command fields and
post-release evidence from the same actor. Missing or contradictory evidence
fails. Current readers certify the new suite; historical reports and the baseline
reader implementation remain preserved in original runs and Git.

## Ordinary-resident playcheck

### Paused for the evening — October 13

The user stopped for the evening and plans to resume in a couple of days.
No further gameplay or installation is authorized by this pause. The current
manual session is `37d6a709`, corrected build `0.6.0+20261013-063045`.
Naturally generated Slum Dweller UID `1226` is successfully enrolled, and the
user enabled **Autonomous choices**. Policy v2 observations confirm idle with
no native obligation and no eligible food. The user has no Fud or Banana.

The last suggested step was to return near that resident and click the existing
**Start banana favor (once this run)** button, leaving its ordinary Banana on
the ground. Placement has **not been confirmed**. This button also creates two
offer NPCs; keep the original resident enrolled rather than enrolling those
fixtures. Record this controlled item source separately from natural level
generation. Food pickup, exact retained transfer and the resident's ordinary
menu/paid recruitment remain pending; this is not a completed playcheck.

On resumption, inspect the current session/log before asking the user to repeat
anything. If the game was closed, use the separate profile command below and
select a new naturally generated resident if necessary; UID `1226` and enrollment
are session-specific. Autonomy is disabled by default on a fresh session.
The disposable player had $0; the existing **Add $30 test money** control can
fund the real paid Assist Me interaction when needed. Do not claim recruitment
interrupted an approach unless an active pickup was actually observed.

Current log: `.runtime/npc-coordination/20261013T053844Z/logs/game-20261013-003601-17790.log`.
World observations: the same directory's `world-37d6a709-0000.jsonl`.
No DLL replacement, game shutdown or additional test was performed at this pause.

### Earlier session and resumption procedure

The original manual session was `7cf6b4f2`, build `0.6.0+20261013-055842`,
in its own prototype profile. Automated clicks (including a held
click) and a held Return key did not advance the native menu; this is an input
limitation, not a completed playcheck. Manual input has been requested. The
existing prototype profile and normal Steam saves are untouched.

To relaunch the separate playcheck profile after closing the game:

```sh
BURRITO_PROFILE_PATH="$PWD/.runtime/npc-coordination/20261013T053844Z/playcheck-profile" bash quick-test.command
```

Start a single-player Quick Start in that separate profile. Use F10 World Awareness to select a naturally generated idle Slum Dweller
(`Hobo`), close inspection, then use **Enroll selected NPC**. Enable **Autonomous
choices** and observe a permissible nearby Fud/Banana pickup and retained item.

Check the resident's ordinary **Assist Me** menu. Recruit it through the normal
paid interaction while it approaches another eligible item. Confirm the food
action yields, native follower behavior resumes, and optional food choices stay
inhibited while employed. Preserve matching session logs and observations.
No eligible resident/item means the playcheck is incomplete, not a pass. Use
**Disable / release NPC** when finished; avoid changing a resident's native job
just to make enrollment succeed.

This slice does not implement eating, hunger, theft, new relationships, broader
NPC roles, general combat decisions or a dialogue overhaul. Banana favors and
parked phone, dog and aquarium work remain retained.

## Evidence receipts

Pre-correction DLL SHA256: `1e4bb865d27f9b77a1e87534478da7f74e81a1b8ade66543ab9aac9f70f296ff`.
Saved run binaries match their receipts; no plugin errors or engine exceptions
were observed in the five controlled runs. The first pickup/coordination runs
are retained separately from final verification.

`.runtime/npc-coordination/20261013T053844Z/verification.json` records run IDs,
hashes, source hashes and offline counts. `historical-replays.json` and
`current-replays.json` retain the 24 v1 and eight v2 replay results. The
manual-session log is `logs/game-20261013-000203-1635.log` in that directory.
Local runtime artifacts remain excluded from Git.

Wandering correction receipt: `.runtime/npc-coordination/20261013T053844Z/wander-verification.json`.
Corrected installed DLL SHA256: `4b1eebf8d96e93dc25e942d351412d54afb46ad3dc92ec2d3d92429b2cf51f96`.

Manual retry evidence: `.runtime/npc-coordination/20261013T053844Z/manual-retry-enrollment.json`.
The generic selection prompt can replace the enrollment label on a repeated
Enroll click; the successful event is independently verified in the session log.
