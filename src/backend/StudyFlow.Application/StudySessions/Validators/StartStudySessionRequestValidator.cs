using FluentValidation;
using StudyFlow.Application.StudySessions.DTOs;

namespace StudyFlow.Application.StudySessions.Validators;

public sealed class StartStudySessionRequestValidator : AbstractValidator<StartStudySessionRequest>
{
    public StartStudySessionRequestValidator() { RuleFor(x => x.StudySetId).NotEmpty(); RuleFor(x => x.Mode).IsInEnum(); }
}
