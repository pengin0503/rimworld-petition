using System.Collections.Generic;
using Petitions.Core;
using RimWorld;
using Verse;

namespace Petitions
{
    /// <summary>Who answers petitions for the colony, and how persuasive they are (spec sections 2.5, 10.3).</summary>
    public static class Negotiator
    {
        /// <summary>The assigned negotiator if able, else the ideoligion leader, else the best negotiator; never a signer.</summary>
        public static Pawn Find(IReadOnlyCollection<Pawn> excludedSigners)
        {
            return NegotiatorSelector.Select(
                PetitionsGameComponent.Instance?.assignedNegotiator,
                IdeoLeader(),
                Candidates(),
                pawn => CanNegotiate(pawn, excludedSigners));
        }

        /// <summary>Who answers when there are no signers to exclude; shown in the Petitions tab.</summary>
        public static Pawn Current => Find(System.Array.Empty<Pawn>());

        private static IEnumerable<(Pawn pawn, float ability)> Candidates()
        {
            foreach (Pawn pawn in PawnsFinder.AllMaps_FreeColonistsSpawned)
            {
                yield return (pawn, pawn.GetStatValue(StatDefOf.NegotiationAbility));
            }
        }

        public static bool CanEverNegotiate(Pawn pawn)
        {
            return CanNegotiate(pawn, System.Array.Empty<Pawn>());
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
