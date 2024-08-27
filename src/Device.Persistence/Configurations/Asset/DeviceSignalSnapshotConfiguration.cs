using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration
{
    public class DeviceSignalSnapshotConfiguration : IEntityTypeConfiguration<Domain.Entity.DeviceMetricSnapshot>
    {
        public void Configure(EntityTypeBuilder<Domain.Entity.DeviceMetricSnapshot> builder)
        {
            // configure the model.
            builder.ToTable("device_metric_snapshots");
            builder.HasKey(x => new { x.DeviceId, x.MetricId });
            builder.Property(e => e.DeviceId).HasColumnName("device_id");
            builder.Property(e => e.MetricId).HasColumnName("metric_key");
            builder.Property(e => e.Value).HasColumnName("value");
            builder.Property(e => e.UpdatedUtc).HasColumnName("_ts");
        }
    }
}