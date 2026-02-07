using Identity.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlazorBook.Web.Data.Configurations;

public class MembershipConfiguration : IEntityTypeConfiguration<MembershipDto>
{
    public void Configure(EntityTypeBuilder<MembershipDto> builder)
    {
        builder.ToTable("Memberships");
        
        builder.HasKey(m => m.Id);
        
        builder.Property(m => m.TenantId)
            .IsRequired()
            .HasMaxLength(128);
        
        builder.Property(m => m.UserId)
            .IsRequired()
            .HasMaxLength(128);
        
        builder.Property(m => m.ProfileId)
            .IsRequired()
            .HasMaxLength(128);
        
        builder.Property(m => m.Role)
            .IsRequired()
            .HasMaxLength(64);
        
        builder.Property(m => m.CreatedAt)
            .IsRequired();
        
        // Indexes
        builder.HasIndex(m => new { m.TenantId, m.UserId });
        builder.HasIndex(m => m.ProfileId);
    }
}
