# NPC desires — tooling and native validation handoff

## Current native-coordination checkpoint

[NPC_COORDINATION.md](NPC_COORDINATION.md) owns the new canonical Hobo identity,
native-obligation precedence, policy v2 and current verification status.
The previous identity warning and test checkpoints below are historical.

**Historical October 12 audit correction:** the baseline fixture and enrollment code used
`SlumDweller`; the actual native role ID is `Hobo`. The earlier passes are fixture
evidence, not canonical native Slum Dweller compatibility. Do not widen ordinary
NPC autonomy until this is corrected and natively retested. The banana favor
already uses `Hobo`. See [NPC_DIALOGUE_BEHAVIOR_MAP.md](NPC_DIALOGUE_BEHAVIOR_MAP.md).

The user paused for quota, then explicitly resumed on October 12, 2026. **The tooling and first autonomous pickup milestone are implemented and native-validated.** Usage: [NPC_DESIRES.md](NPC_DESIRES.md). The initial pause notes below are historical and are superseded by this checkpoint.

## Earlier fixture checkpoint — complete

- Final native suite: **PASS, 73 checks**, build `0.6.0+20261012-120818`, session `1234f56b`.
- Evidence: [.runtime/world-harness/20261012-060818-56872/report.md](.runtime/world-harness/20261012-060818-56872/report.md). Exact verification receipt: `npc-final-verification.json` in the same directory.
- Installed DLL matches the saved suite DLL: SHA256 `0612fbb8aba0b9fc61ddce832fa7e173b1328d461d92f5dff5226deaeee52c47`.
- All 13 native scenarios completed: preference reversal, distance tradeoff, protected items, item removal, timeout, newer same-destination native command, actor retirement, physical LOS obstruction, labeled native pickup refusal, pause/resume, forced combat signal, and real level reload. The final record observes a new ready native world with no enrolled actor or leased action.
- All six exported native decision captures replay exactly. Final C# checks: **278 PASS**. Python reader/regression tests: **115 PASS**. Build/analyzers/formatting and `git diff --check` pass. The run's raw game log has no `BURRITO_ERROR` or `Exception:` entries; the closed capture has no loss/issues.
- Native pickup/cancel/flee gate passed before connecting autonomy, as recorded below. The final suite additionally exercises release through timeout, removal, combat, native replacement and level reload.
- Visual check: the running game showed the experiment commands, civilian and food fixture; after reload the inspector showed the sampled NPC identity/time, score components and protected-item rejection reasons readably. The inspector then closed and the harness quit. Subjective gameplay feel remains a user judgment, not a numeric acceptance claim.
- No game is left running by the harness. No saves were reset and no commits were made. The feature is disabled by default; all ordinary NPCs remain unenrolled unless explicitly selected. Broader level/setup work remains paused.

## Resumed corrections and evidence limits

- Enrollment resets cooldown and clears stale item diagnostics for the newly selected actor.
- Decision capture uses a detached dictionary/list payload accepted by the bounded worker; saved native records replay through the same pure policy.
- Native `DestroyMe` can mark an actor dead before deactivation. The reader checks loss of enrollment plus native death/retirement instead of assuming a single callback order.
- The obstruction fixture now uses a removable `BoxCollider2D` on a layer included in the native LOS mask. It validates the real native geometry query, but is explicitly test geometry, not a claim about all map wall/door types. The entire collider belongs to and is removed by the disposable fixture.
- Refusal uses a labeled `NonItem` metadata fault; combat/flee use fixture-set native signals. These are integration checks, not broad combat or full-inventory gameplay validation.
- Earlier failed pilots are preserved: `20261012-002247-89379` (cooldown/capture), `20261012-055809-55801` (overstrict retirement expectation), `20261012-060126-56265` (no nearby obstruction), `20261012-060437-56580` (relocated actor enrollment timeout). They are not passing evidence.

## Next boundary

No additional behavior is authorized by this completion. Further work could broaden NPC roles/actions or tune preferences after ordinary play feedback. Do not resume old level authoring, dog/hydrant setup or broad release work as an implied next step.


## What is implemented

- `NpcPickupProbe`: separate experiment commands within the quick-test panel; enroll one idle single-player Slum Dweller, approach a selected Fud/Banana, invoke native pickup, verify inventory gain and ground retirement, cancel/disable, and release an exact owned native goal. Native job/default-allegiance changes are bounded; no player allegiance change is made.
- A custom `GoalDoJob` instance owns native pathing. Its termination clears its own GoHere fields; it preserves changed job fields. A Harmony GoHere prefix cancels our action before another native GoHere command replaces it, including the same destination. That added prefix has built but has not yet passed the native replacement case.
- `NpcDecision`: pure version-1 policy, Fud/Banana 8/5 or 5/8, distance cost, eligibility/age/range checks, stable tie breaking, commitment, emergency and cooldown/native-busy inhibition.
- `NpcDesires`: disabled by default, one enrolled actor, bounded world queries, geometry checks, latest-decision inspector and transition capture. Commands remain outside read-only World Awareness inspection.
- Replay entrypoint: `dotnet run --project tests/DeliveryRoute.Tests -c Release -- --npc-replay <capture.json>`. Captures contain Input and Output; mismatches and incompatible versions fail.
- Disposable `NpcDesireFixture`, `NpcProbeHarness`, and `NpcDesireHarness`; `make npc-probe` and `make npc-desires`; independent Python report modules and replay export.

Existing dog behavior and earlier dirty work were preserved. No commits were made. Level authoring and broader setup remain paused; only the authorized isolated NPC fixture was added.

## Proven native gate

Run `.runtime/world-harness/20261012-001730-88485/` passed the base harness plus eight NPC checks (see `report.md`/`report.json`).

- Build `0.6.0+20261012-061729`, session `04af0ca0`.
- Saved DLL SHA256 `dd150f75f25be45556d8cf0d77a50f48f8271a098cc938346c7d15969a4ff587`.
- Ordinary Slum Dweller approached Fud; inventory gained exactly one; ground item retired; subsequent native behavior was observed.
- Explicit cancellation and a fixture-set `mustFlee` signal ended the owned action without inventory gain or stale GoHere fields.
- This is a forced emergency-signal test, not a complete live combat encounter. It predates the later policy and command-replacement additions.

Native inspection outputs are in `.runtime/npc-desires-audit-20261012/`. `AgentInteractions.GoHere` assigns job/jobBig/assignedPos; `GoalDoJob.Activate` creates pathing plus a long pause; `Terminate` can clear job; `GoalPathTo.Terminate` stops native pathing. That motivated exact goal ownership instead of simply issuing GoHere and walking away.

## Failed autonomous pilot and subsequent changes

Run `.runtime/world-harness/20261012-002247-89379/` is **INCOMPLETE/FAIL**; keep all evidence.

- Build `0.6.0+20261012-062246`, session `1a054eac`.
- Logs show successful Fud preference, reversed Banana preference, and distance-driven Banana pickup. These observations do not make the overall suite pass.
- The run stopped at the timeout case because cooldown from the previous enrolled actor leaked into the next enrollment. Source now resets cooldown on enrollment; not natively revalidated.
- Decision events were dropped because the bounded capture worker rejects unknown reference types. Source now creates a fully detached dictionary/list payload; a focused test checks worker acceptance and replay. Not natively revalidated.
- The game exited normally through the harness. No subsequent game launch was performed before this pause. Installed DLL is the failed-pilot build, while source/build output is newer. Do not describe the installed game as containing the latest fixes.

## Initial pause checklist (historical; see resumed validation below)

1. Review and finish the expanded native suite before running it. `NpcDesireHarness` was extended from eight to thirteen scenarios: preference cases, distance, protected, removed, timeout, new-command, actor-removed, blocked, refused, pause, combat, level-change. **`report_npc_desires.py` still expects the earlier eight cases. Update it and add negative report-reader tests.** Level-change emits a separate `npc-scope-ended` record rather than a normal complete-case row; pause emits `npc-pause`.
2. Validate fixture edge cases. Blocked-food setup searches for a real nearby obstruction and can fail when no suitable spot exists. Refusal is explicitly a test fault (`itemType = NonItem`) invoking native refusal, not ordinary inventory capacity. Combat is a fixture-set `inCombat` signal. Pause uses existing World Awareness/phone pause and checks game time, position and inventory before resuming. Level-change requests native reload; its completion/cleanup path needs careful review against destroyed actor/player references and paused/loading clocks.
3. Rerun `make npc-probe` on the final build, then `make npc-desires`; never install over a running game. The process-list check needs host permission in this sandbox. Earlier approved harness runs used `require_escalated`; no permission denial occurred.
4. Replay every exported native decision and verify inspector/capture agreement. Current policy tests pass independently only as recorded below; complete malformed/missing capture validation before treating replay as robust external tooling.
5. Add/finish evidence checks for no duplicate pickup, stale task ownership, pause, level transition, player preservation and native refusal. A successful captured label alone must not pass a reader check.
6. Finish a short visible check. No final subjective NPC choice/inspector readability check has been completed.

Potential review points: enrollment is deliberately restricted to SlumDweller and native Idle/Wander roots; awareness level identity is used alongside native UID/instance/scene; automatic discovery is simplified geometry perception; a native callback exception must leave no leased action; latest inspector values are a sampled decision rather than continuously refreshed perception. No broad NPC rollout or model integration is enabled.

## Verification at stop

Final build/format and focused test results are appended below after completion. `make check` builds without installing; the currently installed DLL remains the failed pilot version above. Earlier native runs and dirty work remain intact.

- Final `bash scripts/check.sh`: PASS, zero warnings/errors, formatting verified. Evidence: `.runtime/npc-desires-audit-20261012/stop-build-check.txt`.
- `git diff --check`: PASS. No final native validation or installation was performed.
