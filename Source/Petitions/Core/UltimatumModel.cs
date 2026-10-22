namespace Petitions.Core
{
    public enum UltimatumDecision
    {
        Wait,
        Leave,
        Resolved,
    }

    /// <summary>What happens when a pawn under ultimatum is checked (spec section 2.7).</summary>
    public static class UltimatumModel
    {
        public const int DeadlineTicks = 3 * PetitionRateLimiter.TicksPerDay;

        public static UltimatumDecision Decide(int nowTick, int deadlineTick, ResentmentStage stage, bool hasPendingPromise)
        {
            if (stage < ResentmentStage.Ultimatum)
            {
                return UltimatumDecision.Resolved;
            }
            if (hasPendingPromise)
            {
                return UltimatumDecision.Wait;
            }
            return nowTick >= deadlineTick ? UltimatumDecision.Leave : UltimatumDecision.Wait;
        }
    }
}
