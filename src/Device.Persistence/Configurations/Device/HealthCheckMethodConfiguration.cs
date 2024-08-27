using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration.Device
{
    public class HealthCheckMethodConfiguration : IEntityTypeConfiguration<Domain.Entity.HealthCheckMethod>
    {
        public void Configure(EntityTypeBuilder<Domain.Entity.HealthCheckMethod> builder)
        {
            // configure the model.
            builder.ToTable("device_health_check_methods");
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.Name).HasColumnName("name");
            builder.Property(e => e.Deleted).HasColumnName("deleted");
        }
    }
}
