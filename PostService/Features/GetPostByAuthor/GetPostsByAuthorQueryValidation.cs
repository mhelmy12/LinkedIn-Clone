using System;
using System.Text;
using FluentValidation;

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
        if (string.IsNullOrEmpty(cursor)) return true;

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var parts = decoded.Split(':');
            if (parts.Length != 2) return false;

            return long.TryParse(parts[0], out _)
                && long.TryParse(parts[1], out _);
        }
        catch
        {
            return false;
        }
    }

}


