using System;

namespace APIGateway;

public static class Extensions
{
    public static string BuildRedirectUrl(this HttpContext context, string? redirectUrl, bool isLogoutRequest = false)
    {
        if (string.IsNullOrEmpty(redirectUrl))
        {
            redirectUrl = "/";
        }

        if (redirectUrl.StartsWith('/'))
        {
            redirectUrl =
                context.Request.Scheme
                + "://"
                + context.Request.Host
                + context.Request.PathBase
                + redirectUrl;
        }

        if (isLogoutRequest)
        {
            redirectUrl = "/";
        }

        return redirectUrl;
    }
}

