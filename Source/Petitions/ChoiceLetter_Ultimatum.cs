using System.Collections.Generic;
using Petitions.Core;
using RimWorld;
using Verse;

namespace Petitions
{
    /// <summary>
    /// "Things change within 3 days, or I leave." Offers a promise about the pawn's strongest
    /// grievance, which holds the clock until the promise is kept or broken (spec section 2.7).
    /// </summary>
    public class ChoiceLetter_Ultimatum : ChoiceLetter
    {
        public Pawn pawn;

        private PawnGrievanceState State => PetitionsGameComponent.Instance?.TryGetState(pawn);

        public override bool CanShowInLetterStack =>
            base.CanShowInLetterStack && !PetitionsGameComponent.IsGone(pawn) && State != null && State.ultimatumDeadlineTick >= 0;

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                PawnGrievanceState state = State;
                if (ArchivedOnly || state == null || state.ultimatumDeadlineTick < 0 || PetitionsGameComponent.IsGone(pawn))
                {
                    yield return Option_Close;
                    yield break;
                }
                GrievanceDef grievance = PromisableGrievance(state);
                DiaOption promise = new DiaOption(grievance == null
                    ? "Petitions_Ultimatum_OptionPromiseNone".Translate().ToString()
                    : "Petitions_Ultimatum_OptionPromise".Translate(grievance.label.Named("GRIEVANCE")).ToString())
                {
                    action = () => Promise(state, grievance),
                    resolveTree = true,
                };
                if (grievance == null)
                {
                    promise.Disable("Petitions_Ultimatum_NothingToPromise".Translate());
                }
                yield return promise;
                yield return new DiaOption("Petitions_Ultimatum_OptionWait".Translate())
                {
                    resolveTree = true,
                };
                yield return Option_JumpToLocation;
            }
        }

        /// <summary>The grievance with the most pressure that has no petition in progress; falls back to the last one raised.</summary>
        public static GrievanceDef PromisableGrievance(PawnGrievanceState state)
        {
            PetitionsGameComponent comp = PetitionsGameComponent.Instance;
            if (comp == null)
            {
                return null;
            }
            GrievanceDef best = null;
            float bestPressure = 0f;
            foreach (KeyValuePair<GrievanceDef, float> entry in state.Pressures)
            {
                if (entry.Value > bestPressure && !comp.HasActivePetition(state.pawn, entry.Key))
                {
                    best = entry.Key;
                    bestPressure = entry.Value;
                }
            }
            if (best == null && state.lastGrievance != null && !comp.HasActivePetition(state.pawn, state.lastGrievance))
            {
                best = state.lastGrievance;
            }
            return best;
        }

        private static void Promise(PawnGrievanceState state, GrievanceDef grievance)
        {
            PetitionsGameComponent comp = PetitionsGameComponent.Instance;
            if (comp == null || grievance == null || state.ultimatumDeadlineTick < 0 || comp.HasActivePetition(state.pawn, grievance))
            {
                return;
            }
            Petition petition = PetitionFactory.Create(comp, state.pawn, grievance, Find.TickManager.TicksGame, fromUltimatum: true);
            PetitionResponder.Accept(petition);
        }

        public static void Send(PawnGrievanceState state)
        {
            ChoiceLetter_Ultimatum letter = (ChoiceLetter_Ultimatum)LetterMaker.MakeLetter(PetitionsDefOf.Petitions_Ultimatum);
            letter.pawn = state.pawn;
            letter.Label = "Petitions_Ultimatum_Label".Translate(state.pawn.Named("PAWN"));
            GrievanceDef grievance = PromisableGrievance(state);
            letter.Text = "Petitions_Ultimatum_Text".Translate(state.pawn.Named("PAWN"),
                    (UltimatumModel.DeadlineTicks / (float)GenDate.TicksPerDay).ToString("0").Named("DAYS"),
                    state.resentment.ToString("0").Named("RESENTMENT"),
                    ResentmentModel.ExitThreshold(ResentmentStage.Ultimatum).ToString("0").Named("EXIT"))
                + "\n\n" + (grievance == null
                    ? "Petitions_Ultimatum_TextNoGrievance".Translate()
                    : "Petitions_Ultimatum_TextGrievance".Translate(grievance.label.Named("GRIEVANCE"), grievance.demand.Named("DEMAND")));
            letter.lookTargets = state.pawn;
            Find.LetterStack.ReceiveLetter(letter);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref pawn, "pawn");
        }
    }
}
