using System;
using MediatR;
using Shared.Helpers;
using UserService.Behaviors;

namespace UserService.Features.UpdateCoverPic;

public record UpdateCoverPicCommand(
    string ObjectKey
) : IRequest<Response<UpdateCoverPicCommandResponse>>, ITransactionCommand;
