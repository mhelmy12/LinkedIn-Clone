using System;

namespace GraphService.Features.GetMutualConnections;


public record GetMutualConnectionsResponse(
    IReadOnlyList<MutualConnectionDto> MutualConnections);
public record MutualConnectionDto(
    string UserId,
    string DisplayName,
    string? ProfileImageKey,
    string? Headline);
