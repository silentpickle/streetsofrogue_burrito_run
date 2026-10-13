using System;
using BurritoRun;

internal static class QuestChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var startup = new QuestStartWindow();
        check(!startup.Observe(true, 7956, 20), "outgoing character room cannot immediately consume the favor");
        check(!startup.Observe(true, 7956.25f, 20.25f) && !startup.Observe(false, 7956.34f, 0),
            "observed character-to-Slums transition resets startup without spawning");
        check(!startup.Observe(true, 7960, 1) && !startup.Observe(true, 7961.75f, 2.75f) && startup.Observe(true, 7962, 3),
            "first settled Slums level becomes eligible after two game seconds");
        startup.Reset();
        check(!startup.Observe(true, 8000, 10) && !startup.Observe(true, 8000, 10), "pause cannot advance startup readiness");
        check(!startup.Observe(true, 8001, 0) && !startup.Observe(true, 8002, 1) && startup.Observe(true, 8003, 2),
            "native load clock restart requires a fresh settled window");
        check(!startup.Observe(true, 1, 1), "game clock restart cannot reuse an earlier readiness window");
        var q = QuestPolicy.Banana();
        check(!q.Begin(0) && q.State == QuestState.Offered, "favor requires acceptance");
        check(q.Accept() && q.Accept() && q.Available(0) && q.Available(1), "accepting preserves both offers");
        check(q.Begin(0) && !q.Begin(1), "exchange reservation prevents a second recipient");
        check(q.Complete(3, 2, 0, 1, 7, 22), "cash outcome consumes exactly one stacked banana");
        check(q.Winner == "buyer" && q.State == QuestState.Completed && !q.Accept() && !q.Begin(0) && !q.Begin(1), "completed favor cannot pay twice");
        q.End(QuestState.Expired, "level-change");
        check(q.State == QuestState.Completed, "completed outcome survives level expiration");
        q = QuestPolicy.Banana(); q.Accept(); q.Begin(1);
        check(q.Complete(1, 0, 4, 5, 0, 1) && q.Winner == "neighbor", "medical outcome has its own verified reward");
        foreach (var values in new[] { new[] { 2, 0, 0, 1, 0, 15 }, new[] { 1, 1, 0, 1, 0, 15 },
            new[] { 1, 0, 0, 0, 0, 15 }, new[] { 1, 0, 0, 1, 0, 30 }, new[] { 1, 0, 0, 1, 0, 0 },
            new[] { 1, 0, 0, 1, int.MaxValue, int.MinValue + 14 }, new[] { 1, 0, int.MaxValue, int.MinValue, 0, 15 } })
        {
            q = QuestPolicy.Banana(); q.Accept(); q.Begin(0);
            check(!q.Complete(values[0], values[1], values[2], values[3], values[4], values[5]) &&
                q.State == QuestState.Faulted && !q.Begin(1), "mismatched or overflowing exchange fails closed");
        }
        q = QuestPolicy.Banana(); q.Accept(); q.Begin(0); q.Refuse("inventory-full");
        check(q.State == QuestState.Accepted && q.Winner == null && q.Begin(1), "refused exchange can choose the other offer");
        q = QuestPolicy.Banana(); q.Withdraw(0);
        check(q.Open && q.Available(1) && !q.Available(0), "one lost recipient leaves the other offer");
        q.Accept(); check(!q.Begin(0) && q.Begin(1), "withdrawn recipient cannot claim the objective");
        q = QuestPolicy.Banana(); q.Withdraw(0); q.Withdraw(1);
        check(q.State == QuestState.Unavailable && !q.Accept(), "both lost recipients close the favor");
        foreach (var end in new[] { QuestState.Expired, QuestState.Unavailable, QuestState.Abandoned, QuestState.Faulted })
        {
            q = QuestPolicy.Banana(); q.Accept(); q.End(end, "test");
            check(!q.Begin(0) && !q.Accept() && q.State == end, "terminal favor cannot restart: " + end);
        }
    }
}
