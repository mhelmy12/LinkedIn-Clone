using System;

namespace FeedService.Helpers;

using System.Text;
using System.Text.Json;

public class FeedCursorData
{
    public double Score { get; set; }
    public long PostId { get; set; }
}

public static class FeedCursorCodec
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string Encode(FeedCursorData cursor)
    {
        var json = JsonSerializer.Serialize(cursor, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        return Convert.ToBase64String(bytes);
    }

    public static bool TryDecode(string? raw, out FeedCursorData? cursor)
    {
        cursor = null;

        if (string.IsNullOrWhiteSpace(raw))
            return false;

        try
        {
            var bytes = Convert.FromBase64String(raw);
            var json = Encoding.UTF8.GetString(bytes);
            cursor = JsonSerializer.Deserialize<FeedCursorData>(json, JsonOptions);
            return cursor is not null;
        }
        catch
        {
            return false;
        }
    }
}