namespace ChatService.Events;

/// <summary>
/// Copied from the User Service payload (Constitution V — additive-only
/// changes). Both ids are Keycloak <c>sub</c> values.
/// </summary>
public record ConnectionAcceptedEvent(
    string UserId1,
    string UserId2,
    DateTime AcceptedAt);
