using System.Collections.Generic;

namespace Petitions.Core
{
    public enum PromiseStatus
    {
        Pending,
        Kept,
        Broken,
    }

    /// <summary>Decides whether a promise made in answer to a petition was kept (spec section 2.1).</summary>
    public static class PromiseEvaluator
    {
        /// <param name="clearSinceTicks">
        /// One entry per signer currently at home: the tick since which the grievance's thoughts
        /// have been absent, or -1 while they are present. Empty means nobody can be judged yet.
        /// </param>
        public static PromiseStatus EvaluateRemedy(int nowTick, int deadlineTick, int holdTicks, IReadOnlyList<int> clearSinceTicks)
        {
            if (clearSinceTicks.Count == 0)
            {
                return PromiseStatus.Pending;
            }
            bool allHeld = true;
            for (int i = 0; i < clearSinceTicks.Count; i++)
            {
                int since = clearSinceTicks[i];
                if (since < 0 || nowTick - since < holdTicks)
                {
                    allHeld = false;
                    break;
                }
            }
            if (allHeld)
            {
                return PromiseStatus.Kept;
            }
            return nowTick >= deadlineTick ? PromiseStatus.Broken : PromiseStatus.Pending;
        }

        public static PromiseStatus EvaluateCessation(int nowTick, int deadlineTick, bool newOccurrence)
        {
            if (newOccurrence)
            {
                return PromiseStatus.Broken;
            }
            return nowTick >= deadlineTick ? PromiseStatus.Kept : PromiseStatus.Pending;
        }
    }
}
