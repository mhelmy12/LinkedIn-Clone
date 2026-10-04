using System;

namespace SearchService.Features.SearchPosts;

using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Clients.Elasticsearch.QueryDsl;
using MediatR;
using Microsoft.Extensions.Logging;
using SearchService.Domain;
using SearchService.Helpers;
using Shared.Helpers;


public class SearchPostsHandler(
    ElasticsearchClient _es,
    ILogger<SearchPostsHandler> _logger
) : ResponseHandler, IRequestHandler<SearchPostsQuery, Response<SearchPostsResponse>>
{
    public async Task<Response<SearchPostsResponse>> Handle(
        SearchPostsQuery request,
        CancellationToken cancellationToken)
    {
        SearchCursor? cursor = null;

        if (!string.IsNullOrEmpty(request.Cursor))
        {
            if (!SearchCursorCodec.TryDecode(request.Cursor, out cursor))
                return BadRequest<SearchPostsResponse>("Invalid cursor.");
        }

        var hasQuery = !string.IsNullOrWhiteSpace(request.Query);

        var response = await _es.SearchAsync<PostDocument>(s =>
        {
            s.Indices("posts");
            s.Size(request.Limit + 1);

            s.Query(q => q
                .Bool(b =>
                {
                    if (!string.IsNullOrEmpty(request.AuthorId))
                    {
                        b.Filter(f => f
                            .Term(t => t
                                .Field(ff => ff.AuthorId)
                                .Value(request.AuthorId)));
                    }

                    if (!string.IsNullOrWhiteSpace(request.Hashtag))
                    {
                        var normalized = request.Hashtag.TrimStart('#').ToLowerInvariant();

                        b.Filter(f => f
                            .Term(t => t
                                .Field(ff => ff.Hashtags)
                                .Value(normalized)));
                    }

                    if (hasQuery)
                    {
                        b.Must(m => m
                            .MultiMatch(mm => mm
                                .Query(request.Query!)
                                .Fields(new[]
                                {
                                    "content^3",
                                    "authorDisplayName^2",
                                    "authorHeadline",
                                    "repostOfContent",
                                    "hashtags^2"
                                })
                                .Type(TextQueryType.BestFields)
                                .Fuzziness(new Fuzziness("AUTO"))));
                    }
                    else
                    {
                        b.Must(m => m.MatchAll());
                    }
                }));

            if (hasQuery)
            {
                s.Sort(sort => sort.Score(so => so.Order(SortOrder.Desc)));
            }
            s.Sort(sort => sort
                .Field(f => f
                    .Field(ff => ff.CreatedAt)
                    .Order(SortOrder.Desc)));
            s.Sort(sort => sort
                .Field(f => f
                    .Field(ff => ff.Id)
                    .Order(SortOrder.Desc)));

            if (cursor is not null)
            {
                var sortValues = new List<FieldValue>();
                if (hasQuery && cursor.Score.HasValue)
                {
                    sortValues.Add(FieldValue.Double(cursor.Score.Value));
                }
                sortValues.Add(FieldValue.String(cursor.CreatedAt.ToString("O")));
                sortValues.Add(FieldValue.Long(cursor.Id));

                s.SearchAfter(sortValues.ToArray());
            }

            s.SourceIncludes(
                doc => doc.Id,
                doc => doc.AuthorId,
                doc => doc.Hashtags,
                doc => doc.MentionedUserIds,
                doc => doc.CreatedAt,
                doc => doc.UpdatedAt);
        }, cancellationToken);

        if (!response.IsValidResponse)
        {
            _logger.LogError(
                "Elasticsearch query failed: {Debug}",
                response.DebugInformation);

            return BadRequest<SearchPostsResponse>("Search service unavailable.");
        }

        var hits = response.Hits.ToList();
        var hasMore = hits.Count > request.Limit;
        if (hasMore)
            hits.RemoveAt(hits.Count - 1);

        string? nextCursor = null;
        if (hasMore && hits.Count > 0)
        {
            var last = hits[^1];
            var lastScore = hasQuery ? last.Score : null;

            nextCursor = SearchCursorCodec.Encode(new SearchCursor
            {
                Score = lastScore,
                CreatedAt = last.Source!.CreatedAt,
                Id = last.Source!.Id
            });
        }

        var items = hits
            .Select(h => h.Source!)
            .Select(d => new PostSearchResultDto(
                Id: d.Id,
                AuthorId: d.AuthorId,
                Content: d.Content,
                Hashtags: d.Hashtags,
                MentionedUserIds: d.MentionedUserIds,
                CreatedAt: d.CreatedAt,
                UpdatedAt: d.UpdatedAt))
            .ToList();

        var totalHits = response.Total;

        _logger.LogInformation(
            "SearchPosts: q='{Query}' → {Count} results (total: {Total}, hasMore: {HasMore})",
            request.Query ?? "(no query)", items.Count, totalHits, hasMore);

        return Success(new SearchPostsResponse(
            Items: items,
            NextCursor: nextCursor,
            TotalHits: totalHits));
    }
}