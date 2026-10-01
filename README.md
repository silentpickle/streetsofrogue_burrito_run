# Burrito Run: private Streets of Rogue prototype

This is the first playable experiment from `projectbrief.txt`: pay $10 at a working computer, wait for a courier, and deal with whatever happens to the food and courier on the way. It uses the installed Streets of Rogue 1 game and its existing characters, pathing, inventory, and food rules. It is a private BepInEx plugin, not a Workshop upload.

For the complete project history, implementation methodology, evidence, failed experiments, file map, safety boundaries, and remaining test matrix, see [PROJECT_RECORD.md](PROJECT_RECORD.md).

## Play it on this Mac

1. Close Streets of Rogue if it is open.
2. Double-click `quick-test.command` in this folder. If macOS refuses to open it, right-click the file and choose **Open**. Keep the Terminal window open while playing.
3. Start a single-player run through the game's normal menu. The top-right **Burrito Run quick test** panel appears once you are on a playable level. Use **Add $30 test money**, then **Order here ($10)**. The courier starts after three seconds. Stay near the spot where you ordered to receive the item; leave it to make the courier drop the item there. **Courier drops package** tests recovery from an interruption.

You can also interact with any functioning in-game computer and choose **Order Burrito here ($10)**. The normal launcher, `play-burrito-run.command`, removes the test panel and uses a 45–90 second kitchen delay.

### Services phone prototype

Builds from version 0.1.1 show a compiled UTC timestamp alongside the version (for example, `0.1.1+20261001-200000`) on notices, the phone, the quick-test panel, and a persistent bottom-left label. Plugin logs append the same build ID. This identifies the loaded DLL, not merely the latest source on disk; restart after installing a new build.

Press **F2** on a playable single-player level to open **Services** (on some Mac keyboards, **Fn-F2**). Alternatively, open inventory with **Q** and use **Services Phone**, which is supplied automatically when there is a free inventory slot. Quick-test mode also has a **Services phone** button.

The phone shows your wallet, current order, last outcome, and courier-service cooldown. Choose **Order burrito here ($10)**, then close the phone with its button, F2, or Escape. **The city and delivery timers pause while the phone is open.** The order button is disabled while another order is active, during suspension, or with less than $10. Computers still work. This first phone uses the native Friend Phone icon but does not summon its NPCs or consume charges. New phone UI behavior requires live playtesting; startup checks alone do not prove it works.

### Indoor and stalled deliveries

Indoor orders now try to select a safe outdoor handoff tile beside an exterior door belonging to the addressed building. This applies to open doors too: the driver targets the entrance, not your room, and waits 20 game seconds for pickup before dropping the food. Come outside to meet them; food is not handed through a closed entrance door. Entrance selection is conservative and still needs live testing. If no suitable entrance within 12 units is found, the original-address routing and bounded fallback remain in use.

After failed live entrance selection, the selector now probes several tiles beyond the threshold rather than one adjacent tile, and checks a clear approach. Logs named `BURRITO_ADDRESS`, `BURRITO_ENTRANCE_SCAN`, and `BURRITO_ADDRESS_RESULT` explain classification and candidate rejection; `BURRITO_ENTRANCE` confirms successful selection. The current candidate fix still needs a live indoor order. If selected, the phone explicitly tells you to meet the driver outside.

Native AI still does the walking. If its delivery job ends early, it makes no meaningful progress toward the destination for 12 game seconds, or the route lasts 45 seconds, the driver switches to **come meet me** at their current location. Approach to receive the food if you have inventory room. After 20 game seconds they drop it, end the order, and allow another order. Use **E** to recover the FUD. Opening the phone pauses these clocks too. This is a conservative fallback, not a guarantee of reaching every indoor room; it does not unlock doors, teleport, or guarantee an outdoor meeting point.

Couriers may wander or step aside while waiting and drop food away from the initial meeting coordinate. This is intentionally preserved for natural, emergent behavior; the phone's coordinate is where the wait began, not live tracking.

Routing policy is isolated in `src/BurritoRun/DeliveryRoute.cs`. Run `bash scripts/test-routing.sh` for deterministic policy checks. Indoor native movement and fallback handoff still need live testing on the new build.

The next build adds **native door knocking**: a courier arriving near a closed door at the addressed spot knocks once unless the game's own blocked-door behavior already made them knock. This uses the game's door noise and normal NPC reactions; it doesn't force residents to answer, unlock doors, or guarantee entry. Open doors don't trigger an added knock. The helper is `CourierDoorInteraction.cs`; native AI may still make its own further knocks. Close the game and run `bash scripts/build.sh` to install a newly compiled update before testing.

### Inspect a doorway without ordering

In quick-test mode, stand within five world units of a door and click **Inspect nearest door (F3)** (or press F3 / Fn-F3). This pauses using the Services phone and shows a read-only 9×9 tile snapshot: door direction/type, open/locked state, indoor/outdoor floor, walls, native movement obstruction and safety checks. Grid north is up; `D` marks the door, `P` the player, `T` an active delivery target. `i/o` mean indoor/outdoor, `#` wall, `X` native movement blocked, `!` possible but unsafe. Marker cells override their symbols; the log preserves their full data. Close with the panel button, F2, or Escape.

Detailed `BURRITO_DOOR_GRID_*`, `BURRITO_DOOR_TILE`, `BURRITO_DOOR_GEOMETRY`, and `BURRITO_DOOR_COLLIDER` log entries capture the door/helper positions and collider bounds. Native walkability uses the active courier when available, otherwise the player, and identifies which actor was tested. This is a snapshot, not a complete path test; refresh after changing door state. No orders, money, movement commands, door changes or noise are issued by inspection.

The first test is whether a courier appears, walks toward the original order spot, and either hands over or drops one burrito. Press **Q** to open inventory: the delivered burrito currently uses the game's blue-and-red **FUD** can icon. Click it to eat when below full health. Try intercepting or attacking the courier after that. The burrito uses the game's Hot Fud art and food behavior, including dietary restrictions; the name and description are customized. **Tab opens the map, not inventory.**

The next experimental behavior reacts when *you* hit the courier: native flee AI takes over, a courier still carrying food drops it after about five seconds, and new orders are refused for 20 seconds in quick-test mode (45 seconds in normal mode). The panel shows service status. In live October 1 tests, the courier fled, dropped the FUD can, returned to say “not cool,” and the player recovered the can with **E**. A subsequent order attempt during cooldown produced a service-paused notice and no new order log; the player's cash display still needs a direct check.

## Safety and troubleshooting

- The launcher keeps prototype saves under `.runtime/profile`. It does not write the game's managed assemblies or use your normal save directory. A copy of the original CloudData from before the first prototype launch is in `.runtime/backups/CloudData-before-prototype`.
- Launch the unmodified game through Steam for ordinary play. Close either copy before launching the other.
- Logs are in `.runtime/logs`. If the game crashes or an order misbehaves, send the newest `game-*.log` and `plugin-*.log`, plus what happened just before it. These logs can include paths and Steam diagnostic lines; inspect before sharing publicly.
- The plugin and save isolation passed a live launch and a 25-second startup smoke test. On October 1, a live quick-test order charged $10, spawned a courier, routed to the order spot, and delivered the burrito to inventory. The player then ate it and confirmed it restored health. Courier interruption and later levels still need playtesting.

## Build or restore the local tooling

The source is in `src/`. On this Mac, `bash scripts/build.sh` rebuilds both DLLs and places them in `.tools/loader/BepInEx`. The `.tools` and `.runtime` directories are local and ignored by Git. To provision a fresh checkout, run `bash scripts/setup.sh` with .NET SDK 7 installed and the game installed at the path in `src/BurritoRun/BurritoRun.csproj`.

The Mac game uses Mirror networking. RogueLibs 4 release candidate targets an older Unity networking assembly and failed to load cleanly here, so this prototype uses BepInEx/Harmony directly. The original game install remains intact.

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
