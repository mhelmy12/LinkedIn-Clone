using System;

namespace SearchService.Features.SearchPosts;

using FluentValidation;
using SearchService.Helpers;

public class SearchPostsValidation : AbstractValidator<SearchPostsQuery>
{
    public const int MaxLimit = 50;

    public SearchPostsValidation()
    {
        RuleFor(x => x)
            .Must(x =>
                !string.IsNullOrWhiteSpace(x.Query)
                || string.IsNullOrWhiteSpace(x.AuthorId)
                || !string.IsNullOrWhiteSpace(x.Hashtag))
            .WithMessage("At least one of 'q', 'authorId', or 'hashtag' is required.");

        RuleFor(x => x.Query)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Query));

        RuleFor(x => x.Hashtag)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.Hashtag));

        RuleFor(x => x.AuthorId)
            .NotEmpty().When(x => !string.IsNullOrEmpty(x.AuthorId));

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, MaxLimit)
            .WithMessage($"Limit must be between 1 and {MaxLimit}.");

        RuleFor(x => x.Cursor)
            .Must(BeValidCursor)
            .When(x => !string.IsNullOrEmpty(x.Cursor))
            .WithMessage("Invalid cursor format.");
    }

    private static bool BeValidCursor(string? cursor)
        => SearchCursorCodec.TryDecode(cursor, out _);
}