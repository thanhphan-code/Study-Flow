using FluentValidation;
using StudyFlow.Application.Progress.DTOs;

namespace StudyFlow.Application.Progress.Validators;

public sealed class ReviewFlashcardRequestValidator : AbstractValidator<ReviewFlashcardRequest>
{
    public ReviewFlashcardRequestValidator()
    {
        RuleFor(x => x.Rating).IsInEnum();
        RuleFor(x => x.Mode).IsInEnum().When(x => x.Mode.HasValue);
        RuleFor(x => x.AttemptType).IsInEnum().When(x => x.AttemptType.HasValue);
        RuleFor(x => x.Direction).IsInEnum().When(x => x.Direction.HasValue);
        RuleFor(x => x.SubmittedAnswer).MaximumLength(5000);
        RuleFor(x => x.Confidence).InclusiveBetween(1, 5).When(x => x.Confidence.HasValue);
        RuleFor(x => x.ResponseTimeMs).InclusiveBetween(0, 3_600_000).When(x => x.ResponseTimeMs.HasValue);
    }
}
