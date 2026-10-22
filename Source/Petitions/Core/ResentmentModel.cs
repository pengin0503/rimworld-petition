using System;

namespace Petitions.Core
{
    public enum ResentmentStage
    {
        Calm = 0,
        Grumbling = 1,
        Disgruntled = 2,
        Ultimatum = 3,
    }

    /// <summary>
    /// Per-pawn resentment (0..100) and its escalation stages with hysteresis (spec section 2.7).
    /// </summary>
    public static class ResentmentModel
    {
        public const float Min = 0f;
        public const float Max = 100f;
        public const float DecayPerDay = 1f;
        public const float MaxCosignerScale = 1.5f;
        public const float CosignerScaleStep = 0.1f;

        // Indexed by (int)ResentmentStage. Calm has no thresholds.
        private static readonly float[] EnterThresholds = { 0f, 30f, 55f, 80f };
        private static readonly float[] ExitThresholds = { 0f, 20f, 45f, 70f };

        public static float EnterThreshold(ResentmentStage stage) => EnterThresholds[(int)stage];

        public static float ExitThreshold(ResentmentStage stage) => ExitThresholds[(int)stage];

        /// <summary>
        /// Moves up while the value reaches the next stage's enter threshold,
        /// and down while it is below the current stage's exit threshold.
        /// </summary>
        public static ResentmentStage NextStage(ResentmentStage current, float resentment)
        {
            ResentmentStage stage = current;
            while (stage < ResentmentStage.Ultimatum && resentment >= EnterThreshold(stage + 1))
            {
                stage++;
            }
            while (stage > ResentmentStage.Calm && resentment < ExitThreshold(stage))
            {
                stage--;
            }
            return stage;
        }

        public static float Decay(float resentment, float days)
        {
            return Clamp(resentment - DecayPerDay * days);
        }

        public static float Clamp(float resentment)
        {
            return Math.Min(Max, Math.Max(Min, resentment));
        }

        /// <summary>Group grievances weigh more: x(1 + 0.1 per cosigner), capped at x1.5.</summary>
        public static float ScaleIncrease(float baseAmount, int cosigners)
        {
            float scale = Math.Min(MaxCosignerScale, 1f + CosignerScaleStep * Math.Max(0, cosigners));
            return baseAmount * scale;
        }
    }
}
