using System.Linq;
using System.Text;
using LudeonTK;
using Petitions.Core;
using RimWorld;
using Verse;

namespace Petitions
{
    /// <summary>Dev-mode helpers for manual in-game testing (spec section 7).</summary>
    public static class PetitionsDebugActions
    {
        private const string Category = "Petitions";

        [DebugAction(Category, "Run hourly update now", allowedGameStates = AllowedGameStates.Playing)]
        private static void RunHourlyUpdate()
        {
            PetitionsGameComponent.Instance?.UpdateNow();
        }

        [DebugAction(Category, "Fill grievance pressure", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void FillPressure(Pawn pawn)
        {
            PetitionsGameComponent component = PetitionsGameComponent.Instance;
            if (component == null || !PetitionEligibility.CanHaveGrievances(pawn))
            {
                Messages.Message($"{pawn.LabelShortCap} cannot have grievances.", MessageTypeDefOf.RejectInput, historical: false);
                return;
            }
            PawnGrievanceState state = component.GetOrCreateState(pawn);
            foreach (GrievanceDef grievance in DefDatabase<GrievanceDef>.AllDefsListForReading)
            {
                state.SetPressure(grievance, PetitionsGameComponent.EffectiveThreshold(grievance));
            }
        }

        /// <summary>Files a petition for the pawn's strongest grievance, ignoring thresholds and rate limits.</summary>
        [DebugAction(Category, "Force petition", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForcePetition(Pawn pawn)
        {
            PetitionsGameComponent component = PetitionsGameComponent.Instance;
            if (component == null || !PetitionFactory.CanPetition(pawn))
            {
                Messages.Message($"{pawn.LabelShortCap} cannot petition now.", MessageTypeDefOf.RejectInput, historical: false);
                return;
            }
            PawnGrievanceState state = component.GetOrCreateState(pawn);
            GrievanceDef grievance = DefDatabase<GrievanceDef>.AllDefsListForReading
                .Where(g => !component.HasActivePetition(pawn, g))
                .OrderByDescending(g => state.GetPressure(g) / PetitionsGameComponent.EffectiveThreshold(g))
                .FirstOrDefault();
            if (grievance == null)
            {
                Messages.Message("Every grievance already has a petition in progress.", MessageTypeDefOf.RejectInput, historical: false);
                return;
            }
            Petition petition = PetitionFactory.Create(component, pawn, grievance, Find.TickManager.TicksGame, fromUltimatum: false);
            PetitionsGameComponent.Deliver(petition);
        }

        [DebugAction(Category, "Add 20 resentment", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void AddResentment(Pawn pawn)
        {
            PetitionsGameComponent component = PetitionsGameComponent.Instance;
            if (component == null || !PetitionEligibility.CanHaveGrievances(pawn))
            {
                return;
            }
            PawnGrievanceState state = component.GetOrCreateState(pawn);
            state.resentment = ResentmentModel.Clamp(state.resentment + 20f);
            component.UpdateNow();
        }

        [DebugAction(Category, "Clear resentment", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ClearResentment(Pawn pawn)
        {
            PawnGrievanceState state = PetitionsGameComponent.Instance?.TryGetState(pawn);
            if (state == null)
            {
                return;
            }
            state.resentment = 0f;
            PetitionsGameComponent.Instance.UpdateNow();
        }

        [DebugAction(Category, "Expire open petitions", allowedGameStates = AllowedGameStates.Playing)]
        private static void ExpireOpenPetitions()
        {
            PetitionsGameComponent component = PetitionsGameComponent.Instance;
            if (component == null)
            {
                return;
            }
            foreach (Petition petition in component.Petitions.Where(p => p.state == PetitionState.Open).ToList())
            {
                PetitionResponder.Expire(petition);
            }
        }

        /// <summary>Moves every promise and ultimatum deadline to now, then runs an update.</summary>
        [DebugAction(Category, "Advance deadlines to now", allowedGameStates = AllowedGameStates.Playing)]
        private static void AdvanceDeadlines()
        {
            PetitionsGameComponent component = PetitionsGameComponent.Instance;
            if (component == null)
            {
                return;
            }
            int now = Find.TickManager.TicksGame;
            foreach (Petition petition in component.Petitions.Where(p => p.state == PetitionState.Promised))
            {
                petition.promiseDeadlineTick = now;
            }
            foreach (PawnGrievanceState state in component.States.Where(s => s.ultimatumDeadlineTick >= 0))
            {
                state.ultimatumDeadlineTick = now;
            }
            component.UpdateNow();
        }

        [DebugAction(Category, "Log petition state", allowedGameStates = AllowedGameStates.Playing)]
        private static void LogState()
        {
            PetitionsGameComponent component = PetitionsGameComponent.Instance;
            if (component == null)
            {
                return;
            }
            StringBuilder sb = new StringBuilder("[Petitions] State:");
            foreach (PawnGrievanceState state in component.States)
            {
                sb.AppendLine();
                sb.Append("  ").Append(state.pawn?.LabelShort ?? "<null>")
                    .Append($" resentment={state.resentment:0.0} stage={state.stage} ultimatum={state.ultimatumDeadlineTick}: ");
                sb.Append(string.Join(", ", state.Pressures.Select(e => $"{e.Key.defName}={e.Value:0.00}/{PetitionsGameComponent.EffectiveThreshold(e.Key):0.0}")));
            }
            foreach (Petition petition in component.Petitions)
            {
                sb.AppendLine();
                sb.Append($"  petition #{petition.id} {petition.grievance.defName} {petition.state} signers=")
                    .Append(string.Join("/", petition.signers.Select(p => p?.LabelShort ?? "<null>")))
                    .Append($" deadline={petition.promiseDeadlineTick}");
            }
            Log.Message(sb.ToString());
        }
    }
}
