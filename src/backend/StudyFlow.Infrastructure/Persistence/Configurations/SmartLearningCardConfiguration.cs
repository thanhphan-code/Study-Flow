using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyFlow.Domain.Entities;

namespace StudyFlow.Infrastructure.Persistence.Configurations;

public sealed class SmartLearningCardConfiguration : IEntityTypeConfiguration<SmartLearningCard>
{
    public void Configure(EntityTypeBuilder<SmartLearningCard> builder)
    {
        builder.ToTable("smart_learning_cards"); builder.HasKey(x => x.Id);
        builder.Property(x => x.AttemptType).HasConversion<string>().HasMaxLength(30).IsRequired(); builder.Property(x => x.Direction).HasConversion<string>().HasMaxLength(10).IsRequired(); builder.Property(x => x.Outcome).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(x => new { x.StudySessionId, x.FlashcardId }).IsUnique(); builder.HasIndex(x => new { x.StudySessionId, x.Outcome, x.NextEligibleAttemptNumber });
        builder.HasOne<StudySession>().WithMany().HasForeignKey(x => x.StudySessionId).OnDelete(DeleteBehavior.Cascade); builder.HasOne<Flashcard>().WithMany().HasForeignKey(x => x.FlashcardId).OnDelete(DeleteBehavior.Cascade);
    }
}
