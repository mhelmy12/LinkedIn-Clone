using System;
using EngagementService.Data;
using EngagementService.Enums;
using EngagementService.Helpers;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Helpers;
using Shared.Services.CurrentUserService;

namespace EngagementService.Features.Reactions.GetReactionsByTarget;

public class GetReactionsByTargetHandler(
    EngagmentDbContext _dbContext,
    ILogger<GetReactionsByTargetHandler> _logger
) : ResponseHandler, IRequestHandler<GetReactionsByTargetQuery, Response<GetReactionsByTargetResponse>>
{
    public async Task<Response<GetReactionsByTargetResponse>> Handle(
        GetReactionsByTargetQuery request,
        CancellationToken cancellationToken)
    {
        DateTime? cursorCreatedAt = null;
        long? cursorId = null;

        if (!string.IsNullOrEmpty(request.Cursor))
        {
            if (!CursorCodec.TryDecode(request.Cursor, out var dt, out var id))
                return BadRequest<GetReactionsByTargetResponse>("Invalid cursor.");

            cursorCreatedAt = dt;
            cursorId = id;
        }

        var query = _dbContext.Reactions
            .AsNoTracking()
            .Where(r => r.TargetType == request.TargetType)
            .Where(r => r.TargetId == request.TargetId);

        if (request.Type.HasValue)
        {
            query = query.Where(r => r.Type == request.Type.Value);
        }

        // keyset cursor
        if (cursorCreatedAt.HasValue && cursorId.HasValue)
        {
            query = query.Where(r =>
                r.CreatedAt < cursorCreatedAt.Value
                || (r.CreatedAt == cursorCreatedAt.Value
                    && r.Id < cursorId.Value));
        }

        var rows = await query
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id)
            .Take(request.Limit + 1)
            .Select(r => new
            {
                r.Id,
                r.UserId,
                r.Type,
                r.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > request.Limit;
        if (hasMore)
            rows.RemoveAt(rows.Count - 1);

        string? nextCursor = null;
        if (hasMore && rows.Count > 0)
        {
            var last = rows[^1];
            nextCursor = CursorCodec.Encode(last.CreatedAt, last.Id.ToString());
        }

        var items = rows
            .Select(r => new ReactionItemDto(
                ReactionId: r.Id.ToString(),
                UserId: r.UserId,
                Type: (int)r.Type,
                CreatedAt: r.CreatedAt))
            .ToList();

        _logger.LogInformation(
            "GetReactionsByTarget: {TargetType}:{TargetId} → {Count} reactions (type filter: {Type}, hasMore: {HasMore})",
            request.TargetType, request.TargetId, items.Count, request.Type, hasMore);

        return Success(new GetReactionsByTargetResponse(
            Items: items,
            NextCursor: nextCursor,
            HasMore: hasMore));
    }
}
