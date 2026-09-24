using StudyFlow.Application.Common.Models;
using StudyFlow.Application.StudySessions.DTOs;

namespace StudyFlow.Application.StudySessions.Interfaces;

public interface IStudySessionService
{
    Task<Result<StudySessionDto>> StartAsync(Guid userId, StartStudySessionRequest request, CancellationToken cancellationToken);
    Task<Result<StudySessionDto>> CompleteAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken);
}
