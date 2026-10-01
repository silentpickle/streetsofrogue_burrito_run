# Burrito Run: private Streets of Rogue prototype

This is the first playable experiment from `projectbrief.txt`: pay $10 at a working computer, wait for a courier, and deal with whatever happens to the food and courier on the way. It uses the installed Streets of Rogue 1 game and its existing characters, pathing, inventory, and food rules. It is a private BepInEx plugin, not a Workshop upload.

For the complete project history, implementation methodology, evidence, failed experiments, file map, safety boundaries, and remaining test matrix, see [PROJECT_RECORD.md](PROJECT_RECORD.md).

## Play it on this Mac

1. Close Streets of Rogue if it is open.
2. Double-click `quick-test.command` in this folder. If macOS refuses to open it, right-click the file and choose **Open**. Keep the Terminal window open while playing.
3. Start a single-player run through the game's normal menu. The top-right **Burrito Run quick test** panel appears once you are on a playable level. Use **Add $30 test money**, then **Order here ($10)**. The courier starts after three seconds. Stay near the spot where you ordered to receive the item; leave it to make the courier drop the item there. **Courier drops package** tests recovery from an interruption.

You can also interact with any functioning in-game computer and choose **Order Burrito here ($10)**. The normal launcher, `play-burrito-run.command`, removes the test panel and uses a 45–90 second kitchen delay.

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
