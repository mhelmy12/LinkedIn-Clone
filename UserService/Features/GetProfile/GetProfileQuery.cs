using System;
using MediatR;
using Shared.Helpers;

namespace UserService.Features.GetProfile;

public class GetProfileQuery : IRequest<Response<GetProfileQueryResponse>>
{

    public string UserId { get; set; } = null!;

}
