using System;
using FluentValidation;

namespace EngagementService.Features.Reactions.GetUserReaction;

public class GetUserReactionValidation
    : AbstractValidator<GetUserReactionQuery>
{
    public GetUserReactionValidation()
    {
        RuleFor(x => x.TargetType)
            .IsInEnum()
            .WithMessage("Invalid target type.");

        RuleFor(x => x.TargetId)
            .GreaterThan(0)
            .WithMessage("TargetId must be a positive number.");
    }
}
