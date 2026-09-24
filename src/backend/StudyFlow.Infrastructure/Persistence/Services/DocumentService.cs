using System.IO.Compression;
using System.Text;
using Microsoft.EntityFrameworkCore;
using StudyFlow.Application.Common.Models;
using StudyFlow.Application.Documents.DTOs;
using StudyFlow.Application.Documents.Interfaces;
using StudyFlow.Domain.Entities;
using StudyFlow.Domain.Enums;

namespace StudyFlow.Infrastructure.Persistence.Services;

internal sealed class DocumentService(StudyFlowDbContext dbContext, IFileStorageService storage, IDocumentProcessingQueue queue) : IDocumentService
{
    public const int MaximumFileSize = 20 * 1024 * 1024;
    public async Task<Result<DocumentDto>> UploadAsync(Guid userId, DocumentUpload upload, CancellationToken cancellationToken)
    {
        if (upload.Content.Length == 0) return Fail("DOCUMENT_EMPTY", "The uploaded file is empty.");
        if (upload.Content.Length > MaximumFileSize) return Fail("DOCUMENT_TOO_LARGE", "File size cannot exceed 20 MB.");
        var originalName = SanitizeName(upload.OriginalFileName); var extension = Path.GetExtension(originalName).ToLowerInvariant();
        if (!Types.TryGetValue(extension, out var type) || !AllowedMimeTypes[type].Contains(upload.ContentType, StringComparer.OrdinalIgnoreCase)) return Fail("DOCUMENT_UNSUPPORTED_TYPE", "Only valid PDF, DOCX, PPTX, and TXT files are supported.");
        if (!HasValidStructure(upload.Content, type)) return Fail("DOCUMENT_INVALID_SIGNATURE", "File content does not match its declared type.");
        if (!await OwnsTargets(userId, upload.SubjectId, upload.StudySetId, cancellationToken)) return Result<DocumentDto>.Failure("DOCUMENT_TARGET_NOT_FOUND", "Subject or study set was not found.", ErrorType.NotFound);

        var storedName = $"{Guid.NewGuid():N}{extension}"; var key = $"documents/{userId:N}/{storedName}";
        var document = Document.Create(userId, upload.SubjectId, upload.StudySetId, storedName, originalName, upload.ContentType, type, upload.Content.LongLength, key);
        await using var uploadStream = new MemoryStream(upload.Content, writable: false); await storage.SaveAsync(key, uploadStream, cancellationToken);
        try
        {
            await using var transaction = dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.Ordinal) == true ? await dbContext.Database.BeginTransactionAsync(cancellationToken) : null;
            dbContext.Documents.Add(document); await dbContext.SaveChangesAsync(cancellationToken); await queue.EnqueueAsync(document.Id, cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return Result<DocumentDto>.Success(await ToDtoAsync(document, cancellationToken));
        }
        catch
        {
            await storage.DeleteAsync(key, CancellationToken.None); throw;
        }
    }
    public async Task<IReadOnlyList<DocumentListItemDto>> ListAsync(Guid userId, CancellationToken cancellationToken) => (await dbContext.Documents.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken)).Select(x => new DocumentListItemDto(x.Id, x.SubjectId, x.StudySetId, x.OriginalFileName, x.MimeType, x.FileType, x.FileSize, x.ProcessingStatus, x.RetryCount, CanRetry(x), x.ProcessingRevision, x.CreatedAt)).ToList();
    public async Task<Result<DocumentDto>> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var value = await dbContext.Documents.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);
        return value is null ? Result<DocumentDto>.Failure("DOCUMENT_NOT_FOUND", "Document was not found.", ErrorType.NotFound) : Result<DocumentDto>.Success(await ToDtoAsync(value, cancellationToken));
    }
    public async Task<Result<DocumentDto>> RetryAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var value = await dbContext.Documents.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken); if (value is null) return Result<DocumentDto>.Failure("DOCUMENT_NOT_FOUND", "Document was not found.", ErrorType.NotFound);
        if (value.ProcessingStatus != DocumentProcessingStatus.Failed) return Result<DocumentDto>.Failure("DOCUMENT_NOT_FAILED", "Only failed documents can be retried.", ErrorType.Conflict);
        if (!CanRetry(value)) return Result<DocumentDto>.Failure("DOCUMENT_NOT_RETRYABLE", "This failure cannot be fixed by retrying the same file.", ErrorType.Conflict);
        if (!await storage.ExistsAsync(value.StorageKey, cancellationToken)) return Result<DocumentDto>.Failure("DOCUMENT_STORAGE_FAILED", "The stored document file is missing.", ErrorType.Conflict);
        await queue.EnqueueAsync(id, cancellationToken);
        return Result<DocumentDto>.Success(await ToDtoAsync(value, cancellationToken));
    }
    public async Task<Result<bool>> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var isPostgres = dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.Ordinal) == true; await using var transaction = isPostgres ? await dbContext.Database.BeginTransactionAsync(cancellationToken) : null;
        if (isPostgres) await dbContext.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(hashtext({0}))", [$"document:{id}"], cancellationToken);
        var value = await dbContext.Documents.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken); if (value is null) return Result<bool>.Failure("DOCUMENT_NOT_FOUND", "Document was not found.", ErrorType.NotFound);
        if (value.ProcessingStatus == DocumentProcessingStatus.Processing) return Result<bool>.Failure("DOCUMENT_PROCESSING", "A document cannot be deleted while it is processing.", ErrorType.Conflict);
        var storageKey = value.StorageKey; dbContext.Documents.Remove(value); await dbContext.SaveChangesAsync(cancellationToken); if (transaction is not null) await transaction.CommitAsync(cancellationToken); await storage.DeleteAsync(storageKey, cancellationToken); return Result<bool>.Success(true);
    }
    private async Task<bool> OwnsTargets(Guid userId, Guid? subjectId, Guid? studySetId, CancellationToken cancellationToken)
    {
        if (subjectId.HasValue && !await dbContext.Subjects.AnyAsync(x => x.Id == subjectId && x.UserId == userId, cancellationToken)) return false;
        if (studySetId.HasValue)
        {
            var set = await dbContext.StudySets.Where(x => x.Id == studySetId && dbContext.Subjects.Any(subject => subject.Id == x.SubjectId && subject.UserId == userId)).Select(x => new { x.SubjectId }).SingleOrDefaultAsync(cancellationToken);
            if (set is null || subjectId.HasValue && set.SubjectId != subjectId) return false;
        }
        return true;
    }
    private static bool HasValidStructure(byte[] content, DocumentFileType type)
    {
        if (type == DocumentFileType.Pdf) return content.Length >= 5 && content.AsSpan(0, 5).SequenceEqual("%PDF-"u8);
        if (type == DocumentFileType.Txt) { try { _ = new UTF8Encoding(false, true).GetString(content); return !content.AsSpan().Contains((byte)0); } catch (DecoderFallbackException) { return false; } }
        if (content.Length < 4 || content[0] != (byte)'P' || content[1] != (byte)'K') return false;
        try
        {
            using var archive = new ZipArchive(new MemoryStream(content, false), ZipArchiveMode.Read); if (archive.Entries.Count > 10_000 || archive.Entries.Sum(x => x.Length) > 100L * 1024 * 1024) return false;
            return archive.GetEntry("[Content_Types].xml") is not null && archive.GetEntry(type == DocumentFileType.Docx ? "word/document.xml" : "ppt/presentation.xml") is not null;
        }
        catch (InvalidDataException) { return false; }
    }
    private static string SanitizeName(string value) { var name = Path.GetFileName(value.Trim()); foreach (var invalid in Path.GetInvalidFileNameChars()) name = name.Replace(invalid, '_'); return string.IsNullOrWhiteSpace(name) ? "document" : name[..Math.Min(name.Length, 255)]; }
    private static Result<DocumentDto> Fail(string code, string message) => Result<DocumentDto>.Failure(code, message, ErrorType.Validation);
    private async Task<DocumentDto> ToDtoAsync(Document x, CancellationToken cancellationToken) => new(x.Id, x.SubjectId, x.StudySetId, x.OriginalFileName, x.MimeType, x.FileType, x.FileSize, x.ProcessingStatus, x.ExtractedText, x.FailureCode, x.ProcessingError, x.RetryCount, CanRetry(x), x.QueuedAt, x.ProcessingStartedAt, x.ProcessingCompletedAt, x.FailedAt, x.ProcessingRevision, await dbContext.DocumentChunks.CountAsync(c => c.DocumentId == x.Id, cancellationToken), x.CreatedAt);
    private static bool CanRetry(Document document) => document.ProcessingStatus == DocumentProcessingStatus.Failed && document.FailureCode is "DOCUMENT_STORAGE_FAILED" or "DOCUMENT_PROCESSING_TIMEOUT" or "DOCUMENT_PROCESSING_FAILED" or "DOCUMENT_EXTRACTION_FAILED";
    private static readonly Dictionary<string, DocumentFileType> Types = new(StringComparer.OrdinalIgnoreCase) { [".pdf"] = DocumentFileType.Pdf, [".docx"] = DocumentFileType.Docx, [".pptx"] = DocumentFileType.Pptx, [".txt"] = DocumentFileType.Txt };
    private static readonly Dictionary<DocumentFileType, string[]> AllowedMimeTypes = new() { [DocumentFileType.Pdf] = ["application/pdf"], [DocumentFileType.Docx] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/octet-stream"], [DocumentFileType.Pptx] = ["application/vnd.openxmlformats-officedocument.presentationml.presentation", "application/octet-stream"], [DocumentFileType.Txt] = ["text/plain", "application/octet-stream"] };
}
