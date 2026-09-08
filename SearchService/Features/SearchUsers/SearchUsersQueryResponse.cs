using System;

namespace SearchService.Features.SearchUsers;


public record SearchUsersQueryResponse(

    long Total,
    int Page,
    int PageSize,
    object Data
);