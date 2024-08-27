using Device.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration
{
    public class AssetAttributeCommandHistoryConfiguration : IEntityTypeConfiguration<Domain.Entity.AssetAttributeCommandHistory>
    {
        public void Configure(EntityTypeBuilder<AssetAttributeCommandHistory> builder)
        {
            builder.ToTable("asset_attribute_command_histories");
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.AssetAttributeId).HasColumnName("asset_attribute_id");
            builder.Property(e => e.DeviceId).HasColumnName("device_id");
            builder.Property(e => e.MetricKey).HasColumnName("metric_key");
            builder.Property(e => e.Value).HasColumnName("value");
            builder.Property(e => e.RowVersion).HasColumnName("row_version");
        }
    }
}