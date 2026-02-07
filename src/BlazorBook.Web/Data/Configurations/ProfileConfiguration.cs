using Identity.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlazorBook.Web.Data.Configurations;

public class ProfileConfiguration : IEntityTypeConfiguration<ProfileDto>
{
    public void Configure(EntityTypeBuilder<ProfileDto> builder)
    {
        builder.ToTable("Profiles");
        
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Handle)
            .IsRequired()
            .HasMaxLength(128);
        
        builder.Property(p => p.DisplayName)
            .HasMaxLength(256);
        
        builder.Property(p => p.AvatarUrl)
            .HasMaxLength(2048);
        
        builder.Property(p => p.CreatedAt)
            .IsRequired();
        
        // Indexes
        builder.HasIndex(p => p.Handle)
            .IsUnique();
    }
}
