using System;
using FluentValidation;

namespace EngagementService.Features.Reactions.RemoveReaction;

public class RemoveReactionValidation : AbstractValidator<RemoveReactionCommand>
{
    public RemoveReactionValidation()
    {
        RuleFor(x => x.TargetType)
           .IsInEnum()
           .WithMessage("Invalid target type.");

        RuleFor(x => x.TargetId)
            .GreaterThan(0)
            .WithMessage("TargetId must be a positive number.");

    }

}
