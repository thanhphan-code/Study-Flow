using StudyFlow.Application.Common.Models;
using StudyFlow.Application.Subjects.DTOs;

namespace StudyFlow.Application.Subjects.Interfaces;

public interface ISubjectService
{
    Task<IReadOnlyList<SubjectDto>> ListAsync(Guid userId, CancellationToken cancellationToken);
    Task<Result<SubjectDto>> GetAsync(Guid userId, Guid subjectId, CancellationToken cancellationToken);
    Task<Result<SubjectDto>> CreateAsync(Guid userId, CreateSubjectRequest request, CancellationToken cancellationToken);
    Task<Result<SubjectDto>> UpdateAsync(Guid userId, Guid subjectId, UpdateSubjectRequest request, CancellationToken cancellationToken);
    Task<Result<bool>> DeleteAsync(Guid userId, Guid subjectId, CancellationToken cancellationToken);
}
