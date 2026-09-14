using System;
using MediatR;
using Shared.Helpers;

namespace MediaService.Features.GetDownloadUrlByObjectKey;

public record GetDownloadUrlByObjectKeyQuery(
    string ObjectKey
) : IRequest<Response<GetDownloadUrlByObjectKeyQueryResponse>>;