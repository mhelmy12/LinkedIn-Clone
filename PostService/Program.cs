using System.Reflection;
using Carter;
using IdGen.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PostService.Behaviors;
using PostService.Data;
using PostService.Services.CurrentUserService;
using PostService.Services.UserIdGenerator;
using Shared.Extensions;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddSharedInfrastructure([Assembly.GetExecutingAssembly()], (config) => { config.AddOpenBehavior(typeof(TransactionBehavior<,>)); });



#region Database Configuration
builder.Services.AddDbContext<PostDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("UserDbConnection")));
#endregion

#region SnowflakeId Generator Configuration
builder.Services.AddIdGen(2);
#endregion

#region Redis Configuration
string redisConnectionString = builder.Configuration.GetConnectionString("Redis")
                               ?? throw new InvalidOperationException("Redis connection string is missing.");
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = ConfigurationOptions.Parse(redisConnectionString, true);
    return ConnectionMultiplexer.Connect(configuration);
});
#endregion


#region  Services Container
builder.Services.AddKeyedSingleton<IUserIdGenerator, UserIdSnowflakeGenerator>("Snowflake");
builder.Services.AddHttpContextAccessor();
builder.Services.AddKeyedScoped<ICurrentUserService, CurrentUserFromHeaders>("Headers");

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

app.UseExceptionHandler();
app.MapCarter();

app.MapGet("/posts", () => "Hello Post Service!").AllowAnonymous();

app.Run();
