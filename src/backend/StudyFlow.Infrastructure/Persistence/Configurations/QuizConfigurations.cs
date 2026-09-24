using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyFlow.Domain.Entities;

namespace StudyFlow.Infrastructure.Persistence.Configurations;

public sealed class QuizConfiguration : IEntityTypeConfiguration<Quiz>
{
    public void Configure(EntityTypeBuilder<Quiz> builder) { builder.ToTable("quizzes"); builder.HasKey(x => x.Id); builder.Property(x => x.Title).HasMaxLength(150).IsRequired(); builder.HasIndex(x => x.StudySetId); builder.HasOne<StudySet>().WithMany().HasForeignKey(x => x.StudySetId).OnDelete(DeleteBehavior.Cascade); }
}
public sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder) { builder.ToTable("questions"); builder.HasKey(x => x.Id); builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(30); builder.Property(x => x.QuestionText).HasMaxLength(2000).IsRequired(); builder.Property(x => x.Explanation).HasMaxLength(5000); builder.HasIndex(x => new { x.QuizId, x.OrderIndex }).IsUnique(); builder.HasOne<Quiz>().WithMany().HasForeignKey(x => x.QuizId).OnDelete(DeleteBehavior.Cascade); builder.HasOne<Flashcard>().WithMany().HasForeignKey(x => x.FlashcardId).OnDelete(DeleteBehavior.SetNull); }
}
public sealed class AnswerOptionConfiguration : IEntityTypeConfiguration<AnswerOption>
{
    public void Configure(EntityTypeBuilder<AnswerOption> builder) { builder.ToTable("answer_options"); builder.HasKey(x => x.Id); builder.Property(x => x.Text).HasMaxLength(5000).IsRequired(); builder.HasIndex(x => new { x.QuestionId, x.OrderIndex }).IsUnique(); builder.HasOne<Question>().WithMany().HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Cascade); }
}
public sealed class QuizAttemptConfiguration : IEntityTypeConfiguration<QuizAttempt>
{
    public void Configure(EntityTypeBuilder<QuizAttempt> builder) { builder.ToTable("quiz_attempts"); builder.HasKey(x => x.Id); builder.HasIndex(x => new { x.UserId, x.QuizId }); builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade); builder.HasOne<Quiz>().WithMany().HasForeignKey(x => x.QuizId).OnDelete(DeleteBehavior.Cascade); }
}
public sealed class QuizAnswerConfiguration : IEntityTypeConfiguration<QuizAnswer>
{
    public void Configure(EntityTypeBuilder<QuizAnswer> builder) { builder.ToTable("quiz_answers"); builder.HasKey(x => x.Id); builder.HasIndex(x => new { x.QuizAttemptId, x.QuestionId }).IsUnique(); builder.HasOne<QuizAttempt>().WithMany().HasForeignKey(x => x.QuizAttemptId).OnDelete(DeleteBehavior.Cascade); builder.HasOne<Question>().WithMany().HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Cascade); builder.HasOne<AnswerOption>().WithMany().HasForeignKey(x => x.AnswerOptionId).OnDelete(DeleteBehavior.Restrict); }
}
