# Distribution audit — October 20, 2026

**Purpose: an add-on for an existing Streets of Rogue 1 installation.** The
prepared download is developer source and tooling, not a copy of the game or a
finished Workshop installer. The intended Workshop/plugin direction is retained.

## What the prepared ZIPs actually contain

Both previously supplied archives were opened and every member inspected by
path, extension and bytes. Each contains 149 entries: source, project/build/
launcher text, documentation, one detached replay JSON and three screenshots.
There are no executable/game/dependency DLLs, native libraries, Unity asset
bundles, extracted sprites/audio, game levels, saves or profiles. The screenshots
do contain game artwork as illustrative documentation; that artwork remains
third-party material and is not offered as an asset pack.

Every archive member's SHA-256 was also compared with **218 locally installed
game files totaling 460,560,572 bytes**. Both archives had **zero exact full-file
matches**, including under different names. Source namespaces, build references
and packaging exclusions were reviewed: the plugin references locally installed
game/Unity assemblies rather than bundling their implementation. Native API names
and small explanatory snippets in the research notes are integration documentation;
raw assembly inspection dumps remain in excluded `.runtime/`.

| Earlier source archive | SHA-256 | Exact game-file matches |
| --- | --- | --- |
| `20261020T201906Z-4ada0cb4/burrito-run-source.zip` | `9da7b5f61fef2425edf3dc171d15ad224b655d07421ffdf49c67ced2ba41813a` | 0 |
| `20261020T202406Z-e26ffad2/burrito-run-source.zip` | `a30f3a45136c5f64d4a7adbb429dc816085a639878e9e55d13a05e3c07d911ca` | 0 |

The original full audit receipt is local at
`.runtime/publication/distribution-audit-20261020T202526Z/existing-archives.json`.
This is a bounded content/integrity audit, not a detector of arbitrary copied
fragments or legal clearance for screenshots, dependencies or platform terms.

Reachable local Git history was checked separately: **409 objects, including
309 file blobs totaling 4,342,654 bytes**. Every blob with a recorded path passed
the same content/path policy, with zero installed-game hash matches. The receipt
is `git-history-audit.json` beside the ZIP audit. This describes the local refs
visible during this audit; unreachable objects, external LFS content, future
changes and remote state are outside that check. The source ZIP contains no Git
history at all.

## Enforced packaging boundary

[source_package_policy.py](../scripts/source_package_policy.py) now checks every
completed source ZIP. Source files must be UTF-8 text without binary NULs; known
executable/archive/Unity signatures and forbidden runtime/game/dependency paths
are rejected. Media/data is limited to three reviewed screenshots and one replay
example with exact pinned hashes. A renamed DLL, new texture or altered media
file fails the check. Duplicate ZIP paths and symlinks also fail.

The packer retains rejected output as `.zip.pending` evidence and assigns the
final `.zip` name after checks pass. A successful new package includes a
separate `content-audit.json` and `manifest.json`. Use `--game-dir` to repeat the
exact installed-file hash comparison; [commands](PUBLISHING.md) also allow auditing
an existing archive. Twelve negative/positive controls cover these safeguards,
including a renamed synthetic game file and a rejected pack with no final ZIP.

## Plugin payload versus Workshop content

| Deliverable | Intended content | Current status |
| --- | --- | --- |
| GitHub/developer source | Our source, docs, tests and tools; reviewed documentation screenshots/data | Prepared and content-audited |
| Future BepInEx player package | Explicitly selected `BurritoRun.dll`, required `BurritoRun.Profile.dll`, our configuration/launcher/instructions and notices | Separate release/install work; never copy an entire `bin/`, loader or game directory |
| Future Workshop content | Authored supported custom content with its required code-plugin dependency clearly stated | No authored release or verified code-loader/install route is established here |

The game-specific [Steam Workshop](https://steamcommunity.com/app/512900/workshop/)
describes characters, buildings, levels and campaigns, with Campaign, Chunk Pack
and Character categories. That page does **not** establish that subscribing to
an item installs a BepInEx DLL or its preloader patcher. This is an evidence
limit, not a claim that every possible Workshop integration is impossible.

[BepInEx's own plugin guide](https://docs.bepinex.dev/articles/dev_guide/plugin_tutorial/2_plugin_start.html)
separately describes referencing game libraries for compilation and loading the
resulting plugin DLL through `BepInEx/plugins`. Our profile patcher additionally
depends on an explicit profile path supplied by the launcher. A Workshop item
alone has not been tested to supply this setup.

A future binary/Workshop release therefore needs its own payload inventory,
dependency/notice review and real installation test against the recipient's own
game. Keep source publication, code-plugin installation and Workshop custom
content accurately labeled. No game or release has been uploaded by this audit.
