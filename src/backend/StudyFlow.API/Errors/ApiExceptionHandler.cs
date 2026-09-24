using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace StudyFlow.API.Errors;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is StudyFlow.Application.Social.SocialException social)
        {
            context.Response.StatusCode = social.Status;
            await context.Response.WriteAsJsonAsync(new ApiError("SOCIAL_ERROR", social.Message), cancellationToken);
            return true;
        }
        if (exception is ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(x => char.ToLowerInvariant(x.PropertyName[0]) + x.PropertyName[1..])
                .ToDictionary(group => group.Key, group => group.Select(x => x.ErrorMessage).ToArray());
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new ApiError("VALIDATION_ERROR", "Validation failed.", errors), cancellationToken);
            return true;
        }

        logger.LogError(exception, "Unhandled exception while processing {Path}", context.Request.Path);
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new ApiError("INTERNAL_ERROR", "An unexpected error occurred."), cancellationToken);
        return true;
    }
}

public sealed record ApiError(string Code, string Message, object? Errors = null);
