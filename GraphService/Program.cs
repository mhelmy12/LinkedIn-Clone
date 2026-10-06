using System.Reflection;
using Carter;
using GraphService.Abstractions;
using GraphService.Kafka.Consumers;
using GraphService.Kafka.KafkaConfiguration;
using GraphService.Neo4j;
using Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddSharedInfrastructure([Assembly.GetExecutingAssembly()]);
builder.Services.AddNeo4jDriver(builder.Configuration);


builder.Services.AddScoped<IGraphRepository, Neo4jGraphRepository>();


builder.Services.Configure<KafkaOptions>(
    builder.Configuration.GetSection(KafkaOptions.SectionName));


builder.Services.Configure<GraphOptions>(
    builder.Configuration.GetSection(GraphOptions.SectionName));



builder.Services.AddHostedService<UserProfileUpdatedConsumer>();
builder.Services.AddHostedService<ConnectionAcceptedConsumer>();
builder.Services.AddHostedService<ConnectionRemovedConsumer>();


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

app.MapGet("/graph", () => "Hello Graph Service!").AllowAnonymous();


app.Run();

