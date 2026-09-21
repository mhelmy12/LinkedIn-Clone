using System;
using FluentValidation;

namespace PostService.Features.GetPostById;

public class GetPostByIdQueryValidation : AbstractValidator<GetPostByIdQuery>
{
    public GetPostByIdQueryValidation()
    {
        RuleFor(x => x.PostId)
            .GreaterThan(0)
            .WithMessage("Post id must be greater than zero.");
    }
}
