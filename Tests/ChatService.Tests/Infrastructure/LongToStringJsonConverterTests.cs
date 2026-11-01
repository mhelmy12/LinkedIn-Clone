using System.Text.Json;
using ChatService.Infrastructure;
using Xunit;

namespace ChatService.Tests.Infrastructure;

public class LongToStringJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new LongToStringJsonConverter() }
    };

    [Fact]
    public void Write_SerializesLongAsString()
    {
        const long id = 9_007_199_254_740_993;

        var json = JsonSerializer.Serialize(new { id }, Options);

        Assert.Equal("{\"id\":\"9007199254740993\"}", json);
    }

    [Fact]
    public void Read_AcceptsString()
    {
        var id = JsonSerializer.Deserialize<long>("\"9007199254740993\"", Options);

        Assert.Equal(9_007_199_254_740_993, id);
    }

    [Fact]
    public void Read_AcceptsNumberForBackwardCompatibility()
    {
        var id = JsonSerializer.Deserialize<long>("42", Options);

        Assert.Equal(42, id);
    }

    [Fact]
    public void Read_RejectsNonNumericString()
    {
        Assert.Throws<JsonException>(
            () => JsonSerializer.Deserialize<long>("\"not-a-number\"", Options));
    }
}
