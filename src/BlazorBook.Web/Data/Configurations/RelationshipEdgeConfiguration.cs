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
            f.Property(e => e.Kind).IsRequired();
            f.Property(e => e.Type).IsRequired();
            f.Property(e => e.Id).IsRequired();
            f.Ignore(e => e.Meta);
        });
        
        builder.OwnsOne(r => r.To, t =>
        {
            t.ToJson();
            t.Property(e => e.Kind).IsRequired();
            t.Property(e => e.Type).IsRequired();
            t.Property(e => e.Id).IsRequired();
            t.Ignore(e => e.Meta);
        });
        
        // Composite unique index for relationship uniqueness
        builder.HasIndex(r => new { r.TenantId, r.Kind, r.Scope })
            .HasDatabaseName("IX_Relationships_Unique");
        
        // Indexes for queries
        builder.HasIndex(r => new { r.TenantId, r.IsActive });
        builder.HasIndex(r => r.Kind);
    }
}
