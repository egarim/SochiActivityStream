using Search.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BlazorBook.Web.Data.ValueConverters;

namespace BlazorBook.Web.Data.Configurations;

public class SearchDocumentConfiguration : IEntityTypeConfiguration<SearchDocument>
{
    public void Configure(EntityTypeBuilder<SearchDocument> builder)
    {
        builder.ToTable("SearchDocuments");
        
        builder.HasKey(s => s.Id);
        
        builder.Property(s => s.TenantId)
            .IsRequired()
            .HasMaxLength(128);
        
        builder.Property(s => s.DocumentType)
            .IsRequired()
            .HasMaxLength(64);
        
        builder.Property(s => s.IndexedAt)
            .IsRequired();
        
        // Dictionary fields as JSON
        builder.Property(s => s.TextFields)
            .HasConversion(new JsonValueConverter<Dictionary<string, string>>())
            .HasColumnType("TEXT");
        
        builder.Property(s => s.KeywordFields)
            .HasConversion(new JsonValueConverter<Dictionary<string, List<string>>>())
            .HasColumnType("TEXT");
        
        builder.Property(s => s.NumericFields)
            .HasConversion(new JsonValueConverter<Dictionary<string, double>>())
            .HasColumnType("TEXT");
        
        builder.Property(s => s.DateFields)
            .HasConversion(new JsonValueConverter<Dictionary<string, DateTimeOffset>>())
            .HasColumnType("TEXT");
        
        // SourceEntity as JSON (nullable object)
        builder.Property(s => s.SourceEntity)
            .HasConversion(new JsonValueConverter<object?>())
            .HasColumnType("TEXT");
        
        // Indexes
        builder.HasIndex(s => new { s.TenantId, s.DocumentType, s.IndexedAt });
        builder.HasIndex(s => s.Boost);
    }
}
