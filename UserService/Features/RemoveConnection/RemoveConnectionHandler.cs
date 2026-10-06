using System;


using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Helpers;
using UserService.Data;
using UserService.Models;
using UserService.Services.CurrentUserService;
using UserService.Events;

namespace UserService.Features.RemoveConnection;

public class RemoveConnectionHandler(
    UserDbContext _db,
    [FromKeyedServices("Headers")] ICurrentUserService _currentUserService,
    ILogger<RemoveConnectionHandler> _logger
) : ResponseHandler, IRequestHandler<RemoveConnectionCommand, Response<RemoveConnectionResponse>>
{
    public async Task<Response<RemoveConnectionResponse>> Handle(
        RemoveConnectionCommand request,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (currentUserId is null)
            return Unauthorized<RemoveConnectionResponse>("User is not authenticated.");


        if (currentUserId == request.TargetUserId)
            return BadRequest<RemoveConnectionResponse>("Cannot remove connection with yourself.");


        var connection = await _db.Connections
            .FirstOrDefaultAsync(c =>
                (
                    (c.RequesterId == currentUserId && c.TargetId == request.TargetUserId)
                    ||
                    (c.RequesterId == request.TargetUserId && c.TargetId == currentUserId)
                )
                && c.Status == ConnectionStatus.CONNECTED,
                cancellationToken);

        if (connection is null)
            return NotFound<RemoveConnectionResponse>("Connection not found.");


        connection.IsDeleted = true;

        var now = DateTime.Now;

        var @event = new ConnectionRemovedEvent(
            UserId1: currentUserId,
            UserId2: request.TargetUserId,
            RemovedAt: now);

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid().ToString(),
            Payload = JsonSerializer.Serialize(@event),
            Type = nameof(ConnectionRemovedEvent),
            AggregateType = nameof(Connection),
            AggregateId = connection.Id.ToString(),
            OccurredOn = now
        };

        _db.OutboxMessages.Add(outboxMessage);

        _logger.LogInformation(
            "Connection removed between {User1} and {User2} (removed by {Actor})",
            currentUserId, request.TargetUserId, currentUserId);

        return Success<RemoveConnectionResponse>(new(), "Connection removed successfully.");
    }
}
