using System.Collections.Generic;

namespace Petitions.Core
{
    /// <summary>
    /// Which history events each thought is given for, so a grievance can be defined by events
    /// instead of thought names. Ideoligion precepts give their own thoughts for the same events
    /// (spec section 10.1).
    /// </summary>
    public sealed class ThoughtEventIndex<TThought, TEvent>
    {
        private readonly Dictionary<TThought, List<TEvent>> eventsByThought = new Dictionary<TThought, List<TEvent>>();

        public ThoughtEventIndex(IEnumerable<(TThought thought, TEvent ev)> pairs)
        {
            foreach ((TThought thought, TEvent ev) in pairs)
            {
                if (!eventsByThought.TryGetValue(thought, out List<TEvent> events))
                {
                    events = new List<TEvent>();
                    eventsByThought.Add(thought, events);
                }
                if (!events.Contains(ev))
                {
                    events.Add(ev);
                }
            }
        }

        public bool Matches(TThought thought, ICollection<TThought> directThoughts, ICollection<TEvent> events)
        {
            if (directThoughts.Contains(thought))
            {
                return true;
            }
            if (events.Count == 0 || !eventsByThought.TryGetValue(thought, out List<TEvent> thoughtEvents))
            {
                return false;
            }
            for (int i = 0; i < thoughtEvents.Count; i++)
            {
                if (events.Contains(thoughtEvents[i]))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
