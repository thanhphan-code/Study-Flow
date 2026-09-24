using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyFlow.API.Errors;
using StudyFlow.Application.Common.Models;
using StudyFlow.Application.Flashcards.DTOs;
using StudyFlow.Application.Flashcards.Interfaces;

namespace StudyFlow.API.Controllers;

[ApiController]
[Authorize]
public sealed class FlashcardsController(IFlashcardService flashcardService) : ControllerBase
{
    [HttpGet("api/study-sets/{studySetId:guid}/flashcards")]
    public async Task<IActionResult> List(Guid studySetId, CancellationToken cancellationToken) => ToResponse(await flashcardService.ListAsync(CurrentUserId(), studySetId, cancellationToken));

    [HttpPost("api/study-sets/{studySetId:guid}/flashcards")]
    public async Task<IActionResult> Create(Guid studySetId, CreateFlashcardRequest request, IValidator<CreateFlashcardRequest> validator, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var result = await flashcardService.CreateAsync(CurrentUserId(), studySetId, request, cancellationToken);
        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, result.Value) : ToResponse(result);
    }

    [HttpPost("api/study-sets/{studySetId:guid}/flashcards/bulk")]
    public async Task<IActionResult> BulkCreate(Guid studySetId, BulkCreateFlashcardsRequest request, IValidator<BulkCreateFlashcardsRequest> validator, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var result = await flashcardService.BulkCreateAsync(CurrentUserId(), studySetId, request, cancellationToken);
        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, result.Value) : ToResponse(result);
    }

    [HttpPut("api/flashcards/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateFlashcardRequest request, IValidator<UpdateFlashcardRequest> validator, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return ToResponse(await flashcardService.UpdateAsync(CurrentUserId(), id, request, cancellationToken));
    }

    [HttpDelete("api/flashcards/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await flashcardService.DeleteAsync(CurrentUserId(), id, cancellationToken);
        return result.IsSuccess ? NoContent() : ToResponse(result);
    }

    [HttpPost("api/flashcards/{id:guid}/image")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(Guid id, IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null) return BadRequest(new ApiError("IMAGE_REQUIRED", "An image file is required."));
        await using var stream = new MemoryStream(); await file.CopyToAsync(stream, cancellationToken);
        var result = await flashcardService.UploadImageAsync(CurrentUserId(), id, new FlashcardImageUpload(stream.ToArray(), file.FileName, file.ContentType), cancellationToken);
        return ToResponse(result);
    }

    [HttpGet("api/flashcards/{id:guid}/image")]
    public async Task<IActionResult> GetImage(Guid id, CancellationToken cancellationToken)
    {
        var result = await flashcardService.GetImageAsync(CurrentUserId(), id, cancellationToken);
        return result.IsSuccess ? File(result.Value!.Content, result.Value.ContentType, result.Value.FileName) : ToResponse(result);
    }

    [HttpDelete("api/flashcards/{id:guid}/image")]
    public async Task<IActionResult> DeleteImage(Guid id, CancellationToken cancellationToken)
    {
        var result = await flashcardService.DeleteImageAsync(CurrentUserId(), id, cancellationToken);
        return result.IsSuccess ? NoContent() : ToResponse(result);
    }

    private Guid CurrentUserId() => Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
    private IActionResult ToResponse<T>(Result<T> result)
    {
        if (result.IsSuccess) return Ok(result.Value);
        var error = result.Error!; var body = new ApiError(error.Code, error.Message);
        return error.Type switch { ErrorType.NotFound => NotFound(body), ErrorType.Conflict => Conflict(body), ErrorType.Unauthorized => Unauthorized(body), _ => BadRequest(body) };
    }
}
