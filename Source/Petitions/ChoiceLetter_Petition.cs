using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace Petitions
{
    /// <summary>The petition letter (spec sections 2.4 and 6).</summary>
    public class ChoiceLetter_Petition : ChoiceLetter
    {
        public int petitionId;

        private Petition Petition => PetitionsGameComponent.Instance?.GetPetition(petitionId);

        public override bool CanShowInLetterStack => base.CanShowInLetterStack && Petition?.state == PetitionState.Open;

        public override IEnumerable<DiaOption> Choices
        {
            get
            {
                Petition petition = Petition;
                if (ArchivedOnly || petition == null || petition.state != PetitionState.Open)
                {
                    yield return Option_Close;
                    yield break;
                }
                yield return new DiaOption("Petitions_Letter_OptionAccept".Translate())
                {
                    action = () => PetitionResponder.Accept(petition),
                    resolveTree = true,
                };
                Pawn negotiator = Negotiator.Find(petition.signers);
                DiaOption persuade = new DiaOption(negotiator == null
                    ? "Petitions_Letter_OptionPersuadeNoNegotiator".Translate().ToString()
                    : "Petitions_Letter_OptionPersuade".Translate(negotiator.Named("NEGOTIATOR"),
                        Negotiator.PersuasionChance(negotiator, petition).ToStringPercent().Named("CHANCE")).ToString())
                {
                    action = () => PetitionResponder.Persuade(petition),
                    resolveTree = true,
                };
                if (negotiator == null)
                {
                    persuade.Disable("Petitions_NoNegotiator".Translate());
                }
                yield return persuade;
                yield return new DiaOption("Petitions_Letter_OptionReject".Translate())
                {
                    action = () => PetitionResponder.Reject(petition),
                    resolveTree = true,
                };
                yield return Option_Postpone;
            }
        }

        public static void Send(Petition petition)
        {
            ChoiceLetter_Petition letter = (ChoiceLetter_Petition)LetterMaker.MakeLetter(PetitionsDefOf.Petitions_Petition);
            letter.petitionId = petition.id;
            letter.Label = "Petitions_Letter_Label".Translate(petition.Petitioner.Named("PAWN"));
            letter.Text = BuildText(petition);
            letter.lookTargets = petition.Petitioner;
            Find.LetterStack.ReceiveLetter(letter);
        }

        public static TaggedString BuildText(Petition petition)
        {
            Pawn petitioner = petition.Petitioner;
            GrievanceDef grievance = petition.grievance;
            StringBuilder sb = new StringBuilder();
            sb.Append("Petitions_Letter_Intro".Translate(petitioner.Named("PAWN"), grievance.label.Named("GRIEVANCE"),
                grievance.demand.Named("DEMAND")).Resolve());
            if (petition.CosignerCount > 0)
            {
                sb.Append("\n\n").Append("Petitions_Letter_Cosigners".Translate(
                    petition.signers.Skip(1).Select(p => p.LabelShort).ToCommaList(useAnd: true).Named("NAMES")).Resolve());
            }
            List<(string label, float moodOffset)> causes = GrievanceScanner.Causes(petitioner, grievance, 3);
            if (causes.Count > 0)
            {
                sb.Append("\n\n").Append("Petitions_Letter_Causes".Translate().Resolve());
                foreach ((string label, float moodOffset) in causes)
                {
                    sb.Append("\n  - ").Append(label).Append(" (").Append(moodOffset.ToString("0")).Append(')');
                }
            }

            sb.Append("\n\n").Append(grievance.kind == GrievanceKind.Remedy
                ? "Petitions_Letter_AcceptRemedy".Translate(grievance.promiseDays.ToString("0.#").Named("DAYS"),
                    grievance.remedyHoldDays.ToString("0.#").Named("HOLD")).Resolve()
                : "Petitions_Letter_AcceptCessation".Translate(grievance.promiseDays.ToString("0.#").Named("DAYS")).Resolve());

            Pawn negotiator = Negotiator.Find(petition.signers);
            sb.Append("\n\n").Append(negotiator == null
                ? "Petitions_Letter_PersuadeNoNegotiator".Translate().Resolve()
                : "Petitions_Letter_Persuade".Translate(negotiator.Named("NEGOTIATOR"),
                    Negotiator.PersuasionChance(negotiator, petition).ToStringPercent().Named("CHANCE")).Resolve());
            sb.Append("\n\n").Append("Petitions_Letter_Reject".Translate().Resolve());
            sb.Append("\n\n").Append("Petitions_Letter_Expiry".Translate(
                PetitionsMod.Settings.petitionExpiryDays.ToString("0.#").Named("DAYS")).Resolve());
            return sb.ToString();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref petitionId, "petitionId");
        }
    }
}
