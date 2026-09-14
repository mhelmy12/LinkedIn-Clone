using System;
using FluentValidation;

namespace PostService.Features.DeleteSimpleRepost;

public class DeleteSimpleRepostCommandValidation : AbstractValidator<DeleteSimpleRepostCommand>
{
    public DeleteSimpleRepostCommandValidation()
    {

    }

}
