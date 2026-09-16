using System;
using FluentValidation;

namespace PostService.Features.CreatePost;

public class CreatePostCommandValidation : AbstractValidator<CreatePostCommand>
{
    public CreatePostCommandValidation()
    {
        RuleFor(x => x.Content)
           .MaximumLength(3000).WithMessage("Post content cannot exceed 3000 characters.");

        RuleFor(x => x.Visibility)
            .IsInEnum();

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Content) || x.Media.Count > 0)
            .WithMessage("Post must have content or at least one media item.");

        RuleFor(x => x)
            .Must(x => x.QuotedPostId is null || !string.IsNullOrWhiteSpace(x.Content))
            .WithMessage("Quote repost must include content.");

        RuleFor(x => x.Media)
            .Must(m => m.Count <= 9).WithMessage("Maximum 9 media items per post.");

        RuleForEach(x => x.Media).ChildRules(media =>
        {
            media.RuleFor(m => m.MediaKey).NotEmpty().MaximumLength(500);
            media.RuleFor(m => m.DisplayOrder).GreaterThanOrEqualTo(0);
        });

        RuleForEach(x => x.Mentions).ChildRules(m =>
        {
            m.RuleFor(x => x.MentionedUserId).NotEmpty();
            m.RuleFor(x => x.StartIndex).GreaterThanOrEqualTo(0);
            m.RuleFor(x => x.Length).GreaterThan(0);
        });

    }

}
