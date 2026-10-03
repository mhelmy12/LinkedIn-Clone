using System;

namespace EngagementService.Features.Comments.CreateComment;

using FluentValidation;
public class CreateCommentValidation : AbstractValidator<CreateCommentCommand>
{
    private const int MaxContentLength = 3000;
    private const int MaxMediaCount = 1;
    private const int MaxMentionCount = 10;

    public CreateCommentValidation()
    {
        RuleFor(x => x.PostId)
            .GreaterThan(0)
            .WithMessage("PostId must be a positive number.");

        RuleFor(x => x.ParentCommentId)
            .GreaterThan(0)
            .When(x => x.ParentCommentId.HasValue)
            .WithMessage("ParentCommentId must be a positive number.");

        When(x => !string.IsNullOrWhiteSpace(x.Content), () =>
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
                m.RuleFor(x => x.MentionedUserId).NotNull().WithMessage("MentionedUserId cannot be null.");
                m.RuleFor(x => x.StartIndex).GreaterThanOrEqualTo(0);
                m.RuleFor(x => x.Length).GreaterThan(0);
            });

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Content)
                    || (x.Media is not null && x.Media.Count > 0))
            .WithMessage("Comment must have content or media.");
    }
}