using System;
using MediatR;
using Shared.Helpers;
using UserService.Behaviors;

namespace UserService.Features.AcceptConnection;

public record AcceptConnectionCommand(
    string ConnectionId
) : IRequest<Response<AcceptConnectionCommandResponse>>, ITransactionCommand;