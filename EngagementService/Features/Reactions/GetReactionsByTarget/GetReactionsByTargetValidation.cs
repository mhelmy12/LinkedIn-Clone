using System;
using EngagementService.Helpers;
using FluentValidation;

namespace EngagementService.Features.Reactions.GetReactionsByTarget;

public class GetReactionsByTargetValidation : AbstractValidator<GetReactionsByTargetQuery>
{
    public const int MaxLimit = 50;

    public GetReactionsByTargetValidation()
    {
        RuleFor(x => x.TargetType)
            .IsInEnum()
            .WithMessage("Invalid target type.");

        RuleFor(x => x.TargetId)
            .GreaterThan(0)
            .WithMessage("TargetId must be a positive number.");

        RuleFor(x => x.Type)
            .IsInEnum()
            .When(x => x.Type.HasValue)
            .WithMessage("Invalid reaction type.");

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
