using System;

namespace Petitions.Core
{
    /// <summary>Chance that the negotiator talks the signers out of their petition (spec section 2.5).</summary>
    public static class PersuasionModel
    {
        public const float MinChance = 0.05f;
        public const float MaxChance = 0.95f;

        /// <param name="negotiationAbility">The negotiator's NegotiationAbility stat.</param>
        /// <param name="opinionOfNegotiator">The petitioner's opinion of the negotiator, -100..100.</param>
        /// <param name="cosigners">Number of cosigners, excluding the petitioner.</param>
        /// <param name="difficulty">GrievanceDef.persuasionDifficulty.</param>
        public static float SuccessChance(float negotiationAbility, int opinionOfNegotiator, int cosigners, float difficulty)
        {
            float chance = 0.45f * negotiationAbility
                + opinionOfNegotiator / 500f
                - 0.05f * Math.Max(0, cosigners)
                - difficulty;
            return Math.Min(MaxChance, Math.Max(MinChance, chance));
        }
    }
}
