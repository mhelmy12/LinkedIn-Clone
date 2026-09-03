using System.Security.Claims;
using System.Threading.RateLimiting;
using APIGateway;
using APIGateway.Transformers;
using APIGateway.UserModule;
using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Yarp.ReverseProxy.Transforms;


var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddOpenIdConnectAccessTokenManagement();

builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(
        CookieAuthenticationDefaults.AuthenticationScheme,
        options =>
        {
            options.Cookie.Name = ".linkedIn.yarp";
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

            // automatically revoke refresh token at signout time
            options.Events.OnSigningOut = async e => await e.HttpContext.RevokeRefreshTokenAsync();
        }
    )
    .AddOpenIdConnect(
        OpenIdConnectDefaults.AuthenticationScheme,
        options =>
        {

            var domain = Environment.GetEnvironmentVariable("services__keycloak__https__0");
            var realm = builder.Configuration.GetValue<string>("OpenIDConnectSettings:Domain");
            // options.Authority = builder.Configuration.GetValue<string>(
            //     "OpenIDConnectSettings:Domain"
            // );
            options.Authority = domain + realm;
            options.ClientId = builder.Configuration.GetValue<string>(
                "OpenIDConnectSettings:ClientId"
            );
            options.ClientSecret = builder.Configuration.GetValue<string>(
                "OpenIDConnectSettings:ClientSecret"
            );

            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.ResponseMode = OpenIdConnectResponseMode.Query;

            options.GetClaimsFromUserInfoEndpoint = true;
            options.SaveTokens = true;
            options.MapInboundClaims = false;
            options.CallbackPath = "/signin-oidc";
            options.RequireHttpsMetadata = builder.Environment.IsProduction();

            options.TokenValidationParameters = new TokenValidationParameters
            {
                NameClaimType = ClaimTypes.NameIdentifier,
                RoleClaimType = ClaimTypes.Role,
            };

            options.Scope.Clear();
            options.Scope.Add(OpenIdConnectScope.OpenIdProfile);
            options.Scope.Add(OpenIdConnectScope.OfflineAccess);

            options.Events = new()
            {

                OnRedirectToIdentityProviderForSignOut = (context) =>
                {

                    var idTokenHint = context.ProtocolMessage.IdTokenHint;

                    var logoutUri = $"{domain}{realm}/protocol/openid-connect/logout" +
                                    $"?client_id={builder.Configuration.GetValue<string>("OpenIDConnectSettings:ClientId")}";

                    if (!string.IsNullOrEmpty(idTokenHint))
                    {
                        logoutUri += $"&id_token_hint={idTokenHint}";
                    }

                    var redirectUrl = context.HttpContext.BuildRedirectUrl(context.Properties.RedirectUri);
                    logoutUri += $"&post_logout_redirect_uri={redirectUrl}";

                    context.Response.Redirect(logoutUri);
                    context.HandleResponse();
                    return Task.CompletedTask;
                },

                OnRedirectToIdentityProvider = (context) =>
                {
                    // Auth0 specific parameter to specify the audience
                    context.ProtocolMessage.SetParameter(
                        "audience",
                        builder.Configuration.GetValue<string>("OpenIDConnectSettings:Audience")
                    );
                    return Task.CompletedTask;
                },
            };
        }
    );

builder.Services.AddAuthorizationBuilder()
 .SetDefaultPolicy(
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build()
    );



builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy(
        "user-or-ip",
        httpContext =>
        {
            var partitionKey =
                httpContext.User.Identity?.IsAuthenticated == true
                    ? httpContext.User.FindFirstValue("name") ?? "anonymous"
                    : httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: partitionKey,
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0,
                }
            );
        }
    );

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddSingleton<AddBearerTokenToHeadersTransform>();

builder
    .Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(builderContext =>
    {
        if (!string.IsNullOrEmpty(builderContext.Route.AuthorizationPolicy))
        {
            builderContext.RequestTransforms.Add(
                builderContext.Services.GetRequiredService<AddBearerTokenToHeadersTransform>()
            );
        }

        builderContext.RequestTransforms.Add(new RequestHeaderRemoveTransform("Cookie"));
    })
    .AddServiceDiscoveryDestinationResolver();













builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGroup("bff").MapUserEndpoints();

app.MapReverseProxy();
app.MapDefaultEndpoints();
app.MapGet("/", () => "Hello YARP!").AllowAnonymous();




app.Run();

