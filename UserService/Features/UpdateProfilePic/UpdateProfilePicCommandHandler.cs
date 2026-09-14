using System;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Helpers;
using UserService.Data;
using UserService.Services.CurrentUserService;

namespace UserService.Features.UpdateProfilePic;

public class UpdateProfilePicCommandHandler(
    [FromKeyedServices("Headers")] ICurrentUserService currentUserService,
    UserDbContext dbContext



) : ResponseHandler, IRequestHandler<UpdateProfilePicCommand, Response<UpdateProfilePicCommandResponse>>
{
    public async Task<Response<UpdateProfilePicCommandResponse>> Handle(UpdateProfilePicCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized<UpdateProfilePicCommandResponse>("User not authenticated.");

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.KeycloakId == currentUserId);

        if (user == null)
            return NotFound<UpdateProfilePicCommandResponse>("User not found.");


        user.ProfilePictureUrl = request.ObjectKey;

        return Success<UpdateProfilePicCommandResponse>(new(), " Profile picture updated successfully.");


    }
}
