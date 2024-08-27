

using Device.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration.Device
{
    public class DeviceTemplateConfiguration : IEntityTypeConfiguration<DeviceTemplate>
    {
        public void Configure(EntityTypeBuilder<DeviceTemplate> builder)
        {
            // configure the model.
            builder.ToTable("device_templates");
            builder.HasMany(x => x.Payloads).WithOne(x => x.Template).HasForeignKey(x => x.TemplateId).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(x => x.Bindings).WithOne(x => x.Template).HasForeignKey(x => x.TemplateId).OnDelete(DeleteBehavior.Cascade);
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.Name).HasColumnName("name");
            builder.Property(e => e.Deleted).HasColumnName("deleted");
            builder.Property(e => e.TotalMetric).HasColumnName("total_metric");
            builder.Property(e => e.CreatedUtc).HasColumnName("created_utc");
            builder.Property(e => e.UpdatedUtc).HasColumnName("updated_utc");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.ResourcePath).HasColumnName("resource_path");
            builder.HasMany(e => e.Devices).WithOne(x => x.Template).HasForeignKey(x => x.TemplateId);
            builder.HasMany(e => e.AssetAttributeDynamicTemplates).WithOne(e => e.Template).HasForeignKey(x => x.DeviceTemplateId).OnDelete(DeleteBehavior.SetNull);
            builder.HasMany(e => e.AssetAttributeCommandTemplates).WithOne(e => e.DeviceTemplate).HasForeignKey(x => x.DeviceTemplateId).OnDelete(DeleteBehavior.SetNull);
            builder.HasMany(x => x.EntityTags).WithOne(x => x.DeviceTemplate).HasForeignKey(x => x.EntityIdGuid).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
