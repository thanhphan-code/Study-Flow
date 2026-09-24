using Microsoft.EntityFrameworkCore;
using StudyFlow.Application.Common.Models;
using StudyFlow.Application.Subjects.DTOs;
using StudyFlow.Application.Subjects.Interfaces;
using StudyFlow.Domain.Entities;

namespace StudyFlow.Infrastructure.Persistence.Services;

internal sealed class SubjectService(StudyFlowDbContext dbContext) : ISubjectService
{
    public async Task<IReadOnlyList<SubjectDto>> ListAsync(Guid userId, CancellationToken cancellationToken) =>
        await dbContext.Subjects.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.UpdatedAt)
            .Select(x => new SubjectDto(x.Id, x.Name, x.Description, x.CreatedAt, x.UpdatedAt)).ToListAsync(cancellationToken);

    public async Task<Result<SubjectDto>> GetAsync(Guid userId, Guid subjectId, CancellationToken cancellationToken)
    {
        var subject = await FindOwnedAsync(userId, subjectId, true, cancellationToken);
        return subject is null ? NotFound() : Result<SubjectDto>.Success(ToDto(subject));
    }

    public async Task<Result<SubjectDto>> CreateAsync(Guid userId, CreateSubjectRequest request, CancellationToken cancellationToken)
    {
        var subject = Subject.Create(userId, request.Name, request.Description);
        dbContext.Subjects.Add(subject);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<SubjectDto>.Success(ToDto(subject));
    }

    public async Task<Result<SubjectDto>> UpdateAsync(Guid userId, Guid subjectId, UpdateSubjectRequest request, CancellationToken cancellationToken)
    {
        var subject = await FindOwnedAsync(userId, subjectId, false, cancellationToken);
        if (subject is null) return NotFound();
        subject.Update(request.Name, request.Description);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<SubjectDto>.Success(ToDto(subject));
    }

    public async Task<Result<bool>> DeleteAsync(Guid userId, Guid subjectId, CancellationToken cancellationToken)
    {
        var subject = await FindOwnedAsync(userId, subjectId, false, cancellationToken);
        if (subject is null) return Result<bool>.Failure("SUBJECT_NOT_FOUND", "Subject was not found.", ErrorType.NotFound);
        if (await dbContext.StudySets.AnyAsync(x => x.SubjectId == subjectId, cancellationToken))
            return Result<bool>.Failure("SUBJECT_HAS_STUDY_SETS", "Delete all study sets in this subject first.", ErrorType.Conflict);
        dbContext.Subjects.Remove(subject);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    private Task<Subject?> FindOwnedAsync(Guid userId, Guid subjectId, bool readOnly, CancellationToken cancellationToken)
    {
        IQueryable<Subject> query = dbContext.Subjects;
        if (readOnly) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(x => x.Id == subjectId && x.UserId == userId, cancellationToken);
    }

    private static SubjectDto ToDto(Subject subject) => new(subject.Id, subject.Name, subject.Description, subject.CreatedAt, subject.UpdatedAt);
    private static Result<SubjectDto> NotFound() => Result<SubjectDto>.Failure("SUBJECT_NOT_FOUND", "Subject was not found.", ErrorType.NotFound);
}
