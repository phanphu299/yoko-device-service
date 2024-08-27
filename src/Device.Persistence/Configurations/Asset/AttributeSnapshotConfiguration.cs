using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration
{
    public class AttributeSnapshotConfiguration : IEntityTypeConfiguration<Domain.Entity.AttributeSnapshot>
    {
        public void Configure(EntityTypeBuilder<Domain.Entity.AttributeSnapshot> builder)
        {
            // configure the model.
            builder.ToTable("v_asset_attribute_snapshots");
            builder.HasNoKey();
            builder.Property(e => e.Id).HasColumnName("attribute_id");
            //builder.Property(e => e.Name).HasColumnName("name");
            builder.Property(e => e.AssetId).HasColumnName("asset_id");
            builder.Property(e => e.DataType).HasColumnName("data_type");
            builder.Property(e => e.Value).HasColumnName("value");
            builder.Property(e => e.Timestamp).HasColumnName("_ts");
            builder.Property(e => e.AttributeType).HasColumnName("attribute_type");
            builder.Property(e => e.DeviceId).HasColumnName("device_id");
            builder.Property(e => e.MetricKey).HasColumnName("metric_key");
        }
    }
}
