using System.Globalization;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using StudyFlow.Application.Common.Models;
using StudyFlow.Application.Grounding;
using StudyFlow.Domain.Enums;
using StudyFlow.Infrastructure.Persistence;

namespace StudyFlow.Infrastructure.Grounding;

public sealed class GroundingOptions
{
    public const string SectionName="Grounding"; public int MaxDocuments{get;init;}=5; public int MaxChunks{get;init;}=10; public int MaxContextCharacters{get;init;}=30000; public int SmallDocumentChunkThreshold{get;init;}=8;
}

internal sealed class DocumentContextRetriever(StudyFlowDbContext db,IOptions<GroundingOptions> options,ILogger<DocumentContextRetriever> logger):IDocumentContextRetriever
{
    public const string Revision="lexical-v1";
    public async Task<Result<DocumentContextResult>> RetrieveAsync(Guid userId,IReadOnlyCollection<Guid> documentIds,string query,int? requestedMax,CancellationToken cancellationToken)
    {
        var timer=Stopwatch.StartNew();
        var ids=documentIds.Distinct().ToList();var settings=options.Value;if(ids.Count==0)return Result<DocumentContextResult>.Failure("GROUNDING_CONTEXT_EMPTY","At least one document is required.",ErrorType.Validation);if(ids.Count>settings.MaxDocuments)return Result<DocumentContextResult>.Failure("GROUNDING_CONTEXT_TOO_LARGE","Too many documents were requested.",ErrorType.Validation);
        var documents=await db.Documents.AsNoTracking().Where(x=>ids.Contains(x.Id)&&x.UserId==userId).ToListAsync(cancellationToken);if(documents.Count!=ids.Count)return Result<DocumentContextResult>.Failure("DOCUMENT_NOT_FOUND","Document was not found.",ErrorType.NotFound);if(documents.Any(x=>x.ProcessingStatus!=DocumentProcessingStatus.Ready))return Result<DocumentContextResult>.Failure("DOCUMENT_NOT_READY","Every grounding document must be ready.",ErrorType.Conflict);
        var names=documents.ToDictionary(x=>x.Id,x=>x.OriginalFileName);var rows=await db.DocumentChunks.AsNoTracking().Where(x=>ids.Contains(x.DocumentId)).ToListAsync(cancellationToken);if(rows.Count==0)return Result<DocumentContextResult>.Failure("GROUNDING_CONTEXT_EMPTY","No processed chunks are available.",ErrorType.Validation);
        rows=rows.GroupBy(x=>x.ContentHash).Select(x=>x.OrderBy(y=>y.ChunkIndex).First()).ToList();var max=Math.Min(requestedMax??settings.MaxChunks,settings.MaxChunks);var terms=Tokens(query);IEnumerable<Domain.Entities.DocumentChunk> ranked=rows.Count<=settings.SmallDocumentChunkThreshold?rows.OrderBy(x=>ids.IndexOf(x.DocumentId)).ThenBy(x=>x.ChunkIndex):rows.OrderByDescending(x=>Score(x.Content,x.SectionTitle,query,terms)).ThenBy(x=>x.ChunkIndex);
        var selected=new List<Domain.Entities.DocumentChunk>();var characters=0;foreach(var chunk in ranked){if(selected.Count>=max)break;if(characters+chunk.Content.Length>settings.MaxContextCharacters)continue;selected.Add(chunk);characters+=chunk.Content.Length;}
        if(selected.Count==0)return Result<DocumentContextResult>.Failure("GROUNDING_CONTEXT_TOO_LARGE","No chunk fits the grounding context limits.",ErrorType.Validation);
        var result=selected.Select((x,index)=>new DocumentContextChunk($"C{index+1}",x.DocumentId,names[x.DocumentId],x.Id,x.ChunkIndex,x.Content,x.PageNumber,x.SlideNumber,x.SectionTitle,x.StartOffset,x.EndOffset,x.ContentHash)).ToList();logger.LogInformation("Grounding retrieval completed for user {UserId}: {DocumentCount} documents, {ChunkCount} chunks, revision {RetrievalRevision}, {DurationMs} ms",userId,ids.Count,result.Count,Revision,timer.ElapsedMilliseconds);return Result<DocumentContextResult>.Success(new(result,Revision));
    }
    private static HashSet<string> Tokens(string value)=>Regex.Matches(value.Normalize(NormalizationForm.FormKC).ToLowerInvariant(),@"[\p{L}\p{N}]+(?:[=+\-][\p{L}\p{N}]+)?").Select(x=>x.Value).Where(x=>x.Length>1).ToHashSet(StringComparer.Ordinal);
    private static double Score(string content,string? section,string query,HashSet<string> terms){var normalized=content.Normalize(NormalizationForm.FormKC).ToLowerInvariant();var score=0d;var phrase=query.Trim().ToLowerInvariant();if(phrase.Length>3&&normalized.Contains(phrase,StringComparison.Ordinal))score+=20;foreach(var term in terms){var count=Regex.Matches(normalized,$@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(term)}(?![\p{{L}}\p{{N}}])").Count;if(count>0)score+=3+Math.Min(count,5);if(section?.Contains(term,StringComparison.OrdinalIgnoreCase)==true)score+=6;}return score;}
}

internal sealed class GroundingValidator(StudyFlowDbContext db,ILogger<GroundingValidator> logger):IGroundingValidator
{
    public async Task<GroundingValidationResult> ValidateAsync(Guid userId,IReadOnlyCollection<string> aliases,DocumentContextResult context,CancellationToken cancellationToken)
    {
        var requested=aliases.Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();var map=context.Chunks.ToDictionary(x=>x.Alias,StringComparer.OrdinalIgnoreCase);var candidates=requested.Where(map.ContainsKey).Select(x=>map[x]).ToList();var ids=candidates.Select(x=>x.DocumentChunkId).ToList();
        var current=await (from chunk in db.DocumentChunks.AsNoTracking() join document in db.Documents.AsNoTracking() on chunk.DocumentId equals document.Id where ids.Contains(chunk.Id)&&document.UserId==userId&&document.ProcessingStatus==DocumentProcessingStatus.Ready select new{Chunk=chunk,Document=document}).ToListAsync(cancellationToken);var currentMap=current.ToDictionary(x=>x.Chunk.Id);var valid=new List<ValidatedSource>();var accepted=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach(var item in candidates){if(!currentMap.TryGetValue(item.DocumentChunkId,out var row)||row.Chunk.ContentHash!=item.ContentHash||row.Chunk.DocumentId!=item.DocumentId)continue;accepted.Add(item.Alias);valid.Add(new(item.DocumentId,row.Document.OriginalFileName,item.DocumentChunkId,row.Chunk.PageNumber,row.Chunk.SlideNumber,row.Chunk.SectionTitle,row.Chunk.StartOffset,row.Chunk.EndOffset,row.Chunk.ContentHash,row.Chunk.Content[..Math.Min(500,row.Chunk.Content.Length)]));}
        var rejected=requested.Where(x=>!accepted.Contains(x)).ToList();logger.LogInformation("Grounding validation for user {UserId}: {AcceptedSourceCount} accepted, {RejectedSourceCount} rejected",userId,valid.Count,rejected.Count);return new(valid,rejected);
    }
}

internal sealed class SourceReferenceService(StudyFlowDbContext db):ISourceReferenceService
{
    public async Task<Result<IReadOnlyList<SourceReferenceDto>>> GetForFlashcardAsync(Guid userId,Guid id,CancellationToken token)=>await db.Flashcards.AnyAsync(x=>x.Id==id&&db.StudySets.Any(s=>s.Id==x.StudySetId&&db.Subjects.Any(u=>u.Id==s.SubjectId&&u.UserId==userId)),token)?Result<IReadOnlyList<SourceReferenceDto>>.Success(await Load(userId,GroundedContentType.Flashcard,id,token)):Result<IReadOnlyList<SourceReferenceDto>>.Failure("SOURCE_REFERENCE_NOT_FOUND","Content was not found.",ErrorType.NotFound);
    public async Task<Result<IReadOnlyList<SourceReferenceDto>>> GetForQuestionAsync(Guid userId,Guid id,CancellationToken token)=>await db.Questions.AnyAsync(x=>x.Id==id&&db.Quizzes.Any(q=>q.Id==x.QuizId&&db.StudySets.Any(s=>s.Id==q.StudySetId&&db.Subjects.Any(u=>u.Id==s.SubjectId&&u.UserId==userId))),token)?Result<IReadOnlyList<SourceReferenceDto>>.Success(await Load(userId,GroundedContentType.QuizQuestion,id,token)):Result<IReadOnlyList<SourceReferenceDto>>.Failure("SOURCE_REFERENCE_NOT_FOUND","Content was not found.",ErrorType.NotFound);
    public async Task<Result<IReadOnlyList<SourceReferenceDto>>> GetForJobAsync(Guid userId,Guid id,CancellationToken token){var job=await db.AIJobs.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id&&x.UserId==userId,token);if(job is null)return Result<IReadOnlyList<SourceReferenceDto>>.Failure("SOURCE_REFERENCE_NOT_FOUND","AI job was not found.",ErrorType.NotFound);var type=job.JobType switch{AIJobType.Explain=>GroundedContentType.Explanation,AIJobType.GenerateSimilarQuestion=>GroundedContentType.SimilarQuestion,AIJobType.TutorHint=>GroundedContentType.TutorHint,_=>(GroundedContentType?)null};return Result<IReadOnlyList<SourceReferenceDto>>.Success(type.HasValue?await Load(userId,type.Value,id,token):[]);}
    private async Task<IReadOnlyList<SourceReferenceDto>> Load(Guid userId,GroundedContentType type,Guid id,CancellationToken token)=>await db.SourceReferences.AsNoTracking().Where(x=>x.ContentType==type&&x.ContentId==id&&(x.DocumentId==null||!db.Documents.Any(document=>document.Id==x.DocumentId)||db.Documents.Any(document=>document.Id==x.DocumentId&&document.UserId==userId))).OrderBy(x=>x.CreatedAt).Select(x=>new SourceReferenceDto(x.Id,x.DocumentId,x.DocumentNameSnapshot,x.DocumentChunkId,x.PageNumber,x.SlideNumber,x.SectionTitle,x.SourceSnippetSnapshot,x.StartOffset,x.EndOffset,x.DocumentId!=null&&db.Documents.Any(document=>document.Id==x.DocumentId&&document.UserId==userId))).ToListAsync(token);
}
