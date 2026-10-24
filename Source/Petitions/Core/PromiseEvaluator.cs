using System;
using System.Collections.Generic;

namespace Petitions.Core
{
    public enum PromiseStatus
    {
        Pending,
        Kept,
        Broken,

        /// <summary>Remedy only: nobody could be judged before the deadline, even after extending it (spec 2.1).</summary>
        Unjudged,
    }

    /// <summary>Decides whether a promise made in answer to a petition was kept (spec section 2.1).</summary>
    public static class PromiseEvaluator
    {
        /// <summary>Memory age only advances in 150-tick steps, so allow some slack (spec section 10.2).</summary>
        public const int MemoryAgeToleranceTicks = 300;

        /// <summary>Whether a memory of this age was gained after a promise made this many ticks ago.</summary>
        public static bool IsNewSincePromise(int memoryAge, int ticksSincePromise)
        {
            return memoryAge + MemoryAgeToleranceTicks < ticksSincePromise;
        }

        /// <summary>
        /// Whether a memory of this age was gained while the promise ran: after it was made and no later than its
        /// deadline. A memory gained after the deadline is not a broken promise, even when the hourly update that sees
        /// it comes later. Age only grows in 150-tick steps, so the estimate <c>now - age</c> is never earlier than the
        /// real moment; both ends therefore lean towards the player.
        /// </summary>
        public static bool IsDuringPromise(int memoryAge, int nowTick, int promiseStartTick, int deadlineTick)
        {
            return IsNewSincePromise(memoryAge, nowTick - promiseStartTick) && nowTick - memoryAge <= deadlineTick;
        }

        /// <summary>The longest a remedy deadline is moved for signers who were away (spec 2.1).</summary>
        public const int MaxAbsenceExtensionTicks = 10 * PetitionRateLimiter.TicksPerDay;

        /// <summary>
        /// Moves a remedy deadline for signers who are away, so a promise is never judged on nobody (spec 2.1):
        /// while no signer can be judged the deadline moves with time, and when signers come back too close to the
        /// deadline it moves so they can hold the remedy for <paramref name="holdTicks"/>. All moves together stay
        /// within <see cref="MaxAbsenceExtensionTicks"/>; <paramref name="extendedTicks"/> carries the running total.
        /// </summary>
        public static int AdjustRemedyDeadline(int nowTick, int deadlineTick, int holdTicks, int stepTicks,
            bool anyoneJudgeable, bool nobodyJudgeableBefore, ref int extendedTicks)
        {
            int wanted = 0;
            if (!anyoneJudgeable)
            {
                wanted = stepTicks;
            }
            else if (nobodyJudgeableBefore)
            {
                wanted = nowTick + holdTicks - deadlineTick;
            }
            int granted = Math.Min(Math.Max(0, wanted), MaxAbsenceExtensionTicks - extendedTicks);
            if (granted <= 0)
            {
                return deadlineTick;
            }
            extendedTicks += granted;
            return deadlineTick + granted;
        }

        /// <param name="clearSinceTicks">
        /// One entry per signer currently at home: the tick since which the grievance's thoughts
        /// have been absent, or -1 while they are present. Empty means nobody can be judged; the
        /// promise then waits, and ends as <see cref="PromiseStatus.Unjudged"/> at the deadline so
        /// it cannot hang forever.
        /// </param>
        public static PromiseStatus EvaluateRemedy(int nowTick, int deadlineTick, int holdTicks, IReadOnlyList<int> clearSinceTicks)
        {
            if (clearSinceTicks.Count == 0)
            {
                return nowTick >= deadlineTick ? PromiseStatus.Unjudged : PromiseStatus.Pending;
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
