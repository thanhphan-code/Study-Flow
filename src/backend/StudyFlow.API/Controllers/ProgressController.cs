using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyFlow.API.Errors;
using StudyFlow.Application.Common.Models;
using StudyFlow.Application.LearningInsights.DTOs;
using StudyFlow.Application.LearningInsights.Interfaces;

namespace StudyFlow.API.Controllers;

[ApiController]
[Authorize]
public sealed class ProgressController(ILearningInsightsService insightsService) : ControllerBase
{
    [HttpGet("api/progress/overview")]
    public async Task<ActionResult<ProgressOverviewDto>> Overview(CancellationToken cancellationToken) => Ok(await insightsService.GetOverviewAsync(CurrentUserId(), cancellationToken));

    [HttpGet("api/study-sets/{id:guid}/progress")]
    public async Task<IActionResult> StudySet(Guid id, CancellationToken cancellationToken) => ToResponse(await insightsService.GetStudySetProgressAsync(CurrentUserId(), id, cancellationToken));

    [HttpGet("api/progress/weak-cards")]
    public async Task<ActionResult<IReadOnlyList<WeakCardDto>>> WeakCards(CancellationToken cancellationToken) => Ok(await insightsService.GetWeakCardsAsync(CurrentUserId(), cancellationToken));

    private Guid CurrentUserId() => Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
    private IActionResult ToResponse<T>(Result<T> result)
    {
        if (result.IsSuccess) return Ok(result.Value);
        var error = result.Error!; var body = new ApiError(error.Code, error.Message);
        return error.Type == ErrorType.NotFound ? NotFound(body) : BadRequest(body);
    }
}
