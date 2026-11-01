using System.Text.Json;
using ChatService.Helpers.KafkaConfiguration;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChatService.Consumers;

/// <summary>
/// Base for User Service projection consumers. Fixes the repo's infinite-retry
/// pattern: offsets commit only after a successful handler run, failures are
/// retried a bounded number of times with backoff, then handed to a
/// dead-letter hook and committed so a poison message cannot stall the topic.
/// </summary>
public abstract class KafkaConsumerBase : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IConsumer<string, string> _consumer;
    private readonly IProducer<string, string> _deadLetterProducer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;

    protected KafkaOptions Options { get; }

    protected KafkaConsumerBase(
        IOptions<KafkaOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger logger)
    {
        Options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;

        var config = new ConsumerConfig
        {
            BootstrapServers = Options.BootstrapServers,
            GroupId = GroupId,
            AutoOffsetReset = Options.AutoOffsetReset,
            EnableAutoCommit = false,
            EnablePartitionEof = true
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
        _deadLetterProducer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = Options.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true
        }).Build();
    }

    /// <summary>Kafka consumer group, unique per projection.</summary>
    protected abstract string GroupId { get; }

    /// <summary>Outbox <c>Type</c> value this consumer handles.</summary>
    protected abstract string EventType { get; }

    /// <summary>Source topic; defaults to the User Service outbox topic.</summary>
    protected virtual string Topic => Options.UserServiceTopic;

    /// <summary>Applies one event inside a fresh DI scope.</summary>
    protected abstract Task HandleEventAsync(
        IServiceProvider services,
        string payloadJson,
        CancellationToken cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(Topic);

        _logger.LogInformation(
            "{Consumer} started. Group={GroupId} Topic={Topic}",
            GetType().Name, GroupId, Topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result;

                try
                {
                    result = _consumer.Consume(
                        TimeSpan.FromMilliseconds(Options.ConsumerPollTimeoutMs));
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Consume error: {Reason}", ex.Error.Reason);
                    continue;
                }
                catch (KafkaException ex)
                {
                    _logger.LogError(ex, "Kafka client error: {Reason}", ex.Error.Reason);
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    continue;
                }

                if (result?.Message?.Value is null)
                {
                    continue;
                }

                await ProcessAsync(result.Message.Value, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    _consumer.Commit(result);
                }
                catch (KafkaException ex)
                {
                    _logger.LogError(
                        ex, "{Consumer} failed to commit offset {Offset}",
                        GetType().Name, result.Offset);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Host shutdown.
        }
        finally
        {
            try
            {
                _consumer.Close();
            }
            catch (KafkaException ex)
            {
                _logger.LogWarning(ex, "{Consumer} failed to close cleanly", GetType().Name);
            }

            _deadLetterProducer.Dispose();
        }
    }

    private async Task ProcessAsync(string rawJson, CancellationToken cancellationToken)
    {
        DebeziumEnvelope? envelope;

        try
        {
            envelope = JsonSerializer.Deserialize<DebeziumEnvelope>(rawJson, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex, "{Consumer} received a malformed Debezium envelope; dead-lettering",
                GetType().Name);

            await DeadLetterAsync(rawJson, ex, cancellationToken);
            return;
        }

        var payload = envelope?.After;
        if (payload is null || string.IsNullOrWhiteSpace(payload.Payload))
        {
            return;
        }

        if (!string.Equals(payload.Type, EventType, StringComparison.Ordinal))
        {
            return;
        }

        var attempts = Math.Max(1, Options.MaxRetryAttempts + 1);

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                await HandleEventAsync(scope.ServiceProvider, payload.Payload, cancellationToken);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (attempt < attempts)
            {
                var delay = TimeSpan.FromMilliseconds(250 * Math.Pow(4, attempt - 1));
                _logger.LogWarning(
                    ex,
                    "{Consumer} attempt {Attempt}/{Attempts} failed; retrying in {Delay} ms",
                    GetType().Name, attempt, attempts, delay.TotalMilliseconds);

                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "{Consumer} exhausted {Attempts} attempts; dead-lettering to {DlqTopic}",
                    GetType().Name, attempts, Options.DeadLetterTopic);

                await DeadLetterAsync(payload.Payload, ex, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Dead-letter hook. The default publishes the failed payload to Kafka and
    /// only returns after broker acknowledgement, so the source offset is not
    /// committed unless the payload is recoverable from the DLQ.
    /// </summary>
    protected virtual async Task DeadLetterAsync(
        string payloadJson,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var result = await _deadLetterProducer.ProduceAsync(
            Options.DeadLetterTopic,
            new Message<string, string>
            {
                Key = EventType,
                Value = payloadJson,
                Headers = new Headers
                {
                    { "consumer", System.Text.Encoding.UTF8.GetBytes(GetType().Name) },
                    { "event-type", System.Text.Encoding.UTF8.GetBytes(EventType) },
                    { "error-type", System.Text.Encoding.UTF8.GetBytes(exception.GetType().FullName ?? exception.GetType().Name) }
                }
            },
            cancellationToken);

        _logger.LogWarning(
            "{Consumer} published failed event to {DlqTopic} at partition {Partition}, offset {Offset}",
            GetType().Name,
            Options.DeadLetterTopic,
            result.Partition.Value,
            result.Offset.Value);
    }
}
