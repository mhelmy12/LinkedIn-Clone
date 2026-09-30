using System;
using FluentValidation;

namespace EngagementService.Features.Reactions.AddOrUpdateReaction;

public class AddOrUpdateReactionValidation : AbstractValidator<AddOrUpdateReactionCommand>
{
    public AddOrUpdateReactionValidation()
    {
        RuleFor(x => x.TargetType)
            .IsInEnum()
            .WithMessage("Invalid target type.");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Invalid reaction type.");

        RuleFor(x => x.TargetId)
            .GreaterThan(0)
            .WithMessage("TargetId must be a positive number.");

    }

}
