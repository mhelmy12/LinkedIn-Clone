using System;

namespace MediaService.Services.CurrentUserService;

public class CurrentUserFromHeaders : ICurrentUserService
{
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly ILogger<CurrentUserFromHeaders> logger;

    public CurrentUserFromHeaders(IHttpContextAccessor httpContextAccessor, ILogger<CurrentUserFromHeaders> logger)
    {
        this.httpContextAccessor = httpContextAccessor;
        this.logger = logger;
    }

    public string GetCurrentUserId()
    {
        logger.LogInformation("Getting current user ID from headers.");
        var userId = httpContextAccessor.HttpContext?.Request?.Headers["X-User-Id"].ToString();
        logger.LogInformation("Current user ID from headers: {UserId}", userId);

        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        return userId;
    }


}
