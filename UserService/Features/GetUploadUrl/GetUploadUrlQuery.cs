using System;
using MediatR;
using Shared.Helpers;

namespace UserService.Features.GetUploadUrl;

public record GetUploadUrlQuery() : IRequest<Response<GetUploadUrlQueryResponse>>;