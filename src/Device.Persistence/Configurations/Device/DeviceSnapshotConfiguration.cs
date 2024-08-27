using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration.Device
{
    public class DeviceSnapshotConfiguration : IEntityTypeConfiguration<Domain.Entity.DeviceSnapshot>
    {
        public void Configure(EntityTypeBuilder<Domain.Entity.DeviceSnapshot> builder)
        {
            // configure the model.
            builder.ToTable("v_device_snapshot");
            builder.HasKey(x => x.DeviceId);
            builder.Property(x => x.DeviceId).HasColumnName("device_id");
            builder.Property(x => x.Timestamp).HasColumnName("_ts");
            builder.Property(x => x.CommandDataTimestamp).HasColumnName("command_data_timestamps");
            builder.Property(x => x.Status).HasColumnName("status");
        }
    }
}
