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

        /// <summary>
        /// Which of the pawn's ultimatums this letter is about (<see cref="PawnGrievanceState.ultimatumCount"/>).
        /// Letters saved before this existed load as 0, which matches a pawn's first ultimatum.
        /// </summary>
        public int ultimatumNumber;

        private PawnGrievanceState State => PetitionsGameComponent.Instance?.TryGetState(pawn);

        /// <summary>The ultimatum this letter was sent for is still running; a later ultimatum has its own letter.</summary>
        private bool IsCurrent(PawnGrievanceState state) =>
            state != null && UltimatumModel.LetterIsCurrent(state.HasUltimatum, state.ultimatumCount, ultimatumNumber) && !PetitionsGameComponent.IsGone(pawn);

        public override bool CanShowInLetterStack => base.CanShowInLetterStack && IsCurrent(State);

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                PawnGrievanceState state = State;
                if (ArchivedOnly || !IsCurrent(state))
                {
                    yield return Option_Close;
                    yield break;
                }
                GrievanceDef grievance = PromisableGrievance(state);
                DiaOption promise = new DiaOption(grievance == null
                    ? "Petitions_Ultimatum_OptionPromiseNone".Translate().ToString()
                    : "Petitions_Ultimatum_OptionPromise".Translate(grievance.label.Named("GRIEVANCE")).ToString())
                {
                    action = () => Promise(state, grievance, ultimatumNumber),
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

        private static void Promise(PawnGrievanceState state, GrievanceDef grievance, int number)
        {
            PetitionsGameComponent comp = PetitionsGameComponent.Instance;
            if (comp == null || grievance == null || !UltimatumModel.LetterIsCurrent(state.HasUltimatum, state.ultimatumCount, number)
                || comp.HasActivePetition(state.pawn, grievance))
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
            letter.ultimatumNumber = state.ultimatumCount;
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

        /// <summary>Takes the pawn's ultimatum letters off the stack once that ultimatum is over.</summary>
        public static void RemoveFor(Pawn pawn)
        {
            List<Letter> letters = Find.LetterStack.LettersListForReading;
            for (int i = letters.Count - 1; i >= 0; i--)
            {
                if (letters[i] is ChoiceLetter_Ultimatum letter && letter.pawn == pawn)
                {
                    Find.LetterStack.RemoveLetter(letter);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Values.Look(ref ultimatumNumber, "ultimatumNumber");
        }
    }
}
