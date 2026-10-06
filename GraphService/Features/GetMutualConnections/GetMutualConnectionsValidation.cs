using System;
using FluentValidation;

namespace GraphService.Features.GetMutualConnections;

public class GetMutualConnectionsValidation : AbstractValidator<GetMutualConnectionsQuery>
{
    public const int MaxLimit = 50;

    public GetMutualConnectionsValidation()
    {
        RuleFor(x => x.UserId1)
            .NotEmpty()
            .WithMessage("UserId1 is required.")
            .NotNull()
            .WithMessage("UserId1 cannot be null.");

        RuleFor(x => x.UserId2)
            .NotEmpty()
            .WithMessage("UserId2 is required.")
            .NotNull()
            .WithMessage("UserId2 cannot be null.");

        RuleFor(x => x)
            .Must(x => x.UserId1 != x.UserId2)
            .WithMessage("UserId1 and UserId2 must be different.");

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, MaxLimit)
            .WithMessage($"Limit must be between 1 and {MaxLimit}.");
    }

}
