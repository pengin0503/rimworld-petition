using System.Collections.Generic;
using Petitions.Core;
using Xunit;

namespace Petitions.Tests;

public class ThoughtEventIndexTests
{
    private static readonly ThoughtEventIndex<string, string> Index = new(new[]
    {
        ("KnowExecAbhorrent", "ExecutedGuest"),
        ("KnowExecAbhorrent", "ExecutedPrisoner"),
        ("Happy", "ExecutedGuest"),
    });

    private static readonly HashSet<string> None = new();

    [Fact]
    public void Matches_ByMappedEvent()
    {
        Assert.True(Index.Matches("KnowExecAbhorrent", None, new HashSet<string> { "ExecutedPrisoner" }));
    }

    [Fact]
    public void Matches_ByDirectThought()
    {
        Assert.True(Index.Matches("KnowGuestExecuted", new HashSet<string> { "KnowGuestExecuted" }, None));
    }

    [Fact]
    public void DoesNotMatch_WhenEventsDoNotOverlap()
    {
        Assert.False(Index.Matches("KnowExecAbhorrent", None, new HashSet<string> { "SoldSlave" }));
    }

    [Fact]
    public void DoesNotMatch_UnknownThought()
    {
        Assert.False(Index.Matches("Unknown", None, new HashSet<string> { "ExecutedGuest" }));
    }
}
