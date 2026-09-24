using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyFlow.Domain.Entities;

namespace StudyFlow.Infrastructure.Persistence.Configurations;

public sealed class FlashcardProgressConfiguration : IEntityTypeConfiguration<FlashcardProgress>
{
    public void Configure(EntityTypeBuilder<FlashcardProgress> builder)
    {
        builder.ToTable("flashcard_progress"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired(); builder.Property(x => x.EaseFactor).IsRequired(); builder.Property(x => x.MasteryScore).HasPrecision(5, 2).IsRequired(); builder.Property(x => x.WeaknessScore).HasPrecision(5, 2).IsRequired();
        builder.Property(x => x.SchedulerVersion).HasMaxLength(50).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired(); builder.Property(x => x.UpdatedAt).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.FlashcardId }).IsUnique(); builder.HasIndex(x => new { x.UserId, x.NextReviewAt });
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Flashcard>().WithMany().HasForeignKey(x => x.FlashcardId).OnDelete(DeleteBehavior.Cascade);
    }
}
