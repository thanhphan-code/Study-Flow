using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyFlow.API.Errors;
using StudyFlow.Application.Common.Models;
using StudyFlow.Application.StudySessions.DTOs;
using StudyFlow.Application.StudySessions.Interfaces;

namespace StudyFlow.API.Controllers;

[ApiController]
[Authorize]
[Route("api/study-sessions")]
public sealed class StudySessionsController(IStudySessionService sessionService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Start(StartStudySessionRequest request, IValidator<StartStudySessionRequest> validator, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var result = await sessionService.StartAsync(CurrentUserId(), request, cancellationToken);
        return result.IsSuccess ? Created($"/api/study-sessions/{result.Value!.Id}", result.Value) : ToResponse(result);
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken) => ToResponse(await sessionService.CompleteAsync(CurrentUserId(), id, cancellationToken));

    private Guid CurrentUserId() => Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
    private IActionResult ToResponse<T>(Result<T> result)
    {
        if (result.IsSuccess) return Ok(result.Value);
        var error = result.Error!; var body = new ApiError(error.Code, error.Message);
        return error.Type switch { ErrorType.NotFound => NotFound(body), ErrorType.Conflict => Conflict(body), ErrorType.Unauthorized => Unauthorized(body), _ => BadRequest(body) };
    }
}
