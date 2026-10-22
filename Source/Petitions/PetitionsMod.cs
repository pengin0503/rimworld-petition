using UnityEngine;
using Verse;

namespace Petitions
{
    public class PetitionsSettings : ModSettings
    {
        public const float DefaultThresholdMultiplier = 1f;
        public const float DefaultPressureDecayPerDay = 3f;

        /// <summary>Multiplies every GrievanceDef.pressureThreshold. Higher means fewer petitions.</summary>
        public float thresholdMultiplier = DefaultThresholdMultiplier;

        /// <summary>Pressure lost per day while a grievance is absent.</summary>
        public float pressureDecayPerDay = DefaultPressureDecayPerDay;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref thresholdMultiplier, "thresholdMultiplier", DefaultThresholdMultiplier);
            Scribe_Values.Look(ref pressureDecayPerDay, "pressureDecayPerDay", DefaultPressureDecayPerDay);
        }
    }

    public class PetitionsMod : Mod
    {
        public static PetitionsSettings Settings { get; private set; }

        public PetitionsMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<PetitionsSettings>();
        }

        public override string SettingsCategory()
        {
            return "Petitions_SettingsCategory".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);
            Settings.thresholdMultiplier = listing.SliderLabeled(
                "Petitions_Settings_ThresholdMultiplier".Translate(Settings.thresholdMultiplier.ToStringPercent()),
                Settings.thresholdMultiplier, 0.5f, 3f,
                tooltip: "Petitions_Settings_ThresholdMultiplier_Tip".Translate());
            Settings.pressureDecayPerDay = listing.SliderLabeled(
                "Petitions_Settings_PressureDecay".Translate(Settings.pressureDecayPerDay.ToString("0.0")),
                Settings.pressureDecayPerDay, 0f, 10f,
                tooltip: "Petitions_Settings_PressureDecay_Tip".Translate());
            listing.Gap();
            if (listing.ButtonText("Petitions_Settings_Reset".Translate()))
            {
                Settings.thresholdMultiplier = PetitionsSettings.DefaultThresholdMultiplier;
                Settings.pressureDecayPerDay = PetitionsSettings.DefaultPressureDecayPerDay;
            }
            listing.End();
        }
    }
}
