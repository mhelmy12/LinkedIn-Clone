using System;
using FluentValidation;

namespace PostService.Features.GetPostById;

public class GetPostByIdQueryValidation : AbstractValidator<GetPostByIdQuery>
{
    public GetPostByIdQueryValidation()
    {

    }

}
