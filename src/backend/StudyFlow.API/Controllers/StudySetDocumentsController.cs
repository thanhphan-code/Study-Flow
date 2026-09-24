using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyFlow.API.Errors;
using StudyFlow.Application.Documents.DTOs;
using StudyFlow.Application.Documents.Interfaces;

namespace StudyFlow.API.Controllers;

[ApiController]
[Authorize]
[Route("api/study-sets/{studySetId:guid}/documents")]
public sealed class StudySetDocumentsController(IDocumentService documentService) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(21 * 1024 * 1024)]
    public async Task<IActionResult> Upload(Guid studySetId, IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null) return BadRequest(new ApiError("FILE_REQUIRED", "A document file is required."));
        await using var stream = new MemoryStream(); await file.CopyToAsync(stream, cancellationToken);
        var result = await documentService.UploadAsync(CurrentUserId(), new DocumentUpload(stream.ToArray(), file.FileName, file.ContentType, null, studySetId), cancellationToken);
        if (result.IsSuccess) return Accepted($"/api/documents/{result.Value!.Id}", result.Value);
        return result.Error!.Type == Application.Common.Models.ErrorType.NotFound ? NotFound(new ApiError(result.Error.Code, result.Error.Message)) : BadRequest(new ApiError(result.Error.Code, result.Error.Message));
    }
    private Guid CurrentUserId() => Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
}
