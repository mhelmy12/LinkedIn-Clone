using System;
using FluentValidation;

namespace PostService.Features.DeletePost;

public class DeletePostCommandValidation : AbstractValidator<DeletePostCommand>
{
    public DeletePostCommandValidation()
    {

    }

}
