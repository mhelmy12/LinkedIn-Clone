using System;

namespace UserService.Features.GetUploadUrl;

public record GetUploadUrlQueryResponse(string UploadUrl, string FinalPhotoUrl);