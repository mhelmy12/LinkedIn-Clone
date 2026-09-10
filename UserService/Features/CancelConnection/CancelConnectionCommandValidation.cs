using System;
using FluentValidation;

namespace UserService.Features.CancelConnection;

public class CancelConnectionCommandValidation : AbstractValidator<CancelConnectionCommand>
{
    public CancelConnectionCommandValidation()
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

