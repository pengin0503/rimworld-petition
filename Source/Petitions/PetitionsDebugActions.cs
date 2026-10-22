using System.Linq;
using System.Text;
using LudeonTK;
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

        [DebugAction(Category, "Log grievance state", allowedGameStates = AllowedGameStates.Playing)]
        private static void LogState()
        {
            PetitionsGameComponent component = PetitionsGameComponent.Instance;
            if (component == null)
            {
                return;
            }
            StringBuilder sb = new StringBuilder("[Petitions] Grievance pressure:");
            foreach (PawnGrievanceState state in component.States)
            {
                sb.AppendLine();
                sb.Append("  ").Append(state.pawn?.LabelShort ?? "<null>").Append(": ");
                sb.Append(string.Join(", ", state.Pressures.Select(e => $"{e.Key.defName}={e.Value:0.00}/{PetitionsGameComponent.EffectiveThreshold(e.Key):0.0}")));
            }
            Log.Message(sb.ToString());
        }
    }
}
