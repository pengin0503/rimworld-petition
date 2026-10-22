using System;

namespace Petitions.Core
{
    /// <summary>
    /// Grievance pressure, measured in "mood points x days" (spec section 2.2).
    /// Pure logic: no RimWorld types, so it can be unit tested without the game.
    /// </summary>
    public static class PressureModel
    {
        public const float HoursPerDay = 24f;

        /// <summary>Pressure builds faster when the pawn is unhappy: 1 at mood 0.5 or above, 2 at mood 0.</summary>
        public static float MoodFactor(float mood)
        {
            return 1f + 2f * Math.Max(0f, 0.5f - mood);
        }

        /// <param name="pressure">Current pressure.</param>
        /// <param name="severity">Sum of the absolute values of the matching negative mood offsets. Zero or less means the grievance is absent.</param>
        /// <param name="mood">Current mood level, 0..1.</param>
        /// <param name="decayPerDay">Pressure lost per day while the grievance is absent.</param>
        /// <param name="hours">Length of the step in in-game hours.</param>
        public static float Step(float pressure, float severity, float mood, float decayPerDay, float hours)
        {
            if (severity > 0f)
            {
                return pressure + severity / HoursPerDay * hours * MoodFactor(mood);
            }
            return Math.Max(0f, pressure - decayPerDay / HoursPerDay * hours);
        }
    }
}
