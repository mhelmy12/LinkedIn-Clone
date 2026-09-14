using System;
using FluentValidation;

namespace PostService.Features.CreatePost;

public class CreatePostCommandValidation : AbstractValidator<CreatePostCommand>
{
    public CreatePostCommandValidation()
    {

    }

}
