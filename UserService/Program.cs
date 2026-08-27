using System.Reflection;
using Carter;
using IdGen.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Shared.Extensions;
using UserService.Data;
using UserService.Services.UserIdGenerator;



var builder = WebApplication.CreateBuilder(args);
#region Database Configuration
builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("UserDbConnection")));
#endregion


builder.Services.AddSharedInfrastructure([Assembly.GetExecutingAssembly()], (config) => { config.AddOpenBehavior(typeof(TransactionBehavior<,>)); });
#region SnowflakeId Generator Configuration
builder.Services.AddIdGen(1);
#endregion

// #region Redis Configuration
// string redisConnectionString = builder.Configuration.GetConnectionString("Redis")
//                                ?? throw new InvalidOperationException("Redis connection string is missing.");
// builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
// {
//     var configuration = ConfigurationOptions.Parse(redisConnectionString, true);
//     return ConnectionMultiplexer.Connect(configuration);
// });
// #endregion


#region  Services Container
builder.Services.AddKeyedScoped<IUserIdGenerator, UserIdSnowflakeGenerator>("Snowflake");

#endregion


// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
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
app.Run();