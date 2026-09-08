using System;
using MediatR;
using Shared.Helpers;
namespace SearchService.Features.SearchUsers;

public record SearchUsersQuery(

    string query,
    int page = 1,
    int pageSize = 10

) : IRequest<Response<SearchUsersQueryResponse>>;

