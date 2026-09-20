using System;
using FluentValidation;

namespace PostService.Features.DeletePost;

public class DeletePostCommandValidation : AbstractValidator<DeletePostCommand>
{
    public DeletePostCommandValidation()
    {
        RuleFor(x => x.PostId)
           .NotEmpty()
           .WithMessage("PostId is required.");

    }

}
