using System.Collections.Generic;
using Verse;

namespace Petitions
{
    /// <summary>Saved per-pawn grievance data.</summary>
    public class PawnGrievanceState : IExposable
    {
        public Pawn pawn;

        private Dictionary<GrievanceDef, float> pressure = new Dictionary<GrievanceDef, float>();

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

        public void ExposeData()
        {
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Collections.Look(ref pressure, "pressure", LookMode.Def, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                pressure ??= new Dictionary<GrievanceDef, float>();
                // Grievances removed by a mod update load as null keys.
                pressure.RemoveAll(entry => entry.Key == null);
            }
        }
    }
}
