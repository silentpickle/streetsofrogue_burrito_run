# Agent entry point

Start with [README.md](README.md) and [current state](docs/CURRENT_STATE.md).
Behavior development is paused for a platform reassessment; historical next
steps are retained research, not authorization to resume them. Follow the
current user's scope.

Before extending behavior, changing tooling, or preparing a release, read
[the extension guide](docs/EXTENDING.md). It maps the reusable code, native
inspection tools, checks, and evidence limits.

For current checkout state, read [KNOWLEDGEBASE.md](KNOWLEDGEBASE.md) and
[PAUSE_HANDOFF.md](PAUSE_HANDOFF.md); for the NPC experiment, use the current
checkpoint at the top of [NPC_DESIRES_HANDOFF.md](NPC_DESIRES_HANDOFF.md).

Preserve existing changes, saves, and run evidence. Dog and aquarium work is
parked and retained. Follow the current user's scope over historical next steps.

If using Qwen or another local thinking model, set temperature below 0.25 on a
0–1 scale.

For code orientation, use [architecture](docs/ARCHITECTURE.md); for known native
calls or project methods, use [the method reference](docs/METHODS.md) before
guessing APIs. Most types are internal, not an external SDK.

Before editing, inspect `git status --short` and the relevant implementation.
Keep Unity reads on the main thread, send detached values to bounded capture,
and preserve exact action ownership and newer native commands. Keep fixture
setup writes separate from read-only inspection. New runs get unique names;
preserve failed evidence and saves.

Use [CONTRIBUTING.md](CONTRIBUTING.md) for checks appropriate to the change.
When editing a nontrivial function, follow its function-documentation convention:
explain what it does and why its important constraints exist in a readable header.
Report changed files, validation and remaining limits. Distinguish source
inspection, replay, native fixtures and human acceptance; a logged action is
not a verified outcome. Treat dated build/session IDs as historical until
checked against the current process.

For GitHub/source preparation, use [the publication guide](docs/PUBLISHING.md).
Keep game assemblies, `.tools/`, `.runtime/`, saves and private logs out of the
public bundle; original source/docs use the root MIT license.
Run the source-package content guard; a whole checkout, game folder, loader folder
or build-output directory is not a player release. Use
[the distribution audit](docs/DISTRIBUTION_AUDIT.md) for the intended add-on and
Workshop boundary before making distribution claims.
