using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Petitions
{
    /// <summary>
    /// The Petitions tab (spec section 6). The skeleton shows section (c), each colonist's
    /// grievance pressure; open and promised petitions are added in the next phase.
    /// </summary>
    public class MainTabWindow_Petitions : MainTabWindow
    {
        private const float RowHeight = 26f;
        private const float PawnColumnWidth = 160f;
        private const float GrievanceColumnWidth = 220f;
        private const float BarWidth = 200f;

        private Vector2 scrollPosition;

        private readonly List<(Pawn pawn, GrievanceDef grievance, float pressure)> rows = new List<(Pawn, GrievanceDef, float)>();

        public override Vector2 RequestedTabSize => new Vector2(720f, 520f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Rect titleRect = inRect.TopPartPixels(36f);
            Widgets.Label(titleRect, "Petitions_Tab_Title".Translate());
            Text.Font = GameFont.Small;

            Rect listRect = new Rect(inRect.x, titleRect.yMax + 6f, inRect.width, inRect.height - titleRect.height - 6f);
            CollectRows();
            if (rows.Count == 0)
            {
                Widgets.Label(listRect, "Petitions_Tab_NoGrievances".Translate());
                return;
            }

            Rect viewRect = new Rect(0f, 0f, listRect.width - 16f, rows.Count * RowHeight);
            Widgets.BeginScrollView(listRect, ref scrollPosition, viewRect);
            for (int i = 0; i < rows.Count; i++)
            {
                DrawRow(new Rect(0f, i * RowHeight, viewRect.width, RowHeight), rows[i].pawn, rows[i].grievance, rows[i].pressure);
            }
            Widgets.EndScrollView();
        }

        private void CollectRows()
        {
            rows.Clear();
            PetitionsGameComponent component = PetitionsGameComponent.Instance;
            if (component == null)
            {
                return;
            }
            foreach (PawnGrievanceState state in component.States.OrderBy(s => s.pawn.LabelShortCap))
            {
                foreach (KeyValuePair<GrievanceDef, float> entry in state.Pressures.OrderByDescending(e => e.Value / PetitionsGameComponent.EffectiveThreshold(e.Key)))
                {
                    rows.Add((state.pawn, entry.Key, entry.Value));
                }
            }
        }

        private static void DrawRow(Rect rect, Pawn pawn, GrievanceDef grievance, float pressure)
        {
            float threshold = PetitionsGameComponent.EffectiveThreshold(grievance);
            Widgets.Label(new Rect(rect.x, rect.y, PawnColumnWidth, rect.height), pawn.LabelShortCap);
            Rect grievanceRect = new Rect(rect.x + PawnColumnWidth, rect.y, GrievanceColumnWidth, rect.height);
            Widgets.Label(grievanceRect, grievance.LabelCap);
            TooltipHandler.TipRegion(grievanceRect, grievance.description);

            Rect barRect = new Rect(grievanceRect.xMax, rect.y + 4f, BarWidth, rect.height - 8f);
            Widgets.FillableBar(barRect, Mathf.Clamp01(pressure / threshold));
            Rect valueRect = new Rect(barRect.xMax + 8f, rect.y, rect.width - barRect.xMax - 8f, rect.height);
            string value = $"{pressure:0.0} / {threshold:0.0}";
            if (pressure >= threshold)
            {
                value += " " + "Petitions_Tab_ReadyToPetition".Translate();
            }
            Widgets.Label(valueRect, value);
        }
    }
}
