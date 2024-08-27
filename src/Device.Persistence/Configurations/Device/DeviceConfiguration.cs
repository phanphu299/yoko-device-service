using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration.Device
{
    public class DeviceConfiguration : IEntityTypeConfiguration<Domain.Entity.Device>
    {
        public void Configure(EntityTypeBuilder<Domain.Entity.Device> builder)
        {
            // configure the model.
            builder.ToTable("devices");
            builder.HasQueryFilter(x => !x.Deleted);
            builder.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("uuid_generate_v4()");
            builder.Property(e => e.Name).HasColumnName("name");
            builder.Property(e => e.TelemetryTopic).HasColumnName("telemetry_topic");
            builder.Property(e => e.CommandTopic).HasColumnName("command_topic");
            builder.Property(e => e.HasCommand).HasColumnName("has_command");
            builder.Property(e => e.Status).HasColumnName("status");
            builder.Property(e => e.TemplateId).HasColumnName("device_template_id");
            builder.Property(e => e.RetentionDays).HasColumnName("retention_days");
            builder.Property(e => e.Description).HasColumnName("description");
            builder.Property(e => e.CreatedUtc).HasColumnName("created_utc");
            builder.Property(e => e.UpdatedUtc).HasColumnName("updated_utc");
            builder.Property(e => e.DeviceContent).HasColumnName("device_content");
            builder.Property(e => e.EnableHealthCheck).HasColumnName("enable_health_check");
            builder.Property(e => e.SignalQualityCode).HasColumnName("signal_quality_code");
            builder.Property(e => e.MonitoringTime).HasColumnName("healthz_interval");
            builder.Property(e => e.HealthCheckMethodId).HasColumnName("health_check_method_id");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.ResourcePath).HasColumnName("resource_path");
            builder.Property(e => e.Deleted).HasColumnName("deleted");
            builder.HasOne(e => e.DeviceSnaphot).WithOne(x => x.Device).HasForeignKey<Domain.Entity.Device>(x => x.Id).OnDelete(DeleteBehavior.NoAction);
            builder.HasMany(x => x.EntityTags).WithOne(x => x.Device).HasForeignKey(x => x.EntityIdString).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
