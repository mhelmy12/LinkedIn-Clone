using System;
using MediatR;
using Shared.Helpers;
using UserService.Services.S3;

namespace UserService.Features.GetDownloadUrlByObjectKey;

public class GetDownloadUrlByObjectKeyQueryHandler : ResponseHandler, IRequestHandler<GetDownloadUrlByObjectKeyQuery, Response<GetDownloadUrlByObjectKeyQueryResponse>>
{
    private readonly IS3Service _s3Service;

    public GetDownloadUrlByObjectKeyQueryHandler(IS3Service s3Service)
    {
        _s3Service = s3Service;
    }

    public async Task<Response<GetDownloadUrlByObjectKeyQueryResponse>> Handle(GetDownloadUrlByObjectKeyQuery request, CancellationToken cancellationToken)
    {
        var downloadUrl = _s3Service.GeneratePresignedUrlForDownload(request.ObjectKey);
        var response = new GetDownloadUrlByObjectKeyQueryResponse(downloadUrl);
        return Success(response);
    }
}
