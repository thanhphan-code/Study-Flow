using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StudyFlow.API.Errors;
using StudyFlow.Application.Admin;
using StudyFlow.Application.Common.Models;

namespace StudyFlow.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting("admin")]
public sealed class AdminController(IAdminService admin) : ControllerBase
{
    private Guid ActorId => Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);

    [HttpGet("overview")]
    public async Task<ActionResult<AdminOverviewDto>> Overview(CancellationToken cancellationToken) =>
        Ok(await admin.GetOverviewAsync(cancellationToken));

    [HttpGet("users")]
    public async Task<ActionResult<AdminPagedResult<AdminUserListItemDto>>> Users(
        CancellationToken cancellationToken, string? search = null, string status = "all", string role = "all", int page = 1, int pageSize = 25) =>
        Ok(await admin.GetUsersAsync(search, status, role, page, pageSize, cancellationToken));

    [HttpGet("users/{id:guid}")]
    public async Task<IActionResult> UserDetails(Guid id, CancellationToken cancellationToken) =>
        ToResponse(await admin.GetUserAsync(id, cancellationToken));

    [HttpPut("users/{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateUserStatusRequest request, IValidator<UpdateUserStatusRequest> validator, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return ToResponse(await admin.UpdateStatusAsync(ActorId, id, request, cancellationToken));
    }

    [HttpPut("users/{id:guid}/role")]
    public async Task<IActionResult> UpdateRole(Guid id, UpdateUserRoleRequest request, IValidator<UpdateUserRoleRequest> validator, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        return ToResponse(await admin.UpdateRoleAsync(ActorId, id, request, cancellationToken));
    }

    [HttpPost("users/{id:guid}/revoke-sessions")]
    public async Task<IActionResult> RevokeSessions(Guid id, RevokeUserSessionsRequest request, IValidator<RevokeUserSessionsRequest> validator, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var result = await admin.RevokeSessionsAsync(ActorId, id, request, cancellationToken);
        return result.IsSuccess ? NoContent() : ToResponse(result);
    }

    [HttpGet("audit")]
    public async Task<ActionResult<AdminPagedResult<AdminAuditItemDto>>> Audit(CancellationToken cancellationToken, int page = 1, int pageSize = 30) =>
        Ok(await admin.GetAuditAsync(page, pageSize, cancellationToken));

    private IActionResult ToResponse<T>(Result<T> result)
    {
        if (result.IsSuccess) return Ok(result.Value);
        var error = result.Error!;
        var body = new ApiError(error.Code, error.Message);
        return error.Type switch
        {
            ErrorType.Unauthorized => Unauthorized(body),
            ErrorType.Conflict => Conflict(body),
            ErrorType.NotFound => NotFound(body),
            _ => BadRequest(body)
        };
    }
}
