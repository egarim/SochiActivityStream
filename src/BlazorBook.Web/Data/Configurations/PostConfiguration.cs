using Content.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BlazorBook.Web.Data.ValueConverters;

namespace BlazorBook.Web.Data.Configurations;

public class PostConfiguration : IEntityTypeConfiguration<PostDto>
{
    public void Configure(EntityTypeBuilder<PostDto> builder)
    {
        builder.ToTable("Posts");
        
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.TenantId)
            .IsRequired()
            .HasMaxLength(128);
        
        builder.Property(p => p.Body)
            .IsRequired()
            .HasMaxLength(10000);
        
        builder.Property(p => p.CreatedAt)
            .IsRequired();
        
        // EntityRefDto as owned entity (JSON)
        builder.OwnsOne(p => p.Author, a =>
        {
            a.ToJson();
            a.Property(e => e.Type).IsRequired();
            a.Property(e => e.Id).IsRequired();
        });
        
        // MediaIds as JSON
        builder.Property(p => p.MediaIds)
            .HasConversion(new JsonValueConverter<List<string>>())
            .HasColumnType("TEXT");
        
        // ReactionCounts as JSON
        builder.Property(p => p.ReactionCounts)
            .HasConversion(new JsonValueConverter<Dictionary<ReactionType, int>>())
            .HasColumnType("TEXT");
        
        // Global query filter for soft deletes
        builder.HasQueryFilter(p => !p.IsDeleted);
        
        // Indexes
        builder.HasIndex(p => new { p.TenantId, p.CreatedAt });
        builder.HasIndex(p => p.IsDeleted);
    }
}
