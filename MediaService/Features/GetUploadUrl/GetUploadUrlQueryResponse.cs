using System;

namespace MediaService.Features.GetUploadUrl;

public record GetUploadUrlQueryResponse(string UploadUrl, string FinalPhotoUrl);