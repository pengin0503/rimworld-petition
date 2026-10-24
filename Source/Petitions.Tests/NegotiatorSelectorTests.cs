using System;
using Petitions.Core;
using Xunit;

namespace Petitions.Tests;

public class NegotiatorSelectorTests
{
    private static readonly (string, float)[] Candidates = { ("B", 1.0f), ("C", 1.5f) };

    private static readonly Func<string, bool> Everyone = _ => true;

    [Fact]
    public void Select_PrefersAssigned()
    {
        Assert.Equal("A", NegotiatorSelector.Select("A", "L", Candidates, Everyone));
    }

    [Fact]
    public void Select_FallsBackToLeaderWhenAssignedCannotNegotiate()
    {
        Assert.Equal("L", NegotiatorSelector.Select("A", "L", Candidates, p => p != "A"));
    }

    [Fact]
    public void Select_FallsBackToBestAbility()
    {
        Assert.Equal("C", NegotiatorSelector.Select<string>(null, null, Candidates, Everyone));
    }

    [Fact]
    public void Select_SkipsCandidatesWhoCannotNegotiate()
    {
        Assert.Equal("C", NegotiatorSelector.Select("A", "L", new[] { ("A", 2.0f), ("C", 1.5f) }, p => p == "C"));
    }

    [Fact]
    public void Select_ReturnsNullWhenNobodyCan()
    {
        Assert.Null(NegotiatorSelector.Select<string>(null, null, Array.Empty<(string, float)>(), Everyone));
    }
}
