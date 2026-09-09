using System;

namespace UserService.Services.CurrentUserService;

public class CurrentUserFromToken : ICurrentUserService
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public CurrentUserFromToken(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }
    public string GetCurrentUserId()
    {
        var userId = httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        return userId;
    }
}
