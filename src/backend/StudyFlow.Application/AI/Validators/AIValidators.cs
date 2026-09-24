using FluentValidation;
using StudyFlow.Application.AI.DTOs;
namespace StudyFlow.Application.AI.Validators;
public sealed class GenerateStudyMaterialRequestValidator:AbstractValidator<GenerateStudyMaterialRequest>
{
    public GenerateStudyMaterialRequestValidator(){RuleFor(x=>x).Must(x=>x.DocumentId.HasValue^!string.IsNullOrWhiteSpace(x.PastedText)).WithMessage("Choose exactly one source: document or pasted text.");RuleFor(x=>x).Must(x=>x.GenerateFlashcards||x.GenerateQuiz).WithMessage("Select at least one output.");RuleFor(x=>x.FlashcardCount).InclusiveBetween(1,50).When(x=>x.GenerateFlashcards);RuleFor(x=>x.QuizQuestionCount).InclusiveBetween(1,50).When(x=>x.GenerateQuiz);RuleFor(x=>x.PastedText).MaximumLength(60000);RuleFor(x=>x.Difficulty).Must(x=>new[]{"Easy","Medium","Hard","Mixed"}.Contains(x)).WithMessage("Difficulty is invalid.");}
}
public sealed class SaveAIDraftRequestValidator:AbstractValidator<SaveAIDraftRequest>
{
    public SaveAIDraftRequestValidator(){RuleFor(x=>x.Flashcards).Must(x=>x.Count<=50);RuleForEach(x=>x.Flashcards).ChildRules(c=>{c.RuleFor(x=>x.FrontText).NotEmpty().MaximumLength(1000);c.RuleFor(x=>x.BackText).NotEmpty().MaximumLength(5000);c.RuleFor(x=>x.Explanation).MaximumLength(5000);c.RuleFor(x=>x.LanguageCode).MaximumLength(35).Matches("^[A-Za-z]{2,3}(?:-[A-Za-z0-9]{2,8})*$").When(x=>!string.IsNullOrWhiteSpace(x.LanguageCode));c.RuleFor(x=>x.ReadingText).MaximumLength(500);c.RuleFor(x=>x.Romanization).MaximumLength(500);c.RuleFor(x=>x.ExampleText).MaximumLength(2000);c.RuleFor(x=>x.ExampleTranslation).MaximumLength(2000);c.RuleFor(x=>x.MemoryTip).MaximumLength(2000);c.RuleFor(x=>x.AcceptedAnswers).MaximumLength(2000);});RuleFor(x=>x.Questions).Must(x=>x.Count<=50);RuleFor(x=>x.QuizTitle).NotEmpty().MaximumLength(150);RuleForEach(x=>x.Questions).ChildRules(q=>{q.RuleFor(x=>x.QuestionText).NotEmpty().MaximumLength(2000);q.RuleFor(x=>x.Type).Must(x=>x is "MultipleChoice" or "TrueFalse");q.RuleFor(x=>x.Options).Must(ValidOptions).WithMessage("Question options are invalid.");});}
    private static bool ValidOptions(IReadOnlyList<GeneratedOptionDto> options)=>options.Count>=2&&options.Count<=4&&options.Count(x=>x.IsCorrect)==1&&options.All(x=>!string.IsNullOrWhiteSpace(x.Text)&&x.Text.Length<=5000);
}
public sealed class AIHintRequestValidator:AbstractValidator<AIHintRequest>
{
    public AIHintRequestValidator()=>RuleFor(x=>x.Level).InclusiveBetween(1,2);
}
