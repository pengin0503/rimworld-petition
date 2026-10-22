using System;
using System.Collections.Generic;

namespace Petitions.Core
{
    /// <summary>Drops signers who died or left, keeping the per-signer clear-since list aligned.</summary>
    public static class SignerPruner
    {
        /// <returns>True when no signers remain.</returns>
        public static bool Prune<T>(List<T> signers, List<int> clearSinceTicks, Func<T, bool> isGone)
        {
            while (clearSinceTicks.Count < signers.Count)
            {
                clearSinceTicks.Add(-1);
            }
            for (int i = signers.Count - 1; i >= 0; i--)
            {
                if (isGone(signers[i]))
                {
                    signers.RemoveAt(i);
                    clearSinceTicks.RemoveAt(i);
                }
            }
            if (clearSinceTicks.Count > signers.Count)
            {
                clearSinceTicks.RemoveRange(signers.Count, clearSinceTicks.Count - signers.Count);
            }
            return signers.Count == 0;
        }
    }
}
