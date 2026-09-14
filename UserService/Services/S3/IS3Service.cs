using System;

namespace UserService.Services.S3;

public interface IS3Service
{
    string GeneratePresignedUrlForUpload(string contentType, string? userId = null, int expirationInMinutes = 5);
    string GeneratePresignedUrlForDownload(string objectKey);
}
