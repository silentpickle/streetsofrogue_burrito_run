using System;
using BurritoRun;

static void Check(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException(name);
    Console.WriteLine("PASS " + name);
}

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
