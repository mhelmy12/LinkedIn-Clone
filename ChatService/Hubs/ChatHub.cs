using ChatService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChatService.Hubs;

/// <summary>
/// Realtime chat hub (v1). This phase establishes authentication and
/// connection-scoped group membership; messaging, typing, presence and read
/// receipts are added in later phases.
/// </summary>
[Authorize]
public sealed class ChatHub(
    ChatGroupManager groupManager,
    ILogger<ChatHub> logger) : Hub
{
    public const string Route = "/hubs/chat";

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;

        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogWarning(
                "Rejecting chat connection {ConnectionId}: no user identifier.",
                Context.ConnectionId);

            Context.Abort();
            return;
        }

        await groupManager.JoinUserGroupsAsync(
            Groups, Context.ConnectionId, userId, Context.ConnectionAborted);

        logger.LogInformation(
            "Chat connection {ConnectionId} established for user {UserId}.",
            Context.ConnectionId, userId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            try
            {
                await groupManager.LeaveUserGroupsAsync(
                    Groups, Context.ConnectionId, userId, CancellationToken.None);
            }
            catch (Exception ex)
            {
                // The connection is already gone; SignalR has dropped its
                // memberships, so a failed explicit cleanup is not fatal.
                logger.LogDebug(
                    ex, "Chat connection {ConnectionId} group cleanup failed.",
                    Context.ConnectionId);
            }

            logger.LogInformation(
                "Chat connection {ConnectionId} closed for user {UserId}.",
                Context.ConnectionId, userId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
