using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Petitions
{
    /// <summary>Draws the mod settings page (spec section 5).</summary>
    public static class PetitionsSettingsWindow
    {
        private static Vector2 scrollPosition;

        private static float lastHeight = 900f;

        public static void Draw(Rect inRect, PetitionsSettings settings)
        {
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, lastHeight);
            Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);

            listing.Label("Petitions_Settings_Section_Grievances".Translate());
            settings.thresholdMultiplier = listing.SliderLabeled(
                "Petitions_Settings_ThresholdMultiplier".Translate(settings.thresholdMultiplier.ToStringPercent()),
                settings.thresholdMultiplier, 0.5f, 3f,
                tooltip: "Petitions_Settings_ThresholdMultiplier_Tip".Translate());
            settings.pressureDecayPerDay = listing.SliderLabeled(
                "Petitions_Settings_PressureDecay".Translate(settings.pressureDecayPerDay.ToString("0.0")),
                settings.pressureDecayPerDay, 0f, 10f,
                tooltip: "Petitions_Settings_PressureDecay_Tip".Translate());

            listing.GapLine();
            listing.Label("Petitions_Settings_Section_Frequency".Translate());
            settings.minDaysBetweenPetitions = RoundHalf(listing.SliderLabeled(
                "Petitions_Settings_MinDaysBetween".Translate(settings.minDaysBetweenPetitions.ToString("0.0")),
                settings.minDaysBetweenPetitions, 0f, 10f));
            settings.maxOpenPetitions = Mathf.RoundToInt(listing.SliderLabeled(
                "Petitions_Settings_MaxOpen".Translate(settings.maxOpenPetitions),
                settings.maxOpenPetitions, 1f, 5f));
            settings.pawnCooldownDays = RoundHalf(listing.SliderLabeled(
                "Petitions_Settings_PawnCooldown".Translate(settings.pawnCooldownDays.ToString("0.0")),
                settings.pawnCooldownDays, 0f, 20f));
            settings.grievanceCooldownDays = RoundHalf(listing.SliderLabeled(
                "Petitions_Settings_GrievanceCooldown".Translate(settings.grievanceCooldownDays.ToString("0.0")),
                settings.grievanceCooldownDays, 0f, 30f));
            settings.petitionExpiryDays = RoundHalf(listing.SliderLabeled(
                "Petitions_Settings_Expiry".Translate(settings.petitionExpiryDays.ToString("0.0")),
                settings.petitionExpiryDays, 0.5f, 10f));

            listing.GapLine();
            listing.Label("Petitions_Settings_Section_Escalation".Translate());
            listing.CheckboxLabeled("Petitions_Settings_EnableDisgruntled".Translate(), ref settings.enableDisgruntled,
                "Petitions_Settings_EnableDisgruntled_Tip".Translate());
            listing.CheckboxLabeled("Petitions_Settings_EnableUltimatum".Translate(), ref settings.enableUltimatum,
                "Petitions_Settings_EnableUltimatum_Tip".Translate());

            listing.GapLine();
            listing.Label("Petitions_Settings_Section_AutoResponse".Translate());
            List<GrievanceDef> grievances = DefDatabase<GrievanceDef>.AllDefsListForReading;
            for (int i = 0; i < grievances.Count; i++)
            {
                GrievanceDef grievance = grievances[i];
                AutoResponse current = settings.AutoResponseFor(grievance);
                if (listing.ButtonTextLabeledPct(grievance.LabelCap, AutoResponseLabel(current), 0.6f))
                {
                    settings.SetAutoResponse(grievance, Next(current));
                }
            }

            listing.GapLine();
            if (listing.ButtonText("Petitions_Settings_Reset".Translate()))
            {
                settings.ResetToDefaults();
            }

            lastHeight = listing.CurHeight + 12f;
            listing.End();
            Widgets.EndScrollView();
        }

        public static string AutoResponseLabel(AutoResponse response)
        {
            switch (response)
            {
                case AutoResponse.AlwaysAccept: return "Petitions_AutoResponse_AlwaysAccept".Translate();
                case AutoResponse.AlwaysReject: return "Petitions_AutoResponse_AlwaysReject".Translate();
                default: return "Petitions_AutoResponse_Ask".Translate();
            }
        }

        private static AutoResponse Next(AutoResponse response)
        {
            switch (response)
            {
                case AutoResponse.Ask: return AutoResponse.AlwaysAccept;
                case AutoResponse.AlwaysAccept: return AutoResponse.AlwaysReject;
                default: return AutoResponse.Ask;
            }
        }

        private static float RoundHalf(float value)
        {
            return Mathf.Round(value * 2f) / 2f;
        }
    }
}
