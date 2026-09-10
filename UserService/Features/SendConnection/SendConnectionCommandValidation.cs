using System;
using FluentValidation;

namespace UserService.Features.SendConnection;

public class SendConnectionCommandValidation : AbstractValidator<SendConnectionCommand>
{
    public SendConnectionCommandValidation()
    {
        RuleFor(x => x.TargetUserId)
            .NotEmpty().WithMessage("Target user ID is required.")
            .Must(BeAValidLong).WithMessage("Target user ID must be a valid long.");


    }

    private bool BeAValidLong(string targetUserId)
    {
        return long.TryParse(targetUserId, out _);
    }

}
