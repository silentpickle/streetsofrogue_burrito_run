# NPC dialogue, existing behavior, and our planned additions

> **What this is:** What each NPC dialogue option actually does in the game code, and where our additions overlap.  
> **Read it if:** You plan to add NPC interactions and do not want to duplicate the game's own.  
> **Status:** Dated audit, October 12, 2026.

**Implementation follow-up:** [coordination.md](coordination.md) tracks
the canonical Hobo fix and native-obligation coordination. This map records the
preceding read-only audit; its statements about the then-unfixed identity gap
are historical. Current verification status belongs to the follow-up.

October 12, 2026. Streets of Rogue 1, using the installed game assembly and this
checkout. This is a documentation and native-code audit, with no new gameplay
changes, builds, installation or game restart.

**The game already has substantive NPC behavior behind its dialogue and menus:**
paid recruitment, specialist jobs, bribery, intimidation, faction relationships,
property enforcement, combat and fleeing. Our work needs to account for those
systems when adding desires, fears, favors and personality. Whether a rule was
implemented as a special case does not make its consequences less real.

## What the evidence establishes

- **Player-observed:** the supplied NPC/mission screenshots; the reported
  Blahd/Crepe fight; the later successful banana playcheck and dialogue feedback.
- **Code-inspected:** native rules found in the installed assembly. A possible
  branch is not proof it ran in a particular encounter.
- **Prototype-verified:** a bounded result from our code and recorded tests.
  Fixture results do not establish every naturally generated NPC interaction.
- **Proposed:** extensions or sample dialogue below. These are not installed.

The exact funny Slum Dweller sentences were not transcribed. We can identify
the native dialogue route without pretending to know those words. Native keys
such as `Joined` or `AnnoyedGetOut` below are internal trigger names, not exact
English dialogue quotes. Evidence links and methods are listed at the end.

## The dialogue and menus we actually discussed

| Seen or mentioned | Existing meaning | Overlap with our plans | Evidence |
| --- | --- | --- | --- |
| Funny Slum Dweller talk on interaction | Native `Hobo` interaction calls `SayDialogue("Interact")`; the game has class-specific dialogue lists. Exact funny lines are unavailable. | Much of the character voice already exists. Preserve that voice when adding an offer; a joke alone does not establish a simulated need or backstory. | Player report; N1, N2 |
| Slum Dweller: **Assist Me - $20** | A real recruitment service. `AssistMe` says native `Joined` and queues a party join. The hired Hobo menu can offer **Cause a Ruckus**; its action assigns the native Ruckus job and location. Availability depends on native state. | Direct overlap with cooperation, paid help, commitments and distraction. A food desire must yield to employment and assigned work. | Screenshot; N1, N3 |
| Thief: **Assist Me - $20** | Native recruitment, followed by role-specific **Lockpick Door** work with task restrictions. The Thief is already useful beyond its chatter. | Reuse the existing hire/task pathway before inventing a parallel companion or favors-for-lockpicking system. | Screenshot; N1, N3 |
| Gangster (Blahd): **Hire as Protection - $30** | Native protection recruitment includes qualifications and party joining. Health, prisoner/dismissal state, follower rules, traits and money can affect eligibility. | Direct overlap with loyalty, money, bodyguards and cooperation. A new desire should respect the existing employer and follower relationship. | Screenshot; N1, N4 |
| Gangster: **Bribe with Item**, **Bribe - $5** | The native menu offers these on a live accepted KillAndRetrieve quest-item route. Acquisition can transfer the target item and mark the quest's `boughtItem` state. Other entrance bribes have separate access/relationship effects. | Money already buys particular outcomes. Identify the specific bribe and quest/access obligation before adding alternate payment or negotiation. | Screenshot; N1, N5 |
| Gangster: **Threaten (37%)** | This quest-item route uses native `FindThreat` probability. Success transfers the objective item and triggers `ThreatenedScared`; failure can trigger `ThreatenedAngry`, Hateful/hate 5, an attack and enforcer alerts. | Overlap with fear and bargaining. The displayed percentage is not a universal fear value, affection score or guarantee that an NPC will flee. Calling this handler for our banana favor would also touch native quest state. | Screenshot; N1, N5 |
| Blahd and Crepe attacking each other | Native gang alignment and `HatesBlahds`/`HatesCrepes` rules can establish hostility, with exceptions. The exact initiating event in this fight was not captured. | Hatred and faction allegiance already exist. Personal preferences or memories must reconcile with them rather than silently creating another conflicting allegiance. | Player report; N6 |
| Map: **Retrieve**, **From**, **Reward**, **Not Complete** | Native main objectives, target identities, rewards and progression. | Strong overlap with our word “quest.” The banana favor has its own completion state and leaves these objectives and exit requirements alone. | Screenshot; P1, P2 |
| Banana offer, exchange and **Thanks!** | Our scripted optional contract, presented through native NPC menus and speech bubbles; native items move and a selected reward is granted. | Proves a small favor can feel part of the game. It does not yet prove autonomous hunger, bargaining, gratitude memory or an NPC economy. | User playcheck; P1, P2, P3 |

The dollar amounts above are the observed menus, not universal prices for every
character, relationship, trait or run.

### Related native overlaps worth keeping on the map

These were found in code during this review; the supplied screenshots do not
demonstrate that the player used them.

| Existing interaction | Inspected effect / boundary | Why it matters |
| --- | --- | --- |
| **Offer Drink** | Beer/Whiskey can heal and set Friendly, while preserving Hateful/Aligned/Loyal/Submissive states. Native big-quest/election effects also exist. | Generosity already has social consequences. A new gift system could accidentally apply friendship or progression twice. [N10] |
| **Give Item** | Native transfer can lead to equipment selection or immediate use depending on item and employment/control relationship. | A gift, payment, stored banana and eaten banana must be distinguished. Giving does not imply universal friendship gain. [N7] |
| Doctor **Heal** | Native payment and health change, with full-health and body/trait-related refusals. | A useful service already has price, eligibility and an effect. New fears or favors should honor those conditions. [N10] |
| **Buy**, vouchers and borrowing | Native shop/stock and some voucher/borrow menu paths exist. Method names for repayment, investment and withdrawal also exist. Their full lifecycle was not traced here. | More money choices can reuse existing services. Buying evidence is not proof of selling to any NPC, finite budgets, debt persistence or a general economy. [N10] |

## What our custom dialogue currently says

These are our own source strings, distinct from native NPC dialogue. “Buyer” and
“Neighbor” are encounter labels; both current recipients use the native Hobo
archetype. The menu heading can still say **Slum Dweller**. [P1, P2, P4]

| Current text | Where it appears | What it promises or does |
| --- | --- | --- |
| **Hear banana offers** | Added interaction-menu choice | Accepts the shared favor; it does not select the winning recipient. |
| “One banana. Buyer pays $15; Neighbor offers a first aid kit. Choose either of us.” | Native speech bubble after acceptance | Explains the prototype's two alternatives. Both actors deliver the same exposition, which can feel unnatural; the user's exact awkward lines were not identified. |
| **Give one banana for $15** / **Give one banana for First Aid Kit** | Added menu choice for the respective recipient | Attempts the actual exchange, checking item availability and inventory capacity. |
| “Thanks! A deal's a deal. Here's $15.” | Buyer's speech after verified completion | Reports the completed cash outcome. |
| “Thanks! Take this first aid kit. Stay safe.” | Neighbor's speech after verified completion | Reports the completed kit outcome. |
| “Bring one banana first.” / “Make room for your reward first.” | Mod notice, not necessarily an NPC speech bubble | Refuses an exchange without paying or consuming the banana. |

**Current contract limits:** one banana goes to one recipient; only that reward
is paid; both offers then close. The reward is added by our favor code. It is
not deducted from the recipient's wallet or medical supplies. The actor retains
the banana; our favor does not make them eat it or become friendlier. There is
no completed-favor history across quitting/reloading, native XP award or native
main-quest completion. [P1, P2]

**Proposed copy, not shipped:** Buyer: “Got a banana? I'll give you fifteen bucks.”
Neighbor: “Bring me a banana and this first aid kit's yours.” Each can talk about
their own offer while the tracker explains the competing choices. “Been
starving,” suggested in conversation, is also unshipped; hunger is not currently
a calculated need. Authored flavor is fine, but we should distinguish it from
a state that actually changes decisions.

## How the larger ideas map to existing systems

| Idea we discussed | Foundation already present | Our current addition | Useful next seam / unresolved overlap |
| --- | --- | --- | --- |
| Fud versus Banana; desires/preferences | Native items, food use, ownership, inventory and pickup. | A separate experimental policy scores Fud/Banana preference minus distance and executes one owned pickup action. It collects and retains food. | First correct the native-role identity gap below. Then distinguish wanting, collecting, eating and trading; they have different outcomes. [N7, P5] |
| Fear and low-health self-preservation | `AssessFlee` already uses health deficit, threat/scary state, distance, hatred, toughness and projectile-weapon information. Native flee/combat goals exist. | Food actions cancel on native emergency signals. We have not replaced general combat arbitration. | Trace why a particular injured NPC continues attacking, which exemptions apply, and whether retreat actually creates distance and stops fighting. More fear dialogue alone would not resolve it. [N8, P5] |
| Love/hate of particular NPCs | Native relationships, hatred, faction traits, employers and followers; offering a drink can change a relationship. | No general new affection model in normal favor play. | Define a specific change, such as remembering one completed favor, and reconcile it with native allegiance. This audit does not establish a universal romance/love system. [N3–N6, N10] |
| Attributes and personality | Native roles, traits, toughness, scary state, health and relationships already affect branches. | The food experiment has two preference settings; the favor has two authored offers. | Add only a value that changes a feasible choice. Reuse native state where it already means what we need; keep stable values separate from current fear/desire. [N4, N6, N8, P5] |
| Backstories | Class-specific interaction/idle/reaction dialogue supplies existing character flavor. | No new persistent biography or social history. | Author a small motive or voice variation first. Distinguish authored biography from remembered events and from an actual need; dynamic backstories were not verified in this audit. [N2] |
| Richer dialogue | Native dialogue categories include interaction variants, idle, starting battle, fleeing battle/danger and reacting to noise. | Fixed favor lines use native speech bubbles; no model calls. | Choose a short line from the actual role, event and outcome. Preserve native humor and avoid unrelated chatter contradicting an active agreement. [N2, P1] |
| More money options | Recruitment, specialist services, protection, bribes, healing and shop purchases already expose paid choices. | Fixed cash/kit alternatives; preserved delivery code charges $10. | Specify whether an action is a purchase, bribe, donation, service fee, barter or quest reward. An actual economy needs recipient budgets/stock; our current favor grants rewards. [N3–N5, N10, P1, P6] |
| Favors and commitments | Native main quests, employer/follower state and assigned jobs. | A separate optional one-objective favor, with verified inventory/reward effects and interruptions. | Attach content to eligible existing actors only after checking native obligations. Decide explicitly whether success affects relationships, native progression, both or neither. [N3, P1–P4] |
| Territoriality and permission | Native property ownership, warnings, annoyance countdown, trespassing noise and escalation. | The food probe rejects owned/stealable/quest items. | Reuse ownership and permission checks. A dropped item is not automatically available, and a relationship change may affect access. [N5, N8, P5] |
| Reputation and grudges | Native hatred, relationship and faction rules. | Parked courier safety policy records assaults, changes later armed-driver chance and clears flags through peaceful handoffs. | Reuse that learning without assuming it is a universal social-memory system. Account reputation, individual dislike and faction hostility have different scopes. [N6, P6] |

The inspector's ability to find an entity is also distinct from what a particular
NPC knows about it. A decision should use that actor's perception, ownership,
obligations and current danger, rather than unrestricted world knowledge. [P5]

## Important integration gaps this review exposed

1. **Canonical NPC identity needs correction in the older food experiment.**
   `NpcPickupProbe.Enroll` requires `actor.agentName == "SlumDweller"`, and
   `NpcDesireFixture` spawns that name. Native `Agent.agentName` returns the stored
   identifier without translating a display name; the native stats/dialogue role
   is `Hobo`. A naturally spawned Hobo therefore fails that enrollment predicate.
   The old fixture results prove the fixture's behavior, not canonical Slum
   Dweller compatibility. The banana encounter already uses `Hobo`; this review
   makes no runtime fix to the older experiment. [N9, P4, P5]
2. **Native recruitment changes the meaning of later interactions.** A hired
   Thief has work and a follower obligation; a neutral resident may become
   friendly or gain access-related state after a bribe. These changes need to be
   rechecked before a desire or favor commands that actor. [N3–N5]
3. **Giving food is not one universal action.** Native `GiveItem` has transfer
   and use-item branches depending on item and employment/control relationship.
   Auto-use is not a rule for every gift to every NPC. Our favor uses explicit
   add/subtract and verifies custody before rewarding. Ordinary giving, eating,
   barter and objective delivery must not share an assumed outcome. [N7, P1]
4. **Our current recipients are spawned for the encounter.** It does not yet
   select two existing residents with their established jobs, allegiances and
   quests. Their `DoNothing` setup and later `WanderFar` release are acceptable
   only as this encounter's explicit setup; that pattern must not overwrite an
   arbitrary hired or working NPC's goals. [P4]
5. **Displayed personality can get ahead of mechanics.** The banana offers are
   scripted; the reward is granted; thanks does not create affection. Existing
   native dialogue can coexist with the added choices. Future dialogue should
   express a selected decision or verified result, and promises should correspond
   to a contract we can actually fulfill. [N1, P1–P4]

## A practical direction for existing NPCs

Keep the playable banana favor as a small proven interaction. For a next
behavioral slice, select an actual idle native Hobo after fixing and validating
the identity issue. Record its current role, relationships, employer, job,
health, danger and nearby permissible food. Let one comprehensible preference
change its choice, and release the exact action when native work or danger takes
precedence. This is a proposal, not authorization to widen enrollment now.

A later player-facing favor could reuse that actor's short native interaction
and add a request only while eligible. Its offer would have an explicit reward
policy, transfer rule and cancellation condition. A recruited Thief or Gangster
should initially keep their established native services; adding parallel
employment, lockpicking or protection state would create unnecessary conflict.

For any extension, the useful demonstration is: the player understands why this
NPC chose this action; the promised effect actually happens; an existing native
obligation or emergency interrupts it correctly; and the NPC can resume native
behavior afterward. No dialogue overhaul or large attribute/backstory system is
needed to establish that first example.

## Evidence and source map

Primary source: installed SoR1 `Assembly-CSharp.dll`, SHA256
`4d82cb026f3830f4771a855efe8b4d8dc1f3b2bd4115c371485687bb43a91025`.
Its current hash matches the earlier territory/flee audit. Native output is a
read-only Mono.Cecil inspection, not a new live gameplay test. Extracted evidence
under `.runtime/` is local and ignored; it is not a portable release dependency.

- **N1:** [`Agent.DetermineButtons`](../../../.runtime/npc-dialogue-audit-20261012T235300Z/Agent-DetermineButtons.txt) and earlier [`AgentInteractions.DetermineButtons`](../../../.runtime/banana-startup-fix/20261012T210014Z/native-npc-menus.txt): native menus, Hobo `Interact`, pricing and role-specific hired tasks.
- **N2:** [`Agent.SayDialogue`](../../../.runtime/npc-dialogue-audit-20261012T235300Z/Agent-SayDialogue.txt), [`DialogueType`](../../../.runtime/npc-dialogue-audit-20261012T235300Z/DialogueType-index.txt): class lookup and dialogue categories. These establish routing, not the missing funny sentences.
- **N3:** [`AssistMe`](../../../.runtime/npc-dialogue-audit-20261012T235300Z/AgentInteractions-AssistMe.txt), [`JoinParty`](../../../.runtime/npc-dialogue-audit-20261012T235300Z/Relationships-JoinParty.txt), [`CauseRuckus`](../../../.runtime/npc-dialogue-audit-20261012T235300Z/AgentInteractions-CauseRuckus.txt), [`LockpickDoor`](../../../.runtime/npc-dialogue-audit-20261012T235300Z/AgentInteractions-LockpickDoor.txt): recruitment and specialist action assignments.
- **N4:** [`HireAsProtection`](../../../.runtime/npc-dialogue-audit-20261012T235300Z/AgentInteractions-HireAsProtection.txt): native qualification, payment/acceptance and recruitment branches.
- **N5:** [`Bribe`](../../../.runtime/npc-dialogue-audit-20261012T235300Z/AgentInteractions-Bribe.txt), [`Threaten`](../../../.runtime/npc-dialogue-audit-20261012T235300Z/AgentInteractions-Threaten.txt): context-specific access/relationship/quest consequences.
- **N6:** [`Relationships.SetupRelationshipOriginal`](../../../.runtime/banana-startup-fix/20261012T210014Z/sor-native-rel-20261012.txt), especially IL `108e`–`1244`: gang alignment, hate traits and exceptions.
- **N7:** [`InvDatabase.GiveItem`](../../../.runtime/npc-dialogue-audit-20261012T235300Z/InvDatabase-GiveItem.txt), especially employer/previous/former-employer checks near IL `0228`–`024e` and later `UseItem`: transfers and conditional automatic item use.
- **N8:** [`AssessFlee`](../../../.runtime/npc-territory-audit-20261007/sor-npc-assess-flee.txt), [`ProtectOwnedLight`](../../../.runtime/npc-territory-audit-20261007/sor-npc-territory-il.txt), [audit receipt](../../../.runtime/npc-territory-audit-20261007/receipt.json): existing fear and property calculations. Full arbitration/witness coverage remains open.
- **N9:** [`Agent.get_agentName`](../../../.runtime/npc-dialogue-audit-20261012T235300Z/Agent-get_agentName.txt), [`SetupAgentStats`](../../../.runtime/npc-dialogue-audit-20261012T235300Z/Agent-SetupAgentStats.txt), [`LoadDialogue`](../../../.runtime/npc-dialogue-audit-20261012T235300Z/Agent-LoadDialogue.txt): canonical actor identifier and Hobo role configuration.
- **N10:** [`OfferDrink`](../../../.runtime/npc-dialogue-audit-20261012T235300Z/AgentInteractions-OfferDrink.txt), [`Agent.PressedButton`](../../../.runtime/npc-dialogue-audit-20261012T235300Z/Agent-PressedButton.txt) lines 3043–3118 (Heal), [`DetermineButtons`](../../../.runtime/npc-dialogue-audit-20261012T235300Z/Agent-DetermineButtons.txt) lines 4087–4150 (Heal/Buy/voucher/borrow), [method registry](../../../.runtime/npc-dialogue-audit-20261012T235300Z/agent-interactions-index.txt) lines 28–31: direct service branches versus untraced economy capabilities.
- **P1:** [`QuestService`](../../../src/BurritoRun/QuestService.cs), particularly `Handle`, `Submit`, `CanTalk`, `AddButtons` and `Record`; [`Patches`](../../../src/BurritoRun/Patches.cs), `NamePatch`, `FavorButtons`, `FavorInteraction`: added choices, exact exchanges, notices and native speech.
- **P2:** [`QuestPolicy`](../../../src/BurritoRun/QuestPolicy.cs): fixed alternatives, one winning exchange and terminal states; [banana scope and validation](../04-banana-favors.md).
- **P3:** [96-check native report](../../../.runtime/world-harness/20261012-150511-77399/report.md). Later normal session `953d4ccc`, [game log](../../../.runtime/logs/game-20261012-151326-77887.log): offered at game time 61.92, accepted at 73.96, cash completion at 80.87; player bananas 1→0, recipient bananas 0→1, money 0→15. The user confirmed it works and feels native; the kit alternative is automated-test evidence, not a separately reported human playcheck.
- **P4:** [`BananaEncounter`](../../../src/BurritoRun/BananaEncounter.cs), `Place` and `Release`: two spawned Hobo actors, owned identities, banana placement and bounded setup/release.
- **P5:** [`NpcDecision`](../../../src/BurritoRun/NpcDecision.cs), [`NpcDesires`](../../../src/BurritoRun/NpcDesires.cs), [`NpcPickupProbe`](../../../src/BurritoRun/NpcPickupProbe.cs), [`NpcDesireFixture`](../../../src/BurritoRun/NpcDesireFixture.cs): current preference, enrollment, ownership/perception/emergency checks and the identity gap. [Earlier checkpoint](desires-handoff.md) is historical fixture evidence subject to that gap.
- **P6:** [`CustomerReputation`](../../../src/BurritoRun/CustomerReputation.cs), [`DeliveryService`](../../../src/BurritoRun/DeliveryService.cs): preserved courier account, payment, assault and peaceful-handoff rules. This is not universal NPC reputation.

See also the [native interaction audit](interactions-audit.md) and
[extension guide](../../reference/extending.md). The broader audit is still incomplete;
specific absent features should be marked unverified until their relevant native
branches have been checked.
