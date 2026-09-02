using System;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Helpers;
using UserService.Data;
using UserService.Models;
using UserService.Services.UserIdGenerator;

namespace UserService.Features.RegisterUser;

public class RegisterUserCommandHandler : ResponseHandler, IRequestHandler<RegisterUserCommand, Response<RegisterUserCommandResponse>>
{
    private readonly UserDbContext dbContext;
    private readonly IUserIdGenerator userIdGenerator;

    public RegisterUserCommandHandler(UserDbContext dbContext, [FromKeyedServices("Snowflake")] IUserIdGenerator userIdGenerator)
    {
        this.dbContext = dbContext;
        this.userIdGenerator = userIdGenerator;
    }
    public async Task<Response<RegisterUserCommandResponse>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
