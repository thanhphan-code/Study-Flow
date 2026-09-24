using FluentValidation;
using StudyFlow.Application.StudySets.DTOs;

namespace StudyFlow.Application.StudySets.Validators;

public sealed class CreateStudySetRequestValidator : AbstractValidator<CreateStudySetRequest>
{
    public CreateStudySetRequestValidator() { RuleFor(x => x.Title).NotEmpty().MaximumLength(150); RuleFor(x => x.Description).MaximumLength(2000); }
}
