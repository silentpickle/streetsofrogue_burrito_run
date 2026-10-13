# Burrito Run — Streets of Rogue NPC experiments and tools

# ELI5: What are we trying to make?

**Give the people in Streets of Rogue more reasons to do things with each other.**

Imagine ordering a burrito in a city where everybody has somewhere to be.

The courier would like to finish the delivery. Someone nearby would very much
like a burrito. A third person needs a favor, preferably right now. Each of them
has a perfectly reasonable plan. Unfortunately, their plans involve the same
courier, the same doorstep, and rather more burritos than are available.

That is the sort of small, untidy story this project wants to make possible.
The full scene above is an ambition, and there is plenty left to build.

NPCs are the characters controlled by the game. The original idea was to give
them more things to want, use, offer and react to, then let those simple rules
meet in the street. A favor could become an inconvenience. An ordinary delivery
could become somebody's very strange afternoon. The player could help, meddle,
or stand a little way back and see how everyone gets on.

**What exists today:** small working experiments with burrito delivery, a banana
favor with two possible rewards, and one character choosing which food to pick
up. There are also earlier dog and aquarium experiments, because a neighborhood
can always use a dog and a questionable piece of furniture.

Along the way, we built tools to see what characters are doing, explain their
choices, and check whether an action really worked. It helps to know whether
someone actually collected the food before congratulating them on their lunch.
The details and remaining limits are below.

**Where the goal stands now:** the next step is richer interaction **between
characters**, including situations the player can simply observe. Development
is paused while we compare ways to support that goal. We have learned enough
from these experiments to ask better questions about what comes next. The code,
tools and discoveries are staying here for anyone who can use them.

**Why share it:** you might be building a mod, wondering how the game's
characters work, or carrying a different idea altogether. There may be something
useful here for you. We have left notes about what worked, what surprised us,
and what still needs figuring out. You are welcome to pick up a useful piece
and take it somewhere new.

The current experiments use ordinary rules and calculations. Running them
does not require an AI model or an online service.

## Technical overview

The current implementation is a BepInEx/Harmony prototype for **Streets of
Rogue 1**. It uses the installed game's characters, items and movement systems.

**Add-on only:** players need their own installed copy of Streets of Rogue 1.
Workshop distribution is an intended target; the current implementation loads
through BepInEx. This ZIP is a developer source package, and a subscribe-to-install
Workshop route for the code plugin has not been verified.

The reusable tooling includes a spatial world index,
perception/navigation inspection, bounded capture, independent report readers,
deterministic NPC decisions and replay, and disposable native test fixtures.

**Project status, October 20, 2026:** paused for a platform reassessment. The
current prototype provides building blocks for the larger goal. General
NPC-to-NPC social behavior remains future work.

## What is worth exploring

| Piece | What you can learn or reuse | Evidence and limits |
| --- | --- | --- |
| World Awareness | Nearby entities, activation identities, range filters, LOS/cone/camera distinctions, goal/path inspection | Native fixtures and developer-host benchmarks; sampled observations, incomplete release/performance coverage |
| NPC decisions and replay | Pure preference-minus-distance choices, commitments, native-obligation precedence, versioned captures | One explicitly enrolled native `Hobo` (Slum Dweller), Fud/Banana pickup and retention; autonomy off by default |
| Native action ownership | Bound an action, yield to recruitment/jobs/danger, preserve a newer command | Controlled pickup/replacement/release tests; ordinary food/recruitment playcheck remains incomplete |
| Evidence tooling | Detached recording, independent readers, negative controls, inventory/ground-state checks | Replay proves a policy result; fixtures prove particular native paths; neither proves fun |
| Banana favors | Native menus and speech, one shared banana, competing $15 / First Aid Kit rewards | Cash route received a human playcheck; kit route has automated native evidence |
| Earlier experiments | Courier delivery, reputation, entrance/door diagnostics, dog interests, aquarium effect | Preserved with observations and gaps; see the [experiment catalog](docs/ETHOS_AND_FUTURE.md) |

![World Awareness and quick-test tools in a disposable native NPC fixture](docs/images/native-npc-fixture.png)

Actual development capture, not a mockup. The fixture owns its actors; the UI
is developer tooling. [Gallery and provenance](docs/SCREENSHOTS.md).

## Start here

| Your goal | Read |
| --- | --- |
| Build or try it | [Setup, launch and troubleshooting](docs/GETTING_STARTED.md) |
| Understand the code | [Architecture and source map](docs/ARCHITECTURE.md) |
| Find a known method and its caveats | [Project and native method reference](docs/METHODS.md) |
| Assess what works | [Current state and evidence limits](docs/CURRENT_STATE.md) |
| Understand the pause and possible directions | [Ethos, parked ideas and future questions](docs/ETHOS_AND_FUTURE.md) |
| Contribute with a person or coding agent | [CONTRIBUTING](CONTRIBUTING.md), [AGENTS](AGENTS.md), [extension workflow](docs/EXTENDING.md) |
| Browse the original investigations | [Documentation index](docs/README.md) |
| Prepare a GitHub upload | [Source publication guide](docs/PUBLISHING.md) |

## Build and verification

The existing toolchain is **macOS, .NET SDK 7, a locally owned game install,
and BepInEx 5.4.23.5**. The plugin targets .NET Framework 4.7.2. This is a
developer source release; Windows/Linux and clean-machine installation have
not been verified. See setup instructions for machine-specific paths before
running these commands from the repository root:

```sh
bash scripts/setup.sh                    # downloads verified loader; builds/installs
make check                               # build/analyzers/format; no install
bash scripts/test-routing.sh              # linked C# policies and replay checks
python3 -m unittest discover -s tests -p 'test_*.py'
```

Close the game before setup, `make build`, or native harnesses. `make build`
copies DLLs into the local loader. `make npc-desires`, `make banana-quest`, and
`make world-harness` launch disposable-profile suites and save reports under
ignored `.runtime/`. Longer suites are listed by `make help`.
See [validation boundaries](docs/CURRENT_STATE.md).

## Scope and reuse

Most plugin types are internal and coupled to this game; there is no stable
external SDK. Pure policies and Python readers are easier starting points for
reuse. Native goal names alone do not establish complete actions or portable
APIs. Multiplayer, hunger/eating for the enrolled civilian, bargaining, social
memory and general autonomous NPC-to-NPC relationships are not implemented.

A [small native decision capture](docs/examples/README.md) is included for
inspection and offline replay without the private run directory.
Key functions also have human-readable headers explaining their purpose and
the reasons behind ownership, timing, safety and evidence constraints.

Original code and documentation use the [MIT license](LICENSE). Streets of
Rogue and its artwork belong to their respective owners. Game assemblies,
saves and downloaded dependencies are excluded from the source bundle;
see [third-party notices](THIRD_PARTY_NOTICES.md).
The [distribution audit](docs/DISTRIBUTION_AUDIT.md) records the actual ZIP checks
and the boundary between the source package and a future player/Workshop release.

The [previous development README](docs/history/2026-10-20-development-README.md)
retains legacy controls and dated observations. Historical “next steps” are
research history, not the current work queue.
