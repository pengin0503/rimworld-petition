using Petitions.Core;
using Xunit;

namespace Petitions.Tests;

public class OutcomeEffectsTests
{
    [Theory]
    [InlineData(PetitionOutcome.Accepted, 0, -10f)]
    [InlineData(PetitionOutcome.PromiseKept, 0, -25f)]
    [InlineData(PetitionOutcome.PromiseBroken, 0, 35f)]
    [InlineData(PetitionOutcome.Rejected, 0, 20f)]
    [InlineData(PetitionOutcome.Ignored, 0, 20f)]
    [InlineData(PetitionOutcome.Persuaded, 0, -5f)]
    [InlineData(PetitionOutcome.PersuasionFailed, 0, 30f)]
    public void ResentmentDelta_BaseValuesMatchSpec(PetitionOutcome outcome, int cosigners, float expected)
    {
        Assert.Equal(expected, OutcomeEffects.ResentmentDelta(outcome, cosigners), 4);
    }

    [Fact]
    public void ResentmentDelta_DecreasesAreNotScaledByCosigners()
    {
        Assert.Equal(-10f, OutcomeEffects.ResentmentDelta(PetitionOutcome.Accepted, 3), 4);
        Assert.Equal(-25f, OutcomeEffects.ResentmentDelta(PetitionOutcome.PromiseKept, 5), 4);
    }

    [Fact]
    public void ResentmentDelta_IncreasesAreScaledByCosigners()
    {
        Assert.Equal(26f, OutcomeEffects.ResentmentDelta(PetitionOutcome.Rejected, 3), 4);
        Assert.Equal(52.5f, OutcomeEffects.ResentmentDelta(PetitionOutcome.PromiseBroken, 9), 4);
    }
}
