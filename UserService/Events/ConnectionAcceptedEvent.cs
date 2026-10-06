using System;

namespace UserService.Events;

public record ConnectionAcceptedEvent(
    string UserId1,
    string UserId2,
    DateTime AcceptedAt);