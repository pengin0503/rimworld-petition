using System.Collections.Generic;
using Petitions.Core;
using RimWorld;
using Verse;

namespace Petitions
{
    /// <summary>Follows promised petitions until they are kept or broken (spec section 2.1).</summary>
    public static class PromiseTracker
    {
        private static readonly List<int> tmpClearSince = new List<int>();

        private static readonly Dictionary<GrievanceDef, float> tmpSeverities = new Dictionary<GrievanceDef, float>();

        public static void Update(Petition petition, int nowTick)
        {
            if (petition.state != PetitionState.Promised)
            {
                return;
            }
            for (int i = petition.signers.Count - 1; i >= 0; i--)
            {
                if (PetitionsGameComponent.IsGone(petition.signers[i]))
                {
                    petition.RemoveSigner(petition.signers[i]);
                }
            }
            if (petition.signers.Count == 0)
            {
                PetitionResponder.Withdraw(petition);
                return;
            }

            PromiseStatus status = petition.grievance.kind == GrievanceKind.Remedy
                ? EvaluateRemedy(petition, nowTick)
                : PromiseEvaluator.EvaluateCessation(nowTick, petition.promiseDeadlineTick, HasNewOccurrence(petition, nowTick));
            if (status == PromiseStatus.Kept)
            {
                Keep(petition);
            }
            else if (status == PromiseStatus.Broken)
            {
                Break(petition, nowTick);
            }
        }

        private static PromiseStatus EvaluateRemedy(Petition petition, int nowTick)
        {
            tmpClearSince.Clear();
            for (int i = 0; i < petition.signers.Count; i++)
            {
                Pawn signer = petition.signers[i];
                // Signers who are away (caravans) are neither counted for nor against the promise.
                if (!PetitionEligibility.CanHaveGrievances(signer))
                {
                    continue;
                }
                if (Severity(signer, petition.grievance) > 0f)
                {
                    petition.clearSinceTicks[i] = -1;
                }
                else if (petition.clearSinceTicks[i] < 0)
                {
                    petition.clearSinceTicks[i] = nowTick;
                }
                tmpClearSince.Add(petition.clearSinceTicks[i]);
            }
            int holdTicks = (int)(petition.grievance.remedyHoldDays * GenDate.TicksPerDay);
            PromiseStatus status = PromiseEvaluator.EvaluateRemedy(nowTick, petition.promiseDeadlineTick, holdTicks, tmpClearSince);
            // With nobody at home to judge, a deadline still breaks the promise.
            if (tmpClearSince.Count == 0 && nowTick >= petition.promiseDeadlineTick)
            {
                status = PromiseStatus.Broken;
            }
            tmpClearSince.Clear();
            return status;
        }

        /// <summary>Whether any signer gained a matching memory after the promise was made.</summary>
        private static bool HasNewOccurrence(Petition petition, int nowTick)
        {
            int sincePromise = nowTick - petition.promiseStartTick;
            for (int i = 0; i < petition.signers.Count; i++)
            {
                List<Thought_Memory> memories = petition.signers[i].needs?.mood?.thoughts?.memories?.Memories;
                if (memories == null)
                {
                    continue;
                }
                for (int j = 0; j < memories.Count; j++)
                {
                    if (memories[j].age < sincePromise && petition.grievance.thoughts.Contains(memories[j].def))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static float Severity(Pawn pawn, GrievanceDef grievance)
        {
            GrievanceScanner.Scan(pawn, tmpSeverities);
            tmpSeverities.TryGetValue(grievance, out float severity);
            tmpSeverities.Clear();
            return severity;
        }

        private static void Keep(Petition petition)
        {
            petition.state = PetitionState.Kept;
            PetitionResponder.ApplyOutcome(petition, PetitionOutcome.PromiseKept);
            Find.LetterStack.ReceiveLetter(
                "Petitions_PromiseKept_Label".Translate(),
                "Petitions_PromiseKept_Text".Translate(petition.Petitioner.Named("PAWN"), petition.grievance.label.Named("GRIEVANCE")),
                LetterDefOf.PositiveEvent, petition.Petitioner);
        }

        private static void Break(Petition petition, int nowTick)
        {
            petition.state = PetitionState.Broken;
            PetitionResponder.ApplyOutcome(petition, PetitionOutcome.PromiseBroken);
            // A broken promise ends any ultimatum it was holding off (spec section 2.7).
            PetitionsGameComponent comp = PetitionsGameComponent.Instance;
            for (int i = 0; i < petition.signers.Count; i++)
            {
                PawnGrievanceState state = comp?.TryGetState(petition.signers[i]);
                if (state != null && state.ultimatumDeadlineTick >= 0)
                {
                    state.ultimatumDeadlineTick = nowTick;
                }
            }
            Find.LetterStack.ReceiveLetter(
                "Petitions_PromiseBroken_Label".Translate(),
                "Petitions_PromiseBroken_Text".Translate(petition.Petitioner.Named("PAWN"), petition.grievance.label.Named("GRIEVANCE")),
                LetterDefOf.NegativeEvent, petition.Petitioner);
        }
    }
}
