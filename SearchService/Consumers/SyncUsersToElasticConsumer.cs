using System;
using System.Text.Json;
using Confluent.Kafka;
using Elastic.Clients.Elasticsearch;

namespace SearchService.Consumers;

public class SyncUsersToElasticConsumer : BackgroundService
{
    private readonly ILogger<SyncUsersToElasticConsumer> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ElasticsearchClient _elasticClient;
    public SyncUsersToElasticConsumer(
        ILogger<SyncUsersToElasticConsumer> logger,
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
             GroupId = _configuration.GetValue<string>("Kafka:GroupId"),
             AutoOffsetReset = AutoOffsetReset.Earliest,
             BrokerAddressFamily = BrokerAddressFamily.V4,
         };

         _logger.LogInformation("Kafka consumer configuration: {@Config}", config);
         using var consumer = new ConsumerBuilder<string, string>(config).Build();
         consumer.Subscribe("identityserver_v2.keycloak-db.dbo.USER_ENTITY");

         while (!stoppingToken.IsCancellationRequested)
         {
             _logger.LogInformation("Waiting for messages from Kafka...");
             try
             {
                 var consumeResult = consumer.Consume(stoppingToken);

                 using var document = JsonDocument.Parse(consumeResult.Message.Value);
                 var payload = document.RootElement.GetProperty("payload");
                 _logger.LogInformation("Received message from Kafka: {Message}", payload.ToString());

                 var email = payload.GetProperty("EMAIL").GetString();
                 if (string.IsNullOrEmpty(email))
                 {
                     continue;
                 }
                 var firstName = payload.GetProperty("FIRST_NAME").GetString();
                 var lastName = payload.GetProperty("LAST_NAME").GetString();
                 var keycloakId = payload.GetProperty("ID").GetString();
                 _logger.LogInformation("Received message from Kafka: {Email}, {FirstName}, {LastName}, {KeycloakId}", email, firstName, lastName, keycloakId);
                 //add to elastic

                 var userSearchDocument = new UserSearchDocument
                 {
                     Id = keycloakId,
                     FirstName = firstName ?? "",
                     LastName = lastName ?? "",
                     Email = email
                 };

                 var indexResponse = await _elasticClient.IndexAsync(
                        userSearchDocument,
                        idx => idx.Index("users").Id(keycloakId),
                        stoppingToken
                    );

                 _logger.LogInformation("Indexed document in Elasticsearch: {IndexResponse}", indexResponse);




             }
             catch (OperationCanceledException)
             {
                 break;
             }
             catch (Exception ex)
             {
                 _logger.LogError(ex, "Error processing Kafka message");
             }
         }



     });


    }
}

public class UserSearchDocument
{
    public string Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
}
