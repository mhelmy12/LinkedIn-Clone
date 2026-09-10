using System;

namespace UserService.Events;

public class UserProfileUpdatedEvent
{

    public string UserId { get; set; } = null!;

    public string? FirstName { get; set; } = null!;

    public string? LastName { get; set; } = null!;

    public string? Headline { get; set; } = null!;

    public string? JobTitle { get; set; } = null!;

    public string? Email { get; set; } = null!;

}
