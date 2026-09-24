using StudyFlow.Application.Common.Models;
using StudyFlow.Application.Quizzes.DTOs;

namespace StudyFlow.Application.Quizzes.Interfaces;

public interface IQuizService
{
    Task<Result<IReadOnlyList<QuizDto>>> ListAsync(Guid userId, Guid studySetId, CancellationToken cancellationToken);
    Task<Result<QuizDto>> CreateAsync(Guid userId, Guid studySetId, CreateQuizRequest request, CancellationToken cancellationToken);
    Task<Result<QuizDto>> CreateManualAsync(Guid userId, Guid studySetId, CreateManualQuizRequest request, CancellationToken cancellationToken);
    Task<Result<QuizAttemptSessionDto>> StartAttemptAsync(Guid userId, Guid quizId, CancellationToken cancellationToken);
    Task<Result<QuizAnswerAcceptedDto>> SubmitAnswerAsync(Guid userId, Guid attemptId, SubmitQuizAnswerRequest request, CancellationToken cancellationToken);
    Task<Result<QuizResultDto>> CompleteAttemptAsync(Guid userId, Guid attemptId, CancellationToken cancellationToken);
}
