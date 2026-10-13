# NPC desires: experiment tools and first behavior

**Coordination implementation:** [NPC_COORDINATION.md](NPC_COORDINATION.md) owns
the corrected native Hobo identity, native-obligation precedence, policy v2 and
current validation status. The audit correction below describes the pre-fix
baseline; its historical results remain preserved.

**Historical October 12 audit correction:** the baseline implementation and fixture used the exact
identifier `SlumDweller`, while naturally spawned native Slum Dwellers use
`Hobo`. That enrollment predicate excluded the canonical actor. Earlier
fixture passes below do not establish native role/dialogue compatibility. The coordination follow-up fixes
the identifier and reruns controlled native checks; its ordinary-resident
playcheck remains pending. No runtime change was made in the preceding
documentation review; see [the dialogue/behavior map](NPC_DIALOGUE_BEHAVIOR_MAP.md).

This is a disabled-by-default, single-player experiment for one explicitly enrolled **Slum Dweller**. It collects Fud or Banana and retains it. It does not implement hunger, eating, theft, combat decisions, other NPC roles, or language-model calls.

## Use the tools

Start the existing prototype with `bash scripts/launch.sh --burrito-test` after building/installing with the game closed. In the quick-test panel:

1. Open World Awareness, select an idle Slum Dweller, then close inspection.
2. Scroll to **NPC EXPERIMENT** and click **Enroll selected NPC**.
3. Select an unowned Fud or Banana in World Awareness and close inspection. **Pick up selected food** runs the native action probe.
4. **Cancel action** interrupts the attempt; **Disable / release NPC** also ends enrollment.
5. Enable **Autonomous choices** to let the enrolled NPC choose. Unchecked Banana preference means Fud 8 / Banana 5; checked means Banana 8 / Fud 5.
6. Reopen World Awareness to inspect the last decision: actor/time, candidate identities, observation age, rejection reason, preference, travel cost, score, winner, and action status. Inspection pauses through the existing phone UI and does not command NPC actions.

Enrollment requires an idle/wandering civilian with no recruitment, employer, job, follower obligation or emergency state. Commands do not change allegiance or default behavior. Recruitment, employment and following are rechecked on every active tick; a newer native command takes precedence. Each action has an eight-game-second approach deadline and eight-second cooldown; pause suspends game-time timers. Combat/flee/danger signals, actor loss and level changes interrupt it.

Discovery is bounded to 16 food candidates within six units, refreshed every 0.4 game seconds. Geometry visibility is a simplified perception rule, not full native sensory simulation. Old or incomplete world observations are rejected. Only single, grounded, pickable, unowned, non-quest items not already held are eligible. Decisions commit until completion/interruption; arrival is checked each frame within 0.8 units.

## Repeatable validation

With Streets of Rogue closed:

- `make check`: build, analyzers, formatting; does not install.
- `dotnet run --project tests/DeliveryRoute.Tests -c Release`: policy, replay and existing regression checks.
- `python3 -m unittest discover -s tests -p 'test_*.py'`: independent evidence-reader tests.
- `make npc-probe`: disposable civilian pickup, cancellation and forced flee-signal test.
- `make npc-desires`: disposable preference/distance choices, protected and obstructed items, target removal, timeout, newer native command, actor removal, labeled refusal fault, pause/resume, combat signal and real native level reload. At the end it opens the inspector for 20 seconds, then closes and quits.

The harness copies settings/unlocks into a new disposable profile and saves DLL/hash receipt, logs, closed world capture, report and detached decision captures under `.runtime/world-harness/<unique-run>/`. Failed pilots are retained. Fixture mutations are separate from the read-only world inspector and normal behavior.

Replay an exported decision without launching the game:

```sh
dotnet run --project tests/DeliveryRoute.Tests -c Release -- --npc-replay .runtime/world-harness/<run>/npc-replays/<sequence>.json
```

A capture has versioned `Input` and expected `Output`. Replay rejects missing version/required envelope fields, incompatible versions, invalid candidates and mismatched outputs. Both versions use preference minus distance, positive scores only, with distance then ordinal activation-key tie breaking. Version 2 gives danger and explicit native obligations precedence over an ongoing pickup; version 1 retains its original behavior for historical replay. Version 2 captures require the nullable `NativeObligation` field. Replay is a deterministic policy check, not a simulation of movement or perception.

## Earlier fixture checkpoint

The earlier native suite passed 73 checks; all six saved decisions replay exactly. Build/format checks, 278 C# assertions and 115 Python tests pass. Build `0.6.0+20261012-120818`, session `1234f56b`; see the handoff for exact saved artifacts and limitations. The experiment panel and decision inspector were visually checked in the running game.

## Implementation boundaries

`NpcDecision` is a pure policy. `NpcDesires` reads live candidates, evaluates it and records detached evidence. `NpcPickupProbe` executes through native APIs and owns one exact goal instance; it observes actual inventory/ground changes before reporting pickup. Native AI can replace that goal. The command panel is separate from the read-only decision display. The custom goal preserves changed native job fields on termination; the GoHere prefix handles a newer identical-destination command before it writes its fields.

The obstruction fixture uses a removable collider on the native LOS mask; it does not certify all map geometry. The fixture's emergency/combat cases set native signals; they do not certify all live combat mechanics. The refusal case labels a deliberately invalid native item type so native pickup refuses; it does not represent ordinary inventory capacity. This experiment is not broad NPC, multiplayer, performance, or public-distribution acceptance. Native evidence and current completion status are in [NPC_DESIRES_HANDOFF.md](NPC_DESIRES_HANDOFF.md).
