using FluentValidation;
using StudyFlow.Application.Quizzes.DTOs;

namespace StudyFlow.Application.Quizzes.Validators;

public sealed class CreateQuizRequestValidator : AbstractValidator<CreateQuizRequest>
{
    public CreateQuizRequestValidator() { RuleFor(x => x.Title).NotEmpty().MaximumLength(150); RuleFor(x => x.QuestionCount).InclusiveBetween(1, 50); }
}

public sealed class SubmitQuizAnswerRequestValidator : AbstractValidator<SubmitQuizAnswerRequest>
{
    public SubmitQuizAnswerRequestValidator() { RuleFor(x => x.QuestionId).NotEmpty(); RuleFor(x => x.AnswerOptionId).NotEmpty(); }
}

public sealed class CreateManualQuizRequestValidator : AbstractValidator<CreateManualQuizRequest>
{
    public CreateManualQuizRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Questions).Must(x => x is { Count: >= 1 and <= 50 }).WithMessage("A quiz must contain between 1 and 50 questions.");
        RuleForEach(x => x.Questions).ChildRules(question =>
        {
            question.RuleFor(x => x.QuestionText).NotEmpty().MaximumLength(2000);
            question.RuleFor(x => x.Explanation).MaximumLength(5000);
            question.RuleFor(x => x.Options).Must(options => options is { Count: >= 2 and <= 4 }).WithMessage("Each question must contain between 2 and 4 options.");
            question.RuleFor(x => x.Options).Must(options => options?.Count(option => option.IsCorrect) == 1).WithMessage("Each question must have exactly one correct option.");
            question.RuleForEach(x => x.Options).ChildRules(option => option.RuleFor(x => x.Text).NotEmpty().MaximumLength(1000));
        });
    }
}
