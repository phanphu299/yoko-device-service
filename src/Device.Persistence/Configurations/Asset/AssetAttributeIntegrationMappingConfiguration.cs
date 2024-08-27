using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration
{
    public class AssetAttributeIntegrationMappingConfiguration : IEntityTypeConfiguration<Domain.Entity.AssetAttributeIntegrationMapping>
    {
        public void Configure(EntityTypeBuilder<Domain.Entity.AssetAttributeIntegrationMapping> builder)
        {
            // configure the model.
            builder.ToTable("asset_attribute_integration_mapping");
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.AssetId).HasColumnName("asset_id");
            builder.Property(e => e.AssetAttributeTemplateId).HasColumnName("asset_attribute_template_id");
            builder.Property(e => e.IntegrationId).HasColumnName("integration_id");
            builder.Property(e => e.DeviceId).HasColumnName("device_id");
            builder.Property(e => e.MetricKey).HasColumnName("metric_key");
            builder.Property(e => e.CreatedUtc).HasColumnName("created_utc");
            builder.Property(e => e.UpdatedUtc).HasColumnName("updated_utc");
            builder.Property(e => e.SequentialNumber).HasColumnName("sequential_number");
        }
    }
}
