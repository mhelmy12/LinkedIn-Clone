using ChatService.Services;
using Xunit;

namespace ChatService.Tests.Helpers;

public class PairNormalizerTests
{
    [Fact]
    public void Normalize_OrdersPairByOrdinalValue()
    {
        var (lower, higher) = PairNormalizer.Normalize("b-user", "a-user");

        Assert.Equal("a-user", lower);
        Assert.Equal("b-user", higher);
    }

    [Fact]
    public void Normalize_IsSymmetric()
    {
        var forward = PairNormalizer.Normalize("user-a", "user-b");
        var reverse = PairNormalizer.Normalize("user-b", "user-a");

        Assert.Equal(forward, reverse);
    }

    [Fact]
    public void Normalize_RejectsSameUser()
    {
        Assert.Throws<ArgumentException>(() => PairNormalizer.Normalize("user-a", "user-a"));
    }

    [Theory]
    [InlineData("", "user-b")]
    [InlineData("user-a", " ")]
    public void Normalize_RejectsBlankIds(string userId1, string userId2)
    {
        Assert.Throws<ArgumentException>(() => PairNormalizer.Normalize(userId1, userId2));
    }
}
