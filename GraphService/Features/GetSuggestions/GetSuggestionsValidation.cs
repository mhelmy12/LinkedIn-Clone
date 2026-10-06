using System;
using FluentValidation;

namespace GraphService.Features.GetSuggestions;

public class GetSuggestionsValidation : AbstractValidator<GetSuggestionsQuery>
{
    private const int MaxLimit = 20;
    public GetSuggestionsValidation()
    {
        RuleFor(x => x.Limit)
           .InclusiveBetween(1, MaxLimit)
           .WithMessage($"Limit must be between 1 and {MaxLimit}.");
    }

}
