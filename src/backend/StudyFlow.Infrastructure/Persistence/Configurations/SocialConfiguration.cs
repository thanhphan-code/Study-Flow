using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyFlow.Domain.Entities;

namespace StudyFlow.Infrastructure.Persistence.Configurations;

public sealed class SocialConfiguration : IEntityTypeConfiguration<UserProfile>, IEntityTypeConfiguration<StudySetPublication>,
    IEntityTypeConfiguration<SocialRelationship>, IEntityTypeConfiguration<SocialReaction>, IEntityTypeConfiguration<StudySetRemix>,
    IEntityTypeConfiguration<StudySetComment>, IEntityTypeConfiguration<SocialNotification>, IEntityTypeConfiguration<DirectConversation>,
    IEntityTypeConfiguration<DirectMessage>, IEntityTypeConfiguration<ContentReport>
{
    public void Configure(EntityTypeBuilder<UserProfile> b)
    {
        b.ToTable("user_profiles"); b.HasKey(x => x.Id);
        b.HasIndex(x => x.UserId).IsUnique(); b.HasIndex(x => x.Username).IsUnique();
        b.Property(x => x.Username).HasMaxLength(30); b.Property(x => x.DisplayName).HasMaxLength(100);
        b.Property(x => x.Bio).HasMaxLength(500); b.Property(x => x.AvatarUrl).HasMaxLength(1000); b.Property(x => x.Visibility).HasMaxLength(20);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
    public void Configure(EntityTypeBuilder<StudySetPublication> b)
    {
        b.ToTable("study_set_publications"); b.HasKey(x => x.Id); b.HasIndex(x => x.StudySetId).IsUnique();
        b.HasIndex(x => new { x.Visibility, x.PublishedAt }); b.Property(x => x.Visibility).HasMaxLength(20); b.Property(x => x.Tags).HasMaxLength(200);
        b.HasOne<StudySet>().WithMany().HasForeignKey(x => x.StudySetId).OnDelete(DeleteBehavior.Cascade);
    }
    public void Configure(EntityTypeBuilder<SocialRelationship> b)
    {
        b.ToTable("social_relationships"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.UserId, x.TargetUserId, x.Kind }).IsUnique();
        b.HasIndex(x => new { x.TargetUserId, x.Kind, x.Status }); b.Property(x => x.Kind).HasMaxLength(20); b.Property(x => x.Status).HasMaxLength(20);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.TargetUserId).OnDelete(DeleteBehavior.Cascade);
    }
    public void Configure(EntityTypeBuilder<SocialReaction> b)
    {
        b.ToTable("social_reactions"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.UserId, x.TargetId, x.Kind }).IsUnique();
        b.HasIndex(x => new { x.TargetId, x.Kind }); b.Property(x => x.Kind).HasMaxLength(20);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
    public void Configure(EntityTypeBuilder<StudySetRemix> b)
    {
        b.ToTable("study_set_remixes"); b.HasKey(x => x.Id); b.HasIndex(x => x.StudySetId).IsUnique();
        b.Property(x => x.OriginalTitleSnapshot).HasMaxLength(200); b.Property(x => x.OriginalAuthorDisplayNameSnapshot).HasMaxLength(100);
        b.HasOne<StudySet>().WithMany().HasForeignKey(x => x.StudySetId).OnDelete(DeleteBehavior.Cascade);
    }
    public void Configure(EntityTypeBuilder<StudySetComment> b)
    {
        b.ToTable("study_set_comments"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.StudySetId, x.CreatedAt });
        b.Property(x => x.Content).HasMaxLength(2000);
        b.HasOne<StudySet>().WithMany().HasForeignKey(x => x.StudySetId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
    public void Configure(EntityTypeBuilder<SocialNotification> b)
    {
        b.ToTable("social_notifications"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.UserId, x.CreatedAt });
        b.HasIndex(x => new { x.UserId, x.ActorId, x.Kind, x.EntityId }).IsUnique(); b.Property(x => x.Kind).HasMaxLength(30);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
    public void Configure(EntityTypeBuilder<DirectConversation> b)
    {
        b.ToTable("direct_conversations"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.UserAId, x.UserBId }).IsUnique();
        b.HasIndex(x => x.UserBId);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserAId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserBId).OnDelete(DeleteBehavior.Cascade);
    }
    public void Configure(EntityTypeBuilder<DirectMessage> b)
    {
        b.ToTable("direct_messages"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.ConversationId, x.CreatedAt });
        b.Property(x => x.Content).HasMaxLength(4000); b.Property(x => x.Kind).HasMaxLength(30);
        b.HasOne<DirectConversation>().WithMany().HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
    }
    public void Configure(EntityTypeBuilder<ContentReport> b)
    {
        b.ToTable("content_reports"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.Status, x.CreatedAt });
        b.Property(x => x.Reason).HasMaxLength(40); b.Property(x => x.TargetType).HasMaxLength(20);
        b.Property(x => x.Status).HasMaxLength(20); b.Property(x => x.Details).HasMaxLength(2000);
    }
}
