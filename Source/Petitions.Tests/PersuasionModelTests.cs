using Petitions.Core;
using Xunit;

namespace Petitions.Tests;

public class PersuasionModelTests
{
    [Fact]
    public void SuccessChance_UsesSpecFormula()
    {
        // 0.45 * 1.2 + 40 / 500 - 0.05 * 2 - 0.15 = 0.37
        float chance = PersuasionModel.SuccessChance(negotiationAbility: 1.2f, opinionOfNegotiator: 40, cosigners: 2, difficulty: 0.15f);
        Assert.Equal(0.37f, chance, 4);
    }

    [Fact]
    public void SuccessChance_IsClampedToMinimum()
    {
        float chance = PersuasionModel.SuccessChance(negotiationAbility: 0f, opinionOfNegotiator: -100, cosigners: 10, difficulty: 0.5f);
        Assert.Equal(PersuasionModel.MinChance, chance);
    }

    [Fact]
    public void SuccessChance_IsClampedToMaximum()
    {
        float chance = PersuasionModel.SuccessChance(negotiationAbility: 3f, opinionOfNegotiator: 100, cosigners: 0, difficulty: 0f);
        Assert.Equal(PersuasionModel.MaxChance, chance);
    }

    [Fact]
    public void SuccessChance_DropsWithMoreCosigners()
    {
        float alone = PersuasionModel.SuccessChance(1f, 0, 0, 0f);
        float group = PersuasionModel.SuccessChance(1f, 0, 3, 0f);
        Assert.True(group < alone);
    }
}
