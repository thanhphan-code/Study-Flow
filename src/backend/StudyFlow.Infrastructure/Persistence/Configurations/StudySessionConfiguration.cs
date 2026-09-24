using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyFlow.Domain.Entities;

namespace StudyFlow.Infrastructure.Persistence.Configurations;

public sealed class StudySessionConfiguration : IEntityTypeConfiguration<StudySession>
{
    public void Configure(EntityTypeBuilder<StudySession> builder)
    {
        builder.ToTable("study_sessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Mode).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Ignore(x => x.IsCompleted);
        builder.Ignore(x => x.TotalAnswers);
        builder.HasIndex(x => new { x.UserId, x.StartedAt });
        builder.HasIndex(x => x.StudySetId);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<StudySet>().WithMany().HasForeignKey(x => x.StudySetId).OnDelete(DeleteBehavior.Cascade);
    }
}
