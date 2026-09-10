using System;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Helpers;
using UserService.Data;
using UserService.Models;
using UserService.Services.CurrentUserService;

namespace UserService.Features.CancelConnection;

public class CancelConnectionCommandHandler(
    [FromKeyedServices("Headers")] ICurrentUserService currentUserService,
    UserDbContext dbContext
    )
    : ResponseHandler, IRequestHandler<CancelConnectionCommand, Response<CancelConnectionCommandResponse>>
{
    public async Task<Response<CancelConnectionCommandResponse>> Handle(CancelConnectionCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId))
        {
            return BadRequest<CancelConnectionCommandResponse>("Current user not found or invalid ID.");
        }


        var connection = await dbContext.Connections
            .FirstOrDefaultAsync(c => c.Id == long.Parse(request.ConnectionId), cancellationToken);

        if (connection == null)
        {
            return NotFound<CancelConnectionCommandResponse>("Connection request not found.");
        }

        if (connection.RequesterId != currentUserId)
        {
            return BadRequest<CancelConnectionCommandResponse>("You are not authorized to cancel this connection request.");
        }

        if (connection.Status != ConnectionStatus.PENDING)
        {
            return BadRequest<CancelConnectionCommandResponse>("Only pending connection requests can be canceled.");
        }

        connection.IsDeleted = true;


        return Success<CancelConnectionCommandResponse>(new(),
            "Connection request canceled successfully.");

    }
}
