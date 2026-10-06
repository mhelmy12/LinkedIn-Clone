using System;

namespace GraphService.Features.GetSuggestions;

using GraphService.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Helpers;
using Shared.Services.CurrentUserService;

public class GetSuggestionsQueryHandler(
    IGraphRepository _graphRepository,
    [FromKeyedServices("Headers")] ICurrentUserService _currentUserService,
    ILogger<GetSuggestionsQueryHandler> _logger
) : ResponseHandler, IRequestHandler<GetSuggestionsQuery, Response<GetSuggestionsResponse>>
{
    public async Task<Response<GetSuggestionsResponse>> Handle(
        GetSuggestionsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetCurrentUserId();
        if (userId is null)
            return Unauthorized<GetSuggestionsResponse>("User is not authenticated.");

        var results = await _graphRepository.GetSuggestionsAsync(
            userId,
            request.Limit,
            cancellationToken);

        var items = results
            .Select(r => new SuggestionDto(
                UserId: r.UserId,
                DisplayName: r.DisplayName,
                ProfileImageKey: r.ProfileImageKey,
                Headline: r.Headline,
                MutualCount: r.MutualCount,
                MutualFriendIds: r.MutualFriendIds))
            .ToList();

        _logger.LogInformation(
            "Found {Count} suggestions for user {UserId}",
            items.Count, userId);

        return Success(new GetSuggestionsResponse(items));
    }
}
