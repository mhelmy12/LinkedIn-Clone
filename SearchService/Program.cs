using System.Reflection;
using Carter;
using Elastic.Clients.Elasticsearch;
using SearchService.Consumers;
using SearchService.Helpers.ElasticsearchConfigurations;
using SearchService.Indexes;
using SearchService.Infrastructure.Kafka.Consumers;
using Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddSharedInfrastructure([Assembly.GetExecutingAssembly()]);
// var elasticConnectionString = builder.Configuration.GetConnectionString("elasticsearch");
// var settings = new ElasticsearchClientSettings(new Uri(elasticConnectionString ?? "http://localhost:9200"))
//     .DefaultIndex("users");

// builder.Services.AddSingleton(new ElasticsearchClient(settings));

builder.Services.AddElasticsearchClient(builder.Configuration);
builder.Services.AddHostedService<SearchIndexesInitializer>();


builder.Services.AddScoped<IPostIndexer, PostIndexer>();
builder.Services.AddScoped<ICommentIndexer, CommentIndexer>();

builder.Services.AddHostedService<SyncUsersToElasticConsumer>();
builder.Services.AddHostedService<UpdatedProfileUserConsumer>();
builder.Services.AddHostedService<PostCreatedConsumer>();
builder.Services.AddHostedService<PostUpdatedConsumer>();
builder.Services.AddHostedService<CommentCreatedConsumer>();
builder.Services.AddHostedService<CommentUpdatedConsumer>();
builder.Services.AddHostedService<CommentDeletedConsumer>();

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
