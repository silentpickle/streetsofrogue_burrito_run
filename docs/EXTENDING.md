# Extending Burrito Run with an AI agent

Read [current state](CURRENT_STATE.md) first. As of October 20, behavior work is
paused for a platform reassessment; the workflow below applies when new work is
explicitly scoped. [Architecture](ARCHITECTURE.md) and [methods](METHODS.md)
provide the public source orientation.

This source tree contains more than the playable mod: world inspection, native
assembly inspection, deterministic NPC decisions, replay, disposable native
fixtures, and independent evidence readers. Reuse these before reconstructing
the game's APIs or inventing a second observation system.

This is a contributor entry point, not a stable external SDK. Most types are
internal to the plugin. A separate plugin cannot assume it can call them as a
public API. The player does not need an AI model to run the current NPC behavior.

## Start with the current state

1. Inspect `git status --short` and the relevant source before editing. Preserve
   existing changes and evidence; create uniquely named new runs.
2. Read [KNOWLEDGEBASE.md](../KNOWLEDGEBASE.md) for the documentation map and
   [PAUSE_HANDOFF.md](../PAUSE_HANDOFF.md) for the local operational checkpoint.
   The current NPC checkpoint is at the top of
   [NPC_DESIRES_HANDOFF.md](../NPC_DESIRES_HANDOFF.md); its later pause sections
   are historical. A checkpoint records a dated result, not the reader's current
   installed build.
   [NPC_COORDINATION.md](../NPC_COORDINATION.md) owns the canonical Hobo fix,
   native-obligation precedence and current ordinary-resident verification.
3. Choose one observable behavior and its completion/interruption conditions.
   Dog and aquarium work is preserved and parked. Broader desires, relationships,
   dialogue remain ideas. The explicitly authorized banana favor slice is tracked
   in [BANANA_QUEST.md](../BANANA_QUEST.md); broader level authoring remains parked.

## Where the useful pieces live

All source paths in this table are relative to `src/BurritoRun/`.

| Need | Starting files | Contract or lesson |
| --- | --- | --- |
| Find nearby actors, food, objects, hazards | `WorldAwareness.cs`, `WorldIndex.cs`, `WorldModels.cs` | Bounded queries over sampled state; check identity, age, and coverage. |
| Inspect native state, geometry, goals and paths | `WorldNative.cs`, `WorldPerception.cs`, `WorldAwarenessUI.cs` | LOS, cone, range, camera evidence, and navigation are distinct observations. |
| Choose between actions without touching Unity | `NpcDecision.cs` | Pure versioned input/output; v2 adds native-obligation precedence, with v1 replay retained. |
| Connect live observations to that decision | `NpcDesires.cs` | Bounded candidate collection, eligibility, commitment, and detached decision capture. |
| Execute and release one native action | `NpcPickupProbe.cs` | Own an exact goal; native recruitment/employment, replacements and emergencies take precedence. Verify inventory gain and ground retirement. |
| Reproduce NPC interactions automatically | `NpcDesireFixture.cs`, `NpcProbeHarness.cs`, `NpcDesireHarness.cs` | Setup writes belong to disposable fixtures, separately from inspection. |
| Capture evidence without background Unity access | `CaptureWorker.cs`, `WorldJournal.cs`, `TraceWorld.cs` | Main-thread native reads; detached payloads on one bounded writer. Loss and incomplete closure remain visible. |
| Build an optional item quest | `QuestPolicy.cs`, `BananaEncounter.cs`, `QuestService.cs` | One verified shared objective with competing rewards; no native quest/exit mutations. See `QuestHarness.cs` and `report_banana_quest.py`. |
| Learn from delivery and reputation work | `DeliveryService.cs`, `DeliveryRoute.cs`, `CustomerReputation.cs` | Existing examples to inspect, not a generic trade/contract API. |

[NPC_DESIRES.md](../NPC_DESIRES.md) describes enrollment, inspector fields,
timers, commands, and replay. [WORLD_AWARENESS_ARCHITECTURE.md](../WORLD_AWARENESS_ARCHITECTURE.md)
owns the observation/capture contracts; use the handoff and
[requirements ledger](../WORLD_AWARENESS_REQUIREMENTS.md) for validation status.

## Inspect the actual game before guessing an API

`tools/InspectGame/Program.cs` uses Mono.Cecil to read a locally installed assembly
without launching or modifying the game. With the tool's dependencies configured:

```sh
dotnet run --project tools/InspectGame -- /path/to/Assembly-CSharp.dll AgentInteractions GoHere
```

Arguments are assembly path, type-name substring, and optional method-name
substring. A fourth argument `--names-only` suppresses instruction output.
`--literal` in the type position searches string literals using the next argument.

Use [NPC_INTERACTIONS_AUDIT.md](../NPC_INTERACTIONS_AUDIT.md) and the native
findings in [KNOWLEDGEBASE.md](../KNOWLEDGEBASE.md) to avoid repeating the territory,
self-preservation, and window-response investigations. Inspected code establishes
a possible path; a native goal name alone does not prove arrival, handoff, witness
attribution, or the behavior of another actor type.

## Extend one behavior and verify the result

1. Define the decision in detached data and keep native calls in the execution
   layer. Update policy versions and replay compatibility deliberately.
2. Revalidate the live actor/target before commanding it. Bound the attempt and
   handle cancellation, native replacement, pause, target loss, and level changes.
   Preserve native state that a newer command owns.
3. Add focused policy checks in `tests/DeliveryRoute.Tests/` when the decision
   changes. For native effects, extend the applicable disposable fixture and its
   independent reader in `scripts/report_npc_*.py`, with reader regressions in
   `tests/test_npc_reports.py`. Assert resulting state, not just an emitted label.
4. Run the applicable offline checks below. Then, with the game closed, run the
   relevant native suite. Inspect its report and replay exports; finish with a
   short visible playcheck for behavior or UI changes.
5. Record the tested build/session, actual outcomes, and remaining limits in the
   relevant handoff. Keep unsuccessful runs as evidence.

| Command, from repository root | Effect |
| --- | --- |
| `make check` | Build, analyzers, formatting verification; no installation or game launch. |
| `bash scripts/test-routing.sh` | C# policy/replay/regression checks; the script name predates the broader suite. |
| `python3 -m unittest discover -s tests -p 'test_*.py'` | Python evidence-reader/regression checks. |
| `make banana-quest` | Build/install, run both banana rewards, refusal/interruption/menu/reload checks in an isolated profile, report, and quit. |
| `make npc-probe` | Build/install, launch the native pickup/cancel/release fixture, report, and quit. |
| `make npc-desires` | Build/install, launch the autonomous-choice/interruption suite, report, and quit. |
| `make world-harness` | Build/install, run the base world observation/retirement fixture, report, and quit. |
| `python3 scripts/report-world-harness.py /path/to/run` | Recompute a saved run's report; writes reports/replay exports in that run. |

Replay one exported decision without launching the game:

```sh
dotnet run --project tests/DeliveryRoute.Tests -c Release -- --npc-replay /path/to/capture.json
```

Replay checks the policy against captured input/output; it cannot reproduce native
perception or movement. [LOGGING.md](../LOGGING.md) documents capture fields and
additional report tools. A build pass, replay match, native fixture pass, and fun
ordinary-play encounter each establish different things.

## Fresh-machine and release boundaries

The scripts currently target the development Mac. `scripts/setup.sh` downloads a
pinned, checksum-verified Mac BepInEx loader and builds/installs locally; it is not
a portable player installer. The plugin project, test project's Newtonsoft.Json
reference, launch/profile paths, and harness process checks need review on a new
machine. `GameDir` alone does not remove the Mac-specific managed-assembly layout.
The inspection tool also references Mono.Cecil from the local loader. Native
harnesses require an initialized prototype profile with `TechSettings.dat`.

`make build` installs DLLs; close the game first. The harnesses enforce that check
and create separate run profiles. Profile redirection keeps test saves separate;
it is not a security sandbox for code running inside the game process.

For a future release, include a short developer pointer beside the playable mod
that links to the exact source release and this guide. Publish the corresponding
source, scripts, tests, and referenced Markdown with an explicit reuse license.
Original project source/docs now use the root [MIT license](../LICENSE).
No public source URL has been selected in this checkout.
Player releases should contain the required mod files and instructions; source
releases should explicitly select our source/docs/tooling. Keep local `.runtime/`,
`.tools/`, saves, machine logs, and game assemblies out of both release bundles.
Local evidence paths in handoffs may be unavailable to a recipient; regenerate
evidence with the tooling or separately provide reviewed, portable examples.

Current NPC autonomy is disabled by default, single-player, one enrolled Slum
Dweller, and Fud/Banana pickup/retention. Hunger, eating, autonomous theft, social
memory, bargaining, language-model calls, multiplayer, cross-platform support,
and Workshop installation of the code are not established by that milestone.
