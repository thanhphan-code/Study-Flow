using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyFlow.Domain.Entities;

namespace StudyFlow.Infrastructure.Persistence.Configurations;
public sealed class StudySetSourceConfiguration : IEntityTypeConfiguration<StudySetSource>
{
    public void Configure(EntityTypeBuilder<StudySetSource> builder)
    {
        builder.ToTable("study_set_sources"); builder.HasKey(x => new { x.CombinedStudySetId, x.SourceStudySetId }); builder.Property(x => x.OrderIndex).IsRequired(); builder.HasIndex(x => new { x.CombinedStudySetId, x.OrderIndex }).IsUnique(); builder.HasIndex(x => x.SourceStudySetId);
        builder.HasOne<StudySet>().WithMany().HasForeignKey(x => x.CombinedStudySetId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<StudySet>().WithMany().HasForeignKey(x => x.SourceStudySetId).OnDelete(DeleteBehavior.Restrict);
    }
}
