using System;
using FluentValidation;

namespace PostService.Features.CreateSimpleRepost;

public class CreateSimpleRepostCommandValidation : AbstractValidator<CreateSimpleRepostCommand>
{
    public CreateSimpleRepostCommandValidation()
    {

    }

}
