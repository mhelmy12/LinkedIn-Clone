using System;

namespace MediaService.Features.GetDownloadUrlByObjectKey;

public record GetDownloadUrlByObjectKeyQueryResponse(
    string DownloadUrl
);