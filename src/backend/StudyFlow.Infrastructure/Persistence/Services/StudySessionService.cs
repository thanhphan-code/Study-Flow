using Microsoft.EntityFrameworkCore;
using StudyFlow.Application.Common.Models;
using StudyFlow.Application.StudySessions.DTOs;
using StudyFlow.Application.StudySessions.Interfaces;
using StudyFlow.Domain.Entities;
using StudyFlow.Domain.Enums;

namespace StudyFlow.Infrastructure.Persistence.Services;

internal sealed class StudySessionService(StudyFlowDbContext dbContext, TimeProvider timeProvider) : IStudySessionService
{
    public async Task<Result<StudySessionDto>> StartAsync(Guid userId, StartStudySessionRequest request, CancellationToken cancellationToken)
    {
        var ownsSet = await dbContext.StudySets.AnyAsync(set => set.Id == request.StudySetId &&
            dbContext.Subjects.Any(subject => subject.Id == set.SubjectId && subject.UserId == userId), cancellationToken);
        if (!ownsSet) return Result<StudySessionDto>.Failure("STUDY_SET_NOT_FOUND", "Study set was not found.", ErrorType.NotFound);
        if (request.Mode == StudyMode.Quiz) return Result<StudySessionDto>.Failure("INVALID_SESSION_MODE", "Quiz sessions are created automatically.", ErrorType.Validation);

        var session = StudySession.Start(userId, request.StudySetId, request.Mode, timeProvider.GetUtcNow());
        dbContext.StudySessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<StudySessionDto>.Success(ToDto(session));
    }

    public async Task<Result<StudySessionDto>> CompleteAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await dbContext.StudySessions.SingleOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId, cancellationToken);
        if (session is null) return Result<StudySessionDto>.Failure("STUDY_SESSION_NOT_FOUND", "Study session was not found.", ErrorType.NotFound);
        session.Complete(timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<StudySessionDto>.Success(ToDto(session));
    }

    private static StudySessionDto ToDto(StudySession x) => new(x.Id, x.StudySetId, x.Mode, x.StartedAt, x.EndedAt, x.CardsStudied, x.QuestionsAnswered, x.CorrectAnswers, x.DurationSeconds);
}
