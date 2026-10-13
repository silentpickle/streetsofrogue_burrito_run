# Publication preparation — October 20, 2026

This receipt covers the source/documentation package. It does not establish a
portable installer, full release acceptance or completion of the ordinary-resident
playcheck. The native runs below preceded the function-header follow-up;
that follow-up changes source comments/docstrings, not gameplay logic.

## Fresh checks

- `make check`: PASS; zero build warnings/errors, analyzers and formatting verified.
- `bash scripts/test-routing.sh`: PASS, 329 C# assertions.
- `python3 -m unittest discover -s tests -p 'test_*.py'`: PASS, 131 tests.
- Bundled native decision example: offline replay matches.

## Native documentation runs

Both use fresh disposable profiles. The second run supplied the public tool
screenshots; it repeats the suite because the first final inspector window was
missed during writing. These remain controlled integration fixtures.

- `20261020-140851-25294`: **PASS, 126 gates**; session `1ad63b80`, build `0.6.0+20261020-200850`; DLL SHA-256 `10269dfa498ce4687b253ee63d91112780ba95cb1cdc848a015cb69a181ce179`.
- `20261020-141540-28200`: **PASS, 126 gates**; session `8c32ec38`, build `0.6.0+20261020-201539`; DLL SHA-256 `eb5d56e041343f7d9a974c18bd434571abdf73b163930bd6f313a1708c8e0663`.

Local full captures, reports and command logs remain under ignored `.runtime/`.
They are not bundled. The source example is the unchanged sequence-635 decision
from the first run. Historical October 13 ordinary-resident acceptance is still
incomplete; these fixtures do not close it.

The sandbox initially could not enumerate processes; the harness refused before
installation/launch. The unchanged guard succeeded with host process access. A
separate gallery profile reached the native main menu, but automated menu input
did not advance it; that session was closed and is not counted as gameplay proof.

## Public media and example integrity

| File | SHA-256 |
| --- | --- |
| `docs/images/damaged-window-observation.png` | `f5a89fd277da48a38a7753b01eb9b965ae08e76f1df39db310b874641f908e3e` |
| `docs/images/native-npc-fixture.png` | `518b2909b8ad698f9cdab671abfde08ac3aee89c3389ef8cfafcce46af3adda2` |
| `docs/images/world-decision-inspector.png` | `5bf7804dd7fea1bf306370ebeb36f4c97afb5f4903ed101f71b4e8d1864c501e` |
| `docs/examples/npc-choice-v2.json` | `25ff5978b708d32395985afbdbd0c4ab37755c1e440cae8d228730cb52803eec` |

Screenshots were visually reviewed. They show the native fixture, paused tools
and an older window observation; no private desktop content is included. No
generated gameplay illustration is used.

## Source archive and remaining limits

The [packaging script](../scripts/package-source.py) selects source/docs/tests/
scripts/tools and reviewed images/example data. Each generated ZIP has a
separate manifest with per-file and archive hashes; readback checks validate
the actual selected bytes. The bundle excludes Git history, `.runtime/`,
.tools/`, caches, build output, saves and game assemblies.

Machine-specific paths in source and dated local-evidence links in historical
notes are intentionally disclosed. Fresh-machine installation, full Git-history
review, exhaustive secret auditing, other platforms and binary redistribution
are not certified. No remote repository, upload, release or push was created.
Original work uses the selected MIT license; third-party material retains its
own ownership and terms.

New public guide links and source references were checked locally; legacy
`.runtime/` references are described as maintainer-local evidence. The previous
README is preserved in `docs/history/2026-10-20-development-README.md`.

## Function-header follow-up — October 20

Added 117 human-readable headers across 26 source/tool files: 114 C# XML
summaries and three Python function docstrings. They describe purpose and
reasons for ownership, identity, timing, failure handling and evidence limits.
The contributor guide records the convention, with an agent-entry pointer.

Verification compared every changed C# file against its before-edit copy after
removing XML comment lines: all 24 matched exactly. Python syntax trees in the
two changed tools matched after excluding function docstrings. `make check`
passed again with zero warnings/errors and formatting verified; `git diff
--check` passed. These are documentation-only changes; the native results above
retain their original build/session identities.

Before-edit copies and the equivalence/build receipts remain locally under
`.runtime/publication/function-headers-20261020T202205Z/`. A newly generated
source ZIP includes the annotated source and this receipt; earlier bundles
remain preserved as snapshots.

## Distribution-content follow-up — October 20

[DISTRIBUTION_AUDIT.md](DISTRIBUTION_AUDIT.md) records the direct ZIP inventory,
binary/media checks and comparison with 218 installed game files. The two earlier
149-entry archives contain zero exact installed-game file copies. Reachable
local history also passes the policy and exact-file comparison: 309 file blobs
across 409 objects, with no issues.

The source packer now audits each final archive, pins reviewed documentation
media/data and retains rejected output under `.zip.pending`. Twelve targeted
package-policy controls pass; the complete Python suite passes **143 tests**.
The newly generated package includes these guards and its own `content-audit.json`.
The source release remains an add-on requiring the recipient's own game;
Workshop code-plugin installation remains an unverified intended delivery route.
