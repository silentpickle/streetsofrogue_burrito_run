# Contributing

> **What this is:** How to propose a change: what to check, what to preserve, and how to write function headers.  
> **Read it if:** You want to send a fix, a finding or a pull request.  
> **Status:** Current as of October 20, 2026.

Start with [current state](docs/current-state.md), [setup](docs/getting-started.md)
and [architecture](docs/architecture.md). The project is paused for reassessment;
the old roadmap catalogs possibilities rather than promising their delivery.
Small tooling fixes, reproducibility improvements and well-evidenced native
findings remain useful even if the broader simulation moves elsewhere.

For behavior changes, name one observable outcome and its interruption rules.
Use [the extension workflow](docs/reference/extending.md) and [method reference](docs/reference/methods.md)
before adding a parallel inspection, recording or decision system. A general
social-simulation rewrite needs a separate design discussion.

Preserve existing edits, saves and successful/failed runs. Keep new native runs
in uniquely named directories. Read native state on Unity's main thread and
send detached values to background recording. Give temporary actions exact
ownership and bounded lifetimes; newer native tasks, employment and danger
retain precedence.

Run checks appropriate to the change. Documentation needs working links,
accurate commands/source references and reviewed screenshots. Changed policies
need focused C# checks and replay compatibility. Changed readers need missing,
contradictory and negative evidence cases. Changed native actions need the
relevant disposable fixture plus a short visible check. See [evidence levels](docs/current-state.md).
Report what passed and what remains unverified; include build/session/run
identity when a game run is involved.

Bug reports should include expected/actual behavior, launch mode, loaded build
label, platform/game version, reproduction steps and a small reviewed evidence
excerpt. Paths and Steam diagnostics may be present in logs. Keep full profiles,
saves, downloaded assemblies and credentials local.

Contributions to original project source/docs use the repository's MIT license.
Automated assistants should also read [AGENTS.md](AGENTS.md).

## Function documentation

Give nontrivial entry points a short, human-readable header: what the function
does, then why its important constraint or design choice exists. C# uses XML
`<summary>` comments so the explanation is available in editor tooltips; Python
uses docstrings. Keep prose readable in a normal-width editor.

Explain ownership, side effects, main-thread requirements, timing, failure and
evidence limits where they affect a caller's understanding. For example, pickup
must verify both inventory gain and ground-item retirement because calling the
native method alone does not prove transfer. Say when a retained sample describes
an old actor rather than current state.

Document actual behavior and update the header with the function. Trivial
accessors and self-explanatory wrappers can remain uncommented. Put a local
implementation explanation beside the relevant branch rather than repeating the
whole algorithm in a header. The broader method catalog belongs in
[docs/reference/methods.md](docs/reference/methods.md).
