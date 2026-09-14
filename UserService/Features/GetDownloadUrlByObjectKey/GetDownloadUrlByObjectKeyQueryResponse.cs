using System;

namespace UserService.Features.GetDownloadUrlByObjectKey;

public record GetDownloadUrlByObjectKeyQueryResponse(
    string DownloadUrl
);