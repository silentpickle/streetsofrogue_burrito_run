# Streets of Rogue 1.5 — experiment roadmap

This is an ordered set of experiments, not a feature promise. The objective is emergent comedy from existing game systems. Each slice gets a repeatable setup, native-world observation and a short ordinary-run playtest before adding variants. `PROJECT_RECORD.md` holds evidence and failures; `README.md` holds current controls and build status.

## Workstreams

| Role perspective | Work |
| --- | --- |
| Phil / integration | Installed-game APIs, loader compatibility, Workshop packaging research |
| Steve / systems | Native object, item, NPC and event hooks; bounded lifecycle; diagnostic controls |
| Jim / levels | Test fixtures, entrances, ownership and reusable chunks |
| Peter + Ryan / fun | Choose the smallest interaction worth building; observe clarity and surprises in play |
| Akshata / milestones | Dependencies, acceptance checks and release gates |

These are responsibility areas, not simulated endorsements or a requirement to run multiple agents for every change.

## Ordered experiments

1. **Test playground and delivery finish line.** Version 0.3.0 added named delivery handoff/drop-recovery scenarios, F4 object/NPC snapshots, F5 incident marks and immediate event files. Both delivery scenarios passed once live with a $10 charge and real inventory gain. Installed 0.3.1 filters the technical `DoorHelper` from F4 selection and adds a test-seeded safety-recovery scenario; the latter passed live with two paid handoffs and a cleared flag. A live F4 snapshot then selected an actual `Door` near the player while skipping helper objects. Actual new-run reset remains unverified; it need not block the aquarium experiment. A scenario must be reproducible from a fresh run; no fake world-reset button. The existing phone can receive a visual pass separately.
2. **Aquarium.** The 0.4.0 in-level test established that `FishTank` is not a registered prefab; placement also rejected many nearby spots without numeric diagnostics. Version 0.4.1 uses a registered `Table` stand with a drawn aquarium graphic, a grid fallback and placement numbers. One live run passed spawn and ordinary break; the user saw water, fish and shrimp, while the event log recorded five wet tiles and three loose critters. This is still a quick-test deployable prop, not an inventory item or Pet Store fixture. **Next aquarium candidate:** let one or two shrimp briefly scatter after break, with bounded movement/lifetime and wall-aware destinations; preserve the current visible spill as the baseline. Test the comic payoff before adding mechanics. Then test native ownership response separately. Research electrical interaction as another gate: dry, wet-unpowered and wet-powered fixtures must behave distinctly before claiming electricity works. Jim's Pet Store room supplies the first ordinary-run encounter; it is not built yet.
3. **One pet NPC.** Start with a simple dog-like actor: follow owner, chase food, wander after owner loss. Test each condition directly in the Pet Store fixture. Cats, parrots, snakes and hamsters follow only where their rules touch proven native systems.
4. **Gym machine.** One Stairmaster: ordinary use, temporary effect, unsafe hacked mode and interruption. Test use, sabotage, destruction while occupied and leaving the level. Jim's Gym chunk becomes the second authored encounter. Noise/fire/electricity are later independent probes.
5. **Appointments and dates.** Add a small scheduler for two simultaneous appointments; start with normal, flake and helpful outcomes selected directly in test mode. Use a fixed motel/bar fixture. Add thief, cop, gang member and jealous partner only after cancellation, level change and native faction behavior are understood.
6. **Second wave.** General parcel delivery can reuse the courier path; black market, tip line, handyman and send-an-item add their own custody/target/response tests. Pawn Shop, Storage, Clinic, Library, Media/Audio Store, Laundromat, Arcade and DMV become level-plus-mechanic pairs, prioritized when a demonstrated system makes them interesting. Additional props include vending machine, ATM, copier, dishwasher, refrigerator, breaker box and sprinkler controls. Deep simulation and isolated minigames stay low priority.

## Test and release gates

For each experiment: a deterministic policy check if useful; a known fixture with outcome selection; a log of build/session/scenario/seed and before/after state; one short user-visible test; then an ordinary run to judge surprise. Reproducible setup does not require identical native NPC behavior. Duplicate items, lost controls, runaway spawns and broken loading block release.

Workshop waypoints are separate from plugin installation. First, make a small vanilla-compatible chunk pack worth playing on its own. Later, test Pet Store/Gym chunks together with the plugin and describe the dependency clearly. Campaign/Chunk Pack/Character are visible Workshop categories; whether our code can be distributed there is **not established**. A public plugin package lists only game versions and platforms actually tested. Publishing requires a separate decision after local loading and clean-install checks.
