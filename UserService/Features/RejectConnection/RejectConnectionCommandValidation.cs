using System;
using FluentValidation;

namespace UserService.Features.RejectConnection;

public class RejectConnectionCommandValidation : AbstractValidator<RejectConnectionCommand>
{
    public RejectConnectionCommandValidation()
    {
        RuleFor(x => x.ConnectionId)
            .NotEmpty().WithMessage("Connection ID is required.")
            .Must(BeAValidLong).WithMessage("Connection ID must be a valid long integer.");
    }

    private bool BeAValidLong(string connectionId)
    {
        return long.TryParse(connectionId, out _);
    }

}
