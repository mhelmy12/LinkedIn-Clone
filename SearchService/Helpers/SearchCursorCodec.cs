using System;
using System.Text;

namespace SearchService.Helpers;

using System.Text;
using System.Text.Json;

public class SearchCursor
{
    public double? Score { get; set; }
    public DateTime CreatedAt { get; set; }
    public long Id { get; set; }
}

public static class SearchCursorCodec
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string Encode(SearchCursor cursor)
    {
        var json = JsonSerializer.Serialize(cursor, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        return Convert.ToBase64String(bytes);
    }

    public static bool TryDecode(string? raw, out SearchCursor? cursor)
    {
        cursor = null;

        if (string.IsNullOrWhiteSpace(raw))
            return false;

        try
        {
            var bytes = Convert.FromBase64String(raw);
            var json = Encoding.UTF8.GetString(bytes);
            cursor = JsonSerializer.Deserialize<SearchCursor>(json, JsonOptions);
            return cursor is not null;
        }
        catch
        {
            return false;
        }
    }
}