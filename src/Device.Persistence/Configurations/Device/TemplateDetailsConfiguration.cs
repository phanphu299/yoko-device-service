
using Device.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration.Device
{
    public class TemplateDetailsConfiguration : IEntityTypeConfiguration<TemplateDetail>
    {
        public void Configure(EntityTypeBuilder<TemplateDetail> builder)
        {
            builder.ToTable("template_details");
            builder.HasOne(x => x.Payload).WithMany(x => x.Details).HasForeignKey(x => x.TemplatePayloadId);
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.TemplatePayloadId).HasColumnName("template_payload_id");
            builder.Property(e => e.Key).HasColumnName("key");
            builder.Property(e => e.Name).HasColumnName("name");
            builder.Property(e => e.KeyTypeId).HasColumnName("key_type_id");
            builder.Property(e => e.DataType).HasColumnName("data_type");
            builder.Property(e => e.Enabled).HasColumnName("enabled");
            builder.Property(e => e.Expression).HasColumnName("expression");
            builder.Property(e => e.DetailId).HasColumnName("detail_id");
            builder.Property(e => e.ExpressionCompile).HasColumnName("expression_compile");
            builder.HasOne(e => e.TemplateKeyType).WithMany(e => e.TemplateDetails).HasForeignKey(e => e.KeyTypeId);
        }

    }
}
