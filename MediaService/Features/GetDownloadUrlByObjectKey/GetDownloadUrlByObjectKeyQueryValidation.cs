using System;
using FluentValidation;

namespace MediaService.Features.GetDownloadUrlByObjectKey;

public class GetDownloadUrlByObjectKeyQueryValidation : AbstractValidator<GetDownloadUrlByObjectKeyQuery>
{
    public GetDownloadUrlByObjectKeyQueryValidation()
    {
        RuleFor(x => x.ObjectKey)
            .NotEmpty().WithMessage("ObjectKey is required.")
            .NotNull().WithMessage("ObjectKey cannot be null.");

    }

}
