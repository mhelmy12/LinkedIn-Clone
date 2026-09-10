using System;
using MediatR;
using Shared.Helpers;
using UserService.Behaviors;

namespace UserService.Features.SendConnection;

public record SendConnectionCommand(
    string TargetUserId
) : IRequest<Response<SendConnectionCommandResponse>>, ITransactionCommand;