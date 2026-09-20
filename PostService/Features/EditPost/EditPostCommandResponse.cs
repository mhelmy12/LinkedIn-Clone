using System;

namespace PostService.Features.EditPost;

public record EditPostCommandResponse(
    string PostId,
    DateTimeOffset UpdatedAt,
    string RowVersion,
    bool NoChanges);
