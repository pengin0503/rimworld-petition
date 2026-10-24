using Petitions.Core;
using Xunit;

namespace Petitions.Tests;

public class UltimatumModelTests
{
    [Fact]
    public void Decide_ResolvedWhenStageDroppedBelowUltimatum()
    {
        Assert.Equal(UltimatumDecision.Resolved, UltimatumModel.Decide(5, 10, ResentmentStage.Disgruntled, hasPendingPromise: false));
    }

    [Fact]
    public void Decide_WaitsForPendingPromiseEvenPastDeadline()
    {
        Assert.Equal(UltimatumDecision.Wait, UltimatumModel.Decide(20, 10, ResentmentStage.Ultimatum, hasPendingPromise: true));
    }

    [Fact]
    public void Decide_LeavesAtDeadline()
    {
        Assert.Equal(UltimatumDecision.Leave, UltimatumModel.Decide(10, 10, ResentmentStage.Ultimatum, hasPendingPromise: false));
    }

    [Fact]
    public void Decide_WaitsBeforeDeadline()
    {
        Assert.Equal(UltimatumDecision.Wait, UltimatumModel.Decide(9, 10, ResentmentStage.Ultimatum, hasPendingPromise: false));
    }

    [Fact]
    public void DeadlineIsThreeDays()
    {
        Assert.Equal(3 * 60000, UltimatumModel.DeadlineTicks);
    }

    // Issue #3: give, resolve, give again; only the letter of the running ultimatum counts.
    [Theory]
    [InlineData(true, 2, 2, true)]
    [InlineData(true, 2, 1, false)]
    [InlineData(false, 1, 1, false)]
    [InlineData(true, 0, 0, true)]
    public void LetterIsCurrent_OnlyForTheRunningUltimatum(bool running, int count, int letter, bool expected)
    {
        Assert.Equal(expected, UltimatumModel.LetterIsCurrent(running, count, letter));
    }
}
