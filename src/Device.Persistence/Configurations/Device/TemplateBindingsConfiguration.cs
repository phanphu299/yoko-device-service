
using Device.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration.Device
{
    public class TemplateBindingsConfiguration : IEntityTypeConfiguration<TemplateBinding>
    {
        public void Configure(EntityTypeBuilder<TemplateBinding> builder)
        {
            builder.ToTable("template_bindings");
            builder.HasOne(x => x.Template).WithMany(x => x.Bindings).HasForeignKey(x => x.TemplateId);
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.TemplateId).HasColumnName("device_template_id");
            builder.Property(e => e.DataType).HasColumnName("data_type");
            builder.Property(e => e.Key).HasColumnName("key");
            builder.Property(e => e.DefaultValue).HasColumnName("default_value");
            // builder.HasOne(x => x.DeviceBinding).WithOne(x => x.TemplateBinding).HasForeignKey<DeviceBinding>(x => x.DeviceTemplateBindingId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
