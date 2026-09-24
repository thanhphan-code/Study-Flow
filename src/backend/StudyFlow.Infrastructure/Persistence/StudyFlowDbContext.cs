using Microsoft.EntityFrameworkCore;
using StudyFlow.Domain.Entities;

namespace StudyFlow.Infrastructure.Persistence;

public sealed class StudyFlowDbContext(DbContextOptions<StudyFlowDbContext> options) : DbContext(options)
{
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<StudySetPublication> Publications => Set<StudySetPublication>();
    public DbSet<SocialRelationship> Relationships => Set<SocialRelationship>();
    public DbSet<SocialReaction> Reactions => Set<SocialReaction>();
    public DbSet<StudySetRemix> Remixes => Set<StudySetRemix>();
    public DbSet<StudySetComment> Comments => Set<StudySetComment>();
    public DbSet<SocialNotification> Notifications => Set<SocialNotification>();
    public DbSet<DirectConversation> Conversations => Set<DirectConversation>();
    public DbSet<DirectMessage> Messages => Set<DirectMessage>();
    public DbSet<ContentReport> Reports => Set<ContentReport>();
    public DbSet<BattleRoom> BattleRooms => Set<BattleRoom>();
    public DbSet<BattleParticipant> BattleParticipants => Set<BattleParticipant>();
    public DbSet<BattleQuestion> BattleQuestions => Set<BattleQuestion>();
    public DbSet<BattleAnswer> BattleAnswers => Set<BattleAnswer>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<StudySet> StudySets => Set<StudySet>();
    public DbSet<StudySetSource> StudySetSources => Set<StudySetSource>();
    public DbSet<Flashcard> Flashcards => Set<Flashcard>();
    public DbSet<FlashcardProgress> FlashcardProgress => Set<FlashcardProgress>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<AnswerOption> AnswerOptions => Set<AnswerOption>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
    public DbSet<QuizAnswer> QuizAnswers => Set<QuizAnswer>();
    public DbSet<StudySession> StudySessions => Set<StudySession>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<DocumentProcessingJob> DocumentProcessingJobs => Set<DocumentProcessingJob>();
    public DbSet<AIJob> AIJobs => Set<AIJob>();
    public DbSet<LearningAttempt> LearningAttempts => Set<LearningAttempt>();
    public DbSet<LearningDecision> LearningDecisions => Set<LearningDecision>();
    public DbSet<SourceReference> SourceReferences => Set<SourceReference>();
    public DbSet<SmartLearningCard> SmartLearningCards => Set<SmartLearningCard>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StudyFlowDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
