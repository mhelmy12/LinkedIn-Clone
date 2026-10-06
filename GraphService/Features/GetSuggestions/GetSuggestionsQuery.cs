using System;
using MediatR;
using Shared.Helpers;

namespace GraphService.Features.GetSuggestions;

public record GetSuggestionsQuery(int Limit)
    : IRequest<Response<GetSuggestionsResponse>>;