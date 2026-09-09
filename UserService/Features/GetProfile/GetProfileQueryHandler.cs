using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Helpers;
using UserService.Data;

namespace UserService.Features.GetProfile;

public class GetProfileQueryHandler : ResponseHandler, IRequestHandler<GetProfileQuery, Response<GetProfileQueryResponse>>
{
    private readonly UserDbContext _dbContext;

    public GetProfileQueryHandler(UserDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Response<GetProfileQueryResponse>> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        var profile = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.KeycloakId == request.UserId)
            .Select(user => new GetProfileQueryResponse
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                ProfilePictureUrl = user.ProfilePictureUrl,
                Location = user.Location,
                About = user.About,
                JobTitle = user.JobTitle,
                Headline = user.Headline
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (profile == null)
        {
            return NotFound<GetProfileQueryResponse>("User not found");
        }

        return Success(profile);
    }
}