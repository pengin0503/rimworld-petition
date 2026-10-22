using System.Collections.Generic;

namespace Petitions.Core
{
    /// <summary>Picks the pawns who join a petition as cosigners (spec section 2.3).</summary>
    public static class CosignerSelector
    {
        public const int MaxCosigners = 5;

        /// <returns>Indices into <paramref name="pressures"/>, highest pressure first.</returns>
        public static List<int> Select(IReadOnlyList<float> pressures, int petitionerIndex, float threshold, float fraction, int max)
        {
            float cutoff = threshold * fraction;
            List<int> result = new List<int>();
            for (int i = 0; i < pressures.Count; i++)
            {
                if (i != petitionerIndex && pressures[i] >= cutoff)
                {
                    result.Add(i);
                }
            }
            // Stable order: highest pressure first, ties by index.
            result.Sort((a, b) =>
            {
                int byPressure = pressures[b].CompareTo(pressures[a]);
                return byPressure != 0 ? byPressure : a.CompareTo(b);
            });
            if (result.Count > max)
            {
                result.RemoveRange(max, result.Count - max);
            }
            return result;
        }
    }
}
