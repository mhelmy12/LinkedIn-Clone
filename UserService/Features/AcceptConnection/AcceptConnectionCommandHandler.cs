using System;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Helpers;
using UserService.Data;
using UserService.Events;
using UserService.Models;
using UserService.Services.CurrentUserService;

namespace UserService.Features.AcceptConnection;

public class AcceptConnectionCommandHandler(

    [FromKeyedServices("Headers")] ICurrentUserService currentUserService,
    UserDbContext dbContext
)



 : ResponseHandler, IRequestHandler<AcceptConnectionCommand, Response<AcceptConnectionCommandResponse>>
{
    public async Task<Response<AcceptConnectionCommandResponse>> Handle(AcceptConnectionCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId))
        {
            return BadRequest<AcceptConnectionCommandResponse>("Current user not found or invalid ID.");
        }


        var connection = await dbContext.Connections
            .FirstOrDefaultAsync(c => c.Id == long.Parse(request.ConnectionId), cancellationToken);

        if (connection == null)
        {
            return NotFound<AcceptConnectionCommandResponse>("Connection request not found.");
        }

        if (connection.TargetId != currentUserId)
        {
            return BadRequest<AcceptConnectionCommandResponse>("You are not authorized to accept this connection request.");
        }

        if (connection.Status == ConnectionStatus.CONNECTED)
        {
            return BadRequest<AcceptConnectionCommandResponse>("This connection request is already accepted.");
        }

        if (connection.Status != ConnectionStatus.PENDING)
        {
            return BadRequest<AcceptConnectionCommandResponse>("Cannot accept a connection that is not in pending status.");
        }

        connection.Status = ConnectionStatus.CONNECTED;


        var connectionAcceptedEvent = new ConnectionAcceptedEvent(
            connection.RequesterId,
            connection.TargetId,
            DateTime.Now
        );

        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid().ToString(),
            OccurredOn = DateTime.Now,
            EventType = nameof(ConnectionAcceptedEvent),
            Payload = System.Text.Json.JsonSerializer.Serialize(connectionAcceptedEvent),
            AggregateId = connection.Id.ToString(),
            AggregateType = nameof(Connection)

        });



        return Success(new AcceptConnectionCommandResponse(
            connection.Id.ToString(),
            connection.RequesterId.ToString(),
            connection.TargetId.ToString(),
            connection.Status.ToString()
        ));
    }
}
