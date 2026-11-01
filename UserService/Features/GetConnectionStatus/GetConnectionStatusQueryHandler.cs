using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Helpers;
using UserService.Data;
using UserService.Models;
using UserService.Services.CurrentUserService;

namespace UserService.Features.GetConnectionStatus;

public class GetConnectionStatusQueryHandler(
    UserDbContext db,
    [FromKeyedServices("Headers")] ICurrentUserService currentUserService)
    : ResponseHandler, IRequestHandler<GetConnectionStatusQuery, Response<GetConnectionStatusResponse>>
{
    public async Task<Response<GetConnectionStatusResponse>> Handle(
        GetConnectionStatusQuery request,
        CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return Unauthorized<GetConnectionStatusResponse>("User is not authenticated.");
        }

        if (string.Equals(currentUserId, request.OtherUserId, StringComparison.Ordinal))
        {
            return BadRequest<GetConnectionStatusResponse>("A user cannot be connected to themselves.");
        }

        var connected = await db.Connections
            .AsNoTracking()
            .AnyAsync(
                c => !c.IsDeleted
                     && c.Status == ConnectionStatus.CONNECTED
                     && ((c.RequesterId == currentUserId && c.TargetId == request.OtherUserId)
                         || (c.RequesterId == request.OtherUserId && c.TargetId == currentUserId)),
                cancellationToken);

        return Success(new GetConnectionStatusResponse { Connected = connected });
    }
}
