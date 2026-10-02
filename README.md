# Burrito Run: private Streets of Rogue prototype

This is the first playable experiment from `projectbrief.txt`: pay $10 through Services or a working computer, wait for a courier, and deal with whatever happens to the food and courier on the way. It uses the installed Streets of Rogue 1 game and its existing characters, pathing, inventory, and food rules. It is a private BepInEx plugin, not a Workshop upload.

For the complete project history, implementation methodology, evidence, failed experiments, file map, safety boundaries, and remaining test matrix, see [PROJECT_RECORD.md](PROJECT_RECORD.md). Feature grouping, test gates and possible Workshop waypoints are in [ROADMAP.md](ROADMAP.md).

## Current checkpoint — October 1

Latest installed build: **0.4.2+20261002-043012** (build timestamp is UTC). It passed compilation, 35 policy/route checks and startup smoke. Version 0.4.2 adds short, visual-only shrimp scuttling toward nearby walkable tile centers, with a flop-in-place fallback and per-critter route logs. In two live aquarium breaks, the log recorded five wet tiles, three visuals and two scattering shrimp each time; the user saw a teal and two orange fish-like critters move farther and judged the effect good enough to leave alone. This is a **limited visual pass**, not proof of ownership response, electricity or other layouts. Version 0.4.1 replaced the nonspawnable `FishTank` name with a registered native `Table` base and code-drawn aquarium graphic. The prior 0.3.1 build passed the named **safety recovery** scenario: $20 across two real inventory handoffs cleared one test-seeded flag, without simulating a courier hit. The preceding 0.3.0 build passed **delivery handoff** and **drop/recovery** live. F4 previously selected a technical `DoorHelper`; 0.3.1 selected an actual `Door` in a live doorway snapshot. Earlier 0.2.0 tests covered panic/drop/pickup, several indoor entrances, native knocking/NPC response and armed replacement gunfire. Waiting couriers deliberately retain their native wandering behavior.

The Services app shell is functional but still an **IMGUI overlay**, not a finished phone-shaped interface. Actual new-run reset, normal-mode weapon probabilities, death/knockout, full inventory and transition refunds still need focused live tests. A startup smoke does not certify gameplay.

Recommended next step: park the aquarium effect at its current visual pass and begin the separate one-pet/Pet Store feasibility probe when ready. Owner/electricity rules are distinct later gates. Actual new-run reputation reset is a separate unfinished check. A recognizable phone presentation can follow separately without moving service/pathfinding/reputation logic into UI.

## Play it on this Mac

1. Close Streets of Rogue if it is open.
2. Double-click `quick-test.command` in this folder. If macOS refuses to open it, right-click the file and choose **Open**. Keep the Terminal window open while playing.
3. Start a single-player run through the game's normal menu. The top-right **Burrito Run quick test** panel appears once you are on a playable level. Use **Add $30 test money**, then **Order here ($10)**. The courier starts after three seconds. Stay near the spot where you ordered to receive the item; leave it to make the courier drop the item there. **Courier drops package** tests recovery from an interruption.

Opening Streets of Rogue directly through Steam or the app opens an ordinary session without the private loader. Confirm the bottom-left `Burrito Run` build label at the title screen before testing; if it is absent, quit that game and use `quick-test.command`.

### Aquarium experiment (0.4.2 limited live visual pass)

On a playable quick-test level, stand in an open area and click **Spawn test aquarium** once. The probe finds a nearby walkable square, spawns a registered native `Table` as a breakable stand, and attaches a code-drawn aquarium graphic. The event log records origin, native placement candidate, distance, walkability, fallback count, chosen square, prefab, actual position and whether the graphic was made. This is **not** a native `FishTank`; that name proved nonspawnable in 0.4.0. Hit the stand to try the ordinary break path, or use **Break test aquarium (diagnostic)** to isolate the break hook. If the hook fires, it attempts at most five native `Water` spill tiles and creates one fish plus two short-lived shrimp visuals. New in 0.4.2, each shrimp checks up to two cardinal tile centers for walkability, scuttles there over 2.4 seconds, or flops in place if boxed in; `BURRITO_AQUARIUM_CRITTER` records its chosen route and `BURRITO_AQUARIUM_BREAK` records `scatteringShrimp`. The user saw the extra motion in two live breaks, though the orange shrimp still looked fish-like. This is small visual movement, not collision-accurate native pathfinding. The critters are scenery, not NPCs or pickup items. Owner retaliation, electrical effects, a Pet Store room and a normal-play aquarium encounter are not implemented.

You can also interact with any functioning in-game computer and choose **Order Burrito here ($10)**. The normal launcher, `play-burrito-run.command`, removes the test panel and uses a 45–90 second kitchen delay.

### Quick-test scenarios and incident logs (0.3.1)

On an outdoor spot in a playable single-player level, remove any older Delivery Burrito from inventory and use **Add $30 test money** if needed. Choose **Start: delivery handoff**, stay near the order point and watch the panel for PASS, FAIL or INCONCLUSIVE. It pays the ordinary $10 and checks that food really enters inventory. Or choose **Start: drop and recover**: after the courier receives the food, the test forces the normal drop path; press **E** on the FUD can within 45 game seconds to prove pickup. The phone pauses that clock. A failed route is reported as a failure rather than hidden by test-only teleporting.

For recovery, start a **fresh run** with a clear driver record, stand outdoors with no Delivery Burrito in inventory and at least $20, then click **Start: safety recovery ($20)**. The test **seeds one clearly logged safety flag**; it does not hit a courier or impose an assault cooldown. The first $10 order is real and its driver may be armed. Stay near the order spot for a peaceful inventory handoff, then click **Continue safety recovery ($10)** and stay near that second order spot. The panel checks that the first handoff leaves one flag and 1/2 recovery, and that the second clears both; PASS requires two real paid handoffs and inventory gains. This does not test the hit hook or new-run reset. A failed/drop/intercepted handoff reports FAIL; **Clear scenario** leaves the test flag and world state in place.

**F4** (or **Inspect nearest object/NPC**) opens a read-only snapshot of the nearest actor or non-helper object within five world units, including its native state and tile ownership/hazards. Version 0.3.1 ignores the internal `DoorHelper` proxy; a live snapshot near a room selected native `Door` 1118 instead. Selection uses straight-line distance, not walls, visibility or facing, so an NPC behind a wall can still win if closest. The indoor/tile fields describe the **selected target**, not necessarily where the player stands; the observed door tile was indoors while the player stood outside. **F5** (or **Mark incident**) records the current player, courier, health, money, order and positions when something surprising happens. F3 remains the detailed doorway inspector.

Each launch writes an immediately updated `events-*.log` in `.runtime/logs/`, tagged with build, session, scenario run, level, seed and game time. The full BepInEx/Unity logs remain useful for stack traces. **Clear scenario** only clears tracking: it leaves the world, any active order and any seeded safety flag unchanged. For a clean retry, return to the normal menu and start a new single-player run; fixed test rooms and automatic world reset are future work. The named scenarios standardize the test action and result, but a randomly generated level can still change NPC behavior.

### Services phone prototype

Builds from version 0.1.1 show a compiled UTC timestamp alongside the version (for example, `0.1.1+20261001-200000`) on notices, the phone, the quick-test panel, and a persistent bottom-left label. Plugin logs append the same build ID. This identifies the loaded DLL, not merely the latest source on disk; restart after installing a new build.

Press **F2** on a playable single-player level to open **Services** (on some Mac keyboards, **Fn-F2**). Alternatively, open inventory with **Q** and use **Services Phone**, which is supplied automatically when there is a free inventory slot. Quick-test mode also has a **Services phone** button.

Version 0.2.0 starts the phone's app shell: **Home → Food Delivery** for orders and **My Account** for your driver safety record. It deliberately uses the existing Unity IMGUI overlay: this milestone adds structure, not a finished visual skin or native Canvas implementation. Service rules live outside the screen, so the presentation can change without rewriting delivery. The quick-test panel and doorway inspector remain developer tools, not extra phone apps. The user saw the additions and accepted this distinction.

The phone shows your wallet, current order, last outcome, and courier-service cooldown. Choose **Order burrito here ($10)**, then close the phone with its button, F2, or Escape. **The city and delivery timers pause while the phone is open.** The order button is disabled while another order is active, during suspension, or with less than $10. Computers still work. This first phone uses the native Friend Phone icon but does not summon its NPCs or consume charges. UI additions have been seen live; every opener, close path and inventory-full case has not been separately rechecked on 0.2.0.

### Indoor and stalled deliveries

Indoor orders try to select a safe outdoor handoff tile beside an exterior door belonging to the addressed building. This applies to open doors too: the driver targets the entrance, not your room, and waits 20 game seconds for pickup before dropping the food. Come outside to meet them; food is not handed through a closed entrance door. Several E/W/N doorway cases and a building whose nearest door was internal worked in live tests; this is not universal pathfinding coverage, and south-facing doors remain untested. If no suitable entrance within 12 units is found, the original-address routing and bounded fallback remain in use.

After failed live entrance selection, the selector was changed to probe several tiles beyond the threshold rather than one adjacent tile, and check a clear approach. Logs named `BURRITO_ADDRESS`, `BURRITO_ENTRANCE_SCAN`, and `BURRITO_ADDRESS_RESULT` explain classification and candidate rejection; `BURRITO_ENTRANCE` confirms successful selection. That revision received successful live indoor tests. If selected, the phone explicitly tells you to meet the driver outside.

Native AI still does the walking. If its delivery job ends early, it makes no meaningful progress toward the destination for 12 game seconds, or the route lasts 45 seconds, the driver switches to **come meet me** at their current location. Approach to receive the food if you have inventory room. After 20 game seconds they drop it, end the order, and allow another order. Use **E** to recover the FUD. Opening the phone pauses these clocks too. This is a conservative fallback, not a guarantee of reaching every indoor room; it does not unlock doors, teleport, or guarantee an outdoor meeting point.

Couriers may wander or step aside while waiting and drop food away from the initial meeting coordinate. This is intentionally preserved for natural, emergent behavior; the phone's coordinate is where the wait began, not live tracking.

Routing policy is isolated in `src/BurritoRun/DeliveryRoute.cs`, address selection in `DeliveryDestination.cs`, and optional knocking in `CourierDoorInteraction.cs`. Run `bash scripts/test-routing.sh` for the 30 route/reputation policy checks. Native movement, meet-driver handoff and fallback drops have been exercised; new layouts can still fail and should be diagnosed from snapshots/logs.

**Native door knocking** is installed and observed, including NPC response and the courier stepping aside. A courier near a closed addressed entrance adds one knock unless native blocked-door behavior already made them knock. This uses the game's door noise and normal NPC reactions; it doesn't force residents to answer, unlock doors, or guarantee entry. Open doors don't trigger an added knock. The helper is `CourierDoorInteraction.cs`; native AI may still make its own further knocks.

### Inspect a doorway without ordering

In quick-test mode, stand within five world units of a door and click **Inspect nearest door (F3)** (or press F3 / Fn-F3). This pauses using the Services phone and shows a read-only 9×9 tile snapshot: door direction/type, open/locked state, indoor/outdoor floor, walls, native movement obstruction and safety checks. Grid north is up; `D` marks the door, `P` the player, `T` an active delivery target. `i/o` mean indoor/outdoor, `#` wall, `X` native movement blocked, `!` possible but unsafe. Marker cells override their symbols; the log preserves their full data. Close with the panel button, F2, or Escape.

Detailed `BURRITO_DOOR_GRID_*`, `BURRITO_DOOR_TILE`, `BURRITO_DOOR_GEOMETRY`, and `BURRITO_DOOR_COLLIDER` log entries capture the door/helper positions and collider bounds. Native walkability uses the active courier when available, otherwise the player, and identifies which actor was tested. This is a snapshot, not a complete path test; refresh after changing door state. No orders, money, movement commands, door changes or noise are issued by inspection.

The first test is whether a courier appears, walks toward the original order spot, and either hands over or drops one burrito. Press **Q** to open inventory: the delivered burrito currently uses the game's blue-and-red **FUD** can icon. Click it to eat when below full health. Try intercepting or attacking the courier after that. The burrito uses the game's Hot Fud art and food behavior, including dietary restrictions; the name and description are customized. **Tab opens the map, not inventory.**

When *you* hit the courier, native flee AI is requested, a courier still carrying food drops it after about five seconds, and new orders are refused for 20 seconds in quick-test mode (45 seconds in normal mode). The panel shows service status. In live October 1 tests, the courier fled, dropped the FUD can, returned to say “not cool,” and the player recovered the can with **E**. A subsequent order attempt during cooldown produced a service-paused notice and no new order log; the player's cash display still needs a direct check. An armed courier can still shoot during native combat despite the flee flags.

Assaults add run-scoped **driver safety flags**. In normal mode, 1/2/3 flags give a 50%/75%/100% chance of an added pistol on the next driver. In quick-test mode, any flag guarantees an armed replacement for predictable testing. The phone warns about protection; native AI still controls reactions, with no forced hostility. Two peaceful inventory handoffs clear one flag; abandoned/drop/intercepted orders do not. No extra delivery charge is taken: hazard-pay dialogue is flavor only. Reputation is coded to reset at death/ghost, title return or hub/tutorial/attract, not ordinary playable-level changes, and is not saved. Native equip was logged and the player confirmed the second driver shot them after another assault. The 0.3.1 test-seeded flag cleared after two real paid handoffs; actual new-run reset and normal-mode probabilities remain unverified live.

Next focused reputation check: start a **new run** and check that flags/cooldown are clear. The seeded recovery scenario has already passed; it does not prove the native run boundary. Do not count a doorway drop as a peaceful handoff. Avoid waiting with the phone open because timers pause. The prior assault/reorder test produced `BURRITO_REPUTATION`, `BURRITO_DRIVER_RISK armed=True`, `BURRITO_ARMED ... equipped=True`, and user-observed gunfire; that separately verifies the hit/escalation path, not new-run reset.

## Safety and troubleshooting

- The launcher keeps prototype saves under `.runtime/profile`. It does not write the game's managed assemblies or use your normal save directory. A copy of the original CloudData from before the first prototype launch is in `.runtime/backups/CloudData-before-prototype`.
- Launch the unmodified game through Steam for ordinary play. Close either copy before launching the other.
- Logs are in `.runtime/logs`. The new `events-*.log` is written as events happen, so it can preserve the last scenario event after an abrupt exit. If the game crashes or an order misbehaves, keep the matching `events-*`, `game-*` and `plugin-*` files plus what happened just before it. Logs can include paths and Steam diagnostic lines; inspect before sharing publicly.
- Earlier builds passed a 25-second startup smoke; the current build booted and received live delivery/combat testing. Delivery/healing, player-assault panic/drop/recovery, doorway cases and armed escalation have evidence. Courier death/knockout, transition refunds and later-level coverage remain gaps. A later early exit without `BURRITO_SMOKE_PASS` is not a passed smoke test.

## Build or restore the local tooling

The source is in `src/`. On this Mac, `bash scripts/build.sh` rebuilds both DLLs and places them in `.tools/loader/BepInEx`. The `.tools` and `.runtime` directories are local and ignored by Git. To provision a fresh checkout, run `bash scripts/setup.sh` with .NET SDK 7 installed and the game installed at the path in `src/BurritoRun/BurritoRun.csproj`.

The Mac game uses Mirror networking. RogueLibs 4 release candidate targets an older Unity networking assembly and failed to load cleanly here, so this prototype uses BepInEx/Harmony directly. The original game install remains intact.

A pre-reputation recovery checkpoint is preserved at `.runtime/checkpoints/0.1.1-20261001-204736/`, containing both DLLs and `source-before-reputation.tar.gz`. See the project record for restoration boundaries; it is not a backup of game saves.

## Build and code hygiene commands

C# uses MSBuild through `dotnet build`. The root `BurritoRun.sln` groups both projects; the `Makefile` gives them convenient commands:

| Command | What it does |
| --- | --- |
| `make` or `make build` | Build Release DLLs and copy them into the local BepInEx loader. |
| `make check` | Build both projects with warnings treated as errors, then verify formatting and analyzer diagnostics without changing source or installing DLLs. |
| `make format` | Fix whitespace and unnecessary `using` directives. Run `make check` afterward. |
| `make smoke` | Build and launch the isolated game; the plugin logs `BURRITO_SMOKE_PASS` and quits after 25 seconds. Close any running game first. |

No extra global tools or NuGet analyzer packages are needed. `Directory.Build.props` enables the SDK's .NET analyzers for the game's .NET Framework 4.7.2 target and pins the analysis level to 7.0. `.editorconfig` controls formatting and enables checks for unused imports, private members, assignments, and parameters. Compiler/analyzer warnings fail ordinary builds too. Use the scripts so the SDK home and NuGet cache stay under `.tools`.

These checks cover part of what Knip does for JavaScript. They do not prove that public/internal plugin entry points, game assembly references, or assets are unused. Unity lifecycle methods have narrow `IDE0051` suppressions because the engine discovers them by name; Harmony discovers attributed patches indirectly. Review such entry points manually. The formatter's fix command deliberately limits changes to whitespace and imports.

The smoke test verifies loader/profile-patcher/plugin startup; delivery, combat, and other gameplay still require a live run. The current SDK is 7.0.305; this setup keeps the existing toolchain rather than upgrading it.
