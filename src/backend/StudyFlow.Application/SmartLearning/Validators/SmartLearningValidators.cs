using FluentValidation;
using StudyFlow.Application.SmartLearning.DTOs;
using StudyFlow.Domain.Enums;

namespace StudyFlow.Application.SmartLearning.Validators;

public sealed class StartSmartLearnRequestValidator : AbstractValidator<StartSmartLearnRequest>
{
    public StartSmartLearnRequestValidator() => RuleFor(x => x.ItemCount).InclusiveBetween(5, 50);
}

public sealed class RecordLearningAttemptRequestValidator : AbstractValidator<RecordLearningAttemptRequest>
{
    public RecordLearningAttemptRequestValidator()
    {
        RuleFor(x => x.ClientAttemptId).NotEmpty(); RuleFor(x => x.FlashcardId).NotEmpty(); RuleFor(x => x.AttemptType).Must(x => x is LearningAttemptType.Recall or LearningAttemptType.MultipleChoice).WithMessage("Only recall and multiple-choice attempts are supported."); RuleFor(x => x.Direction).IsInEnum(); RuleFor(x => x.SubmittedAnswer).NotEmpty().MaximumLength(5000); RuleFor(x => x.Confidence).InclusiveBetween(1, 5); RuleFor(x => x.ResponseTimeMs).InclusiveBetween(0, 3_600_000);
    }
}
