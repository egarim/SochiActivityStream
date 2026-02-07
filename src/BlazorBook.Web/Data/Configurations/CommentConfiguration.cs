using Content.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BlazorBook.Web.Data.ValueConverters;

namespace BlazorBook.Web.Data.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<CommentDto>
{
    public void Configure(EntityTypeBuilder<CommentDto> builder)
    {
        builder.ToTable("Comments");
        
        builder.HasKey(c => c.Id);
        
        builder.Property(c => c.TenantId)
            .IsRequired()
            .HasMaxLength(128);
        
        builder.Property(c => c.PostId)
            .IsRequired()
            .HasMaxLength(128);
        
        builder.Property(c => c.ParentCommentId)
            .HasMaxLength(128);
        
        builder.Property(c => c.Body)
            .IsRequired()
            .HasMaxLength(5000);
        
        builder.Property(c => c.CreatedAt)
            .IsRequired();
        
        // EntityRefDto as owned entity (JSON)
        builder.OwnsOne(c => c.Author, a =>
        {
            a.ToJson();
            a.Property(e => e.Type).IsRequired();
            a.Property(e => e.Id).IsRequired();
        });
        
        // ReactionCounts as JSON
        builder.Property(c => c.ReactionCounts)
            .HasConversion(new JsonValueConverter<Dictionary<ReactionType, int>>())
            .HasColumnType("TEXT");
        
        // Global query filter for soft deletes
        builder.HasQueryFilter(c => !c.IsDeleted);
        
        // Indexes
        builder.HasIndex(c => new { c.TenantId, c.PostId, c.CreatedAt });
        builder.HasIndex(c => c.ParentCommentId)
            .HasFilter("ParentCommentId IS NOT NULL");
        builder.HasIndex(c => c.IsDeleted);
    }
}
