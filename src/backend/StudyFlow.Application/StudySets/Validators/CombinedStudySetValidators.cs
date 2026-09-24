using FluentValidation;
using StudyFlow.Application.StudySets.DTOs;

namespace StudyFlow.Application.StudySets.Validators;
public sealed class CreateCombinedStudySetRequestValidator : AbstractValidator<CreateCombinedStudySetRequest>
{
    public CreateCombinedStudySetRequestValidator() { RuleFor(x => x.Title).NotEmpty().MaximumLength(150); RuleFor(x => x.Description).MaximumLength(2000); RuleFor(x => x.SourceStudySetIds).NotNull().Must(ValidSources).WithMessage("Choose between 2 and 20 unique source study sets."); }
    private static bool ValidSources(IReadOnlyList<Guid> values) => values.Count is >= 2 and <= 20 && values.Distinct().Count() == values.Count;
}
public sealed class UpdateStudySetSourcesRequestValidator : AbstractValidator<UpdateStudySetSourcesRequest>
{
    public UpdateStudySetSourcesRequestValidator() => RuleFor(x => x.SourceStudySetIds).NotNull().Must(values => values.Count is >= 2 and <= 20 && values.Distinct().Count() == values.Count).WithMessage("Choose between 2 and 20 unique source study sets.");
}
public sealed class StudyTogetherRequestValidator : AbstractValidator<StudyTogetherRequest>
{
    public StudyTogetherRequestValidator() => RuleFor(x => x.SourceStudySetIds).NotNull().Must(values => values.Count is >= 2 and <= 20 && values.Distinct().Count() == values.Count).WithMessage("Choose between 2 and 20 unique source study sets.");
}
