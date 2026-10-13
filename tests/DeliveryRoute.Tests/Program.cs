using System;
using BurritoRun;

static void Check(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException(name);
    Console.WriteLine("PASS " + name);
}
static bool Near(float value, float expected) => Math.Abs(value - expected) < 0.0001f;

if (args.Length == 2 && args[0] == "--npc-replay")
{ Environment.ExitCode = NpcDecisionChecks.ReplayFile(args[1]); return; }
if (args.Length != 0)
{ Console.Error.WriteLine("Usage: --npc-replay <capture.json>, or no arguments for checks"); Environment.ExitCode = 2; return; }
NpcDecisionChecks.Run(Check);
QuestChecks.Run(Check);

var stalled = new DeliveryRoute(0, 8);
TraceChecks.Run(Check);
WorldChecks.Run(Check);
Check(!stalled.NeedsMeeting(1, 8, false), "native startup grace");
Check(!stalled.NeedsMeeting(11, 8, true), "no premature fallback");
Check(stalled.NeedsMeeting(12, 8, true), "stalled route fallback");
Check(stalled.FallbackReason == "no-progress", "stalled reason");
Check(!stalled.NeedsMeeting(13, 8, false), "fallback only once");
Check(!stalled.MeetingExpired(12), "paused clock does not expire meeting");
Check(!stalled.MeetingExpired(31.9f), "full meeting grace");
Check(stalled.MeetingExpired(32), "bounded meeting timeout");
var failed = new DeliveryRoute(0, 8);
Check(failed.NeedsMeeting(2, 7, false), "native job abandonment");
Check(failed.FallbackReason == "native-job-ended", "abandonment reason");
var arriving = new DeliveryRoute(0, 8);
Check(!arriving.NeedsMeeting(50, 0.5f, false), "arrival wins over timeout and job end");
var progressing = new DeliveryRoute(0, 20);
Check(!progressing.NeedsMeeting(11, 19, true), "progress refreshes stall clock");
Check(!progressing.NeedsMeeting(22, 18, true), "continued progress");
Check(!progressing.NeedsMeeting(33, 17, true), "long route still moving");
Check(progressing.NeedsMeeting(45, 16, true), "absolute route deadline");
Check(progressing.FallbackReason == "route-deadline", "deadline reason");
var doorway = new DeliveryRoute(0, 3, 0.45f);
Check(doorway.NeedsMeeting(12, 0.6f, false), "doorway uses its stricter arrival radius");
var entrance = new DeliveryRoute(0, 3, 0.45f);
Check(entrance.BeginMeeting(5, "entrance-arrival"), "entrance starts timed meeting");
Check(!entrance.BeginMeeting(20, "entrance-arrival"), "arrival cannot reset meeting clock");
Check(entrance.MeetingExpired(25), "entrance wait is bounded");
var reputation = new CustomerReputation();
Check(!reputation.ShouldArm(true, 0) && !reputation.ShouldArm(false, 0), "clear account never arms added driver");
reputation.RecordAssault();
Check(reputation.Reports == 1 && reputation.ArmedChance == 0.5, "first assault raises risk");
Check(reputation.ShouldArm(true, 0.99), "quick test guarantees flagged armed driver");
Check(reputation.ShouldArm(false, 0.49) && !reputation.ShouldArm(false, 0.5), "normal chance boundary");
reputation.RecordPeacefulHandoff();
Check(reputation.Reports == 1 && reputation.PeacefulHandoffs == 1, "one peaceful handoff does not clear flag");
reputation.RecordPeacefulHandoff();
Check(reputation.Reports == 0 && reputation.PeacefulHandoffs == 0, "two handoffs clear flag");
reputation.RecordAssault();
reputation.RecordAssault();
Check(reputation.ArmedChance == 0.75, "repeat assault increases risk");
reputation.RecordAssault();
reputation.RecordAssault();
Check(reputation.Reports == 3 && reputation.ArmedChance == 1, "risk is capped");
reputation.RecordPeacefulHandoff();
reputation.RecordAssault();
Check(reputation.PeacefulHandoffs == 0, "assault resets recovery streak");
reputation.Reset();
Check(reputation.Reports == 0 && reputation.PeacefulHandoffs == 0, "run reset clears reputation");

var openScatter = AquariumScatter.Choose(10, 20, 0, (x, y) => true);
Check(openScatter.Found && Near(openScatter.X, 11.28f) && Near(openScatter.Y, 20) &&
    openScatter.Direction == 0 && openScatter.Steps == 2, "shrimp takes two open cardinal tiles");
var avoidWall = AquariumScatter.Choose(10, 20, 0, (x, y) => x < 10);
Check(avoidWall.Found && avoidWall.Direction == 1 && Near(avoidWall.X, 8.72f) &&
    avoidWall.Steps == 2, "shrimp turns away from blocked preferred direction");
var stopBeforeWall = AquariumScatter.Choose(10, 20, 0, (x, y) => x <= 10.64f);
Check(stopBeforeWall.Found && stopBeforeWall.Direction == 0 &&
    stopBeforeWall.Steps == 1, "shrimp stops before second blocked tile");
var boxedIn = AquariumScatter.Choose(10, 20, 0, (x, y) => false);
Check(!boxedIn.Found, "boxed-in shrimp does not cross a wall");
var wrappedDirection = AquariumScatter.Choose(10, 20, -1, (x, y) => true);
Check(wrappedDirection.Found && wrappedDirection.Direction == 3 &&
    Near(wrappedDirection.Y, 18.72f), "shrimp direction wraps safely");

Check(PetPolicy.Choose(true, false, 0, false) == PetIntent.Follow,
    "pet follows its living owner without a snack");
Check(PetPolicy.Choose(true, true, 3, false) == PetIntent.ChaseFood,
    "pet detours toward nearby dropped food");
Check(PetPolicy.Choose(true, true, 7, false) == PetIntent.Follow,
    "pet ignores distant food");
Check(PetPolicy.Choose(true, true, 0.5f, false) == PetIntent.ChaseFood,
    "nearby food enters the interaction, including at arrival");
Check(PetPolicy.Choose(true, true, 3, true) == PetIntent.Follow,
    "pet does not immediately re-chase a sniffed snack");
Check(PetPolicy.EligibleFood("HotFud", 1, false, false), "one ordinary burrito is eligible");
Check(!PetPolicy.EligibleFood("HotFud", 2, false, false), "food stacks cannot be consumed wholesale");
Check(!PetPolicy.EligibleFood("HotFud", 1, true, false), "protected food is rejected");
Check(!PetPolicy.EligibleFood("HotFud", 1, false, true), "airborne or unpickable food is rejected");
Check(!PetPolicy.EligibleFood("Beer", 1, false, false), "other consumables are outside the first food slice");
Check(!PetPolicy.CanEat(100, 100) && !PetPolicy.CanEat(0, 100) && PetPolicy.CanEat(70, 100),
    "native hunger gate preserves full health and death, permits injured eating");
Check(PetPolicy.Choose(false, true, 3, false) == PetIntent.Wander,
    "owner loss takes priority over food");

var barking = new PetBarkPolicy();
var hydrants = new PetHydrantPolicy();
Check(hydrants.CanNotice("activation-1", 0), "fresh hydrant activation can attract interest");
hydrants.Finish("activation-1", 2);
Check(!hydrants.CanNotice("activation-1", 1000), "same hydrant is not revisited after cooldown");
Check(!hydrants.CanNotice("activation-2", 46) && hydrants.CanNotice("activation-2", 47),
    "different hydrant waits for the full game-time cooldown");
for (int i = 2; i <= 32; i++) hydrants.Finish("activation-" + i, i * 50);
Check(!hydrants.CanNotice("activation-33", 5000), "hydrant memory and detours are capped per level");
hydrants.Reset();
Check(hydrants.CanNotice("activation-1", 0), "real level reset clears hydrant interest memory");
Check(!PetBarkPolicy.GuardInRange(4.99f), "guard probe avoids native close-range investigation cutoff");
Check(PetBarkPolicy.GuardInRange(5), "guard probe accepts minimum useful separation");
Check(PetBarkPolicy.GuardInRange(6.5f), "guard probe stays inside louder native hearing radius");
Check(!PetBarkPolicy.GuardInRange(6.51f), "guard probe rejects overly distant targets");
Check(!PetBarkPolicy.UsefulApproach(false, 2, 2), "ordinary patrol movement cannot certify distraction");
Check(!PetBarkPolicy.UsefulApproach(true, 2, 0), "moving sideways is not approach to the bark");
Check(!PetBarkPolicy.UsefulApproach(true, 0.63f, 0.63f), "tiny noise-goal movement is not useful approach");
Check(PetBarkPolicy.UsefulApproach(true, 0.64f, 0.64f), "one native tile of approach is measurable");
barking.Reset(10);
Check(!barking.TryBegin(14.99f, 100), "new dog gets a quiet five-second grace");
Check(barking.TryBegin(15, 100), "bark starts after spawn grace");
Check(!barking.TryBegin(34.99f, 101), "manual and automatic barks share the cooldown");
Check(!barking.TryBegin(35, 100), "dog does not keep barking at the same stranger");
Check(barking.TryBegin(35, 101), "different stranger can trigger a bark after cooldown");
Check(!barking.TryBegin(35, 102), "unchanged paused game clock cannot trigger another bark");
for (int index = 2; index < PetBarkPolicy.LevelLimit; index++)
    Check(barking.TryBegin(15 + index * PetBarkPolicy.Cooldown, 100 + index),
        "distinct stranger bark " + (index + 1));
Check(!barking.TryBegin(1000, 999) && barking.Count == PetBarkPolicy.LevelLimit,
    "per-level bark cap prevents an endless disturbance loop");
barking.Reset(1000);
Check(barking.Count == 0 && !barking.HasBarkedAt(100) &&
    barking.TryBegin(1005, 100), "new level resets bark history with a fresh grace");

var firstPetLevel = new PetLevelScope(1, 2, 3, 123, "1-1", 30);
Check(PetLevelScope.ResetReason(firstPetLevel,
    new PetLevelScope(1, 2, 3, 123, "1-1", 31), true, false, false, true) == null,
    "pet remains one-per-level while the same level continues");
Check(PetLevelScope.ResetReason(firstPetLevel, firstPetLevel, false, false, false, true) ==
    "load-incomplete", "pet resets as a new level starts loading");
Check(PetLevelScope.ResetReason(firstPetLevel, firstPetLevel, true, false, true, true) ==
    "level-transitioning", "pet resets during a native level transition");
Check(PetLevelScope.ResetReason(firstPetLevel, firstPetLevel, true, true, false, true) ==
    "returning-to-menu", "pet resets on return to menu");
Check(PetLevelScope.ResetReason(firstPetLevel, firstPetLevel, true, false, false, false) ==
    "nonplayable-level", "pet resets in home base");
Check(PetLevelScope.ResetReason(firstPetLevel,
    new PetLevelScope(1, 2, 4, 123, "1-1", 30), true, false, false, true) ==
    "agents-nest-changed", "pet catches a rebuilt level with reused controller and seed");
Check(PetLevelScope.ResetReason(firstPetLevel,
    new PetLevelScope(1, 2, 3, 456, "1-1", 30), true, false, false, true) ==
    "seed-changed", "pet catches a new run with reused controller and floor");
Check(PetLevelScope.ResetReason(firstPetLevel,
    new PetLevelScope(1, 2, 3, 123, "1-2", 30), true, false, false, true) ==
    "floor-changed", "pet catches a new floor without a transition frame");
Check(PetLevelScope.ResetReason(firstPetLevel,
    new PetLevelScope(1, 2, 3, 123, "1-1", 1), true, false, false, true) ==
    "load-clock-restarted", "pet catches a same-seed replay with a restarted load clock");

Check(TestPanelInput.BlocksWorldClick(900, 450, 1024, 600, true),
    "quick-test click is blocked before it becomes a punch");
Check(!TestPanelInput.BlocksWorldClick(600, 450, 1024, 600, true),
    "world click outside quick-test panel still works");
Check(!TestPanelInput.BlocksWorldClick(900, 450, 1024, 600, false),
    "hovering over quick-test panel does not block movement");
Check(!TestPanelInput.BlocksWorldClick(900, 590, 1024, 600, true),
    "bottom-edge click outside panel still works");
