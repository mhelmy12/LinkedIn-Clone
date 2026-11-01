using System.Reflection;
using Carter;
using ChatService.Consumers;
using ChatService.Data;
using ChatService.Helpers.KafkaConfiguration;
using ChatService.Hubs;
using ChatService.Infrastructure;
using ChatService.Services;
using IdGen.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSharedInfrastructure([Assembly.GetExecutingAssembly()]);

#region Database Configuration
builder.Services.AddDbContext<ChatDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ChatDbConnection")));
#endregion

#region SnowflakeId Generator Configuration
builder.Services.AddIdGen(builder.Configuration.GetValue("Chat:SnowflakeWorkerId", 5));
#endregion

#region Redis / SignalR Configuration
builder.AddRedisClient("redis");

var redisConnectionString = builder.Configuration.GetConnectionString("redis");
var signalR = builder.Services.AddSignalR();

signalR.AddJsonProtocol(options =>
    options.PayloadSerializerOptions.Converters.Add(new LongToStringJsonConverter()));

if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    signalR.AddStackExchangeRedis(redisConnectionString);
}
#endregion

#region JSON (Snowflake ids as strings)
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new LongToStringJsonConverter()));
#endregion

#region UserService fallback client
builder.Services.AddMemoryCache();

builder.Services.AddHttpClient<IUserDirectoryClient, UserDirectoryClient>(client =>
{
    client.BaseAddress = new Uri("http://user-service");
    client.Timeout = TimeSpan.FromSeconds(
        builder.Configuration.GetValue("Chat:UserDirectory:TimeoutSeconds", 3));
});

builder.Services.AddScoped<ConversationAccessService>();
#endregion

#region Authentication (gateway-injected identity)
// The API Gateway is the authentication boundary: it validates the Keycloak
// session and YARP injects X-User-Id (the Keycloak sub) on the forwarded
// request, including the SignalR upgrade. Trusting that header is the same
// model the REST services use (research R2); the scheme exists so SignalR can
// populate Context.UserIdentifier and enforce authorization on the hub.
builder.Services
    .AddAuthentication(GatewayHeaderAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, GatewayHeaderAuthenticationHandler>(
        GatewayHeaderAuthenticationHandler.SchemeName, _ => { });

builder.Services.AddAuthorization();

builder.Services.AddScoped<CurrentChatUserResolver>();
builder.Services.AddScoped<ChatGroupManager>();
builder.Services.AddSingleton<IUserIdProvider, ChatUserIdProvider>();
#endregion

#region CORS (SignalR client origins)
var corsOrigins = builder.Configuration
    .GetSection("Chat:Cors:Origins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("ChatCors", policy =>
    {
        if (corsOrigins.Length == 0)
        {
            // No configured origins: keep same-origin clients working and
            // reject cross-origin requests rather than silently allowing all.
            policy.SetIsOriginAllowed(_ => false);
        }
        else
        {
            policy
                .WithOrigins(corsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});
#endregion

#region Kafka Projection Consumers
builder.Services.Configure<KafkaOptions>(
    builder.Configuration.GetSection(KafkaOptions.SectionName));

builder.Services.AddHostedService<ConnectionAcceptedConsumer>();
builder.Services.AddHostedService<ConnectionRemovedConsumer>();
builder.Services.AddHostedService<UserProfileUpdatedConsumer>();
#endregion

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUi(options =>
    {
        options.DocumentPath = "openapi/v1.json";
    });
}

app.UseCors("ChatCors");
app.UseAuthentication();
app.UseAuthorization();

app.MapCarter();

app.MapHub<ChatHub>(ChatHub.Route).RequireAuthorization();

app.MapDefaultEndpoints();

app.MapGet("/chat", () => "Hello Chat Service!").AllowAnonymous();

app.Run();

public partial class Program;
