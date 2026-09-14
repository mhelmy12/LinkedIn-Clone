using System;
using FluentValidation;

namespace PostService.Features.EditPost;

public class EditPostCommandValidation : AbstractValidator<EditPostCommand>
{
    public EditPostCommandValidation()
    {

    }

}
