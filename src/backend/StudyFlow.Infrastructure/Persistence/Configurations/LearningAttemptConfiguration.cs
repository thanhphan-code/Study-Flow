using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyFlow.Domain.Entities;

namespace StudyFlow.Infrastructure.Persistence.Configurations;

public sealed class LearningAttemptConfiguration : IEntityTypeConfiguration<LearningAttempt>
{
    public void Configure(EntityTypeBuilder<LearningAttempt> builder)
    {
        builder.ToTable("learning_attempts"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Mode).HasConversion<string>().HasMaxLength(20).IsRequired(); builder.Property(x => x.AttemptType).HasConversion<string>().HasMaxLength(30).IsRequired(); builder.Property(x => x.Direction).HasConversion<string>().HasMaxLength(10).IsRequired(); builder.Property(x => x.Result).HasConversion<string>().HasMaxLength(10).IsRequired(); builder.Property(x => x.SubmittedAnswer).HasMaxLength(5000).IsRequired();
        builder.Property(x => x.CommittedOutcome).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.CommittedRating).HasConversion<string>().HasMaxLength(10);
        builder.HasIndex(x => new { x.UserId, x.ClientAttemptId }).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.CreatedAt }); builder.HasIndex(x => new { x.FlashcardId, x.CreatedAt }); builder.HasIndex(x => x.StudySessionId);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Flashcard>().WithMany().HasForeignKey(x => x.FlashcardId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<StudySession>().WithMany().HasForeignKey(x => x.StudySessionId).OnDelete(DeleteBehavior.Cascade).IsRequired(false);
    }
}
