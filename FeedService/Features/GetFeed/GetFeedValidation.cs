using System;
using FeedService.Helpers;
using FluentValidation;

namespace FeedService.Features.GetFeed;

public class GetFeedValidation : AbstractValidator<GetFeedQuery>
{
    public const int MaxLimit = 50;
    public const int DefaultLimit = 20;

    public GetFeedValidation()
    {
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, MaxLimit)
            .WithMessage($"Limit must be between 1 and {MaxLimit}.");

        RuleFor(x => x.Cursor)
            .Must(BeValidCursor)
            .When(x => !string.IsNullOrEmpty(x.Cursor))
            .WithMessage("Invalid cursor format.");
    }

    private static bool BeValidCursor(string? cursor)
        => FeedCursorCodec.TryDecode(cursor, out _);
}
