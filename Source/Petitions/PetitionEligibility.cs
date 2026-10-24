using RimWorld;
using Verse;

namespace Petitions
{
    /// <summary>Who can build up grievances (spec section 2.3).</summary>
    public static class PetitionEligibility
    {
        public static bool CanHaveGrievances(Pawn pawn)
        {
            return pawn != null
                && !pawn.Dead
                && pawn.IsFreeNonSlaveColonist
                && pawn.RaceProps.Humanlike
                && pawn.DevelopmentalStage.Adult()
                && !pawn.IsQuestLodger()
                && pawn.needs?.mood != null
                && pawn.Spawned
                && pawn.Map.IsPlayerHome;
        }
    }
}
