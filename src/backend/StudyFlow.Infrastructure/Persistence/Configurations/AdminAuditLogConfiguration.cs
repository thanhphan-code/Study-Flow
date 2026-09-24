using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyFlow.Domain.Entities;

namespace StudyFlow.Infrastructure.Persistence.Configurations;

public sealed class AdminAuditLogConfiguration : IEntityTypeConfiguration<AdminAuditLog>
{
    public void Configure(EntityTypeBuilder<AdminAuditLog> builder)
    {
        builder.ToTable("admin_audit_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Action).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.MetadataJson).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => new { x.TargetUserId, x.CreatedAt });
        builder.HasIndex(x => new { x.ActorUserId, x.CreatedAt });
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.TargetUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
