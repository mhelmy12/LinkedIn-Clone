using System;
using MediatR;
using Shared.Helpers;
using UserService.Behaviors;

namespace UserService.Features.UpdateProfile;

public record UpdateProfileCommand(
    string FirstName,
    string LastName,
    string? Location,
    string? About,
    string? Headline,
    string? ProfilePictureUrl,
    string? JobTitle,
    List<string>? Skills
) : IRequest<Response<UpdateProfileCommandResponse>>, ITransactionCommand;