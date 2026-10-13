# Known methods and native building blocks

This is a curated map of useful entry points verified against the source at
publication preparation. It is not an exhaustive dump of private methods or
a supported external SDK. Project types are mostly `internal`; native behavior
is tied to the installed SoR1 assembly. Reinspect after a game update.

## Observe, query and capture

| Project method | Location under `src/BurritoRun/` | What it does / caveat |
| --- | --- | --- |
| `WorldIndex.Put / Remove / Clear` | [WorldIndex.cs](../src/BurritoRun/WorldIndex.cs) | Maintain detached entries and spatial buckets; writes to the index, not the game world |
| `WorldIndex.Query(WorldQuery)` | [WorldIndex.cs](../src/BurritoRun/WorldIndex.cs) | Range/kind/tag/name/camera filters, distance/key ordering, offset/limit; reports matches and checked candidates, not native perception |
| `WorldAwareness.Tick()` | [WorldAwareness.cs](../src/BurritoRun/WorldAwareness.cs) | Schedule census/detail/quality/query work within budgets; sampled state can be incomplete |
| `WorldAwareness.Lifecycle(...)` | [WorldAwareness.cs](../src/BurritoRun/WorldAwareness.cs) | Observe birth/retirement hooks; lifecycle core can defer details |
| `WorldAwareness.ResolveLive(key)` | [WorldAwareness.cs](../src/BurritoRun/WorldAwareness.cs) | Resolve a sampled activation key to the matching live component; caller still checks current eligibility |
| `SelectObserver / SelectTarget` | [WorldAwareness.cs](../src/BurritoRun/WorldAwareness.cs) | Choose inspection subjects; does not command them |
| `WorldNative.Capture(...)` | [WorldNative.cs](../src/BurritoRun/WorldNative.cs) | Main-thread identity/classification/detail snapshot; lifecycle-core mode intentionally omits detail |
| `WorldNative.Navigation(...) / Tile(...)` | [WorldNative.cs](../src/BurritoRun/WorldNative.cs) | Goal/path/waypoint and tile/door evidence; local walkability is not route completion |
| `WorldPerception.Cameras(...)` | [WorldPerception.cs](../src/BurritoRun/WorldPerception.cs) | Map player agent cameras, deduplicating shared cameras |
| `WorldPerception.Capture(...)` | [WorldPerception.cs](../src/BurritoRun/WorldPerception.cs) | Geometry LOS, facing cone, native range and camera/renderer evidence as separate fields; not a full native perception decision |
| `WorldAwareness.Incident(reason)` | [WorldAwareness.cs](../src/BurritoRun/WorldAwareness.cs) | Queue bounded historical/current evidence; writes capture without issuing gameplay actions |
| `StartBenchmark / StartBenchmarkSuite` | [WorldAwareness.cs](../src/BurritoRun/WorldAwareness.cs) | Start labeled measured modes/trials; use comparable scenarios and read invalidation reasons |
| `CaptureWorker.Measure(value)` | [CaptureWorker.cs](../src/BurritoRun/CaptureWorker.cs) | Conservative detached-payload accounting; arbitrary reference types are rejected |
| `CaptureWorker.Enqueue / AppendBuffered` | [CaptureWorker.cs](../src/BurritoRun/CaptureWorker.cs) | Queue bounded worker writes; backpressure/failure remains observable |
| `CaptureWorker.Drain / Dispose` | [CaptureWorker.cs](../src/BurritoRun/CaptureWorker.cs) | Bounded completion/cleanup; avoid waiting for drains during gameplay |
| `WorldJournal.Write / Close` | [WorldJournal.cs](../src/BurritoRun/WorldJournal.cs) | Sequenced, segmented evidence and terminal closure; missing closure/loss prevents completeness claims |
| `SampledApproach.Sample(...)` | [SampledApproach.cs](../src/BurritoRun/SampledApproach.cs) | Measure approach only across consecutive matching samples; resets across gaps/goals |

World query records live in [WorldModels.cs](../src/BurritoRun/WorldModels.cs).
Call these from the plugin/main-thread context; the spatial index itself does
not make Unity components safe to access in a background worker.

## Decide and execute

| Project method | Location | What it does / caveat |
| --- | --- | --- |
| `NpcDecision.OptionalIdleRoot(name)` | [NpcDecision.cs](../src/BurritoRun/NpcDecision.cs) | Allows exact Idle/Wander/WanderFar roots; preserves specialist/unknown roots |
| `NpcDecision.Evaluate(input)` | [NpcDecision.cs](../src/BurritoRun/NpcDecision.cs) | Pure v1/v2 validation, score = preference minus distance; v2 gives danger/obligations precedence; no world mutation |
| `NpcDecisionCapture.Detach()` | [NpcDecision.cs](../src/BurritoRun/NpcDecision.cs) | Convert input/output into worker-safe dictionaries/lists for replay |
| `NpcDesires.SetEnabled / Tick` | [NpcDesires.cs](../src/BurritoRun/NpcDesires.cs) | Opt-in bounded live candidate collection, policy evaluation, transition capture and action requests |
| `NpcPickupProbe.Enroll(actor)` | [NpcPickupProbe.cs](../src/BurritoRun/NpcPickupProbe.cs) | Enroll one eligible canonical Hobo in single-player; records identity/scope, preserves allegiance/default behavior |
| `NativeObligation / Emergency` | [NpcPickupProbe.cs](../src/BurritoRun/NpcPickupProbe.cs) | Detect recruitment/employment/follow/jobs/busy roots or danger; not a general emotion model |
| `NpcPickupProbe.Eligibility(item)` | [NpcPickupProbe.cs](../src/BurritoRun/NpcPickupProbe.cs) | Check live identity/state, single Fud/Banana, property/quest flags, already-held item, six-unit range and LOS |
| `NpcPickupProbe.Begin(item)` | [NpcPickupProbe.cs](../src/BurritoRun/NpcPickupProbe.cs) | Revalidate, issue native GoHere, install an exact owned PickupGoal, start an eight-second deadline; mutates native action |
| `NpcPickupProbe.Tick()` | [NpcPickupProbe.cs](../src/BurritoRun/NpcPickupProbe.cs) | Recheck scope/danger/obligation/ownership; at 0.8 units call native pickup; require inventory +1 and ground retirement |
| `NpcPickupProbe.Cancel / Disable` | [NpcPickupProbe.cs](../src/BurritoRun/NpcPickupProbe.cs) | End owned action/release enrollment; preserve newer roots/fields; cancel starts eight-second cooldown |
| `PickupGoal.Terminate()` | [NpcPickupProbe.cs](../src/BurritoRun/NpcPickupProbe.cs) | Clear only fields still owned by this goal; a replacement command may update only part of native state |
| `QuestPolicy.Accept / Begin / Complete` | [QuestPolicy.cs](../src/BurritoRun/QuestPolicy.cs) | Pure shared-objective state machine; completion requires exact player/recipient/reward deltas |
| `QuestPolicy.Refuse / Withdraw / End` | [QuestPolicy.cs](../src/BurritoRun/QuestPolicy.cs) | Handle refused exchange, lost offers and terminal interruptions without native writes |
| `QuestService.Ready / SelectingCharacter` | [QuestService.cs](../src/BurritoRun/QuestService.cs) | Native startup/menu guards; pair with QuestStartWindow before automatic placement |
| `QuestService.AddButtons / Handle / Submit` | [QuestService.cs](../src/BurritoRun/QuestService.cs) | Namespaced exact-recipient menus and verified exchange/reward; a player-to-NPC favor, not NPC-to-NPC negotiation |
| `QuestService.Count / HasRoom` | [QuestService.cs](../src/BurritoRun/QuestService.cs) | Read inventory count/capacity; avoids native capacity helper's statistics side effects |

Inspect [delivery policies](../src/BurritoRun/DeliveryRoute.cs),
[reputation](../src/BurritoRun/CustomerReputation.cs) and
[pet policies](../src/BurritoRun/PetPolicy.cs) as additional pure-code examples.
They are not general trade, contract or relationship engines.

## Native methods and goals already investigated

“Used” below means our code calls it. “Inspected” means native code was examined;
it does not imply that a runtime encounter has been validated for every role.

| Native entry point / goal | What is known | Evidence / caution |
| --- | --- | --- |
| `AgentInteractions.GoHere(actor, actor, position)` | Assigns native GoHere job fields/destination | Used by pickup; newer same-destination commands require interception before writes, not comparison of coordinates alone |
| `BrainUpdate.SwitchGoal(goal)` | Replace current native goal | Used for lease/release; can terminate existing work, so validate eligibility/ownership first |
| `GoalDoJob.Activate / Terminate` | Creates native job pathing/pause and may clear job fields | Inspected and extended by PickupGoal; root lifetime and partial replacements matter |
| `GoalPathTo.Terminate` | Stops native pathing | Inspected; reaching a goal name does not prove arrival or transfer |
| `Item.Interact(actor)` | Native ground-item interaction | Used; return/control-flow labels alone are insufficient, verify count and ground retirement |
| `Movement.HasLOSPosition360(point)` | Native geometry linecast using its efficient mask | Used; ignores our separate cone/range/camera distinctions and is not complete sensory reasoning |
| `tileInfo.PossibleToWalk / SafeToWalk` | Local placement/walkability/safety queries | Used; ItemImage colliders can affect placement checks; not end-to-end reachability |
| `spawnerMain.SpawnAgent(..., "Hobo")` | Canonical Slum Dweller actor spawn | Used by favor and corrected fixtures; display identifier SlumDweller produced misleading generic-actor evidence |
| `spawnerMain.SpawnItem(position, itemName)` | Spawn a native item | Used by encounter/fixtures; explicit world write, separate from observation |
| `Agent.Interact / PressedButton` | Native interaction/menu paths | Exercised through favor hooks/fixtures; handler invocation does not prove a human clicked or paid through a menu |
| `AgentInteractions.DetermineButtons` | Dispatches role-specific menus such as Hobo/Thief AssistMe | Inspected; native services have role/state/pricing rules |
| `AssistMe / CauseRuckus` handlers | Recruitment and assigned native Ruckus job | Controlled coordination cases use native handlers; ordinary paid-menu acceptance remains pending |
| `InvDatabase.GiveItem` | Relationship-dependent transfer, sometimes consumable auto-use | Inspected; not a universal retained-gift method; favor verifies explicit transfer instead |
| `hasEmptySlotForItem` | Native capacity helper | Inspection found possible money-found statistics mutation; avoid treating it as a pure read |
| `Relationships.SetupRelationshipOriginal` | Role/faction relationship setup, including gang rivalry branches | Inspected; does not identify what started a particular observed fight |
| `Window.DamagedObject → Noise.SetupNoise → GoalNoiseReact` | WindowHelper incident, eligible responder, path toward window helper | Inspected; window-specific metadata is not a generic interaction channel or culprit attribution |
| Follow / WanderFar | Existing native movement/default behavior | Used by dog/delivery and wandering resident tests; actor-specific conditions still matter |
| FleeDanger / NoiseReact / GoGet / Curious | Native reaction/goal building blocks | Candidates and inspected paths; activation is not useful distraction or a validated social action |

See [native coordination](../NPC_COORDINATION.md), [dialogue map](../NPC_DIALOGUE_BEHAVIOR_MAP.md),
[interaction audit](../NPC_INTERACTIONS_AUDIT.md), and
[window findings](../KNOWLEDGEBASE.md#native-window-investigation--inspected-october-4)
for detailed provenance and qualifications. No decompiled game source is bundled.

## Inspect another native path

After setup, point this read-only CLI at your own assembly:

```sh
assembly="$GameDir/StreetsOfRogueMac.app/Contents/Resources/Data/Managed/Assembly-CSharp.dll"
dotnet run --project tools/InspectGame -- "$assembly" AgentInteractions GoHere
dotnet run --project tools/InspectGame -- "$assembly" AgentInteractions GoHere --names-only
dotnet run --project tools/InspectGame -- "$assembly" --literal WindowHelper
```

Arguments are assembly, case-insensitive type substring, optional method
substring, and optional fourth `--names-only`. Type `*` selects all types;
`--literal` uses the next argument as a case-insensitive string search and
prints nearby instructions. It neither launches nor modifies the game.
Record assembly hash/version and the smallest relevant conclusion locally.
A possible call path becomes a feature claim only after a native outcome check.
