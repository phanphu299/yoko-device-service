using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration
{
    public class AssetAttributeIntegrationConfiguration : IEntityTypeConfiguration<Domain.Entity.AssetAttributeIntegration>
    {
        public void Configure(EntityTypeBuilder<Domain.Entity.AssetAttributeIntegration> builder)
        {
            // configure the model.
            builder.ToTable("asset_attribute_integration");
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.AssetAttributeId).HasColumnName("asset_attribute_id");
            builder.Property(e => e.IntegrationId).HasColumnName("integration_id");
            builder.Property(e => e.DeviceId).HasColumnName("device_id");
            builder.Property(e => e.MetricKey).HasColumnName("metric_key");
            builder.Property(e => e.CreatedUtc).HasColumnName("created_utc");
            builder.Property(e => e.UpdatedUtc).HasColumnName("updated_utc");
            //builder.HasOne(e => e.Integration).WithMany(e => e.AssetAttributeIntegration).HasForeignKey(e => e.IntegrationId);
            //builder.HasOne(e => e.DeviceExternal).WithMany(e => e.AssetAttributeIntegration).HasForeignKey(e => e.DeviceExternalId);

        }
    }
}
