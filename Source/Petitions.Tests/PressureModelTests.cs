using Petitions.Core;
using Xunit;

namespace Petitions.Tests;

public class PressureModelTests
{
    [Theory]
    [InlineData(0.5f, 1.0f)]
    [InlineData(0.8f, 1.0f)]
    [InlineData(0.2f, 1.6f)]
    [InlineData(0.0f, 2.0f)]
    public void MoodFactor_RisesLinearlyBelowHalfMood(float mood, float expected)
    {
        Assert.Equal(expected, PressureModel.MoodFactor(mood), 4);
    }

    [Fact]
    public void Step_WithSeverity_AddsSeverityPerHourScaledByMood()
    {
        // -4 mood for one hour at neutral mood: 4 / 24 mood-days.
        float result = PressureModel.Step(pressure: 10f, severity: 4f, mood: 0.6f, decayPerDay: 3f, hours: 1f);
        Assert.Equal(10f + 4f / 24f, result, 4);
    }

    [Fact]
    public void Step_WithSeverity_IsFasterWhenMoodIsLow()
    {
        float result = PressureModel.Step(pressure: 0f, severity: 6f, mood: 0.2f, decayPerDay: 3f, hours: 2f);
        Assert.Equal(6f / 24f * 2f * 1.6f, result, 4);
    }

    [Fact]
    public void Step_WithoutSeverity_DecaysPerHour()
    {
        float result = PressureModel.Step(pressure: 10f, severity: 0f, mood: 0.5f, decayPerDay: 3f, hours: 1f);
        Assert.Equal(10f - 3f / 24f, result, 4);
    }

    [Fact]
    public void Step_WithoutSeverity_NeverGoesBelowZero()
    {
        float result = PressureModel.Step(pressure: 0.01f, severity: 0f, mood: 0.5f, decayPerDay: 3f, hours: 24f);
        Assert.Equal(0f, result);
    }

    [Fact]
    public void Step_IgnoresNegativeSeverityAsNoGrievance()
    {
        float result = PressureModel.Step(pressure: 5f, severity: -2f, mood: 0.5f, decayPerDay: 3f, hours: 1f);
        Assert.Equal(5f - 3f / 24f, result, 4);
    }

    [Fact]
    public void DailyBarracksExample_ReachesDefaultThresholdInAboutSixDays()
    {
        // Spec section 2.2: mediocre barracks (-4) every day reaches 25 in about six days.
        float pressure = 0f;
        int hours = 0;
        while (pressure < 25f)
        {
            pressure = PressureModel.Step(pressure, severity: 4f, mood: 0.5f, decayPerDay: 3f, hours: 1f);
            hours++;
        }
        Assert.InRange(hours / 24f, 6f, 6.5f);
    }
}
