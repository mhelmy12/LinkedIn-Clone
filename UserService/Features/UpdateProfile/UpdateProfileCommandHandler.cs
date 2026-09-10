using System;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Helpers;
using UserService.Data;
using UserService.Events;
using UserService.Models;
using UserService.Services.CurrentUserService;

namespace UserService.Features.UpdateProfile;

public class UpdateProfileCommandHandler : ResponseHandler, IRequestHandler<UpdateProfileCommand, Response<UpdateProfileCommandResponse>>
{
    private readonly UserDbContext dbContext;
    private readonly ICurrentUserService currentUserService;
    private readonly ILogger<UpdateProfileCommandHandler> logger;

    public UpdateProfileCommandHandler(
        UserDbContext dbContext,
        [FromKeyedServices("Headers")] ICurrentUserService currentUserService,
        ILogger<UpdateProfileCommandHandler> logger

        )
    {
        this.dbContext = dbContext;
        this.currentUserService = currentUserService;
        this.logger = logger;
    }
    public async Task<Response<UpdateProfileCommandResponse>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();
        logger.LogInformation("Updating profile for user with ID: {UserId}", userId);
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.KeycloakId == userId, cancellationToken);
        logger.LogInformation("User found: {User}", user != null ? "Yes" : "No");

        if (user == null)
        {
            return NotFound<UpdateProfileCommandResponse>("User not found.");
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Location = request.Location;
        user.About = request.About;
        user.Headline = request.Headline;
        user.ProfilePictureUrl = request.ProfilePictureUrl;
        user.Skills = request.Skills ?? new List<string>();
        user.JobTitle = request.JobTitle;



        //publish event to kafka
        //outbox pattern
        var initiatedEvent = new UserProfileUpdatedEvent()
        {
            UserId = user.KeycloakId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Headline = user.Headline,
            JobTitle = user.JobTitle,
            Email = user.Email
        };

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid().ToString(),
            EventType = nameof(UserProfileUpdatedEvent),
            Payload = System.Text.Json.JsonSerializer.Serialize(initiatedEvent),
            OccurredOn = DateTime.UtcNow,
            AggregateId = user.KeycloakId.ToString(),
            AggregateType = nameof(User),
        };

        dbContext.OutboxMessages.Add(outboxMessage);




        return Success<UpdateProfileCommandResponse>(new(), "Profile updated successfully.");
    }
}
