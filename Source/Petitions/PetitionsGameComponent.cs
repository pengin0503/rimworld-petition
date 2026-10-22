using System.Collections.Generic;
using Petitions.Core;
using RimWorld;
using Verse;

namespace Petitions
{
    /// <summary>
    /// Entry point of the hourly update and owner of all saved petition data (spec section 3.2).
    /// The skeleton covers steps 1-2 (scan thoughts, update pressure); petitions, promises and
    /// escalation are added in the next phase.
    /// </summary>
    public class PetitionsGameComponent : GameComponent
    {
        public const int UpdateIntervalTicks = 2500;

        private const float HoursPerUpdate = (float)UpdateIntervalTicks / GenDate.TicksPerHour;

        private List<PawnGrievanceState> states = new List<PawnGrievanceState>();

        private readonly Dictionary<GrievanceDef, float> tmpSeverities = new Dictionary<GrievanceDef, float>();

        public PetitionsGameComponent(Game game)
        {
        }

        public static PetitionsGameComponent Instance => Current.Game?.GetComponent<PetitionsGameComponent>();

        public IReadOnlyList<PawnGrievanceState> States => states;

        public static float EffectiveThreshold(GrievanceDef grievance)
        {
            return grievance.pressureThreshold * PetitionsMod.Settings.thresholdMultiplier;
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % UpdateIntervalTicks != 0)
            {
                return;
            }
            Update(HoursPerUpdate);
        }

        /// <summary>Runs one hourly update immediately. Used by debug actions.</summary>
        public void UpdateNow()
        {
            Update(HoursPerUpdate);
        }

        public PawnGrievanceState GetOrCreateState(Pawn pawn)
        {
            for (int i = 0; i < states.Count; i++)
            {
                if (states[i].pawn == pawn)
                {
                    return states[i];
                }
            }
            PawnGrievanceState state = new PawnGrievanceState(pawn);
            states.Add(state);
            return state;
        }

        private void Update(float hours)
        {
            RemoveStaleStates();
            float decayPerDay = PetitionsMod.Settings.pressureDecayPerDay;
            List<GrievanceDef> grievances = DefDatabase<GrievanceDef>.AllDefsListForReading;
            foreach (Pawn pawn in PawnsFinder.AllMaps_FreeColonistsSpawned)
            {
                if (!PetitionEligibility.CanHaveGrievances(pawn))
                {
                    continue;
                }
                PawnGrievanceState state = GetOrCreateState(pawn);
                GrievanceScanner.Scan(pawn, tmpSeverities);
                float mood = pawn.needs.mood.CurLevel;
                for (int i = 0; i < grievances.Count; i++)
                {
                    GrievanceDef grievance = grievances[i];
                    tmpSeverities.TryGetValue(grievance, out float severity);
                    state.SetPressure(grievance, PressureModel.Step(state.GetPressure(grievance), severity, mood, decayPerDay, hours));
                }
            }
            tmpSeverities.Clear();
        }

        /// <summary>
        /// Drops data of pawns who died or left the colony. Pawns who are only away
        /// (caravans) keep their data; it just isn't updated until they return.
        /// </summary>
        private void RemoveStaleStates()
        {
            states.RemoveAll(state => state.pawn == null || state.pawn.Dead || state.pawn.Faction != Faction.OfPlayer);
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref states, "states", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                states ??= new List<PawnGrievanceState>();
                RemoveStaleStates();
            }
        }
    }
}
