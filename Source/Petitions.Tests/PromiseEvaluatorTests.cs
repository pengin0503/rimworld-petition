using Petitions.Core;
using Xunit;

namespace Petitions.Tests;

public class PromiseEvaluatorTests
{
    [Fact]
    public void Remedy_KeptWhenEverySignerClearLongEnough()
    {
        Assert.Equal(PromiseStatus.Kept, PromiseEvaluator.EvaluateRemedy(100_000, 200_000, 60_000, new[] { 40_000, 30_000 }));
    }

    [Fact]
    public void Remedy_PendingWhileAnySignerStillHasThought()
    {
        Assert.Equal(PromiseStatus.Pending, PromiseEvaluator.EvaluateRemedy(100_000, 200_000, 60_000, new[] { 40_000, -1 }));
    }

    [Fact]
    public void Remedy_PendingWhileClearButNotLongEnough()
    {
        Assert.Equal(PromiseStatus.Pending, PromiseEvaluator.EvaluateRemedy(100_000, 200_000, 60_000, new[] { 50_000 }));
    }

    [Fact]
    public void Remedy_BrokenAtDeadlineWhenNotHeldLongEnough()
    {
        Assert.Equal(PromiseStatus.Broken, PromiseEvaluator.EvaluateRemedy(200_000, 200_000, 60_000, new[] { 190_000 }));
    }

    [Fact]
    public void Remedy_KeptWinsOverDeadline()
    {
        Assert.Equal(PromiseStatus.Kept, PromiseEvaluator.EvaluateRemedy(200_000, 200_000, 60_000, new[] { 140_000 }));
    }

    [Fact]
    public void Remedy_NoPresentSignersStaysPendingBeforeDeadline()
    {
        Assert.Equal(PromiseStatus.Pending, PromiseEvaluator.EvaluateRemedy(100_000, 200_000, 60_000, new int[0]));
    }

    [Fact]
    public void Remedy_NoPresentSignersBreaksAtDeadline()
    {
        Assert.Equal(PromiseStatus.Broken, PromiseEvaluator.EvaluateRemedy(200_000, 200_000, 60_000, new int[0]));
    }

    [Fact]
    public void Cessation_BrokenOnNewOccurrence()
    {
        Assert.Equal(PromiseStatus.Broken, PromiseEvaluator.EvaluateCessation(100, 1000, newOccurrence: true));
    }

    [Fact]
    public void Cessation_KeptAtDeadline()
    {
        Assert.Equal(PromiseStatus.Kept, PromiseEvaluator.EvaluateCessation(1000, 1000, newOccurrence: false));
    }

    [Fact]
    public void Cessation_PendingBeforeDeadline()
    {
        Assert.Equal(PromiseStatus.Pending, PromiseEvaluator.EvaluateCessation(999, 1000, newOccurrence: false));
    }
}
