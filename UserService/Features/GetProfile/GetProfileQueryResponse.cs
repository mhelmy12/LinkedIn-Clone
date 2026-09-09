using System;

namespace UserService.Features.GetProfile;

public class GetProfileQueryResponse
{
    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? ProfilePictureUrl { get; set; }

    public string? CoverPictureUrl { get; set; }

    public string? About { get; set; }

    public string? Location { get; set; }

    public string? JobTitle { get; set; }
    public string? Headline { get; set; }

}
