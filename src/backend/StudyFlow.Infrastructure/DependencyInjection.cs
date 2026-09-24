using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StudyFlow.Application.Auth.Interfaces;
using StudyFlow.Domain.Entities;
using StudyFlow.Infrastructure.Authentication;
using StudyFlow.Infrastructure.Persistence;
using StudyFlow.Application.Subjects.Interfaces;
using StudyFlow.Infrastructure.Persistence.Services;
using StudyFlow.Application.StudySets.Interfaces;
using StudyFlow.Application.Flashcards.Interfaces;
using StudyFlow.Application.Progress.Interfaces;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StudyFlow.Application.Quizzes.Interfaces;
using StudyFlow.Application.StudySessions.Interfaces;
using StudyFlow.Application.Dashboard.Interfaces;
using StudyFlow.Application.LearningInsights.Interfaces;
using StudyFlow.Application.LearningHistory.Interfaces;
using StudyFlow.Application.Documents.Interfaces;
using StudyFlow.Infrastructure.Documents;
using StudyFlow.Application.AI.Interfaces;
using StudyFlow.Infrastructure.AI;
using StudyFlow.Application.SmartLearning.Interfaces;
using StudyFlow.Application.DailyStudy.Interfaces;
using StudyFlow.Application.LearningEngine.Interfaces;
using StudyFlow.Application.Time;
using StudyFlow.Infrastructure.Time;
using StudyFlow.Application.Grounding;
using StudyFlow.Infrastructure.Grounding;
using StudyFlow.Application.Admin;

namespace StudyFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
        services.AddDbContext<StudyFlowDbContext>(options => options.UseNpgsql(connectionString));
        services.AddOptions<JwtOptions>().Bind(configuration.GetSection(JwtOptions.SectionName));
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<TokenService>();
        services.AddOptions<EmailOptions>().Bind(configuration.GetSection(EmailOptions.SectionName));
        services.AddOptions<AdminOptions>().Bind(configuration.GetSection(AdminOptions.SectionName));
        services.AddScoped<StudyFlow.Application.Auth.Interfaces.IEmailVerificationSender, SmtpEmailVerificationSender>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<ISubjectService, SubjectService>();
        services.AddScoped<StudyFlow.Application.Social.ISocialService, SocialService>();
        services.AddScoped<IStudySetService, StudySetService>();
        services.AddScoped<IFlashcardService, FlashcardService>();
        services.AddScoped<IFlashcardProgressService, FlashcardProgressService>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IQuizService, QuizService>();
        services.AddScoped<IStudySessionService, StudySessionService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ILearningInsightsService, LearningInsightsService>();
        services.AddScoped<ILearningHistoryService, LearningHistoryService>();
        services.AddOptions<FileStorageOptions>().Bind(configuration.GetSection(FileStorageOptions.SectionName));
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();
        services.AddOptions<DocumentProcessingOptions>().Bind(configuration.GetSection(DocumentProcessingOptions.SectionName));
        services.AddSingleton<IDocumentTextExtractor, PdfTextExtractor>();
        services.AddSingleton<IDocumentTextExtractor, DocxTextExtractor>();
        services.AddSingleton<IDocumentTextExtractor, PptxTextExtractor>();
        services.AddSingleton<IDocumentTextExtractor, TxtTextExtractor>();
        services.AddSingleton<IDocumentChunker, DocumentChunker>();
        services.AddScoped<IDocumentProcessingQueue, DocumentProcessingQueue>();
        services.AddScoped<IDocumentProcessor, DocumentProcessor>();
        services.AddScoped<IDocumentContextService, DocumentContextService>();
        services.AddOptions<GroundingOptions>().Bind(configuration.GetSection(GroundingOptions.SectionName));
        services.AddScoped<IDocumentContextRetriever, DocumentContextRetriever>();
        services.AddScoped<IGroundingValidator, GroundingValidator>();
        services.AddScoped<ISourceReferenceService, SourceReferenceService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddHostedService<DocumentProcessingWorker>();
        services.AddOptions<GeminiOptions>().Bind(configuration.GetSection(GeminiOptions.SectionName));
        services.AddHttpClient<IAIService, GeminiService>((provider, client) => { var settings=provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<GeminiOptions>>().Value;client.BaseAddress=new Uri(settings.BaseUrl);client.Timeout=TimeSpan.FromSeconds(90); });
        services.AddScoped<IAIWorkflowService, AIWorkflowService>();
        services.AddScoped<ISmartLearningService, SmartLearningService>();
        services.AddScoped<IDailyStudyService, DailyStudyService>();
        services.AddScoped<ILearningEngine, LearningEngine>();
        services.AddScoped<StudyFlow.Application.Battles.IBattleService, BattleService>();
        services.AddSingleton<IUserCalendar, UserCalendar>();
        return services;
    }
}
