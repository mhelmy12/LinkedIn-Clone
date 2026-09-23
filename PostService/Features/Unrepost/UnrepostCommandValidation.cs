using FluentValidation;

namespace PostService.Features.Unrepost;

public class UnrepostCommandValidation : AbstractValidator<UnrepostCommand>
{
    public UnrepostCommandValidation()
    {
        RuleFor(x => x.OriginalPostId)
            .GreaterThan(0)
            .WithMessage("OriginalPostId must be a positive number.");
    }
}