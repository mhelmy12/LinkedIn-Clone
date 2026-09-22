using System;
using System.Text;

namespace PostService.Helpers;


public static class CursorCodec
{
    public static string Encode(DateTime createdAt, string id)
    {
        var raw = $"{createdAt.Ticks}:{id}";
        var bytes = Encoding.UTF8.GetBytes(raw);
        return Convert.ToBase64String(bytes);
    }


    public static bool TryDecode(
        string? cursor,
        out DateTime createdAt,
        out long id)
    {
        createdAt = default;
        id = default;

        if (string.IsNullOrWhiteSpace(cursor))
            return false;

        try
        {
            var bytes = Convert.FromBase64String(cursor);
            var raw = Encoding.UTF8.GetString(bytes);
            var parts = raw.Split(':');

            if (parts.Length != 2)
                return false;

            if (!long.TryParse(parts[0], out var ticks))
                return false;

            createdAt = new DateTime(ticks, DateTimeKind.Utc);
            return true;
        }
        catch
        {
            return false;
        }
    }
}