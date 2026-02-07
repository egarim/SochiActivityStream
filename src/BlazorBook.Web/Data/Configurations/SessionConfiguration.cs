using Identity.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BlazorBook.Web.Data.ValueConverters;

namespace BlazorBook.Web.Data.Configurations;

public class SessionConfiguration : IEntityTypeConfiguration<SessionDto>
{
    public void Configure(EntityTypeBuilder<SessionDto> builder)
    {
        builder.ToTable("Sessions");
        
        builder.HasKey(s => s.SessionId);
        
        builder.Property(s => s.UserId)
            .IsRequired()
            .HasMaxLength(128);
        
        builder.Property(s => s.TenantId)
            .IsRequired()
            .HasMaxLength(128);
        
        builder.Property(s => s.AccessToken)
            .IsRequired()
            .HasMaxLength(512);
        
        builder.Property(s => s.ExpiresAt)
            .IsRequired();
        
        // ProfileIds as JSON
        builder.Property(s => s.ProfileIds)
            .HasConversion(new JsonValueConverter<List<string>>())
            .HasColumnType("TEXT");
        
        // Indexes
        builder.HasIndex(s => s.AccessToken)
            .IsUnique();
        
        builder.HasIndex(s => new { s.UserId, s.ExpiresAt });
    }
}
