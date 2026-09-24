using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using StudyFlow.Application.Progress.Interfaces;
using StudyFlow.Application.Progress.Services;
using StudyFlow.Application.LearningEngine.Interfaces;
using StudyFlow.Application.LearningEngine.Services;

namespace StudyFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddSingleton<ISpacedRepetitionService, SpacedRepetitionService>();
        services.AddSingleton<ISrsScheduler, SpacedRepetitionService>();
        services.AddSingleton<ILearningStatePolicy, LearningStatePolicy>();
        services.AddSingleton<IAnswerEvaluator, AnswerEvaluator>();
        return services;
    }
}
