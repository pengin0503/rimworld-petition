using System.Collections.Generic;
using Petitions.Core;
using Verse;

namespace Petitions
{
    /// <summary>Saved per-pawn grievance data.</summary>
    public class PawnGrievanceState : IExposable
    {
        public Pawn pawn;

        private Dictionary<GrievanceDef, float> pressure = new Dictionary<GrievanceDef, float>();

        /// <summary>0..100 (spec section 2.7).</summary>
        public float resentment;

        public ResentmentStage stage = ResentmentStage.Calm;

        /// <summary>When the ultimatum runs out, or -1 when there is none.</summary>
        public int ultimatumDeadlineTick = -1;

        public int lastPetitionTick = -1;

        private Dictionary<GrievanceDef, int> lastPetitionTickByGrievance = new Dictionary<GrievanceDef, int>();

        /// <summary>The grievance of the last petition this pawn signed; used in leaving messages.</summary>
        public GrievanceDef lastGrievance;

        public PawnGrievanceState()
        {
        }

        public PawnGrievanceState(Pawn pawn)
        {
            this.pawn = pawn;
        }

        public IReadOnlyDictionary<GrievanceDef, float> Pressures => pressure;

        public float GetPressure(GrievanceDef grievance)
        {
            return pressure.TryGetValue(grievance, out float value) ? value : 0f;
        }

        public void SetPressure(GrievanceDef grievance, float value)
        {
            if (value <= 0f)
            {
                pressure.Remove(grievance);
            }
            else
            {
                pressure[grievance] = value;
            }
        }

        public int LastPetitionTick(GrievanceDef grievance)
        {
            return lastPetitionTickByGrievance.TryGetValue(grievance, out int tick) ? tick : -1;
        }

        public void NotePetition(GrievanceDef grievance, int tick)
        {
            lastPetitionTick = tick;
            lastPetitionTickByGrievance[grievance] = tick;
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Collections.Look(ref pressure, "pressure", LookMode.Def, LookMode.Value);
            Scribe_Values.Look(ref resentment, "resentment");
            Scribe_Values.Look(ref stage, "stage", ResentmentStage.Calm);
            Scribe_Values.Look(ref ultimatumDeadlineTick, "ultimatumDeadlineTick", -1);
            Scribe_Values.Look(ref lastPetitionTick, "lastPetitionTick", -1);
            Scribe_Collections.Look(ref lastPetitionTickByGrievance, "lastPetitionTickByGrievance", LookMode.Def, LookMode.Value);
            Scribe_Defs.Look(ref lastGrievance, "lastGrievance");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                // Saves from older versions lack some of these; grievances removed by an update load as null keys.
                pressure ??= new Dictionary<GrievanceDef, float>();
                pressure.RemoveAll(entry => entry.Key == null);
                lastPetitionTickByGrievance ??= new Dictionary<GrievanceDef, int>();
                lastPetitionTickByGrievance.RemoveAll(entry => entry.Key == null);
            }
        }
    }
}
