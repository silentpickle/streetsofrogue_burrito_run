# Ethos, parked experiments and future questions

The aim was emergent comedy and understandable interactions using existing
game systems. Burrito delivery made that tangible: a courier could arrive,
get delayed, knock, be attacked, lose food, and leave the player to deal with
the consequences. The investigation then expanded into observing NPCs and
proving small actions without scripting every outcome.

The desired destination is richer **NPC-to-NPC interaction**. The current
project is paused because making more player-facing encounters and isolated
pickup actions does not by itself supply that foundation. Another engine or
simulation may make the central question easier to explore. The tools and
lessons are still worth sharing independently of that decision.

## Principles worth carrying forward

- Start with an interaction someone can notice and describe. A successful API
  call is useful evidence, but its label is not the experience.
- Prefer deterministic, inspectable decisions for wants/fears/priorities.
  Models could express a decision in dialogue later; current behavior needs none.
- Respect native commitments. Employment, recruitment, property and danger are
  existing systems to understand before adding competing priorities.
- Build observation, replay and repeatable setup early. Keep observational tools
  separate from explicit setup/command writes.
- Bound attempts, queues, spawns and timers. Preserve newer native work during
  cleanup. Keep unsuccessful runs and make uncertainty visible.
- Preserve playful native consequences. A courier stepping aside can be more
  interesting than perfect scripted arrival; failures still need a clear outcome.
- Choose one small real encounter before multiplying roles, species, menus or
  infrastructure. Fun and clarity require a human judgment alongside mechanics.

## What is retained, shelved or only proposed

“Parked” means retained for study or a future explicit decision. It does not
mean deleted, promised next, or certified for release.

| Experiment / idea | Status | Useful remains / unresolved question |
| --- | --- | --- |
| Delivery, Services phone and reputation | Implemented legacy prototype; phone shipping shelved | Native courier/routing/fallback/knocking and account rules; presentation and run-boundary/layout coverage incomplete |
| One-resident food preference | Implemented, opt-in and bounded | Pure choice/replay and native ownership; not hunger or a social exchange |
| Banana favor with competing rewards | Implemented optional player encounter | Shared objective, exact transfer, native menus/speech; not autonomous negotiation |
| Dog follow/food/hydrant | Implemented limited slices, parked | Native proxy/movement, cosmetic marking, interruption/capture patterns; no broad animal simulation |
| Bark/guard distraction | Probe exists; useful distraction unproven, parked | Native noise tracing and matched movement analysis; observed reaction activation did not demonstrate useful payoff |
| Aquarium/shrimp scatter | Visual/native-prop experiment, parked | Native Table fallback, break/scatter; real FishTank prefab assumption failed; electricity unverified |
| Pet Store, Gym and authored chunks | Proposed; broader level authoring paused | Placement/reproducibility questions; no finished chunk pack or Stairmaster gameplay |
| Appointments, dates, scheduler | Proposed | Need commitments, cancellation, interruption, competing simultaneous tasks and visible stakes |
| Parcel/black-market/handyman/pawn/storage services | Proposed | Courier seam offers a starting point; custody/trade/contracts are not generalized |
| Territory, survival, warnings and retreat | Native foundations audited; broader decision layer proposed | Distinguish permission, witnesses, attribution, stakes and retreat; no universal territorial memory |
| Relationships, values, social memory, bargaining | Proposed | Need persistent identities, bounded memory, consent/obligation rules and observable consequences |
| Model-generated dialogue | Optional idea, unimplemented | Must represent actual state/actions; no reason to hide decision authority inside unconstrained text |
| Other species, persistent pets, detailed barriers | Deferred | First justify a new interaction; cross-level pet persistence was deliberately left at “probably no” |

Original ordering and exploratory details remain in [ROADMAP](../ROADMAP.md),
[PROJECT_RECORD](../PROJECT_RECORD.md) and [projectbrief.txt](../projectbrief.txt).
They record the investigation rather than an active backlog.

## Why this is harder than adding another NPC action

Many inspected native interactions begin with player menus, prices, selected
recipients, quests, employers or role-specific jobs. Invoking them from another
actor requires understanding those assumptions. Native sensory/ownership rules,
pooled identities, goal termination and partial job replacement also make an
apparently simple action stateful. Display names can differ from native role IDs.

The project solved parts of observation, bounded action ownership and outcome
verification. It has not solved reciprocal negotiation, competing claims over
one resource, durable social memory, a town-wide scheduler or player-readable
NPC-to-NPC causality. Adding an LLM does not automatically provide those systems.

## A brief for surveying alternatives

This is a proposed evaluation protocol, not a researched platform recommendation.
Compare continuing this mod, building a small standalone simulation, and adapting
another extensible game against the same encounter:

> Two NPCs want one useful item. One already owes someone a task. They notice
> each other, resolve the competing claim in an observable way, and react sensibly
> when the item disappears or danger interrupts them. The player can watch.

Use ordinary deterministic decisions first. Compare access to world state and
action APIs, first-class NPC-to-NPC interaction, interruption/cancellation,
identity/memory, repeatable setup/replay, observation UI, content effort,
distribution constraints and how quickly a person can judge the result.
Record failure as well as success. A platform survey should inspect current
primary docs and test a small actual artifact before selecting a replacement.

If returning here, a plausible small next design question is one reciprocal
interaction between two existing roles with a verified obligation conflict.
It needs its own scope and acceptance criteria. Documentation preparation does
not authorize resuming that work or the parked dog/level experiments.
