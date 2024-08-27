using Device.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration
{
    public class AssetAttributeCommandMappingConfiguration : IEntityTypeConfiguration<Domain.Entity.AssetAttributeCommandMapping>
    {
        public void Configure(EntityTypeBuilder<AssetAttributeCommandMapping> builder)
        {
            builder.ToTable("asset_attribute_command_mapping");
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.AssetId).HasColumnName("asset_id");
            builder.Property(e => e.AssetAttributeTemplateId).HasColumnName("asset_attribute_template_id");
            builder.Property(e => e.DeviceId).HasColumnName("device_id");
            builder.Property(e => e.MetricKey).HasColumnName("metric_key");
            builder.Property(e => e.Value).HasColumnName("value");
            builder.Property(e => e.CreatedUtc).HasColumnName("created_utc");
            builder.Property(e => e.UpdatedUtc).HasColumnName("updated_utc");
            builder.Property(e => e.RowVersion).HasColumnName("row_version");
            builder.Property(e => e.SequentialNumber).HasColumnName("sequential_number");
            builder.Property(e => e.Timestamp).HasColumnName("_ts");
        }
    }
}