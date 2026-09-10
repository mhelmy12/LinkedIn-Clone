using System;
using System.Text.Json;
using Confluent.Kafka;
using Elastic.Clients.Elasticsearch;

namespace SearchService.Consumers;

public class UpdatedProfileUserConsumer : BackgroundService
{
    private readonly ILogger<UpdatedProfileUserConsumer> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ElasticsearchClient _elasticClient;
    public UpdatedProfileUserConsumer(
        ILogger<UpdatedProfileUserConsumer> logger,
        IConfiguration configuration,
        IServiceScopeFactory serviceScopeFactory,
        ElasticsearchClient elasticClient
        )
    {
        _logger = logger;
        _configuration = configuration;
        _serviceScopeFactory = serviceScopeFactory;
        _elasticClient = elasticClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Run(async () =>
     {
         _logger.LogInformation("Consumer subscribed to topic and waiting for messages...");

         var config = new ConsumerConfig
         {
             BootstrapServers = _configuration.GetValue<string>("Kafka:BootstrapServers"),
             GroupId = "UpdatedProfileUserGroup1",
             AutoOffsetReset = AutoOffsetReset.Earliest,
             BrokerAddressFamily = BrokerAddressFamily.V4,
         };

         _logger.LogInformation("Kafka consumer configuration: {@Config}", config);
         using var consumer = new ConsumerBuilder<string, string>(config).Build();
         consumer.Subscribe("outboxmessages.LinkedInUserDb.dbo.OutboxMessages");

         while (!stoppingToken.IsCancellationRequested)
         {
             _logger.LogInformation("Waiting for messages from Kafka...");
             try
             {
                 var consumeResult = consumer.Consume(stoppingToken);
                 consumer.Commit(consumeResult);
                 using var mainDoc = JsonDocument.Parse(consumeResult.Message.Value);

                 string innerPayloadJson = mainDoc.RootElement
                     .GetProperty("payload")
                     .GetProperty("Payload")
                     .GetString()!;

                 using var payloadDoc = JsonDocument.Parse(innerPayloadJson);
                 var payload = payloadDoc.RootElement;
                 _logger.LogInformation("Received message from Kafka: {Message}", payload.ToString());

                 var email = payload.GetProperty("Email").GetString();
                 var firstName = payload.GetProperty("FirstName").GetString();
                 var lastName = payload.GetProperty("LastName").GetString();
                 var keycloakId = payload.GetProperty("UserId").GetString();
                 var headline = payload.GetProperty("Headline").GetString();
                 var jobTitle = payload.GetProperty("JobTitle").GetString();
                 _logger.LogInformation("Received message from Kafka: {Email}, {FirstName}, {LastName}, {KeycloakId} {Headline}, {JobTitle}", email, firstName, lastName, keycloakId, headline, jobTitle);
                 //add to elastic

                 var userSearchDocument = new UserSearchDocument
                 {
                     Id = keycloakId,
                     FirstName = firstName ?? "",
                     LastName = lastName ?? "",
                     Email = email,
                     Headline = headline ?? "",
                     JobTitle = jobTitle ?? ""
                 };

                 var indexResponse = await _elasticClient.IndexAsync(
                        userSearchDocument,
                        idx => idx.Index("users").Id(keycloakId),
                        stoppingToken
                    );

                 _logger.LogInformation("Indexed document in Elasticsearch: {IndexResponse}", indexResponse);




             }
             catch (Exception ex)
             {
                 _logger.LogError(ex, "Error processing Kafka message");
             }
         }



     });


    }
}
