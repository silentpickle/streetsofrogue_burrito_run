# NPC interactions — scope and initial findings

For the October 12 dialogue-to-behavior rundown and current overlap findings,
see [NPC_DIALOGUE_BEHAVIOR_MAP.md](NPC_DIALOGUE_BEHAVIOR_MAP.md). It distinguishes
native capabilities, the optional favor, design proposals, and the older food
fixture's unresolved canonical NPC identity gap.

October 7, 2026. The user paused all level-design and setup work and selected a review of all NPC interactions. This document records the new scope and initial read-only findings. It does not claim that a complete audit or any new behavior has been implemented.

## Active scope

- Inspect existing native NPC rules and observable consequences in Streets of Rogue 1, our installed mod target.
- Review survival, territory, property, perception, witnesses, relationships, factions, trade, services, cooperation, refusal and commitments across NPC roles.
- Begin with low-health self-preservation and territory. Ordinary fear/desire calculations are the proposed decision layer. Local/hosted models may express decisions later; they are not required to make them.
- Preserve existing code, levels, profiles and evidence. New level authoring, setup automation, fixture construction and continuation of previous setup/playtest tasks are paused.
- Research breadth does not authorize a rewrite of every NPC or replacement of native AI. Select concrete behavioral changes after identifying existing rules and gaps.

## Territory already present in installed SoR1 code

Read-only Mono.Cecil inspection of the installed `Assembly-CSharp.dll` found:

- `TileData.owner`, `privateProperty`, `privateProperty2`, `chunkID` and prison metadata; `Agent.ownerID`, starting-chunk identity and warning state.
- `TileInfo.DeterminePrivateProperty` assigns private-property metadata using owner, chunk and property-protection rules. It also propagates ownership to eligible world objects/items.
- `Relationships.ProtectOwnedLight` compares tile ownership and NPC ownership/starting chunk, with actor-specific branches and relationship/perception checks. Eligible branches issue `AnnoyedGetOut`, emit a `Trespassing` noise, set a warning target and use an annoyance countdown. A later eligible branch calls `SetRelHate` and can engage native response logic.
- These are concrete property-boundary/enforcement foundations. They do not establish a universal system for personal space, contested territory, negotiated access or territorial memories across every NPC type.

This is inspected code, not a controlled runtime demonstration. Exact role coverage, permissions, exemptions, chase boundaries and escalation outcomes remain to be traced. The `ProtectOwnedLight` method also handles security/mugging cases; its branches must not be generalized to every shopkeeper or civilian.

## Self-preservation already has a native calculation

`Relationships.AssessFlee` reads the NPC's current and maximum health. Native flee flags/goals also exist. The user's observation that some nearly dead NPCs keep fighting therefore calls for tracing arbitration, exemptions and combat behavior, rather than assuming the game has no health-sensitive fear calculation. Whether fleeing suppresses attacks and how the NPC chooses a safe destination remain open.

## SoR2 context, checked separately

The [official developer update dated October 6, 2026](https://steamcommunity.com/app/2165810/announcements/) reports business owners objecting to players going behind counters, making customers leave before closing, and stronger prevention of following owners through closing doors. Earlier official patch notes describe lighter and stricter off-limits areas, explicitly marked internal rather than visible in that demo.

The public Notion roadmap could not be fetched directly during this check. Do not quantify its NPC-versus-level emphasis or treat a sparse roadmap entry as proof that a native feature is absent. SoR2 developer reports are separate from evidence about the installed SoR1 assembly.

## Next research questions

1. When does low health actually cause retreat, and which rules prevent or override it? Can a fleeing NPC still attack?
2. Who owns/protects each existing space, who is allowed in, and what makes a warning become hostility?
3. What changes the response: observed threat, relationship, faction, theft/damage, repeated intrusion, allies and escape routes?
4. Which interactions already support help, trade, recruitment, refusal or de-escalation, and which would require new rules?

Candidate territory response: notice an intrusion, check permission and stakes, then choose ignoring, warning, escorting out, seeking help, fighting or retreating. A shopkeeper at 1 HP may still value the shop while choosing survival. This is a design candidate, not an installed behavior.

Audit evidence: `.runtime/npc-territory-audit-20261007/`; `receipt.json` records the source assembly and audit-output hashes. No plugin build, installation, game launch or world mutation was performed for this review.

## October 12 implementation follow-up

The user authorized tooling followed by one-civilian Fud/Banana choices, with a narrow disposable-fixture exception to the setup pause. That milestone is implemented and native-validated; see [NPC_DESIRES.md](NPC_DESIRES.md) and [NPC_DESIRES_HANDOFF.md](NPC_DESIRES_HANDOFF.md). The broader interaction audit and survival/territory design remain separate future work.

## October 12 player observations: native menus and gangs

The player's afternoon screenshots show native Slum Dweller and Thief
`Assist Me - $20` menus, Gangster (Blahd) bribery/threaten/protection choices,
and the ordinary Slums 1 retrieve objectives. The player also observed a Blahd
and Crepe fighting. The screenshots do not demonstrate our banana encounter;
its separate startup failure and correction are recorded in [BANANA_QUEST.md](BANANA_QUEST.md).

Fresh inspection of the installed SoR1 assembly confirms:

- `AgentInteractions.DetermineButtons` dispatches native `Hobo` to `SayDialogue("Interact")`,
  then conditionally offers `AssistMe` with `HoboAssist` pricing. The Thief branch
  uses native `AssistMe` and `ThiefAssist`. These are existing dialogue/services,
  not added personality or generated dialogue. Exact funny lines were not captured.
- `Relationships.SetupRelationshipOriginal` aligns same-type `Gangbanger` /
  `GangbangerB` actors. Its `HatesCrepes` / `HatesBlahds` branches can establish
  `Hateful` and hate value 5, with quest, transformation and special-ability
  exceptions. This supports native gang rivalry; it does not establish the exact
  initiating event in the player's fight.
- Our favor hooks append namespaced choices only for the encounter's exact two
  living recipient identities. Other buttons pass through native handlers.
  The separate Fud/Banana autonomy experiment remains disabled and unenrolled
  in normal favor play. No general hostility or dialogue rewrite was made.

Evidence and assembly checksum:
`.runtime/banana-startup-fix/20261012T210014Z/native-inspection.json`,
`native-npc-menus.txt` and `sor-native-rel-20261012.txt` in the same directory.
This is a focused baseline check, not completion of the wider NPC audit. Future
preferences and fears should account for native employment, hostility, property
and quest commitments before proposing or taking an action.
