using ChatService.Data;
using ChatService.Services;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Helpers;

/// <summary>
/// Idempotent writer for <c>ChatConnectionProjection</c>. Both directions are
/// upserted in one statement so redelivery of the same event is a no-op.
/// </summary>
internal static class ConnectionProjectionWriter
{
    public static async Task ApplyAsync(
        ChatDbContext db,
        string userId1,
        string userId2,
        bool isActive,
        DateTime occurredAt,
        CancellationToken cancellationToken)
    {
        var (lower, higher) = PairNormalizer.Normalize(userId1, userId2);
        var occurredAtUtc = TimestampHelper.EnsureUtc(occurredAt);

        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "ChatConnectionProjections" ("UserId", "OtherUserId", "IsActive", "UpdatedAt")
            VALUES ({lower}, {higher}, {isActive}, {occurredAtUtc}),
                   ({higher}, {lower}, {isActive}, {occurredAtUtc})
            ON CONFLICT ("UserId", "OtherUserId") DO UPDATE
            SET "IsActive" = CASE
                    WHEN EXCLUDED."UpdatedAt" >= "ChatConnectionProjections"."UpdatedAt"
                        THEN EXCLUDED."IsActive"
                    ELSE "ChatConnectionProjections"."IsActive"
                END,
                "UpdatedAt" = GREATEST(
                    "ChatConnectionProjections"."UpdatedAt",
                    EXCLUDED."UpdatedAt");
            """, cancellationToken);
    }
}
