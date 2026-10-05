using System.Reflection;
using Carter;
using FeedService.Behaviors;
using FeedService.Data;
using FeedService.Helpers;
using FeedService.Helpers.KafkaConfiguration;
using FeedService.Kafka.Consumers;
using IdGen.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSharedInfrastructure([Assembly.GetExecutingAssembly()], (config) => { config.AddOpenBehavior(typeof(TransactionBehavior<,>)); });
builder.Services.AddFeddInfrastructure();



#region Database Configuration
builder.Services.AddDbContext<FeedDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("FeedDbConnection")));
#endregion

#region SnowflakeId Generator Configuration
builder.Services.AddIdGen(4);
#endregion

#region Redis Configuration

builder.AddRedisClient("redis");

#endregion

builder.Services.Configure<KafkaOptions>(
    builder.Configuration.GetSection(KafkaOptions.SectionName));

    
builder.Services.AddHostedService<PostCreatedConsumer>();


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


app.UseHttpsRedirection();
app.MapCarter();

app.MapGet("/feed", () => "Hello Feed Service!").AllowAnonymous();



app.Run();
