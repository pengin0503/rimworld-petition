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

        /// <summary>
        /// Whether an ultimatum letter still speaks for the pawn: only the letter of the running ultimatum does, so an
        /// old letter left in the stack cannot show again or act when the pawn gives another ultimatum.
        /// </summary>
        public static bool LetterIsCurrent(bool ultimatumRunning, int ultimatumCount, int letterNumber)
        {
            return ultimatumRunning && ultimatumCount == letterNumber;
        }

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
