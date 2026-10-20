# Third-party material

> **What this is:** What the MIT license covers, and what it does not (the game, its art, and dependencies).  
> **Read it if:** You plan to reuse code or screenshots from this repository.  
> **Status:** Current as of October 20, 2026.

The root MIT license applies to this project's original source and written
documentation. It does not relicense Streets of Rogue, Unity, BepInEx, Harmony,
Mono.Cecil, Mirror, Newtonsoft.Json, .NET or any other dependency.

Screenshots in `docs/images/` illustrate the actual prototype and include
Streets of Rogue artwork. That artwork remains the property of its respective
owners; screenshots are not a separately reusable game-asset pack. This is an
independent fan prototype, without a claim of official endorsement.

The source bundle references locally installed game assemblies; it does not
contain those assemblies. Obtain the game separately. Setup downloads the
pinned BepInEx archive from upstream, verifies its SHA-256, and keeps it under
ignored `.tools/`. NuGet resolves reference assemblies locally. Consult each
dependency's supplied notices/licenses before distributing a binary package.
This source preparation does not certify binary redistribution.

This is an add-on requiring the recipient's own Streets of Rogue installation.
The source ZIP includes three illustrative game screenshots, explicitly pinned
and identified in the packaging policy; it includes no extracted sprite/audio/
level/asset bundle. Screenshots are not covered by a claim that we own the game
artwork. See [the content audit](docs/maintainers/distribution-audit.md) for the actual checks
and the distinction between a developer source ZIP and intended Workshop delivery.

MIT text reference: [Open Source Initiative](https://opensource.org/license/mit).
