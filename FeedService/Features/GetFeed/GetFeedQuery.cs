using System;
using MediatR;
using Shared.Helpers;

namespace FeedService.Features.GetFeed;

public record GetFeedQuery(
    string? Cursor,
    int Limit
) : IRequest<Response<GetFeedResponse>>;