using System;
using MediatR;
using Shared.Helpers;
using UserService.Behaviors;

namespace UserService.Features.RejectConnection;

public record RejectConnectionCommand(
    string ConnectionId
) : IRequest<Response<RejectConnectionCommandResponse>>, ITransactionCommand;