using System;
using MediatR;
using Shared.Helpers;
using UserService.Behaviors;

namespace UserService.Features.RemoveConnection;

public record RemoveConnectionCommand(string TargetUserId)
 : IRequest<Response<RemoveConnectionResponse>>, ITransactionCommand;
