using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyFlow.Domain.Entities;

namespace StudyFlow.Infrastructure.Persistence.Configurations;

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents"); builder.HasKey(x => x.Id);
        builder.Property(x => x.FileName).HasMaxLength(255).IsRequired(); builder.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired(); builder.Property(x => x.MimeType).HasMaxLength(150).IsRequired();
        builder.Property(x => x.FileType).HasConversion<string>().HasMaxLength(10); builder.Property(x => x.StorageKey).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ProcessingStatus).HasConversion<string>().HasMaxLength(20); builder.Property(x => x.ProcessingError).HasMaxLength(1000); builder.Property(x => x.FailureCode).HasMaxLength(80); builder.Property(x => x.ProcessingRevision).HasMaxLength(40).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.CreatedAt }); builder.HasIndex(x => x.SubjectId); builder.HasIndex(x => x.StudySetId);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Subject>().WithMany().HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<StudySet>().WithMany().HasForeignKey(x => x.StudySetId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.ToTable("document_chunks"); builder.HasKey(x => x.Id); builder.Property(x => x.Content).IsRequired(); builder.Property(x => x.SectionTitle).HasMaxLength(500); builder.Property(x => x.ContentHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => new { x.DocumentId, x.ChunkIndex }).IsUnique(); builder.HasIndex(x => new { x.DocumentId, x.ContentHash });
        builder.HasOne<Document>().WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class DocumentProcessingJobConfiguration : IEntityTypeConfiguration<DocumentProcessingJob>
{
    public void Configure(EntityTypeBuilder<DocumentProcessingJob> builder)
    {
        builder.ToTable("document_processing_jobs"); builder.HasKey(x => x.Id); builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20); builder.Property(x => x.LastErrorCode).HasMaxLength(80); builder.Property(x => x.LastErrorMessage).HasMaxLength(1000);
        builder.HasIndex(x => new { x.Status, x.AvailableAt }); builder.HasIndex(x => x.DocumentId);
        builder.HasOne<Document>().WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
    }
}
