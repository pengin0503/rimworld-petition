using Petitions.Core;
using RimWorld;
using Verse;

namespace Petitions
{
    /// <summary>Resentment decay, stage changes, the disgruntled hediff, the ultimatum and leaving (spec section 2.7).</summary>
    public static class Escalation
    {
        /// <summary>After walking out, resentment is set just below the ultimatum's exit threshold.</summary>
        private const float ResentmentAfterLeaving = 60f;

        public static void Update(PawnGrievanceState state, int nowTick, float days)
        {
            Pawn pawn = state.pawn;
            if (PetitionsGameComponent.IsGone(pawn))
            {
                return;
            }
            PetitionsSettings settings = PetitionsMod.Settings;
            state.resentment = ResentmentModel.Decay(state.resentment, days);
            ResentmentStage oldStage = state.stage;
            state.stage = ResentmentModel.NextStage(oldStage, state.resentment);
            if (state.stage != oldStage)
            {
                NotifyStageChange(pawn, oldStage, state.stage);
            }

            UpdateDisgruntled(pawn, settings.enableDisgruntled && state.stage >= ResentmentStage.Disgruntled);

            if (!settings.enableUltimatum)
            {
                state.ultimatumDeadlineTick = -1;
                return;
            }
            if (pawn.MentalStateDef == MentalStateDefOf_GiveUpExit)
            {
                return;
            }
            if (state.stage == ResentmentStage.Ultimatum && state.ultimatumDeadlineTick < 0)
            {
                state.ultimatumDeadlineTick = nowTick + UltimatumModel.DeadlineTicks;
                ChoiceLetter_Ultimatum.Send(state);
                return;
            }
            if (state.ultimatumDeadlineTick < 0)
            {
                return;
            }
            bool pendingPromise = PetitionsGameComponent.Instance?.HasPendingPromise(pawn) ?? false;
            switch (UltimatumModel.Decide(nowTick, state.ultimatumDeadlineTick, state.stage, pendingPromise))
            {
                case UltimatumDecision.Resolved:
                    state.ultimatumDeadlineTick = -1;
                    Messages.Message("Petitions_UltimatumResolved".Translate(pawn.Named("PAWN")), pawn, MessageTypeDefOf.PositiveEvent);
                    break;
                case UltimatumDecision.Leave:
                    TryLeave(state);
                    break;
            }
        }

        private static MentalStateDef MentalStateDefOf_GiveUpExit => DefDatabase<MentalStateDef>.GetNamedSilentFail("GiveUpExit");

        /// <summary>Starts vanilla "giving up and leaving"; retried next hour if the pawn can't start it now.</summary>
        private static void TryLeave(PawnGrievanceState state)
        {
            Pawn pawn = state.pawn;
            MentalStateDef giveUp = MentalStateDefOf_GiveUpExit;
            if (giveUp == null || !PetitionEligibility.CanHaveGrievances(pawn) || pawn.Downed || pawn.InMentalState)
            {
                return;
            }
            string grievance = state.lastGrievance?.label ?? "Petitions_LeavingReason_Unknown".Translate().ToString();
            string reason = "Petitions_LeavingReason".Translate(pawn.Named("PAWN"), grievance.Named("GRIEVANCE"));
            if (pawn.mindState.mentalStateHandler.TryStartMentalState(giveUp, reason, forced: true, forceWake: true))
            {
                state.ultimatumDeadlineTick = -1;
                state.resentment = ResentmentAfterLeaving;
                state.stage = ResentmentModel.NextStage(ResentmentStage.Ultimatum, state.resentment);
            }
        }

        private static void UpdateDisgruntled(Pawn pawn, bool wanted)
        {
            if (pawn.health == null)
            {
                return;
            }
            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(PetitionsDefOf.Petitions_Disgruntled);
            if (wanted && existing == null)
            {
                pawn.health.AddHediff(PetitionsDefOf.Petitions_Disgruntled);
            }
            else if (!wanted && existing != null)
            {
                pawn.health.RemoveHediff(existing);
            }
        }

        private static void NotifyStageChange(Pawn pawn, ResentmentStage oldStage, ResentmentStage newStage)
        {
            if (newStage > oldStage)
            {
                if (newStage == ResentmentStage.Grumbling)
                {
                    Messages.Message("Petitions_Stage_Grumbling".Translate(pawn.Named("PAWN")), pawn, MessageTypeDefOf.NeutralEvent);
                }
                else if (newStage == ResentmentStage.Disgruntled)
                {
                    Messages.Message("Petitions_Stage_Disgruntled".Translate(pawn.Named("PAWN")), pawn, MessageTypeDefOf.NegativeEvent);
                }
                // The ultimatum stage is announced by its letter.
            }
            else if (newStage == ResentmentStage.Calm)
            {
                Messages.Message("Petitions_Stage_Calm".Translate(pawn.Named("PAWN")), pawn, MessageTypeDefOf.PositiveEvent);
            }
        }
    }
}
