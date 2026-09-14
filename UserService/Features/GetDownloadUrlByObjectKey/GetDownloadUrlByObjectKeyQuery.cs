using System;
using MediatR;
using Shared.Helpers;

namespace UserService.Features.GetDownloadUrlByObjectKey;

public record GetDownloadUrlByObjectKeyQuery(
    string ObjectKey
) : IRequest<Response<GetDownloadUrlByObjectKeyQueryResponse>>;