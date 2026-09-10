using System;
using MediatR;
using Shared.Helpers;
using UserService.Behaviors;

namespace UserService.Features.CancelConnection;

public record CancelConnectionCommand(string ConnectionId) : IRequest<Response<CancelConnectionCommandResponse>>, ITransactionCommand;