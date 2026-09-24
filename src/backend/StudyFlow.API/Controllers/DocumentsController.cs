using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyFlow.API.Errors;
using StudyFlow.Application.Common.Models;
using StudyFlow.Application.Documents.DTOs;
using StudyFlow.Application.Documents.Interfaces;

namespace StudyFlow.API.Controllers;

[ApiController]
[Authorize]
[Route("api/documents")]
public sealed class DocumentsController(IDocumentService documentService) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(21 * 1024 * 1024)]
    public Task<IActionResult> Upload(IFormFile file, [FromForm] Guid? subjectId, [FromForm] Guid? studySetId, CancellationToken cancellationToken) => UploadCore(file, subjectId, studySetId, cancellationToken);

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DocumentListItemDto>>> List(CancellationToken cancellationToken) => Ok(await documentService.ListAsync(CurrentUserId(), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) => ToResponse(await documentService.GetAsync(CurrentUserId(), id, cancellationToken));

    [HttpPost("{id:guid}/retry")]
    public async Task<IActionResult> Retry(Guid id, CancellationToken cancellationToken)
    {
        var result = await documentService.RetryAsync(CurrentUserId(), id, cancellationToken);
        return result.IsSuccess ? AcceptedAtAction(nameof(Get), new { id }, result.Value) : ToResponse(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) { var result = await documentService.DeleteAsync(CurrentUserId(), id, cancellationToken); return result.IsSuccess ? NoContent() : ToResponse(result); }

    private async Task<IActionResult> UploadCore(IFormFile? file, Guid? subjectId, Guid? studySetId, CancellationToken cancellationToken)
    {
        if (file is null) return BadRequest(new ApiError("FILE_REQUIRED", "A document file is required."));
        await using var stream = new MemoryStream(); await file.CopyToAsync(stream, cancellationToken);
        var result = await documentService.UploadAsync(CurrentUserId(), new DocumentUpload(stream.ToArray(), file.FileName, file.ContentType, subjectId, studySetId), cancellationToken);
        return result.IsSuccess ? AcceptedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value) : ToResponse(result);
    }
    private Guid CurrentUserId() => Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
    private IActionResult ToResponse<T>(Result<T> result)
    {
        if (result.IsSuccess) return Ok(result.Value); var error = result.Error!; var body = new ApiError(error.Code, error.Message);
        return error.Type == ErrorType.NotFound ? NotFound(body) : error.Type == ErrorType.Conflict ? Conflict(body) : BadRequest(body);
    }
}
