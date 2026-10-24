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
}
