using System;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Helpers;
using UserService.Data;
using UserService.Models;
using UserService.Services.CurrentUserService;

namespace UserService.Features.RejectConnection;

public class RejectConnectionCommandHandler(
    UserDbContext dbContext,
    [FromKeyedServices("Headers")] ICurrentUserService currentUserService
) : ResponseHandler, IRequestHandler<RejectConnectionCommand, Response<RejectConnectionCommandResponse>>
{
    public async Task<Response<RejectConnectionCommandResponse>> Handle(RejectConnectionCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId))
        {
            return BadRequest<RejectConnectionCommandResponse>("Current user not found or invalid ID.");
        }

        if (!long.TryParse(request.ConnectionId, out var connectionId))
        {
            return BadRequest<RejectConnectionCommandResponse>("Invalid connection ID.");
        }

        var connection = await dbContext.Connections
            .FirstOrDefaultAsync(c => c.Id == connectionId, cancellationToken);

        if (connection == null)
        {
            return NotFound<RejectConnectionCommandResponse>("Connection request not found.");
        }

        if (connection.TargetId != currentUserId)
        {
            return BadRequest<RejectConnectionCommandResponse>("You are not authorized to refuse this connection request.");
        }

        if (connection.Status != ConnectionStatus.PENDING)
        {
            return BadRequest<RejectConnectionCommandResponse>("Only pending connection requests can be refused.");
        }

        connection.IsDeleted = true;


        return Success<RejectConnectionCommandResponse>(new(),
            "Connection request rejected successfully.");
    }
}
