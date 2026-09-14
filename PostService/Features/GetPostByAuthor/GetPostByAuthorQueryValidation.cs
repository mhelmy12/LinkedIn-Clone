using System;
using FluentValidation;

namespace PostService.Features.GetPostByAuthor;

public class GetPostByAuthorQueryValidation : AbstractValidator<GetPostByAuthorQuery>
{
    public GetPostByAuthorQueryValidation()
    {

    }

}
