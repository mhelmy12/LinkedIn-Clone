using System;
using System.Text.Json;
using Confluent.Kafka;
using Elastic.Clients.Elasticsearch;
using SearchService.Events;
using SearchService.Helpers.KafkaConfiguration;

namespace SearchService.Consumers;

public class PostCreatedConsumer : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly ILogger<PostCreatedConsumer> _logger;
    private readonly IConfiguration _config;
    private readonly ElasticsearchClient _elasticsearchClient;

    public PostCreatedConsumer(
    ILogger<PostCreatedConsumer> logger,
    IConfiguration config,
    ElasticsearchClient elasticsearchClient

    )
    {
        _config = config;
        _logger = logger;
        _elasticsearchClient = elasticsearchClient;
        _consumer = KafkaConsumerFactory.Create(
            config.GetValue<string>("Kafka:BootstrapServers")!,
            "post-created-consumer-group"

        );
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe("postService.LinkedInPostDb.dbo.OutboxMessages");


        while (!stoppingToken.IsCancellationRequested)
        {
            var result = _consumer.Consume(TimeSpan.FromMilliseconds(500));
            if (result?.Message?.Value is null)
                continue;

            try
            {
                using var doc = JsonDocument.Parse(result.Message.Value);
                var after = doc.RootElement.GetProperty("after");
                var type = after.GetProperty("Type").GetString();

                if (type != nameof(PostCreatedEvent))
                    continue;

                var payloadJson = after.GetProperty("Payload").GetString()!;
                var postEvent = JsonSerializer.Deserialize<PostCreatedEvent>(
                    payloadJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                _logger.LogInformation($"Received post created event for post {postEvent}");

                if (postEvent is null)
                    continue;

                await _elasticsearchClient.IndexAsync(postEvent, idx => idx
                    .Index("posts")
                    .Id(postEvent.PostId.ToString()));


                _consumer.Commit(result);

                _logger.LogInformation("Indexed post {PostId}", postEvent.PostId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process message");
            }
        }

        _consumer.Close();
    }
}


public class PostDocument
{
    public long Id { get; set; }
    public string AuthorId { get; set; }
    public string? Content { get; set; }
    public List<string> Hashtags { get; set; } = new();
    public List<string> MentionedUserIds { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}