namespace BurritoRun
{
    // Native ready flags can describe the outgoing character-select room briefly.
    // Require two continuous game seconds; pause does not advance this window.
    internal sealed class QuestStartWindow
    {
        private bool waiting;
        private float since, previousTime, previousLoad;
        /// <summary>
        /// Require two continuous ready game seconds before allowing automatic encounter placement.
        /// Native ready flags can briefly describe the outgoing character screen; transitions and
        /// clock rewinds restart the wait.
        /// </summary>
        internal bool Observe(bool ready, float gameTime, float loadClock)
        {
            if (!ready) { Reset(); return false; }
            if (!waiting || gameTime < previousTime || loadClock + .25f < previousLoad)
            { waiting = true; since = gameTime; }
            previousTime = gameTime; previousLoad = loadClock;
            return gameTime - since >= 2;
        }
        internal void Reset() { waiting = false; }
    }
}
