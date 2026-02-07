using Identity.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlazorBook.Web.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<UserDto>
{
    public void Configure(EntityTypeBuilder<UserDto> builder)
    {
        builder.ToTable("Users");
        
        builder.HasKey(u => u.Id);
        
        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);
        
        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(128);
        
        builder.Property(u => u.DisplayName)
            .HasMaxLength(256);
        
        builder.Property(u => u.CreatedAt)
            .IsRequired();
        
        // Indexes
        builder.HasIndex(u => u.Email)
            .IsUnique();
        
        builder.HasIndex(u => u.Username)
            .IsUnique();
    }
}
