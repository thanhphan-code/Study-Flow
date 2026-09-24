using StudyFlow.Application.Common.Models;
using StudyFlow.Application.LearningEngine.DTOs;
using StudyFlow.Domain.Entities;
using StudyFlow.Domain.Enums;

namespace StudyFlow.Application.LearningEngine.Interfaces;

public interface ILearningEngine
{
    Task<Result<LearningEngineResult>> RecordAsync(RecordLearningEvidence evidence, CancellationToken cancellationToken);
    Task<Result<LearningEngineResult>> CommitAsync(Guid userId, LearningAttempt attempt, ReviewRating rating, LearningCardOutcome outcome, bool countTowardsSession, CancellationToken cancellationToken);
}

public interface ILearningStatePolicy
{
    string Version { get; }
    LearningStateAssessment Assess(FlashcardProgress? progress, DateTimeOffset now);
}

public interface IAnswerEvaluator
{
    LearningAttemptResult Evaluate(Flashcard card, RecallDirection direction, string submittedAnswer, LearningAttemptType attemptType);
    LearningAttemptResult EvaluateSequence(IReadOnlyList<string> expected, IReadOnlyList<string> submitted);
}
