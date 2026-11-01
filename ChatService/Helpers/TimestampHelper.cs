namespace ChatService.Helpers;

/// <summary>
/// Npgsql requires <see cref="DateTimeKind.Utc"/> for <c>timestamptz</c>.
/// Existing User Service events use <c>DateTime.Now</c>, so normalize on the
/// way into <c>chatDb</c>.
/// </summary>
public static class TimestampHelper
{
    public static DateTime EnsureUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
