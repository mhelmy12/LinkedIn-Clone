using System;
using MediatR;
using Shared.Helpers;
using Elastic.Clients.Elasticsearch;
using SearchService.Consumers;


namespace SearchService.Features.SearchUsers;

public class SearchUsersQueryHandler : ResponseHandler, IRequestHandler<SearchUsersQuery, Response<SearchUsersQueryResponse>>
{
    private readonly ElasticsearchClient elasticClient;

    public SearchUsersQueryHandler(ElasticsearchClient elasticClient)
    {
        this.elasticClient = elasticClient;
    }
    public async Task<Response<SearchUsersQueryResponse>> Handle(SearchUsersQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.query))
        {
            return BadRequest<SearchUsersQueryResponse>("Search query cannot be empty.");
        }


        var response = await elasticClient.SearchAsync<UserSearchDocument>(s => s
            .Indices("users")
            .From((request.page - 1) * request.pageSize)
            .Size(request.pageSize)
            .Query(q => q
                .Bool(b => b
                    .Should(
                        sh => sh.MultiMatch(m => m
                            .Fields(new[] { "firstName", "lastName", "email" })
                            .Query(request.query)
                            .Fuzziness(new Fuzziness("AUTO"))
                        ),
                        sh => sh.Wildcard(w => w
                            .Field(f => f.FirstName)
                            .Value($"*{request.query.ToLower()}*")
                        )
                    )
                )
            )
        );

        var results = response.Documents.ToList();
        var totalHits = response.Total;

        return Success(new SearchUsersQueryResponse
        (
            totalHits,
            request.page,
            request.pageSize,
            results
        ));
    }
}
