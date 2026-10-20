# The experiments, in order

The project ran for about three weeks in October 2026. Each experiment
grew out of a problem the previous one exposed. The folders are numbered in
the order the work happened, and each folder has its own README with a reading
order.

| # | When | Experiment | One-line summary | Status |
| --- | --- | --- | --- | --- |
| 0 | Oct 1 | Burrito delivery, phone, aquarium, first pet | Order food on an in-game phone; a real game NPC carries it to you through the city | Legacy prototype. Notes are in the [project record](../history/project-record.md) and the [old README](../history/2026-10-20-development-README.md) |
| 1 | Oct 2–6 | [World Awareness](01-world-awareness/README.md) | A live index of everything on the level, a pause-and-inspect tool, and automated in-game tests | Working; full performance sign-off still open |
| 2 | Oct 6–7 | [The dog](02-dog/README.md) | A following dog that notices food and fire hydrants | Working; parked |
| 3 | Oct 7–13 | [NPC desires and coordination](03-npc/README.md) | A Slum Dweller chooses which food to pick up, without breaking the game's own NPC jobs | One NPC, opt-in; paused |
| 4 | Oct 12 | [Banana favors](04-banana-favors.md) | Two neighbours want the same banana; the player decides who gets it | Playable |
| — | Oct 20 | [Pause](../current-state.md) | Development stopped to rethink the foundation for NPC-to-NPC interaction | Current |

## Why it went in this order

1. **Delivery first.** The courier made the goal concrete: a game NPC with
   somewhere to be, and plenty of ways for that trip to go wrong. It also
   showed we could not tell what NPCs were doing or why.
2. **So we built World Awareness.** It indexes what is on the level and what each NPC
   can see and is trying to do. Automated tests run inside the real game, so a
   claim like "the dog ate the burrito" is checked, not assumed.
3. **The dog was the first user** of that index: small, visible behaviours with
   clear start and stop rules.
4. **Then NPCs in general.** An audit of what the game's NPCs already do, then
   one NPC making its own choice. A bug, where `SlumDweller` should have been
   `Hobo`, showed how easily our additions could clash with the game's own NPC
   behaviour, and that fix became the coordination work.
5. **The banana favor** was a deliberately small encounter a player could
   actually play, built from those pieces.
6. **The pause.** More player-facing encounters were not getting closer to the
   real goal of NPCs interacting with *each other*. See
   [ethos and future](../ethos-and-future.md).
