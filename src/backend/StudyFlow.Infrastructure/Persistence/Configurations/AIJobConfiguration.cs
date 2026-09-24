using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudyFlow.Domain.Entities;
namespace StudyFlow.Infrastructure.Persistence.Configurations;
public sealed class AIJobConfiguration:IEntityTypeConfiguration<AIJob>
{
    public void Configure(EntityTypeBuilder<AIJob> builder){builder.ToTable("ai_jobs");builder.HasKey(x=>x.Id);builder.Property(x=>x.JobType).HasConversion<string>().HasMaxLength(40);builder.Property(x=>x.Status).HasConversion<string>().HasMaxLength(20);builder.Property(x=>x.ErrorMessage).HasMaxLength(1000);builder.Property(x=>x.RetrievalRevision).HasMaxLength(40);builder.Property(x=>x.GroundingContextJson).HasColumnType("jsonb");builder.HasIndex(x=>new{x.UserId,x.CreatedAt});builder.HasIndex(x=>x.DocumentId);builder.HasOne<User>().WithMany().HasForeignKey(x=>x.UserId).OnDelete(DeleteBehavior.Cascade);builder.HasOne<Document>().WithMany().HasForeignKey(x=>x.DocumentId).OnDelete(DeleteBehavior.SetNull);builder.HasOne<StudySet>().WithMany().HasForeignKey(x=>x.StudySetId).OnDelete(DeleteBehavior.Cascade);}
}
