using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyFlow.Domain.Entities;

namespace StudyFlow.Infrastructure.Persistence.Configurations;

public sealed class StudySetConfiguration : IEntityTypeConfiguration<StudySet>
{
    public void Configure(EntityTypeBuilder<StudySet> builder)
    {
        builder.ToTable("study_sets"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(150).IsRequired(); builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired(); builder.Property(x => x.UpdatedAt).IsRequired(); builder.HasIndex(x => x.SubjectId);
        builder.HasOne<Subject>().WithMany().HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);
    }
}
