using System;
using FluentValidation;

namespace SearchService.Features.SearchUsers;

public class SearchUsersQueryValidation : AbstractValidator<SearchUsersQuery>
{

    public SearchUsersQueryValidation()
    {
        RuleFor(x => x.query)
            .NotEmpty().WithMessage("Search query cannot be empty.")
            .MaximumLength(100).WithMessage("Search query cannot exceed 100 characters.");

        RuleFor(x => x.page)
            .GreaterThan(0).WithMessage("Page number must be greater than 0.");

        RuleFor(x => x.pageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");
    }


}
