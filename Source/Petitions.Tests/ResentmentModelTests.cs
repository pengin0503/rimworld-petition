using Petitions.Core;
using Xunit;

namespace Petitions.Tests;

public class ResentmentModelTests
{
    [Theory]
    [InlineData(0f, ResentmentStage.Calm)]
    [InlineData(29.9f, ResentmentStage.Calm)]
    [InlineData(30f, ResentmentStage.Grumbling)]
    [InlineData(55f, ResentmentStage.Disgruntled)]
    [InlineData(80f, ResentmentStage.Ultimatum)]
    [InlineData(100f, ResentmentStage.Ultimatum)]
    public void NextStage_FromCalm_EntersAtEnterThresholds(float resentment, ResentmentStage expected)
    {
        Assert.Equal(expected, ResentmentModel.NextStage(ResentmentStage.Calm, resentment));
    }

    [Theory]
    [InlineData(ResentmentStage.Grumbling, 20f, ResentmentStage.Grumbling)]
    [InlineData(ResentmentStage.Grumbling, 19.9f, ResentmentStage.Calm)]
    [InlineData(ResentmentStage.Disgruntled, 45f, ResentmentStage.Disgruntled)]
    [InlineData(ResentmentStage.Disgruntled, 44.9f, ResentmentStage.Grumbling)]
    [InlineData(ResentmentStage.Ultimatum, 70f, ResentmentStage.Ultimatum)]
    [InlineData(ResentmentStage.Ultimatum, 69.9f, ResentmentStage.Disgruntled)]
    public void NextStage_StaysUntilExitThreshold(ResentmentStage current, float resentment, ResentmentStage expected)
    {
        Assert.Equal(expected, ResentmentModel.NextStage(current, resentment));
    }

    [Fact]
    public void NextStage_CanDropSeveralStagesAtOnce()
    {
        Assert.Equal(ResentmentStage.Calm, ResentmentModel.NextStage(ResentmentStage.Ultimatum, 5f));
    }

    [Fact]
    public void NextStage_CanRiseSeveralStagesAtOnce()
    {
        Assert.Equal(ResentmentStage.Ultimatum, ResentmentModel.NextStage(ResentmentStage.Calm, 90f));
    }

    [Theory]
    [InlineData(50f, 1f, 49f)]
    [InlineData(0.5f, 1f, 0f)]
    [InlineData(10f, 3f, 7f)]
    public void Decay_RemovesOnePointPerDayAndClampsAtZero(float resentment, float days, float expected)
    {
        Assert.Equal(expected, ResentmentModel.Decay(resentment, days), 4);
    }

    [Theory]
    [InlineData(-5f, 0f)]
    [InlineData(50f, 50f)]
    [InlineData(150f, 100f)]
    public void Clamp_KeepsValueInRange(float value, float expected)
    {
        Assert.Equal(expected, ResentmentModel.Clamp(value));
    }

    [Theory]
    [InlineData(0, 20f)]
    [InlineData(1, 22f)]
    [InlineData(3, 26f)]
    [InlineData(5, 30f)]
    [InlineData(12, 30f)]
    public void ScaleIncrease_GrowsWithCosignersUpToOnePointFive(int cosigners, float expected)
    {
        Assert.Equal(expected, ResentmentModel.ScaleIncrease(20f, cosigners), 4);
    }
}
