using StudyFlow.Application.Common.Models;
using StudyFlow.Application.SmartLearning.DTOs;

namespace StudyFlow.Application.SmartLearning.Interfaces;

public interface ISmartLearningService
{
    Task<Result<SmartLearnSessionDto>> StartAsync(Guid userId, Guid studySetId, StartSmartLearnRequest request, CancellationToken cancellationToken);
    Task<Result<SmartLearnSessionDto>> ResumeAsync(Guid userId, Guid studySetId, CancellationToken cancellationToken);
    Task<Result<LearningAttemptDto>> RecordAttemptAsync(Guid userId, Guid sessionId, RecordLearningAttemptRequest request, CancellationToken cancellationToken);
    Task<Result<SmartLearnSummaryDto>> CompleteAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken);
}
