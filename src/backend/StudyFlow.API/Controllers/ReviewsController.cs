using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyFlow.API.Errors;
using StudyFlow.Application.Common.Models;
using StudyFlow.Application.Progress.DTOs;
using StudyFlow.Application.Progress.Interfaces;

namespace StudyFlow.API.Controllers;

[ApiController]
[Authorize]
public sealed class ReviewsController(IFlashcardProgressService progressService) : ControllerBase
{
    [HttpPost("api/flashcards/{id:guid}/review")]
    public async Task<IActionResult> Review(Guid id, ReviewFlashcardRequest request, IValidator<ReviewFlashcardRequest> validator, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return ToResponse(await progressService.ReviewAsync(CurrentUserId(), id, request, cancellationToken));
    }

    [HttpGet("api/reviews/due")]
    public async Task<ActionResult<IReadOnlyList<DueFlashcardDto>>> Due(CancellationToken cancellationToken) => Ok(await progressService.GetDueAsync(CurrentUserId(), cancellationToken));

    private Guid CurrentUserId() => Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
    private IActionResult ToResponse<T>(Result<T> result)
    {
        if (result.IsSuccess) return Ok(result.Value);
        var error = result.Error!; var body = new ApiError(error.Code, error.Message);
        return error.Type switch { ErrorType.NotFound => NotFound(body), ErrorType.Conflict => Conflict(body), ErrorType.Unauthorized => Unauthorized(body), _ => BadRequest(body) };
    }
}
