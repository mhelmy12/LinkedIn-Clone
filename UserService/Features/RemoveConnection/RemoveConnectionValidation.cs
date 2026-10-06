using System;
using FluentValidation;

namespace UserService.Features.RemoveConnection;

public class RemoveConnectionValidation : AbstractValidator<RemoveConnectionCommand>
{
    public RemoveConnectionValidation()
    {
        RuleFor(x => x.TargetUserId)
            .NotEmpty().WithMessage("TargetUserId is required.")
            .NotNull().WithMessage("TargetUserId cannot be null.");
    }

}
