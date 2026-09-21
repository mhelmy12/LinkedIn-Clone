using System;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PostService.Data;
using PostService.Models;
using PostService.Services.CurrentUserService;
using Shared.Helpers;

namespace PostService.Features.GetPostById;

public class GetPostByIdQueryHandler(
        PostDbContext _db,
        [FromKeyedServices("Headers")] ICurrentUserService _currentUserService,
        ILogger<GetPostByIdQueryHandler> _logger
) : ResponseHandler, IRequestHandler<GetPostByIdQuery, Response<GetPostByIdQueryResponse>>
{
    public async Task<Response<GetPostByIdQueryResponse>> Handle(
        GetPostByIdQuery request,
        CancellationToken ct)
    {
        var currentUser = _currentUserService.GetCurrentUserId();

        // 1. Load Post + Children (projection)
        var post = await _db.Posts
            .AsNoTracking()
            .Where(p => p.Id == request.PostId)
            .Select(p => new
            {
                p.Id,
                p.AuthorId,
                p.Content,
                p.Visibility,
                p.RepostOfPostId,
                p.IsDeleted,
                p.CreatedAt,
                p.UpdatedAt,
                Media = p.Media
                    .OrderBy(m => m.DisplayOrder)
                    .Select(m => new MediaDto(
                        m.MediaKey,
                        (int)m.MediaType,
                        m.DisplayOrder))
                    .ToList(),
                MentionedUserIds = p.Mentions
                    .Select(m => new MentionedUserDto(
                        m.MentionedEntityId,
                        m.StartIndex,
                        m.Length
                    ))
                    .Distinct()
                    .ToList(),
                Hashtags = p.PostHashtags
                    .Select(ph => ph.NormalizedName)
                    .ToList()
            })
            .FirstOrDefaultAsync(ct);


        if (post is null || post.IsDeleted)
            return NotFound<GetPostByIdQueryResponse>("Post not found.");

        // 3. Visibility check


        if (post.Visibility == VisibilityType.Private
            && post.AuthorId != currentUser)
        {
            return NotFound<GetPostByIdQueryResponse>("Post not found.");
        }

        // 4. Compute isPureRepost
        var isPureRepost = post.Content is null
                        && post.RepostOfPostId.HasValue;

        // 5. Compute canonicalPostId

        var canonicalPostId = isPureRepost
            ? post.RepostOfPostId!.Value
            : post.Id;

        // 6. Load RepostOf (if applicable)
        RepostOfDto? repostOf = null;

        if (post.RepostOfPostId.HasValue)
        {
            repostOf = await LoadRepostOfAsync(
                post.RepostOfPostId.Value.ToString(),
                currentUser,
                ct);
        }

        var isRepostOfUnavailable = post.RepostOfPostId.HasValue
                                  && repostOf is null;

        // 7. Build DTO
        var response = new GetPostByIdQueryResponse(
            Id: post.Id.ToString(),
            CanonicalPostId: canonicalPostId.ToString(),
            AuthorId: post.AuthorId,
            Content: post.Content,
            Visibility: (int)post.Visibility,
            IsPureRepost: isPureRepost,
            RepostOfPostId: post.RepostOfPostId?.ToString(),
            RepostOf: repostOf,
            IsRepostOfUnavailable: isRepostOfUnavailable,
            Media: post.Media,
            MentionedUserIds: post.MentionedUserIds,
            Hashtags: post.Hashtags,
            CreatedAt: post.CreatedAt,
            UpdatedAt: post.UpdatedAt);

        _logger.LogInformation(
            "GetPostById: {PostId} (canonical: {CanonicalId}, isPureRepost: {IsPure})",
            post.Id, canonicalPostId, isPureRepost);

        return Success(response);
    }


    private async Task<RepostOfDto?> LoadRepostOfAsync(
          string repostOfPostId,
          string currentUser,
          CancellationToken ct)
    {
        var quoted = await _db.Posts
            .AsNoTracking()
            .Where(p => p.Id.ToString() == repostOfPostId)
            .Select(p => new
            {
                p.Id,
                p.AuthorId,
                p.Content,
                p.Visibility,
                p.IsDeleted,
                p.CreatedAt,
                Media = p.Media
                    .OrderBy(m => m.DisplayOrder)
                    .Select(m => new MediaDto(
                        m.MediaKey,
                        (int)m.MediaType,
                        m.DisplayOrder))
                    .ToList(),
                MentionedUserIds = p.Mentions
                    .Select(m => new MentionedUserDto(
                        m.MentionedEntityId,
                        m.StartIndex,
                        m.Length
                    ))
                    .ToList(),
                Hashtags = p.PostHashtags
                    .Select(ph => ph.NormalizedName)
                    .ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (quoted is null || quoted.IsDeleted)
            return null;


        if (quoted.Visibility == VisibilityType.Private
            && quoted.AuthorId != currentUser.ToString())
        {
            return null;
        }

        return new RepostOfDto(
            Id: quoted.Id.ToString(),
            AuthorId: quoted.AuthorId,
            Content: quoted.Content,
            Media: quoted.Media,
            MentionedUserIds: quoted.MentionedUserIds,
            Hashtags: quoted.Hashtags,
            CreatedAt: quoted.CreatedAt);
    }
}