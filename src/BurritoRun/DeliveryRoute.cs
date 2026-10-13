namespace BurritoRun
{
    // Policy only: no spawning, inventory, payment, or native pathfinding calls.
    // Native AI owns movement; this bounds how long we trust its delivery job.
    internal sealed class DeliveryRoute
    {
        internal const float ArrivalRadius = 0.8f;
        internal const float MeetingWait = 20f;
        private readonly float started;
        private readonly float arrivalRadius;
        private float lastProgress;
        private float bestDistance;
        private float meetingStarted;
        internal bool WaitingForCustomer { get; private set; }
        internal string FallbackReason { get; private set; }

        internal DeliveryRoute(float now, float distance, float arrivalRadius = ArrivalRadius)
        {
            started = lastProgress = now;
            bestDistance = distance;
            this.arrivalRadius = arrivalRadius;
        }

        /// <summary>
        /// Switch a slow or interrupted route to a bounded meet-the-driver fallback.
        /// Native AI owns movement; the service should offer a recoverable outcome rather than wait
        /// forever for arrival.
        /// </summary>
        internal bool NeedsMeeting(float now, float distance, bool nativeJobActive)
        {
            if (WaitingForCustomer || distance < arrivalRadius) return false;
            if (distance < bestDistance - 0.35f)
            {
                bestDistance = distance;
                lastProgress = now;
            }
            if (now - started < 2f) return false;
            var reason = !nativeJobActive ? "native-job-ended" :
                now - started >= 45f ? "route-deadline" :
                now - lastProgress >= 12f ? "no-progress" : null;
            if (reason == null) return false;
            return BeginMeeting(now, reason);
        }

        /// <summary>
        /// Start the customer meeting timer once and remember why the route ended.
        /// Repeated ticks must not extend the wait indefinitely or change the original fallback
        /// explanation.
        /// </summary>
        internal bool BeginMeeting(float now, string reason)
        {
            if (WaitingForCustomer) return false;
            WaitingForCustomer = true;
            meetingStarted = now;
            FallbackReason = reason;
            return true;
        }

        internal bool MeetingExpired(float now) => WaitingForCustomer && now - meetingStarted >= MeetingWait;
    }
}
