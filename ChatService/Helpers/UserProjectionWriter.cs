using ChatService.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Helpers;

/// <summary>
/// Idempotent writer for <c>ChatUserProjection</c>. Out-of-order deliveries keep
/// the newest profile snapshot.
/// </summary>
internal static class UserProjectionWriter
{
    public static async Task ApplyAsync(
        ChatDbContext db,
        string userId,
        string displayName,
        string? email,
        DateTime updatedAt,
        CancellationToken cancellationToken)
    {
        var updatedAtUtc = TimestampHelper.EnsureUtc(updatedAt);

        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "ChatUserProjections" ("UserId", "DisplayName", "Email", "UpdatedAt")
            VALUES ({userId}, {displayName}, {email}, {updatedAtUtc})
            ON CONFLICT ("UserId") DO UPDATE
            SET "DisplayName" = CASE
                    WHEN EXCLUDED."UpdatedAt" >= "ChatUserProjections"."UpdatedAt"
                        THEN EXCLUDED."DisplayName"
                    ELSE "ChatUserProjections"."DisplayName"
                END,
                "Email" = CASE
                    WHEN EXCLUDED."UpdatedAt" >= "ChatUserProjections"."UpdatedAt"
                        THEN EXCLUDED."Email"
                    ELSE "ChatUserProjections"."Email"
                END,
                "UpdatedAt" = GREATEST(
                    "ChatUserProjections"."UpdatedAt",
                    EXCLUDED."UpdatedAt");
            """, cancellationToken);
    }
}
