using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BlazorBook.Web.Data.ValueConverters;

namespace BlazorBook.Web.Data.Configurations;

/// <summary>
/// Entity for storing password hashes separately from users
/// </summary>
public class UserPasswordEntity
{
    public string UserId { get; set; } = string.Empty;
    public byte[] Salt { get; set; } = Array.Empty<byte>();
    public int Iterations { get; set; }
    public byte[] HashBytes { get; set; } = Array.Empty<byte>();
    public string Algorithm { get; set; } = string.Empty;
}

public class UserPasswordConfiguration : IEntityTypeConfiguration<UserPasswordEntity>
{
    public void Configure(EntityTypeBuilder<UserPasswordEntity> builder)
    {
        builder.ToTable("UserPasswords");
        
        builder.HasKey(p => p.UserId);
        
        builder.Property(p => p.UserId)
            .IsRequired()
            .HasMaxLength(128);
        
        builder.Property(p => p.Salt)
            .IsRequired();
        
        builder.Property(p => p.HashBytes)
            .IsRequired();
        
        builder.Property(p => p.Algorithm)
            .IsRequired()
            .HasMaxLength(64);
    }
}
