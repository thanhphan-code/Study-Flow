using System.Data;
using Microsoft.EntityFrameworkCore;
using StudyFlow.Application.Common.Models;
using StudyFlow.Application.LearningEngine.DTOs;
using StudyFlow.Application.LearningEngine.Interfaces;
using StudyFlow.Application.Quizzes.DTOs;
using StudyFlow.Application.Quizzes.Interfaces;
using StudyFlow.Domain.Entities;
using StudyFlow.Domain.Enums;

namespace StudyFlow.Infrastructure.Persistence.Services;

internal sealed class QuizService(StudyFlowDbContext dbContext, ILearningEngine learningEngine, TimeProvider timeProvider) : IQuizService
{
    public async Task<Result<IReadOnlyList<QuizDto>>> ListAsync(Guid userId, Guid studySetId, CancellationToken cancellationToken)
    {
        if (!await OwnsStudySetAsync(userId, studySetId, cancellationToken)) return Result<IReadOnlyList<QuizDto>>.Failure("STUDY_SET_NOT_FOUND", "Study set was not found.", ErrorType.NotFound);
        var quizzes = await dbContext.Quizzes.AsNoTracking().Where(x => x.StudySetId == studySetId).OrderByDescending(x => x.CreatedAt).Select(x => new QuizDto(x.Id, x.StudySetId, x.Title, x.QuestionCount, x.CreatedAt)).ToListAsync(cancellationToken);
        return Result<IReadOnlyList<QuizDto>>.Success(quizzes);
    }

    public async Task<Result<QuizDto>> CreateAsync(Guid userId, Guid studySetId, CreateQuizRequest request, CancellationToken cancellationToken)
    {
        if (!await OwnsStudySetAsync(userId, studySetId, cancellationToken)) return Result<QuizDto>.Failure("STUDY_SET_NOT_FOUND", "Study set was not found.", ErrorType.NotFound);
        var setType = await dbContext.StudySets.Where(x => x.Id == studySetId).Select(x => x.Type).SingleAsync(cancellationToken); var sourceIds = setType == StudySetType.Standard ? new List<Guid> { studySetId } : await dbContext.StudySetSources.Where(x => x.CombinedStudySetId == studySetId).Select(x => x.SourceStudySetId).ToListAsync(cancellationToken);
        var cards = await dbContext.Flashcards.AsNoTracking().Where(x => sourceIds.Contains(x.StudySetId)).OrderBy(x => x.StudySetId).ThenBy(x => x.OrderIndex).Take(request.QuestionCount).ToListAsync(cancellationToken);
        if (cards.Count < request.QuestionCount) return Result<QuizDto>.Failure("NOT_ENOUGH_FLASHCARDS", $"This study set has only {cards.Count} flashcards.", ErrorType.Validation);

        var quiz = Quiz.Create(studySetId, request.Title, request.QuestionCount); dbContext.Quizzes.Add(quiz);
        for (var index = 0; index < cards.Count; index++) CreateQuestion(quiz.Id, cards, index);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<QuizDto>.Success(new(quiz.Id, quiz.StudySetId, quiz.Title, quiz.QuestionCount, quiz.CreatedAt));
    }

    public async Task<Result<QuizDto>> CreateManualAsync(Guid userId, Guid studySetId, CreateManualQuizRequest request, CancellationToken cancellationToken)
    {
        if (!await OwnsStudySetAsync(userId, studySetId, cancellationToken)) return Result<QuizDto>.Failure("STUDY_SET_NOT_FOUND", "Study set was not found.", ErrorType.NotFound);
        var quiz = Quiz.Create(studySetId, request.Title, request.Questions.Count);
        dbContext.Quizzes.Add(quiz);
        foreach (var (input, questionIndex) in request.Questions.Select((value, index) => (value, index)))
        {
            var question = Question.CreateGenerated(quiz.Id, QuestionType.MultipleChoice, input.QuestionText, input.Explanation?.Trim(), questionIndex);
            dbContext.Questions.Add(question);
            dbContext.AnswerOptions.AddRange(input.Options.Select((option, optionIndex) => AnswerOption.Create(question.Id, option.Text, option.IsCorrect, optionIndex)));
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<QuizDto>.Success(new(quiz.Id, quiz.StudySetId, quiz.Title, quiz.QuestionCount, quiz.CreatedAt));
    }

    public async Task<Result<QuizAttemptSessionDto>> StartAttemptAsync(Guid userId, Guid quizId, CancellationToken cancellationToken)
    {
        var quiz = await FindOwnedQuizAsync(userId, quizId, cancellationToken); if (quiz is null) return Result<QuizAttemptSessionDto>.Failure("QUIZ_NOT_FOUND", "Quiz was not found.", ErrorType.NotFound);
        var now = timeProvider.GetUtcNow(); var attempt = QuizAttempt.Start(userId, quizId, now); dbContext.QuizAttempts.Add(attempt); await dbContext.SaveChangesAsync(cancellationToken);
        var questions = await LoadQuestionsAsync(quizId, cancellationToken);
        return Result<QuizAttemptSessionDto>.Success(new(attempt.Id, quiz.Id, quiz.Title, now, questions));
    }

    public async Task<Result<QuizAnswerAcceptedDto>> SubmitAnswerAsync(Guid userId, Guid attemptId, SubmitQuizAnswerRequest request, CancellationToken cancellationToken)
    {
        var attempt = await dbContext.QuizAttempts.SingleOrDefaultAsync(x => x.Id == attemptId && x.UserId == userId, cancellationToken);
        if (attempt is null) return Result<QuizAnswerAcceptedDto>.Failure("QUIZ_ATTEMPT_NOT_FOUND", "Quiz attempt was not found.", ErrorType.NotFound);
        if (attempt.IsCompleted) return Result<QuizAnswerAcceptedDto>.Failure("QUIZ_ATTEMPT_COMPLETED", "Completed attempts cannot be changed.", ErrorType.Conflict);
        var option = await dbContext.AnswerOptions.Join(dbContext.Questions, option => option.QuestionId, question => question.Id, (option, question) => new { option, question }).SingleOrDefaultAsync(x => x.question.Id == request.QuestionId && x.question.QuizId == attempt.QuizId && x.option.Id == request.AnswerOptionId, cancellationToken);
        if (option is null) return Result<QuizAnswerAcceptedDto>.Failure("INVALID_QUIZ_ANSWER", "Question or answer option is invalid.", ErrorType.Validation);
        var now = timeProvider.GetUtcNow(); var answer = await dbContext.QuizAnswers.SingleOrDefaultAsync(x => x.QuizAttemptId == attemptId && x.QuestionId == request.QuestionId, cancellationToken);
        if (answer is null) dbContext.QuizAnswers.Add(QuizAnswer.Create(attemptId, request.QuestionId, request.AnswerOptionId, option.option.IsCorrect, now)); else answer.Change(request.AnswerOptionId, option.option.IsCorrect, now);
        await dbContext.SaveChangesAsync(cancellationToken); return Result<QuizAnswerAcceptedDto>.Success(new(request.QuestionId, request.AnswerOptionId));
    }

    public async Task<Result<QuizResultDto>> CompleteAttemptAsync(Guid userId, Guid attemptId, CancellationToken cancellationToken)
    {
        await using var transaction = dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.Ordinal) == true
            ? await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken)
            : null;
        var attempt = await dbContext.QuizAttempts.SingleOrDefaultAsync(x => x.Id == attemptId && x.UserId == userId, cancellationToken);
        if (attempt is null) return Result<QuizResultDto>.Failure("QUIZ_ATTEMPT_NOT_FOUND", "Quiz attempt was not found.", ErrorType.NotFound);
        if (attempt.IsCompleted) return Result<QuizResultDto>.Failure("QUIZ_ATTEMPT_COMPLETED", "Quiz attempt is already completed.", ErrorType.Conflict);
        var quiz = await dbContext.Quizzes.SingleAsync(x => x.Id == attempt.QuizId, cancellationToken);
        var answers = await dbContext.QuizAnswers.Where(x => x.QuizAttemptId == attemptId).ToListAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        var correctCount = answers.Count(x => x.IsCorrect);
        attempt.Complete(correctCount, quiz.QuestionCount, now);
        var questions = await dbContext.Questions.AsNoTracking().Where(x => x.QuizId == quiz.Id).OrderBy(x => x.OrderIndex).ToListAsync(cancellationToken);
        var questionIds = questions.Select(x => x.Id).ToList(); var options = await dbContext.AnswerOptions.AsNoTracking().Where(x => questionIds.Contains(x.QuestionId)).ToListAsync(cancellationToken);
        var studySession = StudySession.FromCompletedQuiz(userId, quiz.StudySetId, attempt.StartedAt, now, quiz.QuestionCount, correctCount);
        dbContext.StudySessions.Add(studySession);
        await dbContext.SaveChangesAsync(cancellationToken);
        foreach (var question in questions.Where(x => x.FlashcardId.HasValue))
        {
            var answer = answers.SingleOrDefault(x => x.QuestionId == question.Id);
            var selectedText = options.SingleOrDefault(x => x.Id == answer?.AnswerOptionId)?.Text ?? string.Empty;
            var isCorrect = answer?.IsCorrect == true;
            var engineResult = await learningEngine.RecordAsync(new RecordLearningEvidence(
                userId, question.FlashcardId!.Value, studySession.Id, StudyMode.Quiz,
                question.Type == QuestionType.MultipleChoice ? LearningAttemptType.MultipleChoice : LearningAttemptType.TrueFalse,
                RecallDirection.Forward, selectedText, isCorrect ? LearningAttemptResult.Correct : LearningAttemptResult.Wrong,
                3, 0, false, Combine(attempt.Id, question.Id),
                isCorrect ? ReviewRating.Hard : ReviewRating.Again,
                isCorrect ? LearningCardOutcome.Completed : LearningCardOutcome.NeedsReview,
                false), cancellationToken);
            if (!engineResult.IsSuccess)
                return Result<QuizResultDto>.Failure(engineResult.Error!.Code, engineResult.Error.Message, engineResult.Error.Type);
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        var results = questions.Select(question => { var selected = answers.SingleOrDefault(x => x.QuestionId == question.Id); var selectedOption = options.SingleOrDefault(x => x.Id == selected?.AnswerOptionId); var correct = options.Single(x => x.QuestionId == question.Id && x.IsCorrect); return new QuizQuestionResultDto(question.Id, question.QuestionText, selected?.AnswerOptionId, selectedOption?.Text, correct.Id, correct.Text, selected?.IsCorrect == true, question.Explanation); }).ToList();
        return Result<QuizResultDto>.Success(new(attempt.Id, attempt.Score!.Value, attempt.CorrectCount, attempt.WrongCount, attempt.CompletedAt!.Value, results));
    }

    private void CreateQuestion(Guid quizId, IReadOnlyList<Flashcard> cards, int index)
    {
        var card = cards[index]; var multipleChoice = cards.Count >= 3 && index % 2 == 0;
        if (multipleChoice)
        {
            var question = Question.Create(quizId, card.Id, QuestionType.MultipleChoice, card.FrontText, card.Explanation, index); dbContext.Questions.Add(question);
            var values = new List<(string Text, bool Correct)> { (card.BackText, true) }; values.AddRange(cards.Where(x => x.Id != card.Id).Select(x => x.BackText).Distinct().Take(3).Select(x => (x, false)));
            var shift = index % values.Count; values = values.Skip(shift).Concat(values.Take(shift)).ToList();
            dbContext.AnswerOptions.AddRange(values.Select((value, order) => AnswerOption.Create(question.Id, value.Text, value.Correct, order)));
        }
        else
        {
            var trueStatement = cards.Count == 1 || index % 2 == 0; var answer = trueStatement ? card.BackText : cards[(index + 1) % cards.Count].BackText;
            var question = Question.Create(quizId, card.Id, QuestionType.TrueFalse, Truncate($"{card.FrontText} — {answer}"), card.Explanation, index); dbContext.Questions.Add(question);
            dbContext.AnswerOptions.AddRange(AnswerOption.Create(question.Id, "True", trueStatement, 0), AnswerOption.Create(question.Id, "False", !trueStatement, 1));
        }
    }

    private async Task<IReadOnlyList<QuizQuestionDto>> LoadQuestionsAsync(Guid quizId, CancellationToken cancellationToken)
    {
        var questions = await dbContext.Questions.AsNoTracking().Where(x => x.QuizId == quizId).OrderBy(x => x.OrderIndex).ToListAsync(cancellationToken); var ids = questions.Select(x => x.Id).ToList();
        var options = await dbContext.AnswerOptions.AsNoTracking().Where(x => ids.Contains(x.QuestionId)).OrderBy(x => x.OrderIndex).ToListAsync(cancellationToken);
        return questions.Select(x => new QuizQuestionDto(x.Id, x.Type, x.QuestionText, x.OrderIndex, options.Where(option => option.QuestionId == x.Id).Select(option => new QuizOptionDto(option.Id, option.Text)).ToList())).ToList();
    }
    private Task<bool> OwnsStudySetAsync(Guid userId, Guid studySetId, CancellationToken cancellationToken) => dbContext.StudySets.AnyAsync(set => set.Id == studySetId && dbContext.Subjects.Any(subject => subject.Id == set.SubjectId && subject.UserId == userId), cancellationToken);
    private Task<Quiz?> FindOwnedQuizAsync(Guid userId, Guid quizId, CancellationToken cancellationToken) => dbContext.Quizzes.AsNoTracking().SingleOrDefaultAsync(quiz => quiz.Id == quizId && dbContext.StudySets.Any(set => set.Id == quiz.StudySetId && dbContext.Subjects.Any(subject => subject.Id == set.SubjectId && subject.UserId == userId)), cancellationToken);
    private static string Truncate(string value) => value.Length <= 2000 ? value : value[..2000];
    private static Guid Combine(Guid left, Guid right)
    {
        var leftBytes = left.ToByteArray(); var rightBytes = right.ToByteArray();
        for (var index = 0; index < leftBytes.Length; index++) leftBytes[index] ^= rightBytes[index];
        return new Guid(leftBytes);
    }
}
