# First dog food interaction

> **What this is:** The dog's first behaviour: noticing dropped food, sniffing it, or eating it when injured.  
> **Read it if:** You want to see how a small behaviour was scoped, built and proven.  
> **Status:** Parked experiment. Dated October 6, 2026.

Authorized October 6, 2026: food is the first behavior after the native World Awareness readiness checks.

The existing Courier dog notices a nearby dropped Delivery Burrito or HotFud through a read-only World Awareness query, commands native GoHere once, and gets eight game seconds to arrive within 0.8 units. A healthy dog sniffs and leaves it. An injured dog uses native Item.Interact and ItemFunctions.UseItem to eat one, then returns to native Follow. Native food refuses full-health consumption; the behavior does not manufacture hunger or change health itself. Discovery is checked every 0.4 seconds; arrival/identity are checked each frame during the detour so the native Courier cannot pass through the radius between samples. There is an eight-second cooldown after a completed or interrupted detour.

Keep this slice small: only single, unprotected, grounded, pickable burritos; no stacks, quest/stealable items, other foods, retrieval, hunger simulation, persistence, or movement recovery. Recheck activation key, current native identity, eligibility, proximity and geometry at consumption. Combat, pause, owner loss, release, missing targets and timeout interrupt or suspend the bounded interaction. The inspector never commands these changes.

Automated proof uses a disposable native harness profile. A separate fixture owns spawning/removal and native test injury; the ordinary PetService owns the behavior. Evidence must show ordinary query discovery, actual approach, native ground-item retirement, inventory count 1 to 0, positive native healing, resumed Follow, no repeat consumption, and unchanged player health/money/inventory. A healthy sniff, target removal and release are separate negative cases. Saved DLL, receipt, closed capture and an independent report are required. Intent logs alone do not pass.

After automated validation, the user's remaining check is a short visible trial: whether the detour reads clearly and feels useful. Controlled native validation does not certify combat/hazards or all character diets.

## Native audit and regression findings

The installed Assembly-CSharp.dll audit is preserved under `.runtime/food-native-audit/` (assembly SHA256 `4d82cb026f3830f4771a855efe8b4d8dc1f3b2bd4115c371485687bb43a91025`). `Item.Interact` performs inventory pickup and ground destruction; `ItemFunctions.UseItem` applies native health change and subtracts one inventory item. Its food branch refuses full health. The ordinary dog behavior never writes health. The fixture subtracts at most 15 health, capped at half current health.

Native `GoalDoJob.Terminate` can clear the temporary job label while resuming actual `GoalFollow`; evidence therefore requires the native goal and followed owner rather than the old label. Arrival checks at the old 0.4-second cadence could miss a fast Courier crossing the 0.8-unit radius. Per-frame arrival checks fix that without increasing discovery frequency or reissuing movement. Both non-passing pilots remain saved: `20261006-223004-52240` (overstrict Follow job label) and `20261006-223304-53994` (first arrival missed; later native eating did not rescue the failed first attempt).

## Validated checkpoint

Two successive generated-level captures passed all five food cases and the original base harness (65 gates each): `2e71c06b` / `0.6.0+20261007-043526`, `.runtime/world-harness/20261006-223527-54843/`; `6026bd3c` / `0.6.0+20261007-043815`, `.runtime/world-harness/20261006-223816-60147/`. These precede the final removal-limit check. Their saved reports retain that narrower scope.

Original final implementation validation: `0.6.0+20261007-044122`, `249a0ae7`, 66 gates, SHA256 `fd9f0b09b2ee4023d8ceda381e144eab64900f10942f82d2aae6f8b00fcb551d`; `.runtime/world-harness/20261006-224122-61012/`. This includes a real second-spawn request after native dog cleanup; the existing one-per-level limit still rejects it. Pet removal/recycled identity no longer clears that latch; a true level change does. All five ordinary food cases pass again. The game closed automatically; reports, exact DLL and receipt stay together. Player health, money and inventory are unchanged; the ground activation retires and the held food count goes 1→0 with healing 18→33. Actual approach is derived from the notice and arrival positions. Native `GoalFollow` and followed player are required after the action.

The first successful capture initially failed an overly strict reader rule that used net displacement at the later Follow sample: returning near the start canceled the apparent distance. `report-before-approach-correction.*` preserves that reader correction. Raw logs are unchanged; the corrected reader measures actual approach between notice and arrival. `resultDisplacement` is only the endpoint displacement after returning, never cumulative travel. The final build, analyzer and formatting checks have zero warnings/errors; 250 C# assertions, 96 Python tests and shell/whitespace checks pass. Full food-in-combat/pause/diet/other-hardware acceptance and subjective visible clarity remain open.

To watch with no scene setup: close the game, run `make world-food`, bring the game to the front during startup and keep focus stable. The five cases finish and quit automatically. Assess whether the food detour and return are readable; no manual dog injury is needed.

Requested visible rerun: `0.6.0+20261007-051237`, `2cf1bc70`, 66 gates PASS in `.runtime/world-harness/20261006-231238-67676/`; installed/saved DLL SHA256 `dbcd7d81946223fdb987ba73a93dca826b6321448852b5c42717450af0437d6a`. Game focus stayed true, native eating healed 18→33 with inventory 1→0 and actual GoalFollow, capture closed cleanly. The earlier focus-interrupted attempt `20261006-231020-66891` stays saved. No behavior code changed in this rerun.
