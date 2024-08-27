using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration.Device
{
    public class DeviceSignalQualityConfiguration : IEntityTypeConfiguration<Domain.Entity.DeviceSignalQuality>
    {
        public void Configure(EntityTypeBuilder<Domain.Entity.DeviceSignalQuality> builder)
        {
            // configure the model.
            builder.ToTable("device_signal_quality_codes");
        }
    }
}
