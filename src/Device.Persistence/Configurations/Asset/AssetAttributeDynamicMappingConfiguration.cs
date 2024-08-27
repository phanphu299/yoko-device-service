using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration
{
    public class AssetAttributeDynamicMappingConfiguration : IEntityTypeConfiguration<Domain.Entity.AssetAttributeDynamicMapping>
    {
        public void Configure(EntityTypeBuilder<Domain.Entity.AssetAttributeDynamicMapping> builder)
        {
            // configure the model.
            builder.ToTable("asset_attribute_dynamic_mapping");
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.AssetId).HasColumnName("asset_id");
            builder.Property(e => e.AssetAttributeTemplateId).HasColumnName("asset_attribute_template_id");
            builder.Property(e => e.DeviceId).HasColumnName("device_id");
            builder.Property(e => e.MetricKey).HasColumnName("metric_key");
            builder.Property(e => e.CreatedUtc).HasColumnName("created_utc");
            builder.Property(e => e.UpdatedUtc).HasColumnName("updated_utc");
            builder.Property(e => e.SequentialNumber).HasColumnName("sequential_number");
            builder.HasIndex(x => new { x.MetricKey, x.DeviceId }).IsUnique();

        }
    }
}
