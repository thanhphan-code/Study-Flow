using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyFlow.API.Errors;
using StudyFlow.Application.Common.Models;
using StudyFlow.Application.SmartLearning.DTOs;
using StudyFlow.Application.SmartLearning.Interfaces;

namespace StudyFlow.API.Controllers;

[ApiController]
[Authorize]
public sealed class SmartLearningController(ISmartLearningService smartLearning) : ControllerBase
{
    [HttpPost("api/study-sets/{studySetId:guid}/smart-learn")]
    public async Task<IActionResult> Start(Guid studySetId, StartSmartLearnRequest request, IValidator<StartSmartLearnRequest> validator, CancellationToken cancellationToken) { await validator.ValidateAndThrowAsync(request, cancellationToken); var result = await smartLearning.StartAsync(UserId(), studySetId, request, cancellationToken); return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, result.Value) : ToResponse(result); }
    [HttpGet("api/study-sets/{studySetId:guid}/smart-learn/active")]
    public async Task<IActionResult> Resume(Guid studySetId, CancellationToken cancellationToken) => ToResponse(await smartLearning.ResumeAsync(UserId(), studySetId, cancellationToken));
    [HttpPost("api/smart-learn/sessions/{sessionId:guid}/attempts")]
    public async Task<IActionResult> Record(Guid sessionId, RecordLearningAttemptRequest request, IValidator<RecordLearningAttemptRequest> validator, CancellationToken cancellationToken) { await validator.ValidateAndThrowAsync(request, cancellationToken); var result = await smartLearning.RecordAttemptAsync(UserId(), sessionId, request, cancellationToken); return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, result.Value) : ToResponse(result); }
    [HttpPost("api/smart-learn/sessions/{sessionId:guid}/complete")]
    public async Task<IActionResult> Complete(Guid sessionId, CancellationToken cancellationToken) => ToResponse(await smartLearning.CompleteAsync(UserId(), sessionId, cancellationToken));
    private Guid UserId() => Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
    private IActionResult ToResponse<T>(Result<T> result) { if (result.IsSuccess) return Ok(result.Value); var error=result.Error!;var body=new ApiError(error.Code,error.Message);return error.Type switch{ErrorType.NotFound=>NotFound(body),ErrorType.Conflict=>Conflict(body),ErrorType.Unauthorized=>Unauthorized(body),_=>BadRequest(body)}; }
}
