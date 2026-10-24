namespace Petitions.Core
{
    public enum RateLimitResult
    {
        Allowed,
        TooManyOpen,
        ColonyCooldown,
        PawnCooldown,
        GrievanceCooldown,
    }

    public readonly struct RateLimits
    {
        public readonly float MinDaysBetweenPetitions;
        public readonly int MaxOpenPetitions;
        public readonly float PawnCooldownDays;
        public readonly float GrievanceCooldownDays;

        public RateLimits(float minDaysBetweenPetitions, int maxOpenPetitions, float pawnCooldownDays, float grievanceCooldownDays)
        {
            MinDaysBetweenPetitions = minDaysBetweenPetitions;
            MaxOpenPetitions = maxOpenPetitions;
            PawnCooldownDays = pawnCooldownDays;
            GrievanceCooldownDays = grievanceCooldownDays;
        }
    }

    /// <summary>Tick values use -1 for "never".</summary>
    public readonly struct RateLimitInput
    {
        public readonly int NowTick;
        public readonly int LastColonyPetitionTick;
        public readonly int OpenPetitions;
        public readonly int LastPawnPetitionTick;
        public readonly int LastPawnGrievancePetitionTick;

        public RateLimitInput(int nowTick, int lastColonyPetitionTick, int openPetitions, int lastPawnPetitionTick, int lastPawnGrievancePetitionTick)
        {
            NowTick = nowTick;
            LastColonyPetitionTick = lastColonyPetitionTick;
            OpenPetitions = openPetitions;
            LastPawnPetitionTick = lastPawnPetitionTick;
            LastPawnGrievancePetitionTick = lastPawnGrievancePetitionTick;
        }
    }

    /// <summary>Keeps petitions rare enough not to become micromanagement (spec section 2.6).</summary>
    public static class PetitionRateLimiter
    {
        // Same value as GenDate.TicksPerDay; duplicated so this file stays free of RimWorld references.
        public const int TicksPerDay = 60000;

        public static RateLimitResult Check(RateLimitInput input, RateLimits limits)
        {
            if (input.OpenPetitions >= limits.MaxOpenPetitions)
            {
                return RateLimitResult.TooManyOpen;
            }
            if (!Elapsed(input.NowTick, input.LastColonyPetitionTick, limits.MinDaysBetweenPetitions))
            {
                return RateLimitResult.ColonyCooldown;
            }
            if (!Elapsed(input.NowTick, input.LastPawnPetitionTick, limits.PawnCooldownDays))
            {
                return RateLimitResult.PawnCooldown;
            }
            if (!Elapsed(input.NowTick, input.LastPawnGrievancePetitionTick, limits.GrievanceCooldownDays))
            {
                return RateLimitResult.GrievanceCooldown;
            }
            return RateLimitResult.Allowed;
        }

        private static bool Elapsed(int nowTick, int sinceTick, float days)
        {
            return sinceTick < 0 || nowTick - sinceTick >= (int)(days * TicksPerDay);
        }
    }
}
