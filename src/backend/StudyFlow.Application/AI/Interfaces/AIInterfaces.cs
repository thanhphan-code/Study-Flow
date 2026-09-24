using StudyFlow.Application.AI.DTOs;
using StudyFlow.Application.Common.Models;
namespace StudyFlow.Application.AI.Interfaces;
public interface IAIService
{
    Task<FlashcardGenerationResult> GenerateFlashcardsAsync(AIGenerationContext context,int count,CancellationToken cancellationToken);
    Task<QuizGenerationResult> GenerateQuizAsync(AIGenerationContext context,int count,CancellationToken cancellationToken);
    Task<GroundedTextProviderResult> ExplainAsync(string material,string question,string answer,CancellationToken cancellationToken);
    Task<GroundedTextProviderResult> GenerateSimilarQuestionAsync(string material,string question,string answer,CancellationToken cancellationToken);
    Task<GroundedTextProviderResult> GenerateHintAsync(string material,string question,int level,CancellationToken cancellationToken);
}
public interface IAIWorkflowService
{
    Task<Result<IReadOnlyList<AIJobDto>>> GenerateAsync(Guid userId,Guid studySetId,GenerateStudyMaterialRequest request,CancellationToken cancellationToken);
    Task<Result<AIJobDto>> GetJobAsync(Guid userId,Guid jobId,CancellationToken cancellationToken);
    Task<Result<SavedAIDraftDto>> SaveDraftAsync(Guid userId,Guid jobId,SaveAIDraftRequest request,CancellationToken cancellationToken);
    Task<Result<AITextResultDto>> ExplainAsync(Guid userId,Guid flashcardId,CancellationToken cancellationToken);
    Task<Result<AITextResultDto>> SimilarQuestionAsync(Guid userId,Guid flashcardId,CancellationToken cancellationToken);
    Task<Result<AITextResultDto>> HintAsync(Guid userId,Guid flashcardId,int level,CancellationToken cancellationToken);
}
