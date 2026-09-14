using System;
using FluentValidation;

namespace PostService.Features.CreateQuoteRepost;

public class CreateQuoteRepostCommandValidation : AbstractValidator<CreateQuoteRepostCommand>
{
    public CreateQuoteRepostCommandValidation()
    {

    }

}
