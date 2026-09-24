using FluentValidation;
using StudyFlow.Application.Subjects.DTOs;

namespace StudyFlow.Application.Subjects.Validators;

public sealed class CreateSubjectRequestValidator : AbstractValidator<CreateSubjectRequest>
{
    public CreateSubjectRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}
