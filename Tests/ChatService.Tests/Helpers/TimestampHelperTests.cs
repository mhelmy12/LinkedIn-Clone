using ChatService.Helpers;
using Xunit;

namespace ChatService.Tests.Helpers;

public class TimestampHelperTests
{
    [Fact]
    public void EnsureUtc_KeepsUtcValue()
    {
        var value = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);

        var result = TimestampHelper.EnsureUtc(value);

        Assert.Equal(DateTimeKind.Utc, result.Kind);
        Assert.Equal(value, result);
    }

    [Fact]
    public void EnsureUtc_ConvertsLocalValue()
    {
        var value = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Local);

        var result = TimestampHelper.EnsureUtc(value);

        Assert.Equal(DateTimeKind.Utc, result.Kind);
        Assert.Equal(value.ToUniversalTime(), result);
    }

    [Fact]
    public void EnsureUtc_TreatsUnspecifiedAsUtc()
    {
        var value = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Unspecified);

        var result = TimestampHelper.EnsureUtc(value);

        Assert.Equal(DateTimeKind.Utc, result.Kind);
        Assert.Equal(value.Ticks, result.Ticks);
    }
}
