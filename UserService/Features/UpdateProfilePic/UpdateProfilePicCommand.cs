using System;
using MediatR;
using Shared.Helpers;
using UserService.Behaviors;

namespace UserService.Features.UpdateProfilePic;

public record UpdateProfilePicCommand(
    string ObjectKey
) : IRequest<Response<UpdateProfilePicCommandResponse>>, ITransactionCommand;
