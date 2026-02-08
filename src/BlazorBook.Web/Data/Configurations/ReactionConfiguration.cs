using Content.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlazorBook.Web.Data.Configurations;

public class ReactionConfiguration : IEntityTypeConfiguration<ReactionDto>
{
    public void Configure(EntityTypeBuilder<ReactionDto> builder)
    {
        builder.ToTable("Reactions");
        
        builder.HasKey(r => r.Id);
        
        builder.Property(r => r.TenantId)
            .IsRequired()
            .HasMaxLength(128);
        
        builder.Property(r => r.TargetId)
            .IsRequired()
            .HasMaxLength(128);
        
        builder.Property(r => r.ActorId)
            .IsRequired()
            .HasMaxLength(128);
        
        builder.Property(r => r.CreatedAt)
            .IsRequired();
        
        // EntityRefDto as owned entity (JSON)
        builder.OwnsOne(r => r.Actor, actorBuilder =>
        {
            actorBuilder.ToJson();
            actorBuilder.Property(e => e.Type).IsRequired();
            actorBuilder.Property(e => e.Id).IsRequired();
        });
        
        // Indexes
        builder.HasIndex(r => new { r.TenantId, r.TargetId, r.TargetKind });
        builder.HasIndex(r => new { r.TenantId, r.TargetId, r.TargetKind, r.ActorId })
            .HasDatabaseName("IX_Reactions_Unique")
            .IsUnique();
    }
}
