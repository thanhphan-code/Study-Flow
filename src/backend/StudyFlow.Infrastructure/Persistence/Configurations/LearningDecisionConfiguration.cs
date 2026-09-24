using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyFlow.Domain.Entities;

namespace StudyFlow.Infrastructure.Persistence.Configurations;

public sealed class LearningDecisionConfiguration : IEntityTypeConfiguration<LearningDecision>
{
    public void Configure(EntityTypeBuilder<LearningDecision> builder)
    {
        builder.ToTable("learning_decisions"); builder.HasKey(x => x.Id);
        builder.Property(x => x.MasteryBefore).HasPrecision(5, 2); builder.Property(x => x.MasteryAfter).HasPrecision(5, 2);
        builder.Property(x => x.WeaknessBefore).HasPrecision(5, 2); builder.Property(x => x.WeaknessAfter).HasPrecision(5, 2);
        builder.Property(x => x.ActionBefore).HasMaxLength(100).IsRequired(); builder.Property(x => x.ActionAfter).HasMaxLength(100).IsRequired();
        builder.Property(x => x.StatePolicyVersion).HasMaxLength(50).IsRequired(); builder.Property(x => x.SchedulerVersion).HasMaxLength(50).IsRequired();
        builder.Property(x => x.DecisionTrace).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => x.LearningAttemptId).IsUnique();
        builder.HasOne<LearningAttempt>().WithOne().HasForeignKey<LearningDecision>(x => x.LearningAttemptId).OnDelete(DeleteBehavior.Cascade);
    }
}
