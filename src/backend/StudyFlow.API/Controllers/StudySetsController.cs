using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyFlow.API.Errors;
using StudyFlow.Application.Common.Models;
using StudyFlow.Application.StudySets.DTOs;
using StudyFlow.Application.StudySets.Interfaces;

namespace StudyFlow.API.Controllers;

[ApiController]
[Authorize]
public sealed class StudySetsController(IStudySetService studySetService) : ControllerBase
{
    [HttpGet("api/subjects/{subjectId:guid}/study-sets")]
    public async Task<IActionResult> List(Guid subjectId, CancellationToken cancellationToken) => ToResponse(await studySetService.ListAsync(CurrentUserId(), subjectId, cancellationToken));

    [HttpPost("api/subjects/{subjectId:guid}/study-sets")]
    public async Task<IActionResult> Create(Guid subjectId, CreateStudySetRequest request, IValidator<CreateStudySetRequest> validator, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var result = await studySetService.CreateAsync(CurrentUserId(), subjectId, request, cancellationToken);
        return result.IsSuccess ? CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value) : ToResponse(result);
    }

    [HttpGet("api/study-sets/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) => ToResponse(await studySetService.GetAsync(CurrentUserId(), id, cancellationToken));

    [HttpPost("api/subjects/{subjectId:guid}/combined-study-sets")]
    public async Task<IActionResult> CreateCombined(Guid subjectId, CreateCombinedStudySetRequest request, IValidator<CreateCombinedStudySetRequest> validator, CancellationToken cancellationToken) { await validator.ValidateAndThrowAsync(request, cancellationToken); var result = await studySetService.CreateCombinedAsync(CurrentUserId(), subjectId, request, cancellationToken); return result.IsSuccess ? CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value) : ToResponse(result); }

    [HttpPut("api/study-sets/{id:guid}/sources")]
    public async Task<IActionResult> UpdateSources(Guid id, UpdateStudySetSourcesRequest request, IValidator<UpdateStudySetSourcesRequest> validator, CancellationToken cancellationToken) { await validator.ValidateAndThrowAsync(request, cancellationToken); return ToResponse(await studySetService.UpdateSourcesAsync(CurrentUserId(), id, request, cancellationToken)); }

    [HttpPost("api/study-together")]
    public async Task<IActionResult> StudyTogether(StudyTogetherRequest request, IValidator<StudyTogetherRequest> validator, CancellationToken cancellationToken) { await validator.ValidateAndThrowAsync(request, cancellationToken); return ToResponse(await studySetService.StudyTogetherAsync(CurrentUserId(), request, cancellationToken)); }

    [HttpPut("api/study-sets/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateStudySetRequest request, IValidator<UpdateStudySetRequest> validator, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return ToResponse(await studySetService.UpdateAsync(CurrentUserId(), id, request, cancellationToken));
    }

    [HttpDelete("api/study-sets/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await studySetService.DeleteAsync(CurrentUserId(), id, cancellationToken);
        return result.IsSuccess ? NoContent() : ToResponse(result);
    }

    private Guid CurrentUserId() => Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);

    private IActionResult ToResponse<T>(Result<T> result)
    {
        if (result.IsSuccess) return Ok(result.Value);
        var error = result.Error!;
        var body = new ApiError(error.Code, error.Message);
        return error.Type switch
        {
            ErrorType.NotFound => NotFound(body),
            ErrorType.Conflict => Conflict(body),
            ErrorType.Unauthorized => Unauthorized(body),
            _ => BadRequest(body)
        };
    }
}
