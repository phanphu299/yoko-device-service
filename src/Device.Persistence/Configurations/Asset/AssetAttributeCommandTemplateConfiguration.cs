using Device.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration
{
    public class AssetAttributeCommandTemplateConfiguration : IEntityTypeConfiguration<Domain.Entity.AssetAttributeCommandTemplate>
    {
        public void Configure(EntityTypeBuilder<AssetAttributeCommandTemplate> builder)
        {
            builder.ToTable("asset_attribute_template_commands");
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.AssetAttributeTemplateId).HasColumnName("asset_attribute_template_id");
            builder.Property(e => e.DeviceTemplateId).HasColumnName("device_template_id");
            builder.Property(e => e.MarkupName).HasColumnName("markup_name");
            builder.Property(e => e.MetricKey).HasColumnName("metric_key");
            builder.Property(e => e.CreatedUtc).HasColumnName("created_utc");
            builder.Property(e => e.UpdatedUtc).HasColumnName("updated_utc");
            builder.HasMany(e => e.AssetAttributeCommandMappings).WithOne(x => x.AssetAttributeCommandTemplate)
                .HasPrincipalKey(x => x.AssetAttributeTemplateId)
                .HasForeignKey(x => x.AssetAttributeTemplateId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
        }
    }
}