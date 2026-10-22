using Petitions.Core;
using Xunit;

namespace Petitions.Tests;

public class CosignerSelectorTests
{
    [Fact]
    public void Select_PicksTopPressuresAtOrAboveFractionExcludingPetitioner()
    {
        float[] pressures = { 30f, 5f, 12.5f, 20f, 12.4f, 25f, 26f, 27f, 28f };
        Assert.Equal(new[] { 8, 7, 6, 5, 3 }, CosignerSelector.Select(pressures, 0, 25f, 0.5f, CosignerSelector.MaxCosigners));
    }

    [Fact]
    public void Select_IncludesValueExactlyAtCutoff()
    {
        float[] pressures = { 30f, 12.5f };
        Assert.Equal(new[] { 1 }, CosignerSelector.Select(pressures, 0, 25f, 0.5f, 5));
    }

    [Fact]
    public void Select_ReturnsEmptyWhenNobodyQualifies()
    {
        float[] pressures = { 30f, 1f, 2f };
        Assert.Empty(CosignerSelector.Select(pressures, 0, 25f, 0.5f, 5));
    }

    [Fact]
    public void MaxCosignersIsFive()
    {
        Assert.Equal(5, CosignerSelector.MaxCosigners);
    }
}
