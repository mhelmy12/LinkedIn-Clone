using System;

namespace UserService.Events;

public record ConnectionRemovedEvent(
    string UserId1,
    string UserId2,
    DateTime RemovedAt);