using System;
using FluentValidation;

namespace UserService.Features.GetProfile;

public class GetProfileQueryValidation : AbstractValidator<GetProfileQuery>
{
    public GetProfileQueryValidation()
    {

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.")
            .NotNull().WithMessage("UserId cannot be null.");

    }


}
