using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyFlow.Application.DailyStudy.DTOs;
using StudyFlow.Application.DailyStudy.Interfaces;

namespace StudyFlow.API.Controllers;

[ApiController]
[Authorize]
[Route("api/daily-study")]
public sealed class DailyStudyController(IDailyStudyService dailyStudy) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DailyStudyRequest request, IValidator<DailyStudyRequest> validator, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var userId = Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
        return Ok(await dailyStudy.CreatePlanAsync(userId, request, cancellationToken));
    }
}
