using System;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Helpers;
using UserService.Data;
using UserService.Models;
using UserService.Services.CurrentUserService;
using UserService.Services.UserIdGenerator;

namespace UserService.Features.SendConnection;

public class SendConnectionCommandHandler(
    UserDbContext dbContext,
    [FromKeyedServices("Headers")] ICurrentUserService currentUserService,
    [FromKeyedServices("Snowflake")] IUserIdGenerator IdGenerator





) : ResponseHandler, IRequestHandler<SendConnectionCommand, Response<SendConnectionCommandResponse>>
{
    private readonly UserDbContext dbContext = dbContext;
    private readonly ICurrentUserService currentUserService = currentUserService;
    private readonly IUserIdGenerator IdGenerator = IdGenerator;

    public async Task<Response<SendConnectionCommandResponse>> Handle(SendConnectionCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.GetCurrentUserId();
        if (currentUserId == null)
        {
            return BadRequest<SendConnectionCommandResponse>("Current user not found.");
        }

        if (currentUserId == request.TargetUserId)
        {
            return BadRequest<SendConnectionCommandResponse>("You cannot send a connection request to yourself.");
        }

        var existingConnection = await dbContext.Connections
            .AnyAsync(c => (c.RequesterId.ToString() == currentUserId && c.TargetId.ToString() == request.TargetUserId) ||
                           (c.RequesterId.ToString() == request.TargetUserId && c.TargetId.ToString() == currentUserId),
                       cancellationToken);

        if (existingConnection)
        {
            return BadRequest<SendConnectionCommandResponse>("A connection request already exists between these users.");
        }

        var connection = new Connection
        {
            Id = long.Parse(IdGenerator.Generate()),
            RequesterId = long.Parse(currentUserId),
            TargetId = long.Parse(request.TargetUserId),
            Status = ConnectionStatus.PENDING,
        };

        dbContext.Connections.Add(connection);
        return Success(new SendConnectionCommandResponse
        (
            connection.Id,
            connection.RequesterId,
            connection.TargetId

        ));
    }
}
