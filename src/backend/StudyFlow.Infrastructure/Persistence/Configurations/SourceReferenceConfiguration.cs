using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyFlow.Domain.Entities;

namespace StudyFlow.Infrastructure.Persistence.Configurations;

public sealed class SourceReferenceConfiguration : IEntityTypeConfiguration<SourceReference>
{
    public void Configure(EntityTypeBuilder<SourceReference> builder)
    {
        builder.ToTable("source_references"); builder.HasKey(x=>x.Id); builder.Property(x=>x.ContentType).HasConversion<string>().HasMaxLength(30); builder.Property(x=>x.SectionTitle).HasMaxLength(500); builder.Property(x=>x.ChunkContentHash).HasMaxLength(64).IsRequired(); builder.Property(x=>x.RetrievalRevision).HasMaxLength(40).IsRequired(); builder.Property(x=>x.GroundingRevision).HasMaxLength(40).IsRequired(); builder.Property(x=>x.DocumentNameSnapshot).HasMaxLength(255).IsRequired(); builder.Property(x=>x.SourceSnippetSnapshot).HasMaxLength(1000).IsRequired();
        builder.HasIndex(x=>new{x.ContentType,x.ContentId}); builder.HasIndex(x=>x.DocumentId); builder.HasIndex(x=>x.DocumentChunkId);
        builder.HasOne<Document>().WithMany().HasForeignKey(x=>x.DocumentId).OnDelete(DeleteBehavior.SetNull); builder.HasOne<DocumentChunk>().WithMany().HasForeignKey(x=>x.DocumentChunkId).OnDelete(DeleteBehavior.SetNull);
    }
}
