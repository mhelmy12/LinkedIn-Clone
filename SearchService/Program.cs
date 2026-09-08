using System.Reflection;
using Carter;
using Elastic.Clients.Elasticsearch;
using SearchService.Consumers;
using Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddSharedInfrastructure([Assembly.GetExecutingAssembly()]);
var elasticConnectionString = builder.Configuration.GetConnectionString("elasticsearch");
var settings = new ElasticsearchClientSettings(new Uri(elasticConnectionString ?? "http://localhost:9200"))
    .DefaultIndex("users");

builder.Services.AddSingleton(new ElasticsearchClient(settings));

builder.Services.AddHostedService<SyncUsersToElasticConsumer>();
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
