using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyFlow.API.Errors;
using StudyFlow.Application.AI.DTOs;
using StudyFlow.Application.AI.Interfaces;
using StudyFlow.Application.Common.Models;
namespace StudyFlow.API.Controllers;
[ApiController][Authorize]
public sealed class AIController(IAIWorkflowService workflow):ControllerBase
{
    [HttpPost("api/study-sets/{studySetId:guid}/ai/generate")]
    public async Task<IActionResult> Generate(Guid studySetId,GenerateStudyMaterialRequest request,IValidator<GenerateStudyMaterialRequest> validator,CancellationToken cancellationToken){await validator.ValidateAndThrowAsync(request,cancellationToken);return ToResponse(await workflow.GenerateAsync(UserId(),studySetId,request,cancellationToken));}
    [HttpGet("api/ai/jobs/{id:guid}")]
    public async Task<IActionResult> Job(Guid id,CancellationToken cancellationToken)=>ToResponse(await workflow.GetJobAsync(UserId(),id,cancellationToken));
    [HttpPost("api/ai/jobs/{id:guid}/save")]
    public async Task<IActionResult> Save(Guid id,SaveAIDraftRequest request,IValidator<SaveAIDraftRequest> validator,CancellationToken cancellationToken){await validator.ValidateAndThrowAsync(request,cancellationToken);return ToResponse(await workflow.SaveDraftAsync(UserId(),id,request,cancellationToken));}
    [HttpPost("api/flashcards/{id:guid}/ai/explain")]
    public async Task<IActionResult> Explain(Guid id,CancellationToken cancellationToken)=>ToResponse(await workflow.ExplainAsync(UserId(),id,cancellationToken));
    [HttpPost("api/flashcards/{id:guid}/ai/similar-question")]
    public async Task<IActionResult> Similar(Guid id,CancellationToken cancellationToken)=>ToResponse(await workflow.SimilarQuestionAsync(UserId(),id,cancellationToken));
    [HttpPost("api/flashcards/{id:guid}/ai/hint")]
    public async Task<IActionResult> Hint(Guid id,AIHintRequest request,IValidator<AIHintRequest> validator,CancellationToken cancellationToken){await validator.ValidateAndThrowAsync(request,cancellationToken);return ToResponse(await workflow.HintAsync(UserId(),id,request.Level,cancellationToken));}
    private Guid UserId()=>Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
    private IActionResult ToResponse<T>(Result<T> result){if(result.IsSuccess)return Ok(result.Value);var error=result.Error!;var body=new ApiError(error.Code,error.Message);return error.Type switch{ErrorType.NotFound=>NotFound(body),ErrorType.Conflict=>Conflict(body),_=>BadRequest(body)};}
}
