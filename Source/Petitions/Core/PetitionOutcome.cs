namespace Petitions.Core
{
    public enum PetitionOutcome
    {
        Accepted,
        PromiseKept,
        PromiseBroken,
        Rejected,
        Ignored,
        Persuaded,
        PersuasionFailed,
    }

    /// <summary>Resentment change per outcome (spec section 2.4).</summary>
    public static class OutcomeEffects
    {
        public static float BaseResentmentDelta(PetitionOutcome outcome)
        {
            switch (outcome)
            {
                case PetitionOutcome.Accepted: return -10f;
                case PetitionOutcome.PromiseKept: return -25f;
                case PetitionOutcome.PromiseBroken: return 35f;
                case PetitionOutcome.Rejected: return 20f;
                case PetitionOutcome.Ignored: return 20f;
                case PetitionOutcome.Persuaded: return -5f;
                case PetitionOutcome.PersuasionFailed: return 30f;
                default: return 0f;
            }
        }

        /// <summary>Increases are scaled by the number of cosigners; decreases are not.</summary>
        public static float ResentmentDelta(PetitionOutcome outcome, int cosigners)
        {
            float delta = BaseResentmentDelta(outcome);
            return delta > 0f ? ResentmentModel.ScaleIncrease(delta, cosigners) : delta;
        }
    }
}
