# Documentation map

Read from the top down and stop when you know enough. Each level assumes
only the one above it.

## 10,000 ft: what is this?

- [Repository README](../README.md): the idea, what exists, and a screenshot. Five minutes.
- [Original brief](history/original-brief.md): the pitch that started it, in the author's words.

## 1,000 ft: what was built, and does it work?

- [Current state](current-state.md): one page on what works, what does not, and how strong the evidence is.
- [The experiments, in order](experiments/README.md): a timeline of the three weeks of work, and why each step led to the next.
- [Ethos and future](ethos-and-future.md): why it paused, and the open questions.
- [Screenshots](screenshots.md): what the tools look like.

## 100 ft: how does it fit together?

- [Getting started](getting-started.md): build it, launch it, run the checks.
- [Architecture](architecture.md): the observe, decide, act, check loop and a map of the source files.
- One folder per experiment, each with its own README and reading order:
  1. [World Awareness](experiments/01-world-awareness/README.md)
  2. [The dog](experiments/02-dog/README.md)
  3. [NPC desires and coordination](experiments/03-npc/README.md)
  4. [Banana favors](experiments/04-banana-favors.md)

## 10 ft: working on it

- [Contributing](../CONTRIBUTING.md): what to check and preserve when changing things.
- [Extending](reference/extending.md): the workflow for adding behaviour, by hand or with an agent.
- [Reference](reference/README.md): method lookup, capture format, item catalog.
- [Example decision](examples/README.md): a real captured NPC choice you can replay offline.

## 0.1 in: the evidence and the source

- Each experiment folder's validation and plan pages: every test run, build ID and number.
- [History](history/README.md): the dated working notes, dead ends included.
- The source itself: key functions have plain-English header comments explaining
  what they do and why.

## Other

- [Maintainers](maintainers/README.md): packaging and publishing the source ZIP.
- [AGENTS.md](../AGENTS.md): standing instructions for AI coding agents.
- [Third-party notices](../THIRD_PARTY_NOTICES.md): what the MIT license does and does not cover.

### A note on how these were written

Most of these documents were written by AI coding agents during development,
for other agents. Every page now opens with a short **What this is / Read it if /
Status** box for human readers. The text below that box is the original, and can be
dense. Dates matter: in a page marked as dated, "current" means current on that
date. Links into `.runtime/` point at the author's local evidence files, which are
not part of this repository.
