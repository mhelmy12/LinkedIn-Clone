using System;
using System.Text;
using FluentValidation;
using PostService.Helpers;

namespace PostService.Features.GetPostByAuthor;

public class GetPostsByAuthorQueryValidation : AbstractValidator<GetPostsByAuthorQuery>
{
    private const int MaxLimit = 50;

    public GetPostsByAuthorQueryValidation()
    {
        RuleFor(x => x.AuthorId)
    .GreaterThan(0)
    .WithMessage("AuthorId must be positive.");

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, MaxLimit)
            .WithMessage($"Limit must be between 1 and {MaxLimit}.");

        RuleFor(x => x.Cursor)
            .Must(BeValidCursor)
            .When(x => !string.IsNullOrEmpty(x.Cursor))
            .WithMessage("Invalid cursor format.");
    }

    private static bool BeValidCursor(string? cursor)
    {

        return CursorCodec.TryDecode(cursor, out _, out _);
    }

}


