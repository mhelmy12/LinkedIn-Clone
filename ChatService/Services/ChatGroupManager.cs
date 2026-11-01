using ChatService.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Services;

/// <summary>
/// Owns the SignalR group naming scheme and connection-scoped membership.
/// A connection auto-joins <c>user:{userId}</c> plus one <c>conv:{id}</c> group
/// per conversation the caller participates in (contracts/signalr-hub.md).
/// </summary>
public sealed class ChatGroupManager(ChatDbContext db)
{
    public const string UserGroupPrefix = "user:";

    public const string ConversationGroupPrefix = "conv:";

    public const string PresenceGroupPrefix = "presence:";

    public static string UserGroup(string userId) => $"{UserGroupPrefix}{userId}";

    public static string ConversationGroup(long conversationId) =>
        $"{ConversationGroupPrefix}{conversationId}";

    public static string PresenceGroup(string userId) => $"{PresenceGroupPrefix}{userId}";

    /// <summary>Conversations the user participates in, ordered by id.</summary>
    public Task<List<long>> GetConversationIdsAsync(string userId, CancellationToken cancellationToken) =>
        db.Conversations
            .AsNoTracking()
            .Where(c => c.Participant1Id == userId || c.Participant2Id == userId)
            .OrderBy(c => c.Id)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

    /// <summary>Joins the caller's user group and every conversation group it watches.</summary>
    public async Task JoinUserGroupsAsync(
        IGroupManager groups,
        string connectionId,
        string userId,
        CancellationToken cancellationToken)
    {
        await groups.AddToGroupAsync(connectionId, UserGroup(userId), cancellationToken);

        foreach (var conversationId in await GetConversationIdsAsync(userId, cancellationToken))
        {
            await groups.AddToGroupAsync(
                connectionId, ConversationGroup(conversationId), cancellationToken);
        }
    }

    /// <summary>
    /// Explicitly leaves the caller's groups. SignalR also drops memberships
    /// when a connection ends; this keeps disconnect cleanup deterministic.
    /// </summary>
    public async Task LeaveUserGroupsAsync(
        IGroupManager groups,
        string connectionId,
        string userId,
        CancellationToken cancellationToken)
    {
        foreach (var conversationId in await GetConversationIdsAsync(userId, cancellationToken))
        {
            await groups.RemoveFromGroupAsync(
                connectionId, ConversationGroup(conversationId), cancellationToken);
        }

        await groups.RemoveFromGroupAsync(connectionId, UserGroup(userId), cancellationToken);
    }
}
