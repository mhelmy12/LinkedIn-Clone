using System;

namespace UserService.Services.CurrentUserService;

public class CurrentUserFromToken : ICurrentUserService
{
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly ILogger<CurrentUserFromToken> logger;

    public CurrentUserFromToken(IHttpContextAccessor httpContextAccessor , ILogger<CurrentUserFromToken> logger)
    {
        this.httpContextAccessor = httpContextAccessor;
        this.logger = logger;
    }
    public string GetCurrentUserId()
    {
        logger.LogInformation("Getting current user ID from token.");
        var userId = httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;


        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        return userId;
    }
}
