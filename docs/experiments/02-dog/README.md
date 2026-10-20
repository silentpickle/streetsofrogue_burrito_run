# 2 · The dog (October 6–7, 2026)

**In one sentence:** a following companion (a game Courier NPC drawn as a dog)
that notices dropped food and fire hydrants, makes a short detour, and comes back.

**Why it exists:** it was the first real consumer of World Awareness. These behaviours
are small and easy to see, with clear stop conditions, so they made a good test of
whether we could add behaviour without fighting the game.

**Where it ended up:** both behaviours pass their automated in-game checks
(66 and 67 gates) and the dog was judged "decent" in ordinary play. Parked: there
is no adoption system, no other species, and it does not persist between levels.

## Reading order

1. [road-to-the-dog.md](road-to-the-dog.md): the readiness checks that came first.
2. [food.md](food.md): sniff food, or eat it when injured.
3. [hydrant.md](hydrant.md): walk over, brief cosmetic mark, return to following.

Source: `src/BurritoRun/Pet*.cs`.
