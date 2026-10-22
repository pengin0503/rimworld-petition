using System.Collections.Generic;
using Petitions.Core;
using RimWorld;
using Verse;

namespace Petitions
{
    /// <summary>Who answers petitions for the colony, and how persuasive they are (spec section 2.5).</summary>
    public static class Negotiator
    {
        public static Pawn Find(IReadOnlyCollection<Pawn> excludedSigners)
        {
            Pawn leader = IdeoLeader();
            if (leader != null && CanNegotiate(leader, excludedSigners))
            {
                return leader;
            }
            Pawn best = null;
            float bestAbility = float.MinValue;
            foreach (Pawn pawn in PawnsFinder.AllMaps_FreeColonistsSpawned)
            {
                if (!CanNegotiate(pawn, excludedSigners))
                {
                    continue;
                }
                float ability = pawn.GetStatValue(StatDefOf.NegotiationAbility);
                if (ability > bestAbility)
                {
                    best = pawn;
                    bestAbility = ability;
                }
            }
            return best;
        }

        public static float PersuasionChance(Pawn negotiator, Petition petition)
        {
            if (negotiator == null)
            {
                return 0f;
            }
            float opinionSum = 0f;
            int counted = 0;
            for (int i = 0; i < petition.signers.Count; i++)
            {
                Pawn signer = petition.signers[i];
                if (signer?.relations != null && signer != negotiator)
                {
                    opinionSum += signer.relations.OpinionOf(negotiator);
                    counted++;
                }
            }
            int averageOpinion = counted > 0 ? (int)System.Math.Round(opinionSum / counted) : 0;
            return PersuasionModel.SuccessChance(
                negotiator.GetStatValue(StatDefOf.NegotiationAbility),
                averageOpinion,
                petition.CosignerCount,
                petition.grievance.persuasionDifficulty);
        }

        private static bool CanNegotiate(Pawn pawn, IReadOnlyCollection<Pawn> excludedSigners)
        {
            if (!PetitionEligibility.CanHaveGrievances(pawn) || pawn.Downed || pawn.InMentalState)
            {
                return false;
            }
            foreach (Pawn signer in excludedSigners)
            {
                if (signer == pawn)
                {
                    return false;
                }
            }
            return true;
        }

        private static Pawn IdeoLeader()
        {
            if (!ModsConfig.IdeologyActive || PreceptDefOf.IdeoRole_Leader == null)
            {
                return null;
            }
            Ideo ideo = Faction.OfPlayer?.ideos?.PrimaryIdeo;
            if (ideo == null)
            {
                return null;
            }
            List<Precept_Role> roles = ideo.RolesListForReading;
            for (int i = 0; i < roles.Count; i++)
            {
                if (roles[i].def == PreceptDefOf.IdeoRole_Leader)
                {
                    return roles[i].ChosenPawnSingle();
                }
            }
            return null;
        }
    }
}
