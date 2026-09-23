using System;
using FluentValidation;
using PostService.Helpers;

namespace PostService.Features.GetPostReposts;

public class GetPostRepostsQueryValidation : AbstractValidator<GetPostRepostsQuery>
{
    public const int MaxLimit = 50;

    public GetPostRepostsQueryValidation()
    {
        RuleFor(x => long.Parse(x.PostId))
            .GreaterThan(0)
            .WithMessage("PostId must be a positive number.");

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
