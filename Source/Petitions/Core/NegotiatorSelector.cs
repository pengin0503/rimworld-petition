using System;
using System.Collections.Generic;

namespace Petitions.Core
{
    /// <summary>Who negotiates: the assigned pawn, else the ideoligion leader, else the best negotiator (spec sections 2.5, 10.3).</summary>
    public static class NegotiatorSelector
    {
        public static T Select<T>(T assigned, T leader, IEnumerable<(T pawn, float ability)> candidates, Func<T, bool> canNegotiate)
            where T : class
        {
            if (assigned != null && canNegotiate(assigned))
            {
                return assigned;
            }
            if (leader != null && canNegotiate(leader))
            {
                return leader;
            }
            T best = null;
            float bestAbility = float.MinValue;
            foreach ((T pawn, float ability) in candidates)
            {
                if (pawn != null && ability > bestAbility && canNegotiate(pawn))
                {
                    best = pawn;
                    bestAbility = ability;
                }
            }
            return best;
        }
    }
}
