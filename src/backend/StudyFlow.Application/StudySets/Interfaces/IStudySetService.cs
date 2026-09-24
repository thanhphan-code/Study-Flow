using StudyFlow.Application.Common.Models;
using StudyFlow.Application.StudySets.DTOs;

namespace StudyFlow.Application.StudySets.Interfaces;

public interface IStudySetService
{
    Task<Result<IReadOnlyList<StudySetDto>>> ListAsync(Guid userId, Guid subjectId, CancellationToken cancellationToken);
    Task<Result<StudySetDto>> GetAsync(Guid userId, Guid studySetId, CancellationToken cancellationToken);
    Task<Result<StudySetDto>> CreateAsync(Guid userId, Guid subjectId, CreateStudySetRequest request, CancellationToken cancellationToken);
    Task<Result<StudySetDto>> UpdateAsync(Guid userId, Guid studySetId, UpdateStudySetRequest request, CancellationToken cancellationToken);
    Task<Result<bool>> DeleteAsync(Guid userId, Guid studySetId, CancellationToken cancellationToken);
    Task<Result<StudySetDto>> CreateCombinedAsync(Guid userId, Guid subjectId, CreateCombinedStudySetRequest request, CancellationToken cancellationToken);
    Task<Result<StudySetDto>> UpdateSourcesAsync(Guid userId, Guid studySetId, UpdateStudySetSourcesRequest request, CancellationToken cancellationToken);
    Task<Result<StudyTogetherDto>> StudyTogetherAsync(Guid userId, StudyTogetherRequest request, CancellationToken cancellationToken);
}
