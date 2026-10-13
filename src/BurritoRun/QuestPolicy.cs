using System;

namespace BurritoRun
{
    internal enum QuestState { Offered, Accepted, Exchanging, Completed, Abandoned, Unavailable, Expired, Faulted }

    internal sealed class QuestOffer
    {
        internal string Id { get; }
        internal string Recipient { get; }
        internal string RewardItem { get; }
        internal int RewardCount { get; }
        internal string RewardLabel { get; }
        internal string Thanks { get; }
        internal QuestOffer(string id, string recipient, string rewardItem, int rewardCount, string rewardLabel, string thanks)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(recipient) || string.IsNullOrEmpty(rewardItem) || rewardCount <= 0)
                throw new ArgumentException("Invalid quest offer");
            Id = id; Recipient = recipient; RewardItem = rewardItem; RewardCount = rewardCount;
            RewardLabel = rewardLabel; Thanks = thanks;
        }
    }

    // Pure shared objective: accepting does not choose a recipient. One verified exchange wins.
    internal sealed class QuestPolicy
    {
        internal const string RequiredItem = "Banana";
        internal QuestState State { get; private set; } = QuestState.Offered;
        internal string Winner { get; private set; }
        internal string Reason { get; private set; } = "two-offers";
        private readonly QuestOffer[] offers;
        private readonly bool[] available;
        internal int OfferCount => offers.Length;
        internal bool Open => State == QuestState.Offered || State == QuestState.Accepted;
        internal QuestPolicy(params QuestOffer[] offers)
        {
            if (offers == null || offers.Length < 1 || offers.Length > 8) throw new ArgumentException("Expected 1–8 offers");
            this.offers = (QuestOffer[])offers.Clone(); available = new bool[offers.Length];
            for (int i = 0; i < offers.Length; i++)
            {
                if (offers[i] == null) throw new ArgumentException("Missing offer");
                for (int j = 0; j < i; j++) if (offers[j].Id == offers[i].Id) throw new ArgumentException("Duplicate offer");
                available[i] = true;
            }
        }
        internal QuestOffer Offer(int index) => offers[index];
        internal bool Available(int index) => index >= 0 && index < available.Length && available[index];
        /// <summary>
        /// Accept the shared banana objective while keeping both reward offers open.
        /// Acceptance should not choose a recipient before the player decides which reward to
        /// pursue.
        /// </summary>
        internal bool Accept()
        {
            if (State == QuestState.Accepted) return true;
            if (State != QuestState.Offered) return false;
            State = QuestState.Accepted; Reason = "choose-either-recipient"; return true;
        }
        /// <summary>
        /// Reserve one available offer while its native exchange is being verified.
        /// The temporary exchanging state prevents competing completion paths from paying twice.
        /// </summary>
        internal bool Begin(int index)
        {
            if (State != QuestState.Accepted || !Available(index)) return false;
            State = QuestState.Exchanging; Winner = offers[index].Id; Reason = "verifying-exchange"; return true;
        }
        /// <summary>
        /// Reopen an exchange that could not proceed and retain its refusal reason.
        /// Capacity or reward refusal should allow another attempt without falsely completing the
        /// favor.
        /// </summary>
        internal void Refuse(string reason)
        {
            if (State != QuestState.Exchanging) return;
            State = QuestState.Accepted; Winner = null; Reason = reason;
        }
        /// <summary>
        /// Complete only when the player, recipient and reward counts have the exact expected
        /// changes.
        /// A native API call returning successfully is insufficient evidence of custody transfer
        /// and payment.
        /// </summary>
        internal bool Complete(int playerBefore, int playerAfter, int recipientBefore, int recipientAfter,
            int rewardBefore, int rewardAfter)
        {
            if (State != QuestState.Exchanging) return false;
            QuestOffer winner = null;
            foreach (var offer in offers) if (offer.Id == Winner) winner = offer;
            if (winner == null || playerBefore < 1 || playerAfter != playerBefore - 1 || recipientBefore < 0 ||
                recipientAfter - (long)recipientBefore != 1 || rewardBefore < 0 || rewardAfter - (long)rewardBefore != winner.RewardCount)
            { End(QuestState.Faulted, "exchange-state-mismatch"); return false; }
            State = QuestState.Completed; Reason = "verified-item-and-reward"; return true;
        }
        /// <summary>
        /// Remove a lost recipient's offer while leaving any other offer available.
        /// Losing one neighbor should not end a shared objective that the surviving neighbor can
        /// still fulfill.
        /// </summary>
        internal void Withdraw(int index)
        {
            if (!Open || !Available(index)) return;
            available[index] = false;
            foreach (bool value in available) if (value) return;
            End(QuestState.Unavailable, "recipients-unavailable");
        }
        /// <summary>
        /// Close the objective for an explicit interruption or fault.
        /// Restricting terminal states keeps lifecycle cleanup from masquerading as a completed
        /// exchange.
        /// </summary>
        internal void End(QuestState state, string reason)
        {
            if (!Open && State != QuestState.Exchanging) return;
            if (state != QuestState.Abandoned && state != QuestState.Unavailable && state != QuestState.Expired && state != QuestState.Faulted)
                throw new ArgumentException("Expected a terminal interruption");
            State = state; Reason = reason;
        }
        internal static QuestPolicy Banana() => new QuestPolicy(
            new QuestOffer("buyer", "Buyer", "Money", 15, "$15", "Thanks! A deal's a deal. Here's $15."),
            new QuestOffer("neighbor", "Neighbor", "FirstAidKit", 1, "First Aid Kit", "Thanks! Take this first aid kit. Stay safe."));
    }
}
