using System;

namespace UserService.Features.SendConnection;

public record SendConnectionCommandResponse(
    long ConnectionId,
    long RequesterId,
    long TargetUserId
);
