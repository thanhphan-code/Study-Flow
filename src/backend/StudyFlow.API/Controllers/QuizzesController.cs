using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyFlow.API.Errors;
using StudyFlow.Application.Common.Models;
using StudyFlow.Application.Quizzes.DTOs;
using StudyFlow.Application.Quizzes.Interfaces;

namespace StudyFlow.API.Controllers;

[ApiController]
[Authorize]
public sealed class QuizzesController(IQuizService quizService) : ControllerBase
{
    [HttpGet("api/study-sets/{studySetId:guid}/quizzes")]
    public async Task<IActionResult> List(Guid studySetId, CancellationToken cancellationToken) => ToResponse(await quizService.ListAsync(CurrentUserId(), studySetId, cancellationToken));
    [HttpPost("api/study-sets/{studySetId:guid}/quizzes")]
    public async Task<IActionResult> Create(Guid studySetId, CreateQuizRequest request, IValidator<CreateQuizRequest> validator, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken); var result = await quizService.CreateAsync(CurrentUserId(), studySetId, request, cancellationToken);
        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, result.Value) : ToResponse(result);
    }
    [HttpPost("api/study-sets/{studySetId:guid}/quizzes/manual")]
    public async Task<IActionResult> CreateManual(Guid studySetId, CreateManualQuizRequest request, IValidator<CreateManualQuizRequest> validator, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken); var result = await quizService.CreateManualAsync(CurrentUserId(), studySetId, request, cancellationToken);
        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, result.Value) : ToResponse(result);
    }
    [HttpPost("api/quizzes/{id:guid}/attempts")]
    public async Task<IActionResult> Start(Guid id, CancellationToken cancellationToken) => ToResponse(await quizService.StartAttemptAsync(CurrentUserId(), id, cancellationToken));
    [HttpPost("api/quiz-attempts/{id:guid}/answers")]
    public async Task<IActionResult> Answer(Guid id, SubmitQuizAnswerRequest request, IValidator<SubmitQuizAnswerRequest> validator, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken); return ToResponse(await quizService.SubmitAnswerAsync(CurrentUserId(), id, request, cancellationToken));
    }
    [HttpPost("api/quiz-attempts/{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken) => ToResponse(await quizService.CompleteAttemptAsync(CurrentUserId(), id, cancellationToken));
    private Guid CurrentUserId() => Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
    private IActionResult ToResponse<T>(Result<T> result)
    {
        if (result.IsSuccess) return Ok(result.Value); var error = result.Error!; var body = new ApiError(error.Code, error.Message);
        return error.Type switch { ErrorType.NotFound => NotFound(body), ErrorType.Conflict => Conflict(body), ErrorType.Unauthorized => Unauthorized(body), _ => BadRequest(body) };
    }
}
