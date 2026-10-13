# Build and try the prototype

This is a developer-oriented Mac checkout, not a portable player installer.
Source/docs/readers are available without the game; building the plugin and
running native fixtures require a locally installed copy of Streets of Rogue 1.

## Prerequisites and paths

The existing setup uses .NET SDK 7 (`dotnet --info`), Python 3, bash, make,
macOS command-line utilities, and BepInEx 5.4.23.5. The main plugin and profile
patcher target `net472`; the linked policy test runner and InspectGame target
`net7.0`. SDK upgrade/cross-platform support have not been tested.

Before a fresh-machine build, account for these paths:

| Location | Adaptation |
| --- | --- |
| `src/BurritoRun/BurritoRun.csproj` | `GameDir` defaults to the original developer's Steam folder. Override it as below. `ManagedDir` still assumes the Mac app layout. |
| `tests/DeliveryRoute.Tests/DeliveryRoute.Tests.csproj` | The Newtonsoft.Json HintPath is absolute. Change it to your game's `Contents/Resources/Data/Managed/Newtonsoft.Json.dll` before running linked tests. |
| `scripts/launch.sh` | `SOR_GAME_APP` overrides the executable/app. `original_profile` still names the developer's normal CloudData folder; adapt it if you want the initial unlock/settings copy. |
| `scripts/world-harness.sh` | Process guard assumes the Mac executable name; fixtures copy settings from `.runtime/profile` and require its `TechSettings.dat`. |
| `tools/InspectGame/InspectGame.csproj` | Mono.Cecil comes from `.tools/loader/BepInEx/core/` after setup. |

On a Mac with the same Steam app layout:

```sh
export GameDir="$HOME/Library/Application Support/Steam/steamapps/common/Streets of Rogue"
export SOR_GAME_APP="$GameDir/StreetsOfRogueMac.app"
```

MSBuild inherits `GameDir`; keep it exported for later builds/checks. Another
valid build invocation is `bash scripts/build.sh "/p:GameDir=$GameDir"`.
Changing only GameDir does not port the loader, managed layout or process guard.
Paths still needing edits above are known limitations, not a verified fresh-machine recipe.

## Setup and first launch

1. Close the game. Run `bash scripts/setup.sh` from the repository root. It
   downloads the pinned Mac BepInEx archive, verifies its hardcoded SHA-256,
   extracts it under `.tools/loader`, builds both DLLs and installs them locally.
2. For the smallest playable slice, run `bash play-banana-favors.command`.
   Start a normal single-player run and follow the [banana instructions](../BANANA_QUEST.md).
   The launcher does not rebuild automatically.
3. For inspection/experiments, run `bash quick-test.command` instead. Start a
   single-player run through the native menu. Confirm the visible Burrito Run
   build label. A normal Steam launch does not load this private loader.
4. Complete the native first-run settings flow if prompted. Close normally before
   native suites; they require the generated prototype `TechSettings.dat`.

The profile patcher changes save-path calls in the loaded assembly in memory;
it does not rewrite the installed game DLL. Launchers normally use
`.runtime/profile`. New harnesses use `.runtime/world-harness/<unique>/profile`
and copy settings/unlocks, not saved runs. Normal Steam saves and prototype saves
are distinct. Launch one game process at a time. This is not a code sandbox.

## Useful controls

| Control in developer mode | Purpose |
| --- | --- |
| F10 / World awareness inspector | Pause and inspect entities, filters, perception/navigation and benchmarks |
| F5 / Mark incident | Capture bounded history and current evidence |
| F4 | Legacy nearest actor/object snapshot; nearest is not necessarily visible |
| F3 | Read-only doorway grid and geometry inspection |
| F2 | Services phone; opening it pauses city/timers |
| NPC EXPERIMENT panel | Explicit enrollment, manual food pickup, cancellation/release and opt-in autonomous choice |

Some Mac keyboards require Fn. World inspection and command actions are separate;
close inspection before issuing an experiment command. Banana-favor mode suppresses
Services provisioning/opening; the legacy phone remains in its separate flow.
Q opens inventory; Tab opens the map. See [NPC controls](../NPC_DESIRES.md) and
[legacy controls](history/2026-10-20-development-README.md) for preserved details.

## Checks and outputs

```sh
make check
bash scripts/test-routing.sh
python3 -m unittest discover -s tests -p 'test_*.py'
```

`make check` builds/analyzes/formats without installation. `make build` builds
and installs. `make format` changes source formatting/imports. The routing script
name predates the broader pure-policy/replay checks it now runs.

With the game closed, choose one relevant native suite: `make world-harness`,
`make npc-probe`, `make npc-desires`, or `make banana-quest`. They build/install,
launch a new profile, collect evidence, quit and write `report.md` / `report.json`
with saved build/hash/run identity. `make npc-desires` also exports `npc-replays/`.
Use `make help` for longer performance/pressure suites; some take about 11 minutes.

Replay a captured policy decision without launching the game, after adapting
the test project's dependency path:

```sh
dotnet run --project tests/DeliveryRoute.Tests -c Release -- --npc-replay docs/examples/npc-choice-v2.json
```

## Troubleshooting

The [bundled example](examples/README.md) is an actual detached native decision;
you can inspect it without the game or the linked test runner.

| Symptom | Check |
| --- | --- |
| Missing game/Unity/Newtonsoft references | GameDir, Mac ManagedDir layout, loader extraction and test HintPath |
| No build label | Current process was likely launched outside the private loader; close it and use the repository launcher |
| Process-list failure | The guard refuses safely when it cannot enumerate processes; grant the execution environment access rather than removing the guard |
| Harness refuses missing TechSettings | Initialize the separate prototype profile once through normal startup |
| Cannot enroll Slum Dweller | Native ID is Hobo; actor must be idle/wandering, single-player, with no employment/follow/job/emergency obligation |
| No food choice | Enrollment and autonomy are separate; items must be supported, unowned, grounded, single, fresh and within range/LOS |
| Timers appear stuck | Inspector/phone pauses game time intentionally |
| Run exits after testing | Automatic fixtures deliberately clean up and quit; use a normal developer launch for manual exploration |

Keep matching logs and failed runs. Read [evidence limits](CURRENT_STATE.md)
before treating a startup pass or report exit code as gameplay acceptance.
