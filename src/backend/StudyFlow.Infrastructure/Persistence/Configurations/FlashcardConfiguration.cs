using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyFlow.Domain.Entities;

namespace StudyFlow.Infrastructure.Persistence.Configurations;

public sealed class FlashcardConfiguration : IEntityTypeConfiguration<Flashcard>
{
    public void Configure(EntityTypeBuilder<Flashcard> builder)
    {
        builder.ToTable("flashcards"); builder.HasKey(x => x.Id);
        builder.Property(x => x.FrontText).HasMaxLength(1000).IsRequired(); builder.Property(x => x.BackText).HasMaxLength(5000).IsRequired(); builder.Property(x => x.Explanation).HasMaxLength(5000);
        builder.Property(x => x.LanguageCode).HasMaxLength(35); builder.Property(x => x.ReadingText).HasMaxLength(500); builder.Property(x => x.Romanization).HasMaxLength(500);
        builder.Property(x => x.ExampleText).HasMaxLength(2000); builder.Property(x => x.ExampleTranslation).HasMaxLength(2000); builder.Property(x => x.MemoryTip).HasMaxLength(2000); builder.Property(x => x.AcceptedAnswers).HasMaxLength(2000);
        builder.Property(x => x.ImageStorageKey).HasMaxLength(500); builder.Property(x => x.ImageContentType).HasMaxLength(100); builder.Property(x => x.ImageFileName).HasMaxLength(255);
        builder.Property(x => x.OrderIndex).IsRequired(); builder.Property(x => x.CreatedAt).IsRequired(); builder.Property(x => x.UpdatedAt).IsRequired();
        builder.HasIndex(x => new { x.StudySetId, x.OrderIndex }).IsUnique();
        builder.HasOne<StudySet>().WithMany().HasForeignKey(x => x.StudySetId).OnDelete(DeleteBehavior.Cascade);
    }
}
