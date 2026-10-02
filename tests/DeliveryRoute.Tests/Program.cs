using System;
using BurritoRun;

static void Check(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException(name);
    Console.WriteLine("PASS " + name);
}
static bool Near(float value, float expected) => Math.Abs(value - expected) < 0.0001f;

var stalled = new DeliveryRoute(0, 8);
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
