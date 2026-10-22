using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Petitions
{
    public enum GrievanceKind
    {
        /// <summary>"Improve this situation." The promise is fulfilled when the thoughts stay gone long enough.</summary>
        Remedy,

        /// <summary>"Stop doing this." The promise is broken if a matching memory appears after it was made.</summary>
        Cessation,
    }

    /// <summary>
    /// A kind of grievance, defined as a bundle of vanilla thoughts (spec section 2.1).
    /// </summary>
    public class GrievanceDef : Def
    {
        public GrievanceKind kind = GrievanceKind.Remedy;

        /// <summary>Negative mood thoughts that count as evidence of this grievance.</summary>
        public List<ThoughtDef> thoughts = new List<ThoughtDef>();

        /// <summary>Pressure (mood points x days) at which the pawn files a petition.</summary>
        public float pressureThreshold = 25f;

        /// <summary>Other pawns join as cosigners at this fraction of the threshold.</summary>
        public float cosignFraction = 0.5f;

        /// <summary>Remedy: days to fix the situation. Cessation: days the practice must stop.</summary>
        public float promiseDays = 5f;

        /// <summary>Remedy only: days the thoughts must stay absent for the promise to count as kept.</summary>
        public float remedyHoldDays = 1f;

        /// <summary>Subtracted from the persuasion chance.</summary>
        public float persuasionDifficulty;

        /// <summary>What the petitioner asks for, e.g. "a proper bed of my own". Shown in the petition letter.</summary>
        [MustTranslate]
        public string demand;

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }
            if (thoughts.NullOrEmpty())
            {
                yield return "has no thoughts.";
            }
            else if (kind == GrievanceKind.Cessation)
            {
                // Cessation detects new occurrences by memory age; situational thoughts have no age.
                foreach (ThoughtDef thought in thoughts)
                {
                    if (thought != null && !thought.IsMemory)
                    {
                        yield return $"is a Cessation grievance but thought {thought.defName} is not a memory.";
                    }
                }
            }
            if (pressureThreshold <= 0f)
            {
                yield return "pressureThreshold must be positive.";
            }
            if (cosignFraction <= 0f || cosignFraction > 1f)
            {
                yield return "cosignFraction must be in (0, 1].";
            }
            if (demand.NullOrEmpty())
            {
                yield return "has no demand text.";
            }
        }
    }
}
