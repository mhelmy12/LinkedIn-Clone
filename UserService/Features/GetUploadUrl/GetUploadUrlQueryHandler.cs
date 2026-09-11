using System;
using MediatR;
using Shared.Helpers;
using UserService.Services.CurrentUserService;
using UserService.Services.S3;

namespace UserService.Features.GetUploadUrl;

public class GetUploadUrlQueryHandler(
    [FromKeyedServices("Headers")] ICurrentUserService currentUserService,
    IS3Service s3Service,
    IConfiguration configuration

) : ResponseHandler, IRequestHandler<GetUploadUrlQuery, Response<GetUploadUrlQueryResponse>>
{
    public async Task<Response<GetUploadUrlQueryResponse>> Handle(GetUploadUrlQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized<GetUploadUrlQueryResponse>("User not authenticated.");

        var uploadUrl = s3Service.GeneratePresignedUrlForUpload(currentUserId, "image/webp");

        var serviceUrl = configuration["AWS:ServiceUrl"];
        var bucketName = configuration["AWS:BucketName"];
        var fileKey = $"profile-pictures/{currentUserId}.webp";
        var finalPhotoUrl = string.IsNullOrEmpty(serviceUrl)
            ? $"https://{bucketName}.s3.amazonaws.com/{fileKey}"
            : $"{serviceUrl}/{bucketName}/{fileKey}";



        var response = new GetUploadUrlQueryResponse(uploadUrl, finalPhotoUrl);
        return Success(response);
    }
}
