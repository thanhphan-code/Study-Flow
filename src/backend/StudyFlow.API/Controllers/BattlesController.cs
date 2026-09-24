using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StudyFlow.API.Errors;
using StudyFlow.Application.Battles;
using StudyFlow.Application.Common.Models;

namespace StudyFlow.API.Controllers;

[ApiController, Authorize, EnableRateLimiting("battles")]
[Route("api/battles")]
public sealed class BattlesController(IBattleService battles) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
    [HttpPost]
    public async Task<IActionResult> Create(CreateBattleRequest request, CancellationToken ct)
    {
        var result = await battles.CreateAsync(UserId, request, ct);
        return result.IsSuccess ? StatusCode(201, result.Value) : Respond(result);
    }
    [HttpGet("join/{code}")]
    public async Task<IActionResult> Find(string code, CancellationToken ct) => Respond(await battles.FindAsync(UserId, code, ct));
    [HttpGet("{id:guid}")]
    [HttpGet("{id:guid}/leaderboard")]
    [HttpGet("{id:guid}/results")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) => Respond(await battles.GetAsync(UserId, id, ct));
    [HttpPost("{id:guid}/join")]
    public async Task<IActionResult> Join(Guid id, CancellationToken ct) => Respond(await battles.ActAsync(UserId, id, "join", null, ct));
    [HttpPost("{id:guid}/leave")]
    public async Task<IActionResult> Leave(Guid id, CancellationToken ct) => Respond(await battles.ActAsync(UserId, id, "leave", null, ct));
    [HttpPost("{id:guid}/lock")]
    public async Task<IActionResult> Lock(Guid id, CancellationToken ct) => Respond(await battles.ActAsync(UserId, id, "lock", null, ct));
    [HttpPost("{id:guid}/unlock")]
    public async Task<IActionResult> Unlock(Guid id, CancellationToken ct) => Respond(await battles.ActAsync(UserId, id, "unlock", null, ct));
    [HttpPost("{id:guid}/start")]
    public async Task<IActionResult> Start(Guid id, CancellationToken ct) => Respond(await battles.ActAsync(UserId, id, "start", null, ct));
    [HttpPost("{id:guid}/next")]
    public async Task<IActionResult> Next(Guid id, CancellationToken ct) => Respond(await battles.ActAsync(UserId, id, "next", null, ct));
    [HttpPost("{id:guid}/end-question")]
    public async Task<IActionResult> EndQuestion(Guid id, CancellationToken ct) => Respond(await battles.ActAsync(UserId, id, "end-question", null, ct));
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct) => Respond(await battles.ActAsync(UserId, id, "cancel", null, ct));
    [HttpPost("{id:guid}/participants/{userId:guid}/kick")]
    public async Task<IActionResult> Kick(Guid id, Guid userId, CancellationToken ct) => Respond(await battles.ActAsync(UserId, id, "kick", userId, ct));
    [HttpPost("{id:guid}/questions/{questionId:guid}/answer")]
    public async Task<IActionResult> Answer(Guid id, Guid questionId, BattleAnswerRequest request, CancellationToken ct) => Respond(await battles.AnswerAsync(UserId, id, questionId, request, ct));
    private IActionResult Respond(Result<BattleRoomDto> result)
    {
        if (result.IsSuccess) return Ok(result.Value);
        var error = new ApiError(result.Error!.Code, result.Error.Message);
        return result.Error.Type switch { ErrorType.NotFound => NotFound(error), ErrorType.Unauthorized => Unauthorized(error), ErrorType.Validation => BadRequest(error), _ => Conflict(error) };
    }
}
