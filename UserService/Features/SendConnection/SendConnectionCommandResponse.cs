using System;

namespace UserService.Features.SendConnection;

public record SendConnectionCommandResponse(
    string ConnectionId,
    string RequesterId,
    string TargetUserId
);
