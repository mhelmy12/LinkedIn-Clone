using System;
using EngagementService.Helpers;
using FluentValidation;

namespace EngagementService.Features.Comments.GetCommentReplies;

public class GetCommentRepliesValidation
    : AbstractValidator<GetCommentRepliesQuery>
{
    public const int MaxLimit = 50;

    public GetCommentRepliesValidation()
    {
        RuleFor(x => x.CommentId)
            .GreaterThan(0)
            .WithMessage("CommentId must be a positive number.");

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, MaxLimit)
            .WithMessage($"Limit must be between 1 and {MaxLimit}.");

        RuleFor(x => x.Cursor)
            .Must(BeValidCursor)
            .When(x => !string.IsNullOrEmpty(x.Cursor))
            .WithMessage("Invalid cursor format.");
    }

    private static bool BeValidCursor(string? cursor)
        => CursorCodec.TryDecode(cursor, out _, out _);
}
