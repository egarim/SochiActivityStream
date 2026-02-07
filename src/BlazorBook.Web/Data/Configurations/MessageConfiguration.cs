using Chat.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BlazorBook.Web.Data.ValueConverters;
using EntityRefDto = Content.Abstractions.EntityRefDto;

namespace BlazorBook.Web.Data.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<MessageDto>
{
    public void Configure(EntityTypeBuilder<MessageDto> builder)
    {
        builder.ToTable("Messages");
        
        builder.HasKey(m => m.Id);
        
        builder.Property(m => m.TenantId)
            .IsRequired()
            .HasMaxLength(128);
        
        builder.Property(m => m.ConversationId)
            .IsRequired()
            .HasMaxLength(128);
        
        builder.Property(m => m.Body)
            .IsRequired()
            .HasMaxLength(10000);
        
        builder.Property(m => m.ReplyToMessageId)
            .HasMaxLength(128);
        
        builder.Property(m => m.CreatedAt)
            .IsRequired();
        
        // EntityRefDto as owned entity (JSON)
        builder.OwnsOne(m => m.Sender, s =>
        {
            s.ToJson();
            s.Property(e => e.Type).IsRequired();
            s.Property(e => e.Id).IsRequired();
        });
        
        // Media as JSON
        builder.Property(m => m.Media)
            .HasConversion(new JsonValueConverter<List<MediaRefDto>>())
            .HasColumnType("TEXT");
        
        // DeletedByProfileIds as JSON
        builder.Property(m => m.DeletedByProfileIds)
            .HasConversion(new JsonValueConverter<List<string>>())
            .HasColumnType("TEXT");
        
        // ReplyTo navigation (populated via query)
        builder.Ignore(m => m.ReplyTo);
        
        // Indexes
        builder.HasIndex(m => new { m.TenantId, m.ConversationId, m.CreatedAt });
        builder.HasIndex(m => m.ReplyToMessageId)
            .HasFilter("ReplyToMessageId IS NOT NULL");
        builder.HasIndex(m => m.IsDeleted);
    }
}
