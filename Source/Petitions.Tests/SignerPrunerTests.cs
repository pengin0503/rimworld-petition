using System.Collections.Generic;
using Petitions.Core;
using Xunit;

namespace Petitions.Tests;

public class SignerPrunerTests
{
    [Fact]
    public void Prune_RemovesGoneSignersAndKeepsClearSinceAligned()
    {
        List<string> signers = new() { "alive1", "gone1", "alive2", "gone2" };
        List<int> clearSince = new() { 10, 20, 30, 40 };

        bool empty = SignerPruner.Prune(signers, clearSince, s => s.StartsWith("gone"));

        Assert.False(empty);
        Assert.Equal(new[] { "alive1", "alive2" }, signers);
        Assert.Equal(new[] { 10, 30 }, clearSince);
    }

    [Fact]
    public void Prune_ReportsEmptyWhenEverySignerIsGone()
    {
        List<string> signers = new() { "gone1", "gone2" };
        List<int> clearSince = new() { 1, 2 };

        Assert.True(SignerPruner.Prune(signers, clearSince, s => true));
        Assert.Empty(signers);
        Assert.Empty(clearSince);
    }

    [Fact]
    public void Prune_ToleratesShortClearSinceList()
    {
        List<string> signers = new() { "a", "gone", "b" };
        List<int> clearSince = new() { 5 };

        Assert.False(SignerPruner.Prune(signers, clearSince, s => s == "gone"));
        Assert.Equal(new[] { "a", "b" }, signers);
        Assert.Equal(new[] { 5, -1 }, clearSince);
    }
}
