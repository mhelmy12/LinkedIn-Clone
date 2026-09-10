using System;
using FluentValidation;

namespace UserService.Features.AcceptConnection;

public class AcceptConnectionCommandValidation : AbstractValidator<AcceptConnectionCommand>
{
    public AcceptConnectionCommandValidation()
    {
        RuleFor(x => x.ConnectionId)
            .NotEmpty().WithMessage("Connection ID is required.")
            .Must(id => long.TryParse(id, out _)).WithMessage("Connection ID must be a valid long integer.");

    }

}
