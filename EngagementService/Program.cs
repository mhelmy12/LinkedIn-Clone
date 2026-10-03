using System.Reflection;
using Carter;
using EngagementService.Behaviors;
using EngagementService.Consumers;
using EngagementService.Data;
using IdGen.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Shared.Extensions;
var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddSharedInfrastructure([Assembly.GetExecutingAssembly()], (config) => { config.AddOpenBehavior(typeof(TransactionBehavior<,>)); });



#region Database Configuration
builder.Services.AddDbContext<EngagmentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("EngagementDbConnection")));
#endregion

#region SnowflakeId Generator Configuration
builder.Services.AddIdGen(3);
#endregion

#region Redis Configuration

builder.AddRedisClient("redis");

#endregion

#region  Services Container

builder.Services.AddHostedService<PostDeletedConsumer>();


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


app.MapCarter();

app.MapGet("/engagements", () => "Hello Engagement Service!").AllowAnonymous();

app.Run();
