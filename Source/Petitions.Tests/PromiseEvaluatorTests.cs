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
    public void Remedy_NoPresentSignersIsUnjudgedAtDeadline()
    {
        // Issue #5: nobody could see the promise kept or broken, so it is not a broken promise.
        Assert.Equal(PromiseStatus.Unjudged, PromiseEvaluator.EvaluateRemedy(200_000, 200_000, 60_000, new int[0]));
    }

    // Issue #2's case: promise 1000..901000, last update before the deadline at 900000, next at 902500.
    [Theory]
    [InlineData(1000, 902_500, false)]   // gained at 901500, after the deadline: the promise was kept
    [InlineData(2000, 902_500, true)]    // gained at 900500, before the deadline: found one update later, still broken
    [InlineData(901_200, 902_500, false)] // gained at 1300, within the slack after the promise was made
    public void IsDuringPromise_LimitsToThePromisePeriod(int memoryAge, int nowTick, bool expected)
    {
        Assert.Equal(expected, PromiseEvaluator.IsDuringPromise(memoryAge, nowTick, 1000, 901_000));
    }

    [Fact]
    public void AbsentSigners_DeadlineMovesWhileNobodyCanBeJudged()
    {
        int extended = 0;
        int deadline = PromiseEvaluator.AdjustRemedyDeadline(100_000, 120_000, 60_000, 2500, anyoneJudgeable: false, nobodyJudgeableBefore: false, ref extended);
        Assert.Equal(122_500, deadline);
        Assert.Equal(2500, extended);
    }

    [Fact]
    public void AbsentSigners_ReturningLateGetsTheHoldTime()
    {
        int extended = 2500;
        int deadline = PromiseEvaluator.AdjustRemedyDeadline(110_000, 130_000, 60_000, 2500, anyoneJudgeable: true, nobodyJudgeableBefore: true, ref extended);
        Assert.Equal(170_000, deadline);
        Assert.Equal(42_500, extended);
    }

    [Fact]
    public void AbsentSigners_PresentSignersNeverMoveTheDeadline()
    {
        int extended = 0;
        Assert.Equal(130_000, PromiseEvaluator.AdjustRemedyDeadline(110_000, 130_000, 60_000, 2500, anyoneJudgeable: true, nobodyJudgeableBefore: false, ref extended));
        Assert.Equal(0, extended);
    }

    [Fact]
    public void AbsentSigners_ExtensionIsCapped()
    {
        int extended = PromiseEvaluator.MaxAbsenceExtensionTicks - 1000;
        Assert.Equal(201_000, PromiseEvaluator.AdjustRemedyDeadline(199_000, 200_000, 60_000, 2500, anyoneJudgeable: false, nobodyJudgeableBefore: true, ref extended));
        Assert.Equal(PromiseEvaluator.MaxAbsenceExtensionTicks, extended);
        Assert.Equal(201_000, PromiseEvaluator.AdjustRemedyDeadline(201_000, 201_000, 60_000, 2500, anyoneJudgeable: false, nobodyJudgeableBefore: true, ref extended));
        Assert.Equal(PromiseStatus.Unjudged, PromiseEvaluator.EvaluateRemedy(201_000, 201_000, 60_000, new int[0]));
    }

    [Fact]
    public void AbsentSigners_SomeAwayOthersJudged()
    {
        // Away signers are left out of the list; the present one alone decides.
        Assert.Equal(PromiseStatus.Kept, PromiseEvaluator.EvaluateRemedy(200_000, 200_000, 60_000, new[] { 100_000 }));
    }

    [Theory]
    [InlineData(100, 2500, true)]
    [InlineData(2300, 2500, false)]
    [InlineData(2200, 2500, false)]
    [InlineData(2199, 2500, true)]
    public void IsNewSincePromise_AllowsMemoryAgeGranularity(int memoryAge, int ticksSincePromise, bool expected)
    {
        Assert.Equal(expected, PromiseEvaluator.IsNewSincePromise(memoryAge, ticksSincePromise));
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
