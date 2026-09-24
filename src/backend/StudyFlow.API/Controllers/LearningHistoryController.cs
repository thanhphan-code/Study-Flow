using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyFlow.API.Errors;
using StudyFlow.Application.LearningHistory.DTOs;
using StudyFlow.Application.LearningHistory.Interfaces;

namespace StudyFlow.API.Controllers;

[ApiController]
[Authorize]
[Route("api/progress")]
public sealed class LearningHistoryController(ILearningHistoryService historyService) : ControllerBase
{
    [HttpGet("streak")]
    public async Task<ActionResult<StreakDto>> Streak(CancellationToken cancellationToken) => Ok(await historyService.GetStreakAsync(CurrentUserId(), cancellationToken));

    [HttpGet("history")]
    public async Task<ActionResult<LearningHistoryDto>> History([FromQuery] int days = 30, CancellationToken cancellationToken = default)
    {
        if (days is < 7 or > 365) return BadRequest(new ApiError("INVALID_HISTORY_RANGE", "Days must be between 7 and 365."));
        return Ok(await historyService.GetHistoryAsync(CurrentUserId(), days, cancellationToken));
    }

    private Guid CurrentUserId() => Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
}
