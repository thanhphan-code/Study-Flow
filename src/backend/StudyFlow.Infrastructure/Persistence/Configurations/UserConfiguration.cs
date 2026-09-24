using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyFlow.Domain.Entities;

namespace StudyFlow.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Email).HasMaxLength(254).IsRequired();
        builder.Property(x => x.NormalizedEmail).HasMaxLength(254).IsRequired();
        builder.HasIndex(x => x.NormalizedEmail).IsUnique();
        builder.Property(x => x.PasswordHash).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.TimeZoneId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.AvatarUrl).HasMaxLength(2048);
        builder.Property(x => x.RefreshTokenHash).HasMaxLength(64);
        builder.Property(x => x.EmailVerificationCodeHash).HasMaxLength(64);
        builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.SuspensionReason).HasMaxLength(500);
        builder.HasIndex(x => x.LastActiveAt);
        builder.HasIndex(x => new { x.Role, x.IsSuspended });
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
    }
}
