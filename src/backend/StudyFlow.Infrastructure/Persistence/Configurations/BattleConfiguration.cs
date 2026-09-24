using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyFlow.Domain.Entities;

namespace StudyFlow.Infrastructure.Persistence.Configurations;

public sealed class BattleRoomConfiguration : IEntityTypeConfiguration<BattleRoom>
{
    public void Configure(EntityTypeBuilder<BattleRoom> b)
    {
        b.ToTable("battle_rooms");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(120);
        b.Property(x => x.JoinCode).HasMaxLength(6);
        b.HasIndex(x => x.JoinCode).IsUnique();
        b.HasIndex(x => new { x.Status, x.ExpiresAt });
        b.HasOne<User>().WithMany().HasForeignKey(x => x.HostUserId).OnDelete(DeleteBehavior.Restrict);
        // Source ID is historical provenance. Room questions are immutable snapshots and survive source deletion.
    }
}
public sealed class BattleParticipantConfiguration : IEntityTypeConfiguration<BattleParticipant>
{
    public void Configure(EntityTypeBuilder<BattleParticipant> b)
    {
        b.ToTable("battle_participants");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.BattleRoomId, x.UserId }).IsUnique();
        b.HasOne<BattleRoom>().WithMany().HasForeignKey(x => x.BattleRoomId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class BattleQuestionConfiguration : IEntityTypeConfiguration<BattleQuestion>
{
    public void Configure(EntityTypeBuilder<BattleQuestion> b)
    {
        b.ToTable("battle_questions");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.BattleRoomId, x.OrderIndex }).IsUnique();
        b.HasOne<BattleRoom>().WithMany().HasForeignKey(x => x.BattleRoomId).OnDelete(DeleteBehavior.Cascade);
    }
}
public sealed class BattleAnswerConfiguration : IEntityTypeConfiguration<BattleAnswer>
{
    public void Configure(EntityTypeBuilder<BattleAnswer> b)
    {
        b.ToTable("battle_answers");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.BattleQuestionId, x.UserId }).IsUnique();
        b.HasOne<BattleQuestion>().WithMany().HasForeignKey(x => x.BattleQuestionId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<BattleParticipant>().WithMany().HasForeignKey(x => new { x.BattleRoomId, x.UserId })
            .HasPrincipalKey(x => new { x.BattleRoomId, x.UserId }).OnDelete(DeleteBehavior.Restrict);
    }
}
