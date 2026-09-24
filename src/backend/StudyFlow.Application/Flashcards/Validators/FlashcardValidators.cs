using FluentValidation;
using StudyFlow.Application.Flashcards.DTOs;

namespace StudyFlow.Application.Flashcards.Validators;

public sealed class CreateFlashcardRequestValidator : AbstractValidator<CreateFlashcardRequest>
{
    public CreateFlashcardRequestValidator()
    {
        RuleFor(x => x.FrontText).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.BackText).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.Explanation).MaximumLength(5000);
        RuleFor(x => x.LanguageCode).MaximumLength(35).Matches("^[A-Za-z]{2,3}(?:-[A-Za-z0-9]{2,8})*$").When(x => !string.IsNullOrWhiteSpace(x.LanguageCode));
        RuleFor(x => x.ReadingText).MaximumLength(500);
        RuleFor(x => x.Romanization).MaximumLength(500);
        RuleFor(x => x.ExampleText).MaximumLength(2000); RuleFor(x => x.ExampleTranslation).MaximumLength(2000); RuleFor(x => x.MemoryTip).MaximumLength(2000); RuleFor(x => x.AcceptedAnswers).MaximumLength(2000);
    }
}

public sealed class UpdateFlashcardRequestValidator : AbstractValidator<UpdateFlashcardRequest>
{
    public UpdateFlashcardRequestValidator()
    {
        RuleFor(x => x.FrontText).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.BackText).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.Explanation).MaximumLength(5000);
        RuleFor(x => x.LanguageCode).MaximumLength(35).Matches("^[A-Za-z]{2,3}(?:-[A-Za-z0-9]{2,8})*$").When(x => !string.IsNullOrWhiteSpace(x.LanguageCode));
        RuleFor(x => x.ReadingText).MaximumLength(500);
        RuleFor(x => x.Romanization).MaximumLength(500);
        RuleFor(x => x.ExampleText).MaximumLength(2000); RuleFor(x => x.ExampleTranslation).MaximumLength(2000); RuleFor(x => x.MemoryTip).MaximumLength(2000); RuleFor(x => x.AcceptedAnswers).MaximumLength(2000);
    }
}

public sealed class BulkCreateFlashcardsRequestValidator : AbstractValidator<BulkCreateFlashcardsRequest>
{
    public BulkCreateFlashcardsRequestValidator()
    {
        RuleFor(x => x.Cards).NotNull().NotEmpty().Must(cards => cards.Count <= 100).WithMessage("A maximum of 100 cards can be created at once.");
        RuleForEach(x => x.Cards).SetValidator(new CreateFlashcardRequestValidator());
    }
}
