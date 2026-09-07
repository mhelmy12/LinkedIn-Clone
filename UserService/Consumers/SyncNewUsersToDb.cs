using System;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.Models;
using UserService.Services.UserIdGenerator;

namespace UserService.Consumers;

public class SyncNewUsersToDb(
    IConfiguration _configuration,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<SyncNewUsersToDb> logger,
    [FromKeyedServices("Snowflake")] IUserIdGenerator userIdGenerator) : BackgroundService
{


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {



        await Task.Run(async () =>
        {
            logger.LogInformation("Consumer subscribed to topic and waiting for messages...");

            var config = new ConsumerConfig
            {
                BootstrapServers = _configuration.GetValue<string>("Kafka:BootstrapServers"),
                GroupId = "user-sync-group",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            logger.LogInformation("Kafka consumer configuration: {@Config}", config);
            using var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe("identityserver_v2.keycloak-db.dbo.USER_ENTITY");

            while (!stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Waiting for messages from Kafka...");
                try
                {
                    var consumeResult = consumer.Consume(stoppingToken);

                    using var document = JsonDocument.Parse(consumeResult.Message.Value);
                    var payload = document.RootElement.GetProperty("payload");
                    logger.LogInformation("Received message from Kafka: {Message}", payload.ToString());

                    var email = payload.GetProperty("EMAIL").GetString();
                    if (string.IsNullOrEmpty(email))
                    {
                        continue;
                    }
                    var firstName = payload.GetProperty("FIRST_NAME").GetString();
                    var lastName = payload.GetProperty("LAST_NAME").GetString();
                    var keycloakId = payload.GetProperty("ID").GetString();



                    logger.LogInformation("Received message from Kafka: {Email}, {FirstName}, {LastName}, {KeycloakId}", email, firstName, lastName, keycloakId);
                    //add to db




                    using var scope = serviceScopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();
                    var existingUser = await dbContext.Users.FirstOrDefaultAsync(u => u.KeycloakId == keycloakId, stoppingToken);

                    if (existingUser == null)
                    {
                        var user = new User
                        {
                            Id = long.Parse(userIdGenerator.Generate()),
                            KeycloakId = keycloakId!,
                            Email = email,
                            FirstName = firstName ?? "",
                            LastName = lastName ?? "",
                            CreatedAt = DateTime.UtcNow,
                            Role = "User"
                        };
                        dbContext.Users.Add(user);
                        logger.LogInformation("Added new user from Keycloak with ID: {Id}", keycloakId);
                    }
                    else
                    {
                        existingUser.Email = email;
                        existingUser.FirstName = firstName ?? "";
                        existingUser.LastName = lastName ?? "";

                        dbContext.Users.Update(existingUser);
                        logger.LogInformation("Updated existing user from Keycloak with ID: {Id}", keycloakId);
                    }

                    await dbContext.SaveChangesAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing Kafka message");
                }
            }



        });
    }
}
