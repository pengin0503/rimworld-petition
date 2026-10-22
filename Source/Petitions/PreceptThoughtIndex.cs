using System.Collections.Generic;
using Petitions.Core;
using RimWorld;
using Verse;

namespace Petitions
{
    /// <summary>
    /// Thought -> history event pairs taken from every precept's memory-thought comps, so that
    /// grievances defined by events also catch ideoligion and modded precept thoughts (spec section 10.1).
    /// </summary>
    public static class PreceptThoughtIndex
    {
        private static ThoughtEventIndex<ThoughtDef, HistoryEventDef> index;

        public static ThoughtEventIndex<ThoughtDef, HistoryEventDef> Index => index ??= Build();

        private static ThoughtEventIndex<ThoughtDef, HistoryEventDef> Build()
        {
            List<(ThoughtDef, HistoryEventDef)> pairs = new List<(ThoughtDef, HistoryEventDef)>();
            foreach (PreceptDef precept in DefDatabase<PreceptDef>.AllDefsListForReading)
            {
                if (precept.comps == null)
                {
                    continue;
                }
                foreach (PreceptComp comp in precept.comps)
                {
                    if (comp is PreceptComp_KnowsMemoryThought knows && knows.thought != null && knows.eventDef != null)
                    {
                        pairs.Add((knows.thought, knows.eventDef));
                    }
                    else if (comp is PreceptComp_SelfTookMemoryThought selfTook && selfTook.thought != null && selfTook.eventDef != null)
                    {
                        pairs.Add((selfTook.thought, selfTook.eventDef));
                    }
                }
            }
            return new ThoughtEventIndex<ThoughtDef, HistoryEventDef>(pairs);
        }
    }
}
