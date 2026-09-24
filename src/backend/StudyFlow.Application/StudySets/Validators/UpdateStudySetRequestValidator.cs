using FluentValidation;
using StudyFlow.Application.StudySets.DTOs;

namespace StudyFlow.Application.StudySets.Validators;

public sealed class UpdateStudySetRequestValidator : AbstractValidator<UpdateStudySetRequest>
{
    public UpdateStudySetRequestValidator() { RuleFor(x => x.Title).NotEmpty().MaximumLength(150); RuleFor(x => x.Description).MaximumLength(2000); }
}
