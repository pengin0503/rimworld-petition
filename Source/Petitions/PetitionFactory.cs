using System.Collections.Generic;
using Petitions.Core;
using RimWorld;
using Verse;

namespace Petitions
{
    /// <summary>Turns built-up grievance pressure into petitions (spec section 2.3).</summary>
    public static class PetitionFactory
    {
        private static readonly List<Pawn> tmpPawns = new List<Pawn>();

        private static readonly List<float> tmpPressures = new List<float>();

        /// <summary>A pawn who can speak up right now.</summary>
        public static bool CanPetition(Pawn pawn)
        {
            return PetitionEligibility.CanHaveGrievances(pawn) && !pawn.Downed && !pawn.InMentalState;
        }

        /// <summary>Creates at most one petition, or returns null.</summary>
        public static Petition TryCreate(PetitionsGameComponent comp, int nowTick)
        {
            PetitionsSettings settings = PetitionsMod.Settings;
            RateLimits limits = settings.ToRateLimits();
            if (comp.OpenPetitionCount >= limits.MaxOpenPetitions)
            {
                return null;
            }

            Pawn bestPawn = null;
            GrievanceDef bestGrievance = null;
            float bestRatio = 0f;
            List<GrievanceDef> grievances = DefDatabase<GrievanceDef>.AllDefsListForReading;
            foreach (Pawn pawn in PawnsFinder.AllMaps_FreeColonistsSpawned)
            {
                if (!CanPetition(pawn))
                {
                    continue;
                }
                PawnGrievanceState state = comp.TryGetState(pawn);
                if (state == null)
                {
                    continue;
                }
                for (int i = 0; i < grievances.Count; i++)
                {
                    GrievanceDef grievance = grievances[i];
                    float ratio = state.GetPressure(grievance) / PetitionsGameComponent.EffectiveThreshold(grievance);
                    if (ratio < 1f || ratio <= bestRatio || comp.HasActivePetition(pawn, grievance))
                    {
                        continue;
                    }
                    RateLimitInput input = new RateLimitInput(nowTick, comp.lastColonyPetitionTick, comp.OpenPetitionCount,
                        state.lastPetitionTick, state.LastPetitionTick(grievance));
                    if (PetitionRateLimiter.Check(input, limits) != RateLimitResult.Allowed)
                    {
                        continue;
                    }
                    bestPawn = pawn;
                    bestGrievance = grievance;
                    bestRatio = ratio;
                }
            }
            if (bestPawn == null)
            {
                return null;
            }
            return Create(comp, bestPawn, bestGrievance, nowTick, fromUltimatum: false);
        }

        /// <summary>Creates a petition for a chosen pawn and grievance, gathering cosigners. Ignores rate limits.</summary>
        public static Petition Create(PetitionsGameComponent comp, Pawn petitioner, GrievanceDef grievance, int nowTick, bool fromUltimatum)
        {
            List<Pawn> signers = new List<Pawn> { petitioner };
            if (!fromUltimatum)
            {
                signers.AddRange(Cosigners(comp, petitioner, grievance));
            }
            Petition petition = comp.AddPetition(grievance, signers, nowTick, fromUltimatum);
            for (int i = 0; i < signers.Count; i++)
            {
                PawnGrievanceState state = comp.GetOrCreateState(signers[i]);
                state.SetPressure(grievance, 0f);
                state.lastGrievance = grievance;
            }
            comp.GetOrCreateState(petitioner).NotePetition(grievance, nowTick);
            if (!fromUltimatum)
            {
                comp.lastColonyPetitionTick = nowTick;
            }
            return petition;
        }

        private static List<Pawn> Cosigners(PetitionsGameComponent comp, Pawn petitioner, GrievanceDef grievance)
        {
            tmpPawns.Clear();
            tmpPressures.Clear();
            tmpPawns.Add(petitioner);
            tmpPressures.Add(float.MaxValue);
            foreach (Pawn pawn in PawnsFinder.AllMaps_FreeColonistsSpawned)
            {
                if (pawn == petitioner || !PetitionEligibility.CanHaveGrievances(pawn) || comp.HasActivePetition(pawn, grievance))
                {
                    continue;
                }
                PawnGrievanceState state = comp.TryGetState(pawn);
                if (state == null)
                {
                    continue;
                }
                tmpPawns.Add(pawn);
                tmpPressures.Add(state.GetPressure(grievance));
            }
            List<int> chosen = CosignerSelector.Select(tmpPressures, 0, PetitionsGameComponent.EffectiveThreshold(grievance),
                grievance.cosignFraction, CosignerSelector.MaxCosigners);
            List<Pawn> cosigners = new List<Pawn>(chosen.Count);
            for (int i = 0; i < chosen.Count; i++)
            {
                cosigners.Add(tmpPawns[chosen[i]]);
            }
            tmpPawns.Clear();
            tmpPressures.Clear();
            return cosigners;
        }
    }
}
