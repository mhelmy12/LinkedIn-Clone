using MediatR;
using Shared.Helpers;

namespace UserService.Features.GetConnectionStatus;

/// <summary>
/// Internal read used by ChatService to close the eventual-consistency gap
/// between a newly accepted connection and its Kafka projection.
/// </summary>
public class GetConnectionStatusQuery : IRequest<Response<GetConnectionStatusResponse>>
{
    public string OtherUserId { get; set; } = null!;
}
