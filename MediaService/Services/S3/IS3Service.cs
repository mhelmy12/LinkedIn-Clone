using System;

namespace MediaService.Services.S3;

public interface IS3Service
{

    public Task<string> GeneratePresignedUrlForUpload(string contentType, string? userId = null, int expirationInMinutes = 5);
    public Task<string> GeneratePresignedUrlForDownload(string objectKey);

}
