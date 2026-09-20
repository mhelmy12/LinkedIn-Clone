using System;
using FluentValidation;

namespace PostService.Features.EditPost;

public class EditPostCommandValidation : AbstractValidator<EditPostCommand>
{
    private const int MaxContentLength = 3000;
    private const int MaxMediaCount = 9;

    public EditPostCommandValidation()
    {
        RuleFor(x => x.PostId)
            .NotEmpty()
            .WithMessage("PostId is required.");

        RuleFor(x => x.RowVersion)
            .NotEmpty()
            .WithMessage("RowVersion is required for concurrency check.")
            .Must(BeValidBase64)
            .WithMessage("RowVersion must be a valid Base64 string.");


        When(x => x.Content is not null, () =>
        {
            RuleFor(x => x.Content!)
                .MaximumLength(MaxContentLength)
                .WithMessage($"Content cannot exceed {MaxContentLength} characters.");
        });

        When(x => x.Media is not null, () =>
        {
            RuleFor(x => x.Media!)
                .Must(m => m.Count <= MaxMediaCount)
                .WithMessage($"Maximum {MaxMediaCount} media items per post.");

            RuleForEach(x => x.Media!).ChildRules(media =>
            {
                media.RuleFor(m => m.MediaKey)
                    .NotEmpty()
                    .MaximumLength(500);

                media.RuleFor(m => m.MediaType)
                    .IsInEnum();

                media.RuleFor(m => m.DisplayOrder)
                    .GreaterThanOrEqualTo(0);
            });
        });

        When(x => x.Mentions is not null, () =>
        {
            RuleForEach(x => x.Mentions!).ChildRules(m =>
            {
                m.RuleFor(x => x.MentionedUserId)
                    .NotEmpty();

                m.RuleFor(x => x.StartIndex)
                    .GreaterThanOrEqualTo(0);

                m.RuleFor(x => x.Length)
                    .GreaterThan(0);
            });
        });

        RuleFor(x => x)
            .Must(HaveAtLeastOneChange)
            .WithMessage("At least one field must be provided for update.");

        RuleFor(x => x)
            .Must(HaveContentOrMedia)
            .WithMessage("Post must have content or at least one media item.");
    }


    private static bool BeValidBase64(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        Span<byte> buffer = stackalloc byte[value.Length];
        return Convert.TryFromBase64String(value, buffer, out _);
    }

    private static bool HaveAtLeastOneChange(EditPostCommand cmd)
    {
        return cmd.Content is not null
            || cmd.Visibility is not null
            || cmd.Media is not null
            || cmd.Mentions is not null;
    }

    private static bool HaveContentOrMedia(EditPostCommand cmd)
    {
        if (cmd.Content is null && cmd.Media is null)
            return true;

        var hasContent = cmd.Content is not null
            ? !string.IsNullOrWhiteSpace(cmd.Content)
            : true;


        var hasMedia = cmd.Media is not null
            ? cmd.Media.Count > 0
            : true;

        return hasContent || hasMedia;
    }

}


