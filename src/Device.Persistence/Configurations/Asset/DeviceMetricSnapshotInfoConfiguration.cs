using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration
{
    public class DeviceMetricSnapshotInfoConfiguration : IEntityTypeConfiguration<Domain.Entity.DeviceMetricSnapshotInfo>
    {
        public void Configure(EntityTypeBuilder<Domain.Entity.DeviceMetricSnapshotInfo> builder)
        {
            // configure the model.
            builder.ToTable("v_device_metric_snapshots");
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.Name).HasColumnName("name");
            builder.Property(e => e.DeviceId).HasColumnName("device_id");
            builder.Property(e => e.DataType).HasColumnName("data_type");
            builder.Property(e => e.Value).HasColumnName("value");
            builder.Property(e => e.UpdatedUtc).HasColumnName("update_utc");
        }
    }
}
