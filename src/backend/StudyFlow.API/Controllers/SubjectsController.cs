using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyFlow.API.Errors;
using StudyFlow.Application.Common.Models;
using StudyFlow.Application.Subjects.DTOs;
using StudyFlow.Application.Subjects.Interfaces;

namespace StudyFlow.API.Controllers;

[ApiController]
[Authorize]
[Route("api/subjects")]
public sealed class SubjectsController(ISubjectService subjectService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SubjectDto>>> List(CancellationToken cancellationToken) =>
        Ok(await subjectService.ListAsync(CurrentUserId(), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        ToResponse(await subjectService.GetAsync(CurrentUserId(), id, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(CreateSubjectRequest request, IValidator<CreateSubjectRequest> validator, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var result = await subjectService.CreateAsync(CurrentUserId(), request, cancellationToken);
        return result.IsSuccess ? CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value) : ToResponse(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateSubjectRequest request, IValidator<UpdateSubjectRequest> validator, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return ToResponse(await subjectService.UpdateAsync(CurrentUserId(), id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await subjectService.DeleteAsync(CurrentUserId(), id, cancellationToken);
        return result.IsSuccess ? NoContent() : ToResponse(result);
    }

    private Guid CurrentUserId()
    {
        var subject = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(subject, out var userId) ? userId : throw new UnauthorizedAccessException("Access token subject is invalid.");
    }

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
