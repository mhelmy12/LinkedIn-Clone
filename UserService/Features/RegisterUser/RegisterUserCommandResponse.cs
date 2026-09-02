using System;

namespace UserService.Features.RegisterUser;

public class RegisterUserCommandResponse
{
    public long UserId { get; set; }

    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public long ExpiresIn { get; set; }

}
