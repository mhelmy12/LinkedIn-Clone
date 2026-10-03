using System;
using FluentValidation;

namespace EngagementService.Features.Comments.DeleteComment;

public class DeleteCommentValidation : AbstractValidator<DeleteCommentCommand>
{
    public DeleteCommentValidation()
    {
        RuleFor(x => x.CommentId)
           .GreaterThan(0)
           .WithMessage("CommentId must be a positive number.");

    }

}
