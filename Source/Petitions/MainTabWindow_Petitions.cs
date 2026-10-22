using System.Collections.Generic;
using System.Linq;
using Petitions.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace Petitions
{
    /// <summary>
    /// The Petitions tab (spec section 6): (a) open petitions with answer buttons,
    /// (b) promises in progress, (c) each colonist's resentment and strongest grievances.
    /// </summary>
    public class MainTabWindow_Petitions : MainTabWindow
    {
        private const float RowHeight = 28f;
        private const float NameWidth = 150f;
        private const float GrievanceWidth = 170f;
        private const float ButtonWidth = 110f;
        private const float BarWidth = 140f;
        private const int TopGrievancesShown = 3;

        private Vector2 scrollPosition;

        private float lastHeight = 400f;

        public override Vector2 RequestedTabSize => new Vector2(900f, 640f);

        public override void DoWindowContents(Rect inRect)
        {
            PetitionsGameComponent comp = PetitionsGameComponent.Instance;
            if (comp == null)
            {
                return;
            }
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, lastHeight);
            Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);
            float curY = 0f;
            DrawOpenPetitions(comp, ref curY, viewRect.width);
            DrawPromises(comp, ref curY, viewRect.width);
            DrawColonists(comp, ref curY, viewRect.width);
            lastHeight = curY + 12f;
            Widgets.EndScrollView();
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }

        private static void DrawOpenPetitions(PetitionsGameComponent comp, ref float curY, float width)
        {
            Widgets.ListSeparator(ref curY, width, "Petitions_Tab_OpenPetitions".Translate());
            List<Petition> open = comp.Petitions.Where(p => p.state == PetitionState.Open).ToList();
            if (open.Count == 0)
            {
                DrawNote(ref curY, width, "Petitions_Tab_None".Translate());
                return;
            }
            int expiryTicks = (int)(PetitionsMod.Settings.petitionExpiryDays * GenDate.TicksPerDay);
            int now = Find.TickManager.TicksGame;
            foreach (Petition petition in open)
            {
                Rect row = new Rect(0f, curY, width, RowHeight);
                Widgets.DrawHighlightIfMouseover(row);
                float x = DrawSigners(row, petition);
                DrawCell(row, ref x, GrievanceWidth, petition.grievance.LabelCap, petition.grievance.description);
                int left = Mathf.Max(0, petition.createdTick + expiryTicks - now);
                DrawCell(row, ref x, 110f, "Petitions_Tab_TimeLeft".Translate(left.ToStringTicksToPeriod()));

                if (Widgets.ButtonText(new Rect(x, row.y + 2f, ButtonWidth, RowHeight - 4f), "Petitions_Tab_Accept".Translate()))
                {
                    PetitionResponder.Accept(petition);
                }
                x += ButtonWidth + 4f;
                Pawn negotiator = Negotiator.Find(petition.signers);
                Rect persuadeRect = new Rect(x, row.y + 2f, ButtonWidth + 30f, RowHeight - 4f);
                if (negotiator == null)
                {
                    Widgets.ButtonText(persuadeRect, "Petitions_Tab_Persuade".Translate("-"), active: false);
                    TooltipHandler.TipRegion(persuadeRect, "Petitions_NoNegotiator".Translate());
                }
                else
                {
                    string chance = Negotiator.PersuasionChance(negotiator, petition).ToStringPercent();
                    if (Widgets.ButtonText(persuadeRect, "Petitions_Tab_Persuade".Translate(chance)))
                    {
                        PetitionResponder.Persuade(petition);
                    }
                    TooltipHandler.TipRegion(persuadeRect, "Petitions_Tab_PersuadeTip".Translate(negotiator.Named("NEGOTIATOR")));
                }
                x += persuadeRect.width + 4f;
                if (Widgets.ButtonText(new Rect(x, row.y + 2f, ButtonWidth, RowHeight - 4f), "Petitions_Tab_Reject".Translate()))
                {
                    PetitionResponder.Reject(petition);
                }
                curY += RowHeight;
            }
        }

        private static void DrawPromises(PetitionsGameComponent comp, ref float curY, float width)
        {
            curY += 8f;
            Widgets.ListSeparator(ref curY, width, "Petitions_Tab_Promises".Translate());
            List<Petition> promised = comp.Petitions.Where(p => p.state == PetitionState.Promised).ToList();
            if (promised.Count == 0)
            {
                DrawNote(ref curY, width, "Petitions_Tab_None".Translate());
                return;
            }
            int now = Find.TickManager.TicksGame;
            foreach (Petition petition in promised)
            {
                Rect row = new Rect(0f, curY, width, RowHeight);
                Widgets.DrawHighlightIfMouseover(row);
                float x = DrawSigners(row, petition);
                DrawCell(row, ref x, GrievanceWidth, petition.grievance.LabelCap, petition.grievance.description);
                int left = Mathf.Max(0, petition.promiseDeadlineTick - now);
                DrawCell(row, ref x, 150f, "Petitions_Tab_DaysLeft".Translate(left.ToStringTicksToPeriod()));
                string status;
                if (petition.grievance.kind == GrievanceKind.Remedy)
                {
                    int holdTicks = (int)(petition.grievance.remedyHoldDays * GenDate.TicksPerDay);
                    int held = petition.clearSinceTicks.Count(since => since >= 0 && now - since >= holdTicks);
                    status = "Petitions_Tab_RemedyStatus".Translate(held, petition.signers.Count);
                }
                else
                {
                    status = "Petitions_Tab_CessationStatus".Translate();
                }
                DrawCell(row, ref x, width - x, status);
                curY += RowHeight;
            }
        }

        private static void DrawColonists(PetitionsGameComponent comp, ref float curY, float width)
        {
            curY += 8f;
            Widgets.ListSeparator(ref curY, width, "Petitions_Tab_Colonists".Translate());
            List<PawnGrievanceState> states = comp.States
                .Where(s => s.resentment > 0.05f || s.Pressures.Count > 0)
                .OrderByDescending(s => s.resentment)
                .ToList();
            if (states.Count == 0)
            {
                DrawNote(ref curY, width, "Petitions_Tab_NoGrievances".Translate());
                return;
            }
            foreach (PawnGrievanceState state in states)
            {
                Rect row = new Rect(0f, curY, width, RowHeight);
                Widgets.DrawHighlightIfMouseover(row);
                float x = 0f;
                DrawCell(row, ref x, NameWidth, state.pawn.LabelShortCap);

                Rect barRect = new Rect(x, row.y + 6f, BarWidth, RowHeight - 12f);
                Widgets.FillableBar(barRect, state.resentment / ResentmentModel.Max);
                TooltipHandler.TipRegion(barRect, "Petitions_Tab_ResentmentTip".Translate());
                x += BarWidth + 6f;
                string stage = StageLabel(state.stage);
                if (state.ultimatumDeadlineTick >= 0)
                {
                    stage += " " + "Petitions_Tab_UltimatumLeft".Translate(
                        Mathf.Max(0, state.ultimatumDeadlineTick - Find.TickManager.TicksGame).ToStringTicksToPeriod());
                }
                DrawCell(row, ref x, 210f, state.resentment.ToString("0") + "  " + stage);

                string grievances = string.Join(", ", state.Pressures
                    .OrderByDescending(e => e.Value / PetitionsGameComponent.EffectiveThreshold(e.Key))
                    .Take(TopGrievancesShown)
                    .Select(e => $"{e.Key.label} {e.Value:0}/{PetitionsGameComponent.EffectiveThreshold(e.Key):0}"));
                DrawCell(row, ref x, width - x, grievances, "Petitions_Tab_PressureTip".Translate());
                curY += RowHeight;
            }
        }

        private static float DrawSigners(Rect row, Petition petition)
        {
            float x = 0f;
            string label = petition.Petitioner?.LabelShortCap ?? "?";
            if (petition.CosignerCount > 0)
            {
                label += " +" + petition.CosignerCount;
            }
            string tip = string.Join("\n", petition.signers.Where(p => p != null).Select(p => p.LabelShortCap));
            DrawCell(row, ref x, NameWidth, label, tip);
            return x;
        }

        private static void DrawCell(Rect row, ref float x, float width, string text, string tooltip = null)
        {
            Rect rect = new Rect(x, row.y, width, row.height);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(rect, text.Truncate(width));
            Text.Anchor = TextAnchor.UpperLeft;
            if (!tooltip.NullOrEmpty())
            {
                TooltipHandler.TipRegion(rect, tooltip);
            }
            x += width + 6f;
        }

        private static void DrawNote(ref float curY, float width, string text)
        {
            GUI.color = Color.gray;
            Widgets.Label(new Rect(6f, curY, width - 6f, RowHeight), text);
            GUI.color = Color.white;
            curY += RowHeight;
        }

        public static string StageLabel(ResentmentStage stage)
        {
            switch (stage)
            {
                case ResentmentStage.Grumbling: return "Petitions_StageLabel_Grumbling".Translate();
                case ResentmentStage.Disgruntled: return "Petitions_StageLabel_Disgruntled".Translate();
                case ResentmentStage.Ultimatum: return "Petitions_StageLabel_Ultimatum".Translate();
                default: return "Petitions_StageLabel_Calm".Translate();
            }
        }
    }
}
