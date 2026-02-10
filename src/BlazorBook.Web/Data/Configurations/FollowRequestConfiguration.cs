using Inbox.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BlazorBook.Web.Data.ValueConverters;
using RelationshipService.Abstractions;

namespace BlazorBook.Web.Data.Configurations;

public class FollowRequestConfiguration : IEntityTypeConfiguration<FollowRequestDto>
{
    public void Configure(EntityTypeBuilder<FollowRequestDto> builder)
    {
        builder.ToTable("FollowRequests");
        
        builder.HasKey(f => f.Id);
        
        builder.Property(f => f.TenantId)
            .IsRequired()
            .HasMaxLength(128);
        
        builder.Property(f => f.IdempotencyKey)
            .HasMaxLength(256);
        
        //  Filter is ignored for now (optional relationship filter)
        builder.Ignore(f => f.Filter);
        
        builder.Property(f => f.DecisionReason)
            .HasMaxLength(1000);
        
        builder.Property(f => f.CreatedAt)
            .IsRequired();
        
        // EntityRefDto as owned entities (JSON)
        builder.OwnsOne(f => f.Requester, r =>
        {
            r.ToJson();
            r.Ignore(e => e.Meta);
            r.Ignore(e => e.AvatarUrl);
            r.Ignore(e => e.Display);
            r.Ignore(e => e.Kind);
            r.Property(e => e.Type).IsRequired();
            r.Property(e => e.Id).IsRequired();
        });
        
        builder.OwnsOne(f => f.Target, t =>
        {
            t.ToJson();
            t.Ignore(e => e.Meta);
            t.Ignore(e => e.AvatarUrl);
            t.Ignore(e => e.Display);
            t.Ignore(e => e.Kind);
            t.Property(e => e.Type).IsRequired();
            t.Property(e => e.Id).IsRequired();
        });
        
        builder.OwnsOne(f => f.DecidedBy, d =>
        {
            d.ToJson();
            d.Ignore(e => e.Meta);
            d.Ignore(e => e.AvatarUrl);
            d.Ignore(e => e.Display);
            d.Ignore(e => e.Kind);
            d.Property(e => e.Type).IsRequired();
            d.Property(e => e.Id).IsRequired();
        });
        
        // Indexes
        builder.HasIndex(f => new { f.TenantId, f.Status, f.CreatedAt });
        builder.HasIndex(f => f.IdempotencyKey)
            .IsUnique()
            .HasFilter("IdempotencyKey IS NOT NULL");
    }
}
