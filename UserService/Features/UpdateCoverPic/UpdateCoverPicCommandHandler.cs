using System;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Helpers;
using UserService.Data;
using UserService.Services.CurrentUserService;

namespace UserService.Features.UpdateCoverPic;

public class UpdateCoverPicCommandHandler(
    [FromKeyedServices("Headers")] ICurrentUserService currentUserService,
    UserDbContext dbContext



) : ResponseHandler, IRequestHandler<UpdateCoverPicCommand, Response<UpdateCoverPicCommandResponse>>
{
    public async Task<Response<UpdateCoverPicCommandResponse>> Handle(UpdateCoverPicCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized<UpdateCoverPicCommandResponse>("User not authenticated.");

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.KeycloakId == currentUserId);

        if (user == null)
            return NotFound<UpdateCoverPicCommandResponse>("User not found.");


        user.CoverPictureUrl = request.ObjectKey;

        return Success<UpdateCoverPicCommandResponse>(new(), " Cover picture updated successfully.");


    }
}
