using Petitions.Core;
using Xunit;

namespace Petitions.Tests;

public class PetitionRateLimiterTests
{
    private const int Day = PetitionRateLimiter.TicksPerDay;

    private static readonly RateLimits Defaults = new RateLimits(
        minDaysBetweenPetitions: 2f,
        maxOpenPetitions: 2,
        pawnCooldownDays: 5f,
        grievanceCooldownDays: 10f);

    private static RateLimitInput Input(
        int now = 100 * Day,
        int lastColonyPetitionTick = -1,
        int openPetitions = 0,
        int lastPawnPetitionTick = -1,
        int lastPawnGrievancePetitionTick = -1)
    {
        return new RateLimitInput(now, lastColonyPetitionTick, openPetitions, lastPawnPetitionTick, lastPawnGrievancePetitionTick);
    }

    [Fact]
    public void Check_AllowsWhenNothingHappenedBefore()
    {
        Assert.Equal(RateLimitResult.Allowed, PetitionRateLimiter.Check(Input(), Defaults));
    }

    [Fact]
    public void Check_BlocksWhenTooManyOpen()
    {
        Assert.Equal(RateLimitResult.TooManyOpen, PetitionRateLimiter.Check(Input(openPetitions: 2), Defaults));
    }

    [Fact]
    public void Check_BlocksColonyIntervalUntilExactlyElapsed()
    {
        int now = 100 * Day;
        Assert.Equal(RateLimitResult.ColonyCooldown,
            PetitionRateLimiter.Check(Input(now: now, lastColonyPetitionTick: now - 2 * Day + 1), Defaults));
        Assert.Equal(RateLimitResult.Allowed,
            PetitionRateLimiter.Check(Input(now: now, lastColonyPetitionTick: now - 2 * Day), Defaults));
    }

    [Fact]
    public void Check_BlocksPawnCooldown()
    {
        int now = 100 * Day;
        Assert.Equal(RateLimitResult.PawnCooldown,
            PetitionRateLimiter.Check(Input(now: now, lastPawnPetitionTick: now - 4 * Day), Defaults));
    }

    [Fact]
    public void Check_BlocksSameGrievanceCooldown()
    {
        int now = 100 * Day;
        Assert.Equal(RateLimitResult.GrievanceCooldown,
            PetitionRateLimiter.Check(Input(now: now, lastPawnPetitionTick: now - 6 * Day, lastPawnGrievancePetitionTick: now - 6 * Day), Defaults));
    }

    [Fact]
    public void Check_ReportsTooManyOpenBeforeCooldowns()
    {
        int now = 100 * Day;
        Assert.Equal(RateLimitResult.TooManyOpen,
            PetitionRateLimiter.Check(Input(now: now, lastColonyPetitionTick: now, openPetitions: 5), Defaults));
    }
}
