namespace ChatService.Events;

/// <summary>
/// Copied from the User Service payload (Constitution V — additive-only
/// changes). Both ids are Keycloak <c>sub</c> values.
/// </summary>
public record ConnectionRemovedEvent(
    string UserId1,
    string UserId2,
    DateTime RemovedAt);
