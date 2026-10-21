# Preparing a GitHub source upload

> **What this is:** How to build a clean source ZIP that leaves out game files, saves and private logs.  
> **Read it if:** You are the one uploading or releasing this source.  
> **Status:** Current as of October 20, 2026.

This checkout is prepared as a developer source/tooling reference. It is not a
tested binary distribution. The source license is [MIT](../../LICENSE); artwork
and dependency boundaries are in [third-party notices](../../THIRD_PARTY_NOTICES.md).

## Make a source snapshot

From the repository root:

```sh
python3 scripts/package-source.py
# For an exact-file comparison against your own local game as well:
python3 scripts/package-source.py --game-dir "$GameDir"
# Check an existing ZIP without making another package:
python3 scripts/package-source.py --audit /path/to/burrito-run-source.zip --game-dir "$GameDir"
```

The script creates a unique directory under `.runtime/publication/` containing
`burrito-run-source.zip` and `manifest.json`. It selects root project/docs/config/
launcher files, source/tests/tools/scripts, and reviewed documentation images.
It excludes `.git/`, `.tools/`, runtime profiles/evidence, build output, caches,
game DLLs and downloaded binaries. Each bundled file and the ZIP have SHA-256
receipts; the actual archive is read back to verify selected bytes. It does not
edit, delete, commit, push or upload anything.

Every completed ZIP also passes the byte/path checks in
`scripts/source_package_policy.py`; results are saved in `content-audit.json`.
Only the three reviewed screenshots and one replay JSON are admitted as non-source
documentation files, pinned to their reviewed hashes. Executables, Unity bundles,
unreviewed media, binary data hidden under text extensions, duplicate entries,
symlinks and game/runtime/dependency paths cause failure. The optional game-dir
comparison also rejects exact copies of installed files regardless of name.

An in-progress or rejected archive retains the `.zip.pending` suffix. The final
`.zip` name is assigned only after content and readback checks pass. Keep a
rejected pending artifact as evidence; it is not a publication deliverable.

The allowlist is deliberately narrow. If adding a new source file type or folder,
review it and update the script and policy. Changed/new documentation media also
need explicit review and a pinned hash. Inspect the manifest and preview the extracted
README/images before publishing. A successful pack is an inclusion/integrity
check, not an exhaustive secret scan or a clean-machine installation test.

## Choose the public shape

A clean GitHub repository populated from the extracted snapshot avoids sharing
unreviewed local Git history. Keep this existing checkout and its evidence intact.
After extraction to a new folder, initialize Git there and review `git status`
before committing. Alternatively attach the ZIP to a source release. Publishing
the existing repository history requires reviewing that history separately;
`.gitignore` cannot remove content already committed.

Suggested repository description:

> Streets of Rogue 1 NPC experiments: world inspection, deterministic decisions
> and replay, native action ownership, and outcome-checked test fixtures.

Suggested topics: `streets-of-rogue`, `bepinex`, `harmony`, `game-ai`,
`npc`, `modding`, `csharp`, `observability`.

Use the new [README](../../README.md) as the landing page. Keep the methods,
architecture, current-state guide, gallery and historical investigations together.
They make the tooling useful even to someone who never runs the playable mod.
Local `.runtime/` links in older records are intentionally unavailable to public
readers; the current docs explain their provenance and reproduction commands.

## Remaining boundaries to state publicly

- Mac developer setup, installation-path configuration and the existing .NET 7 toolchain;
  no verified clean-machine or Windows/Linux installation.
- Controlled native checks versus unfinished ordinary-resident acceptance.
- No stable SDK, general NPC-to-NPC social simulation or model integration.
- Paused development, retained experiments and no promised delivery roadmap.
- Third-party game/assets required separately; screenshots illustrate the mod.

Before any player/binary release, separately verify the actual supported game
version, loader/dependency notices, clean installation and player experience.
Workshop code installation is not established by this source package.
For the current content audit and the intended plugin/Workshop boundary, see
[distribution-audit.md](distribution-audit.md).

No GitHub repository, remote upload or release is created by this preparation.
The public URL and publication action remain the maintainer's choice.
