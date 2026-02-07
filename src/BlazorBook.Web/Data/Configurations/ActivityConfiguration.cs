using ActivityStream.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BlazorBook.Web.Data.ValueConverters;

namespace BlazorBook.Web.Data.Configurations;

public class ActivityConfiguration : IEntityTypeConfiguration<ActivityDto>
{
    public void Configure(EntityTypeBuilder<ActivityDto> builder)
    {
        builder.ToTable("Activities");
        
        builder.HasKey(a => a.Id);
        
        builder.Property(a => a.TenantId)
            .IsRequired()
            .HasMaxLength(128);
        
        builder.Property(a => a.TypeKey)
            .IsRequired()
            .HasMaxLength(128);
        
        builder.Property(a => a.Summary)
            .HasMaxLength(1000);
        
        builder.Property(a => a.OccurredAt)
            .IsRequired();
        
        builder.Property(a => a.CreatedAt)
            .IsRequired();
        
        // EntityRefDto as owned entities (JSON)
        builder.OwnsOne(a => a.Actor, actor =>
        {
            actor.ToJson();
            actor.Property(e => e.Kind).IsRequired();
            actor.Property(e => e.Type).IsRequired();
            actor.Property(e => e.Id).IsRequired();
            actor.Ignore(e => e.Meta);
        });
        
        builder.OwnsOne(a => a.Owner, owner =>
        {
            owner.ToJson();
            owner.Property(e => e.Kind).IsRequired();
            owner.Property(e => e.Type).IsRequired();
            owner.Property(e => e.Id).IsRequired();
            owner.Ignore(e => e.Meta);
        });
        
        // Source as owned entity (JSON)
        builder.OwnsOne(a => a.Source, s =>
        {
            s.ToJson();
            s.Property(src => src.System).IsRequired();
        });
        
        // Targets as JSON
        builder.Property(a => a.Targets)
            .HasConversion(new JsonValueConverter<List<EntityRefDto>>())
            .HasColumnType("TEXT");
        
        // Tags as JSON
        builder.Property(a => a.Tags)
            .HasConversion(new JsonValueConverter<List<string>>())
            .HasColumnType("TEXT");
        
        // Payload as JSON (dynamic object)
        builder.Property(a => a.Payload)
            .HasConversion(new JsonValueConverter<object>())
            .HasColumnType("TEXT");
        
        // Indexes
        builder.HasIndex(a => new { a.TenantId, a.OccurredAt });
        builder.HasIndex(a => a.TypeKey);
        builder.HasIndex(a => a.Visibility);
    }
}
