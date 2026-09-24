using FluentValidation;
using StudyFlow.Application.DailyStudy.DTOs;

namespace StudyFlow.Application.DailyStudy.Validators;

public sealed class DailyStudyRequestValidator : AbstractValidator<DailyStudyRequest>
{
    private static readonly string[] FocusValues = ["Balanced", "Written", "Language"];
    public DailyStudyRequestValidator()
    {
        RuleFor(x => x.Minutes).Must(value => value is 0 or 5 or 10 or 15).WithMessage("Minutes must be 5, 10, 15, or 0 for unlimited.");
        RuleFor(x => x.Focus).Must(value => FocusValues.Contains(value, StringComparer.OrdinalIgnoreCase)).WithMessage("Focus must be Balanced, Written, or Language.");
    }
}
