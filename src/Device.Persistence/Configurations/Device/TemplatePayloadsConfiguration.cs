
using Device.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration.Device
{
    public class TemplatePayloadsConfiguration : IEntityTypeConfiguration<TemplatePayload>
    {
        public void Configure(EntityTypeBuilder<TemplatePayload> builder)
        {
            builder.ToTable("template_payloads");
            builder.HasOne(x => x.Template).WithMany(x => x.Payloads).HasForeignKey(x => x.TemplateId);
            builder.HasMany(x => x.Details).WithOne(x => x.Payload).HasForeignKey(x => x.TemplatePayloadId).OnDelete(DeleteBehavior.Cascade);
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.TemplateId).HasColumnName("device_template_id");
            builder.Property(e => e.JsonPayload).HasColumnName("json_payload");
        }
    }
}
