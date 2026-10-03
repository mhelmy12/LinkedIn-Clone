using System;

namespace EngagementService.Features.Comments.EditComment;

using FluentValidation;

public class EditCommentValidation : AbstractValidator<EditCommentCommand>
{
    private const int MaxContentLength = 3000;
    private const int MaxMediaCount = 1;
    private const int MaxMentionCount = 10;

    public EditCommentValidation()
    {
        RuleFor(x => x.CommentId)
            .GreaterThan(0)
            .WithMessage("CommentId must be a positive number.");

        When(x => x.Content is not null, () =>
        {
            RuleFor(x => x.Content!)
                .MaximumLength(MaxContentLength)
                .WithMessage($"Content cannot exceed {MaxContentLength} characters.");
        });

        RuleFor(x => x.Media)
            .Must(m => m is null || m.Count <= MaxMediaCount)
            .WithMessage($"Max {MaxMediaCount} media item per comment.");

        RuleForEach(x => x.Media)
            .Where(x => x is not null)
            .ChildRules(media =>
            {
                media.RuleFor(m => m.ObjectKey).NotEmpty().MaximumLength(500);
                media.RuleFor(m => m.DisplayOrder).GreaterThanOrEqualTo(0);
            });

        RuleFor(x => x.Mentions)
            .Must(m => m is null || m.Count <= MaxMentionCount)
            .WithMessage($"Max {MaxMentionCount} mentions per comment.");

        RuleForEach(x => x.Mentions)
            .Where(x => x is not null)
            .ChildRules(m =>
            {
                m.RuleFor(x => x.MentionedUserId).NotNull().NotEmpty().MaximumLength(100);
                m.RuleFor(x => x.StartIndex).GreaterThanOrEqualTo(0);
                m.RuleFor(x => x.Length).GreaterThan(0);
            });

        RuleFor(x => x)
            .Must(x => x.Content is not null
                    || x.Media is not null
                    || x.Mentions is not null)
            .WithMessage("At least one field must be provided for update.");
    }
}