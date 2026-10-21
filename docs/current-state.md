# Current state — October 20, 2026

> **What this is:** One page on what works, what does not, and how much weight each kind of evidence can bear.  
> **Read it if:** You want an honest status report before reading anything deeper.  
> **Status:** Current as of October 20, 2026.

The maintainer has paused behavior development to consider better foundations
for **NPC-to-NPC interaction**. The repository is being prepared as a useful
source/tooling reference. No replacement platform has been selected. Existing
experiments and evidence are retained.

## What exists

| Area | Implemented / observed | Remaining boundary |
| --- | --- | --- |
| World Awareness | Registry/spatial hash, range/tag/kind filtering, lifecycle retirement, sampled perception/navigation, paused UI, capture and readers | Partial sampled coverage; not omniscience or a full path solver |
| NPC autonomy | One opted-in native Hobo; Fud/Banana choice and retained pickup; policy v2 respects danger/obligations; v1 replay retained | No general social simulation, hunger/eating, theft, bargaining, multi-role or multiplayer rollout |
| Native coordination | Hobo identity and WanderFar eligibility corrected; owned-goal release and replacement-command preservation | Natural-resident food pickup and paid menu recruitment remain incomplete |
| Banana favor | Once-per-run shared objective, two recipients, exact transfer, competing rewards, startup/menu/lifecycle guards | In-memory history; kit route lacks separate human acceptance |
| Delivery/services | Paid native courier, routing/fallback, door knocking, reputation, IMGUI phone | Legacy prototype; limited layout/run-boundary coverage |
| Dog | Visual proxy over native Courier, follow/release, food and cosmetic hydrant interests | Parked; no adoption/species system or cross-level persistence |
| Aquarium | Native Table with drawn aquarium and bounded break/scatter effect | Parked; no inventory product, Pet Store or verified electrical behavior |

## Evidence vocabulary

| Evidence | Establishes | Does not establish |
| --- | --- | --- |
| Source/assembly inspection | A code path, guard or primitive exists in the inspected version | That an encounter used that path |
| Offline policy checks | Specific pure input/output behavior and regressions | Native movement, perception or transfer |
| Replay | Captured input reproduces expected policy output | Replaying the game world |
| Native fixture | Particular APIs and resulting state in a controlled profile | Every map/actor, physical menu interaction, fun or release readiness |
| Human playcheck | A described person-visible encounter/readability result | Universal behavior or exhaustive correctness |
| Benchmark | Measured configuration and comparable trials | Other hardware or unmeasured combat/hazard/allocations |

Missing closure, dropped capture, failed pilots and unavailable local evidence
stay incomplete/unknown. A reader's zero exit code can mean structurally readable
evidence with skipped/inconclusive outcomes; read the actual report.

## Prior checkpoints and their owners

- [coordination.md](experiments/03-npc/coordination.md): October 13 corrected native suite,
  **126 gates**, build `0.6.0+20261013-063045`, session `e39185fd`. Natural Hobo
  enrollment succeeded in `37d6a709`; food pickup and paid recruitment remained
  pending. Earlier SlumDweller fixture passes do not prove canonical role compatibility.
- [04-banana-favors.md](experiments/04-banana-favors.md): corrected automatic startup, **96 native
  checks** in `8aadb87d`; normal cash session `953d4ccc` had actual transfer and
  $15 reward with user acceptance. Kit route is fixture evidence.
- [World requirements](experiments/01-world-awareness/requirements.md) and
  [validation](experiments/01-world-awareness/validation.md): targeted functional and controlled
  busy/performance results. Full 30 FPS release acceptance remains open.

These are dated results from retained local artifacts and checked-in handoffs.
Original `.runtime/` evidence is absent from public source. Reproduce with the
harnesses; see the [gallery](screenshots.md) for portable visuals. Fresh capture
verification is recorded in [publication-check.md](maintainers/publication-check.md).

## Runtime versus historical pause

The October 13 resident session was a historical stopping point, not a promise
that a game is still running. October 20 documentation capture uses a fresh
disposable fixture; it does not complete that ordinary-play session. A compiled
build label identifies loaded code; branch names, source timestamps and an old
handoff cannot establish the installed DLL.

## Practical limitations

Build, test and launch installation paths share `GamePaths.props`, with defaults
under the current user's home directory and environment overrides. The Mac layout
and process guards remain platform-specific. There is no supported installer, stable external API, verified
Windows/Linux port or established Workshop code-installation flow. The SDK is
the existing .NET 7 toolchain; an upgrade has not been evaluated. The profile
patcher redirects save-path calls in memory and fails when it cannot patch them.
It separates saves; it is not a security sandbox.
