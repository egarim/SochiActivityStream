using Inbox.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BlazorBook.Web.Data.ValueConverters;
using ActivityStream.Abstractions;

namespace BlazorBook.Web.Data.Configurations;

public class InboxItemConfiguration : IEntityTypeConfiguration<InboxItemDto>
{
    public void Configure(EntityTypeBuilder<InboxItemDto> builder)
    {
        builder.ToTable("InboxItems");
        
        builder.HasKey(i => i.Id);
        
        builder.Property(i => i.TenantId)
            .IsRequired()
            .HasMaxLength(128);
        
        builder.Property(i => i.Title)
            .HasMaxLength(512);
        
        builder.Property(i => i.Body)
            .HasMaxLength(2000);
        
        builder.Property(i => i.DedupKey)
            .HasMaxLength(256);
        
        builder.Property(i => i.ThreadKey)
            .HasMaxLength(256);
        
        builder.Property(i => i.CreatedAt)
            .IsRequired();
        
        // EntityRefDto as owned entity (JSON) - from ActivityStream.Abstractions
        builder.OwnsOne(i => i.Recipient, recipientBuilder =>
        {
            recipientBuilder.ToJson();
            recipientBuilder.Ignore(e => e.Meta);
            recipientBuilder.Ignore(e => e.AvatarUrl);
            recipientBuilder.Ignore(e => e.Display);
            recipientBuilder.Ignore(e => e.Kind);
            recipientBuilder.Property(e => e.Type).IsRequired();
            recipientBuilder.Property(e => e.Id).IsRequired();
        });
        
        // Event as owned entity (JSON)
        builder.OwnsOne(i => i.Event, eventBuilder =>
        {
            eventBuilder.ToJson();
            eventBuilder.Property(evt => evt.Kind).IsRequired();
            eventBuilder.Property(evt => evt.Id).IsRequired();
        });
        
        // Targets as JSON
        builder.Property(i => i.Targets)
            .HasConversion(new JsonValueConverter<List<EntityRefDto>>())
            .HasColumnType("TEXT");
        
        // Data as JSON (dynamic dictionary)
        builder.Property(i => i.Data)
            .HasConversion(new JsonValueConverter<Dictionary<string, object?>>())
            .HasColumnType("TEXT");
        
        // Indexes
        builder.HasIndex(i => new { i.TenantId, i.Status, i.CreatedAt });
        builder.HasIndex(i => i.DedupKey)
            .IsUnique()
            .HasFilter("DedupKey IS NOT NULL");
        builder.HasIndex(i => i.ThreadKey)
            .HasFilter("ThreadKey IS NOT NULL");
    }
}
