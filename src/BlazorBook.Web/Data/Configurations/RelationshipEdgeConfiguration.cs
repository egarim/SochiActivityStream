using RelationshipService.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlazorBook.Web.Data.Configurations;

public class RelationshipEdgeConfiguration : IEntityTypeConfiguration<RelationshipEdgeDto>
{
    public void Configure(EntityTypeBuilder<RelationshipEdgeDto> builder)
    {
        builder.ToTable("RelationshipEdges");
        
        builder.HasKey(r => r.Id);
        
        builder.Property(r => r.TenantId)
            .IsRequired()
            .HasMaxLength(128);
        
        // Filter is a complex type not supported by SQLite, ignore it
        builder.Ignore(r => r.Filter);
        
        builder.Property(r => r.CreatedAt)
            .IsRequired();
        
        // EntityRefDto as owned entities (JSON)
        builder.OwnsOne(r => r.From, f =>
        {
            f.ToJson();
            f.Ignore(e => e.Meta);
            f.Ignore(e => e.AvatarUrl);
            f.Ignore(e => e.Display);
            f.Ignore(e => e.Kind);
            f.Property(e => e.Type).IsRequired();
            f.Property(e => e.Id).IsRequired();
        });
        
        builder.OwnsOne(r => r.To, t =>
        {
            t.ToJson();
            t.Ignore(e => e.Meta);
            t.Ignore(e => e.AvatarUrl);
            t.Ignore(e => e.Display);
            t.Ignore(e => e.Kind);
            t.Property(e => e.Type).IsRequired();
            t.Property(e => e.Id).IsRequired();
        });
        
        // Composite unique index for relationship uniqueness
        builder.HasIndex(r => new { r.TenantId, r.Kind, r.Scope })
            .HasDatabaseName("IX_Relationships_Unique");
        
        // Indexes for queries
        builder.HasIndex(r => new { r.TenantId, r.IsActive });
        builder.HasIndex(r => r.Kind);
    }
}
