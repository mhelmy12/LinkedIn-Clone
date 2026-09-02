using System;
using MediatR;
using Shared.Helpers;
using UserService.Behaviors;

namespace UserService.Features.RegisterUser;

public class RegisterUserCommand : IRequest<Response<RegisterUserCommandResponse>>, ITransactionCommand
{

    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Location { get; set; }
    public string? Headline { get; set; }


}
