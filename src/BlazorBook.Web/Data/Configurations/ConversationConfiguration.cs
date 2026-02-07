using Chat.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BlazorBook.Web.Data.ValueConverters;

namespace BlazorBook.Web.Data.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<ConversationDto>
{
    public void Configure(EntityTypeBuilder<ConversationDto> builder)
    {
        builder.ToTable("Conversations");
        
        builder.HasKey(c => c.Id);
        
        builder.Property(c => c.TenantId)
            .IsRequired()
            .HasMaxLength(128);
        
        builder.Property(c => c.Title)
            .HasMaxLength(256);
        
        builder.Property(c => c.AvatarUrl)
            .HasMaxLength(2048);
        
        builder.Property(c => c.CreatedAt)
            .IsRequired();
        
        builder.Property(c => c.UpdatedAt)
            .IsRequired();
        
        // Participants as JSON
        builder.Property(c => c.Participants)
            .HasConversion(new JsonValueConverter<List<ConversationParticipantDto>>())
            .HasColumnType("TEXT");
        
        // LastMessage navigation (optional - can be populated via query)
        builder.Ignore(c => c.LastMessage);
        
        // Per-user properties (would need separate UserConversationSettings table in production)
        builder.Ignore(c => c.IsArchived);
        builder.Ignore(c => c.IsMuted);
        builder.Ignore(c => c.UnreadCount);
        
        // Indexes
        builder.HasIndex(c => new { c.TenantId, c.UpdatedAt });
        builder.HasIndex(c => c.Type);
    }
}
