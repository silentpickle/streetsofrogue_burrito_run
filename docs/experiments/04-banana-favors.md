# Banana favors

> **What this is:** The banana favor: two neighbours want the same banana, and you choose who gets it ($15 or a First Aid Kit).  
> **Read it if:** You want the smallest playable thing in the repo, or to see how an encounter is built and tested.  
> **Status:** Dated build log from October 12, 2026. The playable slice still works the same way.

October 12, 2026. Current slice: one optional encounter per single-player run in
an ordinary generated level. Dog/aquarium work remains parked; phone and courier
services remain available in the existing developer/legacy launch, outside this
quest's normal player flow. This explicitly reopens setup for this encounter;
it does not reopen broader level authoring.

## Human playcheck correction — October 12 afternoon

The first normal-player session `e057b37b` failed startup. Its log offered the
favor at game time 7956.00, between `Character Accepted` and native `ChangeLevel`,
then expired it at 7956.34 before Slums 1 was generated. The ready flags and
`levelType = Normal` briefly described the outgoing character-select room.
The once-per-run attempt was spent there. This explains ordinary NPC menus and
no favor tracker/labels in the user's screenshots; it was not a successful
visible playcheck. The earlier native suite bypassed automatic startup.

`QuestService.Ready` now rejects open character-selection/title screens using
native menu flags. The selection object can remain active when its menu is shut;
its Unity active flag alone is insufficient.
`QuestStartWindow` requires two continuous ready game seconds before automatic
placement and resets on transition/unready or a restarted load clock. Pausing
does not advance it. The banana suite now enables the actual automatic startup
from the title screen and records that an offered encounter exists on a generated
floor with native objectives, then survives until the exchange cases begin.
This adds startup assertions instead of assuming a fixture-started exchange
proves the normal launch path. Original logs, source backups and inspection
receipts are retained in `.runtime/banana-startup-fix/20261012T210014Z/`.

**Correction PASS: 96 native checks**, session `8aadb87d`, build
`0.6.0+20261012-210510`, run
[20261012-150511-77399 report](../../.runtime/world-harness/20261012-150511-77399/report.md).
Normal auto-start was first observed on floor `1-1` with two native objectives at
game time 17.52 and remained the same offered encounter at 29.52, before the
eleven exchange/interruption cases. Both cash and kit outcomes, speech bubbles,
refusals, native pause and actual reload passed. The harness exited successfully.
Installed/preserved DLL SHA256:
`54d38cb5b60f296beb2a548b3f4222a5a2aa6b6353a4bc0dd20bef40c88de438`.
Build/analyzers/format pass; 306 C# checks and 120 Python tests pass. The user
closed the failed manual session before installation; the test game also quit.
**Later human playcheck passed for the cash choice.** The user confirmed
“Cool, it works” and that it felt native, with some unnatural dialogue. Normal
session `953d4ccc` logged the offer at game time 61.92, acceptance at 73.96, then
cash completion at 80.87: player bananas 1→0, recipient bananas 0→1, money 0→15.
Log: `.runtime/logs/game-20261012-151326-77887.log`. The kit choice remains
automated native evidence, not a separately reported human playcheck. Dialogue
and native-system overlap are mapped in [dialogue-behavior-map.md](03-npc/dialogue-behavior-map.md).

Pilot `20261012-150258-77116` is retained as a failed regression run: checking the
selection object's active flag prevented all automatic startup. Native
`MainGUI.HideCharacterSelect` clears `openedCharacterSelect` while retaining the
UI object. The corrected guard uses `openedCharacterSelect`,
`doingInitialCharacterSelect`, and the menu's `onMenu` state. Native inspection
and the pilot prevented promoting an untested startup fix.

## Play on this Mac

With the game closed, run `make build`, then double-click
`play-banana-favors.command` or run `bash play-banana-favors.command`. Start a
single-player run normally. The new launcher uses the existing separate prototype
profile, not the normal Steam saves. It does not build automatically.

Two nearby marked Slum Dwellers offer competing rewards. Press E near either and
choose **Hear banana offers**. Pick up the marked native banana with E, then talk
to the Buyer for **$15** or the Neighbor for **one First Aid Kit**. They say
**Thanks!** in a native speech bubble. Completing one closes both offers. A compact
tracker and world labels replace the phone. The banana remains ordinary food, so
you can accidentally eat it; any other ordinary banana can satisfy the favor.

Alternatively, set `[Favors] Enabled = true` in the loader's generated
`local.streetsofrogue.burritorun.cfg`. The default is false. This mode suppresses
Services phone provisioning/opening and the computer order option; existing
service code is retained. `quick-test.command` still exposes developer tools,
including a button to start the favor once in a run.

If there is no clear nearby placement, the encounter declines to start and shows
a notice. It never respawns bananas indefinitely. Leaving the level expires an
unfinished favor. Abandoning, losing both recipients, or losing the source with
no banana held also ends it. Losing one recipient leaves the other available.
A full reward or recipient inventory refuses the exchange without moving items.
The once-per-run state and completed outcome are in memory; quitting/reloading a
saved run is not persistent quest history.

Native main quests, XP, floor completion and exit eligibility are untouched by
this quest implementation. Native rewards/items still follow ordinary inventory
rules. There are no model calls, social memory, generated dialogue, multiplayer
support, custom level files or Workshop code installation in this slice.

## Source and extension seams

| File | Responsibility |
| --- | --- |
| `src/BurritoRun/QuestPolicy.cs` | Pure offer/accept/exchange/complete/interruption state machine, two reward definitions. Acceptance preserves the player's choice. |
| `src/BurritoRun/QuestStartWindow.cs` | Wait for continuous native readiness before spending the once-per-run encounter. |
| `src/BurritoRun/BananaEncounter.cs` | Bounded nearby placement, native actors/item, exact identities and level scope. Production setup is separate from test fixtures. |
| `src/BurritoRun/QuestService.cs` | Native menus, exact one-item transfer and selected reward, speech, tracker, once-per-run lifecycle. |
| `src/BurritoRun/Patches.cs` | Namespaced labels and native Agent menu/click hooks. |
| `src/BurritoRun/Plugin.cs` | Opt-in config/launch flag, ticking and drawing. |
| `src/BurritoRun/QuestHarness.cs` | Disposable-profile scripted native menu/pickup/exchange and interruption cases. |
| `scripts/report_banana_quest.py` | Independent inventory, speech-bubble, native progression and repeat-payment assertions. |
| `tests/DeliveryRoute.Tests/QuestChecks.cs` | Pure state/choice/exact-delta checks. |
| `tests/test_banana_quest_report.py` | Synthetic negative controls for the evidence reader. |

`QuestService.Completed` is an internal in-process notification after verified
completion; it is not a stable public API. Start new content by separating the
pure objective from the game's native execution and proving the result. See
[the agent extension guide](../reference/extending.md) for inspection and tooling.

Native integration lessons: `InvDatabase.GiveItem` has relationship-dependent
food/consumable auto-use branches, including employment/control checks; it is
not a universal retained-gift operation. This exchange adds one native banana to
the recipient then subtracts one from the player and verifies both counts.
Reward capacity uses a read-only slot/stack
check because native `hasEmptySlotForItem` can update money-found statistics.
Slum Dweller uses native actor ID `Hobo`; the display identifier `SlumDweller`
can spawn a generic actor without valid dialogue.
`SafeToWalk` rejects ItemImage colliders; placement validates the empty endpoint
before spawning, then checks a clear approach ending within pickup distance.

## Validation

Run `make check`, `bash scripts/test-routing.sh`, and
`python3 -m unittest discover -s tests -p 'test_*.py'`. With the game closed,
`make banana-quest` builds/installs, creates a new disposable profile, runs eleven
native cases, reloads the level, closes the stream and writes an independent
report. Existing saves and old run evidence are preserved.

The fixture calls native `Item.Interact` and `Agent.Interact/PressedButton` through
the real patches. It relocates only its own recipients into interaction range;
it does not establish physical keyboard navigation. Capacity fault setup
occupies slots temporarily with fixture names and restores them in the same
frame. Native quest status/floor-complete/exit eligibility are compared around
acceptance and exchanges. Reload proves scope expiration; it does not claim an
ordinary exit was earned without completing the game's main objectives.

**Earlier fixture PASS: 94 checks**, eleven cases, session `99ef4488`, build
`0.6.0+20261012-183103`. Run:
[20261012-123104-3417 report](../../.runtime/world-harness/20261012-123104-3417/report.md).
The harness and game exited successfully. Preserved/installed DLL SHA256:
`a323796dd4dbaab5160c2432eab6be13eb149b410736808da21de78d6cc24079`.
Build/analyzers/format pass; **300 C# checks and 119 Python tests** pass. Offline
logs are in `.runtime/checkpoints/before-banana-quests-20261012T180836Z/`.

The native evidence verifies both menu alternatives, exact stacked/single banana
transfers, only the selected reward, real TalkText Thanks bubbles, repeat/other
recipient rejection, refusal without inventory changes, recipient/source loss,
abandonment, native menu pause, unchanged native objectives/exit eligibility and
expiration on a real native reload. This is functional proof on the development
Mac; it is not a fun/balance, other-hardware, multiplayer or public-release gate.

Earlier normal launcher playcheck: session `e057b37b`, earlier installed build/hash, fresh
profile `.runtime/banana-playcheck/20261012-123206/profile`, log
`.runtime/banana-playcheck/20261012-123206/logs/game-20261012-123250-5774.log`.
CUA screenshot confirms the normal title screen and Local Favors notice without
the developer panel. Computer-control clicks/keys did not reliably advance the
native start menu, so physical movement, E/menu input, marker layout and an
ordinary-player handoff were **unverified** at that checkpoint. The game was left
at that title screen for a human playcheck. The user subsequently reported the
missing encounter and closed this session; the afternoon correction above
supersedes that handoff and installed-build state.

Earlier failed runs are retained in `.runtime/world-harness/`: `20261012-122204-95456`
and `20261012-122324-96006` rejected the banana's own ItemImage in the approach
check; `20261012-122503-97872` exposed invalid native dialogue with the display
identifier SlumDweller (the correct native identifier is Hobo);
`20261012-122657-99147` passed every case except an invalid direct-Time.timeScale
pause fixture, now replaced with the native pause menu. `20261012-122917-2128`
passed all gameplay checks, but changing the launch shell file while it was
running caused a shell-offset error on exit. The final run above reran the final
source with stable launch scripts and exited cleanly. Never edit a running launch
script in place.

