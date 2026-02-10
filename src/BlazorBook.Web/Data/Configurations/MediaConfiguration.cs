using Media.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BlazorBook.Web.Data.ValueConverters;

namespace BlazorBook.Web.Data.Configurations;

public class MediaConfiguration : IEntityTypeConfiguration<MediaDto>
{
    public void Configure(EntityTypeBuilder<MediaDto> builder)
    {
        builder.ToTable("Media");
        
        builder.HasKey(m => m.Id);
        
        builder.Property(m => m.TenantId)
            .IsRequired()
            .HasMaxLength(128);
        
        builder.Property(m => m.FileName)
            .IsRequired()
            .HasMaxLength(512);
        
        builder.Property(m => m.ContentType)
            .IsRequired()
            .HasMaxLength(128);
        
        builder.Property(m => m.BlobPath)
            .HasMaxLength(1024);
        
        builder.Property(m => m.ThumbnailBlobPath)
            .HasMaxLength(1024);
        
        builder.Property(m => m.CreatedAt)
            .IsRequired();
        
        // EntityRefDto as owned entity (JSON)
        builder.OwnsOne(m => m.Owner, o =>
        {
            o.ToJson();
            o.Ignore(e => e.Meta);
            o.Ignore(e => e.AvatarUrl);
            o.Ignore(e => e.Display);
            o.Ignore(e => e.Kind);
            o.Property(e => e.Type).IsRequired();
            o.Property(e => e.Id).IsRequired();
        });
        
        // Metadata as JSON
        builder.Property(m => m.Metadata)
            .HasConversion(new JsonValueConverter<Dictionary<string, string>>());
        
        // Computed properties (URLs generated on read)
        builder.Ignore(m => m.Url);
        builder.Ignore(m => m.ThumbnailUrl);
        
        // Indexes
        builder.HasIndex(m => new { m.TenantId, m.Status, m.CreatedAt });
        builder.HasIndex(m => m.DeletedAt)
            .HasFilter("DeletedAt IS NOT NULL");
    }
}
