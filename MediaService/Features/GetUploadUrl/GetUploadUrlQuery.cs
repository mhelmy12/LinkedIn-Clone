using System;
using MediatR;
using Shared.Helpers;

namespace MediaService.Features.GetUploadUrl;

public record GetUploadUrlQuery() : IRequest<Response<GetUploadUrlQueryResponse>>;