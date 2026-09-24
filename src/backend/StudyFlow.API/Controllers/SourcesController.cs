using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyFlow.API.Errors;
using StudyFlow.Application.Common.Models;
using StudyFlow.Application.Grounding;

namespace StudyFlow.API.Controllers;

[ApiController,Authorize]
public sealed class SourcesController(ISourceReferenceService sources):ControllerBase
{
    [HttpGet("api/flashcards/{id:guid}/sources")] public async Task<IActionResult> Flashcard(Guid id,CancellationToken token)=>ToResponse(await sources.GetForFlashcardAsync(UserId(),id,token));
    [HttpGet("api/quiz-questions/{id:guid}/sources")] public async Task<IActionResult> Question(Guid id,CancellationToken token)=>ToResponse(await sources.GetForQuestionAsync(UserId(),id,token));
    [HttpGet("api/ai/jobs/{id:guid}/sources")] public async Task<IActionResult> Job(Guid id,CancellationToken token)=>ToResponse(await sources.GetForJobAsync(UserId(),id,token));
    private Guid UserId()=>Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
    private IActionResult ToResponse<T>(Result<T> result)=>result.IsSuccess?Ok(result.Value):result.Error!.Type==ErrorType.NotFound?NotFound(new ApiError(result.Error.Code,result.Error.Message)):BadRequest(new ApiError(result.Error!.Code,result.Error.Message));
}
