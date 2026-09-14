using System.Reflection;
using Carter;
using MediaService.Services.CurrentUserService;
using MediaService.Services.S3;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSharedInfrastructure([Assembly.GetExecutingAssembly()]);

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IS3Service, S3Service>();
builder.Services.AddKeyedScoped<ICurrentUserService, CurrentUserFromHeaders>("Headers");


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

app.MapGet("/media", () => "Media Service is running....").AllowAnonymous();



app.Run();
