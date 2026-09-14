using System;

namespace MediaService.Services.CurrentUserService;

public interface ICurrentUserService
{
    public string GetCurrentUserId();

}
