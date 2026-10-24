using System.Collections.Generic;
using Petitions.Core;
using Verse;

namespace Petitions
{
    public enum AutoResponse
    {
        Ask,
        AlwaysAccept,
        AlwaysReject,
    }

    /// <summary>Mod settings (spec sections 2.6 and 5).</summary>
    public class PetitionsSettings : ModSettings
    {
        public const float DefaultThresholdMultiplier = 1f;
        public const float DefaultPressureDecayPerDay = 3f;
        public const float DefaultMinDaysBetweenPetitions = 2f;
        public const int DefaultMaxOpenPetitions = 2;
        public const float DefaultPawnCooldownDays = 5f;
        public const float DefaultGrievanceCooldownDays = 10f;
        public const float DefaultPetitionExpiryDays = 2f;

        /// <summary>Multiplies every GrievanceDef.pressureThreshold. Higher means fewer petitions.</summary>
        public float thresholdMultiplier = DefaultThresholdMultiplier;

        /// <summary>Pressure lost per day while a grievance is absent.</summary>
        public float pressureDecayPerDay = DefaultPressureDecayPerDay;

        public float minDaysBetweenPetitions = DefaultMinDaysBetweenPetitions;
        public int maxOpenPetitions = DefaultMaxOpenPetitions;
        public float pawnCooldownDays = DefaultPawnCooldownDays;
        public float grievanceCooldownDays = DefaultGrievanceCooldownDays;
        public float petitionExpiryDays = DefaultPetitionExpiryDays;

        /// <summary>Resentment stage 2 slows work.</summary>
        public bool enableDisgruntled = true;

        /// <summary>Resentment stage 3 issues an ultimatum and the pawn may leave. On by default (spec section 9).</summary>
        public bool enableUltimatum = true;

        /// <summary>Keyed by GrievanceDef.defName; missing means Ask.</summary>
        public Dictionary<string, AutoResponse> autoResponses = new Dictionary<string, AutoResponse>();

        public RateLimits ToRateLimits()
        {
            return new RateLimits(minDaysBetweenPetitions, maxOpenPetitions, pawnCooldownDays, grievanceCooldownDays);
        }

        public AutoResponse AutoResponseFor(GrievanceDef grievance)
        {
            return autoResponses.TryGetValue(grievance.defName, out AutoResponse response) ? response : AutoResponse.Ask;
        }

        public void SetAutoResponse(GrievanceDef grievance, AutoResponse response)
        {
            if (response == AutoResponse.Ask)
            {
                autoResponses.Remove(grievance.defName);
            }
            else
            {
                autoResponses[grievance.defName] = response;
            }
        }

        public void ResetToDefaults()
        {
            thresholdMultiplier = DefaultThresholdMultiplier;
            pressureDecayPerDay = DefaultPressureDecayPerDay;
            minDaysBetweenPetitions = DefaultMinDaysBetweenPetitions;
            maxOpenPetitions = DefaultMaxOpenPetitions;
            pawnCooldownDays = DefaultPawnCooldownDays;
            grievanceCooldownDays = DefaultGrievanceCooldownDays;
            petitionExpiryDays = DefaultPetitionExpiryDays;
            enableDisgruntled = true;
            enableUltimatum = true;
            autoResponses.Clear();
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref thresholdMultiplier, "thresholdMultiplier", DefaultThresholdMultiplier);
            Scribe_Values.Look(ref pressureDecayPerDay, "pressureDecayPerDay", DefaultPressureDecayPerDay);
            Scribe_Values.Look(ref minDaysBetweenPetitions, "minDaysBetweenPetitions", DefaultMinDaysBetweenPetitions);
            Scribe_Values.Look(ref maxOpenPetitions, "maxOpenPetitions", DefaultMaxOpenPetitions);
            Scribe_Values.Look(ref pawnCooldownDays, "pawnCooldownDays", DefaultPawnCooldownDays);
            Scribe_Values.Look(ref grievanceCooldownDays, "grievanceCooldownDays", DefaultGrievanceCooldownDays);
            Scribe_Values.Look(ref petitionExpiryDays, "petitionExpiryDays", DefaultPetitionExpiryDays);
            Scribe_Values.Look(ref enableDisgruntled, "enableDisgruntled", true);
            Scribe_Values.Look(ref enableUltimatum, "enableUltimatum", true);
            Scribe_Collections.Look(ref autoResponses, "autoResponses", LookMode.Value, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit || autoResponses == null)
            {
                autoResponses ??= new Dictionary<string, AutoResponse>();
            }
        }
    }
}
