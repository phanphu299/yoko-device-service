using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration
{
    public class AssetAttributeDynamicConfiguration : IEntityTypeConfiguration<Domain.Entity.AssetAttributeDynamic>
    {
        public void Configure(EntityTypeBuilder<Domain.Entity.AssetAttributeDynamic> builder)
        {
            // configure the model.
            builder.ToTable("asset_attribute_dynamic");
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.AssetAttributeId).HasColumnName("asset_attribute_id");
            builder.Property(e => e.DeviceId).HasColumnName("device_id");
            builder.Property(e => e.MetricKey).HasColumnName("metric_key");
            builder.Property(e => e.CreatedUtc).HasColumnName("created_utc");
            builder.Property(e => e.UpdatedUtc).HasColumnName("updated_utc");

            builder.HasIndex(x => new { x.MetricKey, x.DeviceId }).IsUnique();
        }
    }
}
