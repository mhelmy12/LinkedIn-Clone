using System;

namespace UserService.Features.AcceptConnection;

public record AcceptConnectionCommandResponse(
    string ConnectionId,
    string RequesterId,
    string TargetId,
    string Status
);
