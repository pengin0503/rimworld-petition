using System.Collections.Generic;
using Petitions.Core;
using RimWorld;
using Verse;

namespace Petitions
{
    /// <summary>
    /// The player's answers to a petition and their effects (spec section 2.4). The letter,
    /// the Petitions tab and auto-responses all go through here.
    /// </summary>
    public static class PetitionResponder
    {
        private static readonly List<Letter> tmpLetters = new List<Letter>();

        private static int Now => Find.TickManager.TicksGame;

        public static void Accept(Petition petition)
        {
            if (petition == null || petition.state != PetitionState.Open)
            {
                return;
            }
            petition.state = PetitionState.Promised;
            petition.promiseStartTick = Now;
            petition.promiseDeadlineTick = Now + (int)(petition.grievance.promiseDays * GenDate.TicksPerDay);
            for (int i = 0; i < petition.clearSinceTicks.Count; i++)
            {
                petition.clearSinceTicks[i] = -1;
            }
            ApplyOutcome(petition, PetitionOutcome.Accepted);
            RemoveLetters(petition);
            Messages.Message("Petitions_Message_Accepted".Translate(petition.Petitioner.Named("PAWN"),
                petition.grievance.label.Named("GRIEVANCE"), petition.grievance.promiseDays.ToString("0.#").Named("DAYS")),
                petition.Petitioner, MessageTypeDefOf.NeutralEvent);
        }

        public static void Persuade(Petition petition)
        {
            if (petition == null || petition.state != PetitionState.Open)
            {
                return;
            }
            Pawn negotiator = Negotiator.Find(petition.signers);
            if (negotiator == null)
            {
                return;
            }
            bool success = Rand.Chance(Negotiator.PersuasionChance(negotiator, petition));
            petition.state = success ? PetitionState.Persuaded : PetitionState.PersuasionFailed;
            ApplyOutcome(petition, success ? PetitionOutcome.Persuaded : PetitionOutcome.PersuasionFailed);
            if (!success)
            {
                GiveOpinionOf(petition, negotiator, PetitionsDefOf.Petitions_BrushedMeOff);
            }
            RemoveLetters(petition);
            if (success)
            {
                Messages.Message("Petitions_Message_Persuaded".Translate(negotiator.Named("NEGOTIATOR"), petition.Petitioner.Named("PAWN")),
                    petition.Petitioner, MessageTypeDefOf.PositiveEvent);
            }
            else
            {
                Messages.Message("Petitions_Message_PersuasionFailed".Translate(negotiator.Named("NEGOTIATOR"), petition.Petitioner.Named("PAWN")),
                    petition.Petitioner, MessageTypeDefOf.NegativeEvent);
            }
        }

        public static void Reject(Petition petition)
        {
            if (petition == null || petition.state != PetitionState.Open)
            {
                return;
            }
            petition.state = PetitionState.Rejected;
            ApplyOutcome(petition, PetitionOutcome.Rejected);
            Pawn negotiator = Negotiator.Find(petition.signers);
            if (negotiator != null)
            {
                GiveOpinionOf(petition, negotiator, PetitionsDefOf.Petitions_RejectedMyPetition);
            }
            RemoveLetters(petition);
            Messages.Message("Petitions_Message_Rejected".Translate(petition.Petitioner.Named("PAWN")),
                petition.Petitioner, MessageTypeDefOf.NegativeEvent);
        }

        /// <summary>The petition went unanswered for too long.</summary>
        public static void Expire(Petition petition)
        {
            if (petition == null || petition.state != PetitionState.Open)
            {
                return;
            }
            petition.state = PetitionState.Ignored;
            ApplyOutcome(petition, PetitionOutcome.Ignored);
            RemoveLetters(petition);
            Messages.Message("Petitions_Message_Ignored".Translate(petition.Petitioner.Named("PAWN")),
                petition.Petitioner, MessageTypeDefOf.NegativeEvent);
        }

        /// <summary>Closes an active petition that has no signers left, without effect.</summary>
        public static void Withdraw(Petition petition)
        {
            if (petition == null || !petition.IsActive)
            {
                return;
            }
            petition.state = PetitionState.Withdrawn;
            RemoveLetters(petition);
        }

        /// <summary>Gives every remaining signer the outcome's mood thought and resentment change.</summary>
        public static void ApplyOutcome(Petition petition, PetitionOutcome outcome)
        {
            PetitionsGameComponent comp = PetitionsGameComponent.Instance;
            float delta = OutcomeEffects.ResentmentDelta(outcome, petition.CosignerCount);
            ThoughtDef thought = MoodThoughtFor(outcome);
            for (int i = 0; i < petition.signers.Count; i++)
            {
                Pawn signer = petition.signers[i];
                if (PetitionsGameComponent.IsGone(signer))
                {
                    continue;
                }
                signer.needs?.mood?.thoughts?.memories?.TryGainMemory(thought);
                if (comp != null)
                {
                    PawnGrievanceState state = comp.GetOrCreateState(signer);
                    state.resentment = ResentmentModel.Clamp(state.resentment + delta);
                }
            }
        }

        public static ThoughtDef MoodThoughtFor(PetitionOutcome outcome)
        {
            switch (outcome)
            {
                case PetitionOutcome.Accepted: return PetitionsDefOf.Petitions_PetitionHeard;
                case PetitionOutcome.PromiseKept: return PetitionsDefOf.Petitions_PromiseKept;
                case PetitionOutcome.PromiseBroken: return PetitionsDefOf.Petitions_PromiseBroken;
                case PetitionOutcome.Rejected: return PetitionsDefOf.Petitions_PetitionRejected;
                case PetitionOutcome.Ignored: return PetitionsDefOf.Petitions_PetitionIgnored;
                case PetitionOutcome.Persuaded: return PetitionsDefOf.Petitions_TalkedItThrough;
                default: return PetitionsDefOf.Petitions_PersuasionFailed;
            }
        }

        private static void GiveOpinionOf(Petition petition, Pawn negotiator, ThoughtDef socialThought)
        {
            for (int i = 0; i < petition.signers.Count; i++)
            {
                Pawn signer = petition.signers[i];
                if (signer != negotiator && !PetitionsGameComponent.IsGone(signer))
                {
                    signer.needs?.mood?.thoughts?.memories?.TryGainMemory(socialThought, negotiator);
                }
            }
        }

        public static void RemoveLetters(Petition petition)
        {
            tmpLetters.Clear();
            List<Letter> letters = Find.LetterStack.LettersListForReading;
            for (int i = 0; i < letters.Count; i++)
            {
                if (letters[i] is ChoiceLetter_Petition letter && letter.petitionId == petition.id)
                {
                    tmpLetters.Add(letter);
                }
            }
            for (int i = 0; i < tmpLetters.Count; i++)
            {
                Find.LetterStack.RemoveLetter(tmpLetters[i]);
            }
            tmpLetters.Clear();
        }
    }
}
