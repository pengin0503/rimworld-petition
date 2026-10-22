using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Petitions
{
    /// <summary>
    /// Reads a pawn's current mood thoughts and sums, per grievance, how much negative mood
    /// the matching thoughts cause (spec section 2.2, "severity").
    /// </summary>
    public static class GrievanceScanner
    {
        private static Dictionary<ThoughtDef, List<GrievanceDef>> grievancesByThought;

        private static readonly List<Thought> tmpGroups = new List<Thought>();

        private static Dictionary<ThoughtDef, List<GrievanceDef>> GrievancesByThought
        {
            get
            {
                if (grievancesByThought == null)
                {
                    grievancesByThought = new Dictionary<ThoughtDef, List<GrievanceDef>>();
                    foreach (GrievanceDef grievance in DefDatabase<GrievanceDef>.AllDefsListForReading)
                    {
                        foreach (ThoughtDef thought in grievance.thoughts)
                        {
                            if (thought == null)
                            {
                                continue;
                            }
                            if (!grievancesByThought.TryGetValue(thought, out List<GrievanceDef> list))
                            {
                                list = new List<GrievanceDef>();
                                grievancesByThought.Add(thought, list);
                            }
                            list.Add(grievance);
                        }
                    }
                }
                return grievancesByThought;
            }
        }

        /// <summary>The pawn's current negative thoughts behind a grievance, worst first.</summary>
        public static List<(string label, float moodOffset)> Causes(Pawn pawn, GrievanceDef grievance, int max)
        {
            List<(string label, float moodOffset)> causes = new List<(string, float)>();
            ThoughtHandler thoughts = pawn.needs?.mood?.thoughts;
            if (thoughts == null)
            {
                return causes;
            }
            thoughts.GetDistinctMoodThoughtGroups(tmpGroups);
            for (int i = 0; i < tmpGroups.Count; i++)
            {
                Thought group = tmpGroups[i];
                if (!grievance.thoughts.Contains(group.def))
                {
                    continue;
                }
                float offset = thoughts.MoodOffsetOfGroup(group);
                if (offset < 0f)
                {
                    causes.Add((group.LabelCap, offset));
                }
            }
            tmpGroups.Clear();
            causes.Sort((a, b) => a.moodOffset.CompareTo(b.moodOffset));
            if (causes.Count > max)
            {
                causes.RemoveRange(max, causes.Count - max);
            }
            return causes;
        }

        /// <summary>
        /// Fills <paramref name="severities"/> with grievance -> positive severity.
        /// Grievances without negative matching thoughts are left out.
        /// </summary>
        public static void Scan(Pawn pawn, Dictionary<GrievanceDef, float> severities)
        {
            severities.Clear();
            ThoughtHandler thoughts = pawn.needs?.mood?.thoughts;
            if (thoughts == null)
            {
                return;
            }
            // Groups and their stacked offsets match what the Needs tab shows.
            thoughts.GetDistinctMoodThoughtGroups(tmpGroups);
            for (int i = 0; i < tmpGroups.Count; i++)
            {
                Thought group = tmpGroups[i];
                if (!GrievancesByThought.TryGetValue(group.def, out List<GrievanceDef> grievances))
                {
                    continue;
                }
                float offset = thoughts.MoodOffsetOfGroup(group);
                if (offset >= 0f)
                {
                    continue;
                }
                for (int j = 0; j < grievances.Count; j++)
                {
                    severities.TryGetValue(grievances[j], out float current);
                    severities[grievances[j]] = current - offset;
                }
            }
            tmpGroups.Clear();
        }
    }
}
