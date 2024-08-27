using Device.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration
{
    public class AssetAttributeCommandConfiguration : IEntityTypeConfiguration<Domain.Entity.AssetAttributeCommand>
    {
        public void Configure(EntityTypeBuilder<AssetAttributeCommand> builder)
        {
            builder.ToTable("asset_attribute_commands");
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.AssetAttributeId).HasColumnName("asset_attribute_id");
            builder.Property(e => e.DeviceId).HasColumnName("device_id");
            builder.Property(e => e.MetricKey).HasColumnName("metric_key");
            builder.Property(e => e.Value).HasColumnName("value");
            builder.Property(e => e.CreatedUtc).HasColumnName("created_utc");
            builder.Property(e => e.UpdatedUtc).HasColumnName("updated_utc");
            builder.Property(e => e.RowVersion).HasColumnName("row_version");
            builder.Property(e => e.Deleted).HasColumnName("deleted");
            builder.Property(e => e.SequentialNumber).HasColumnName("sequential_number");
            builder.Property(e => e.Timestamp).HasColumnName("_ts");
            builder.HasQueryFilter(e => !e.Deleted);
            builder.HasOne(x => x.AssetAttribute).WithOne(x => x.AssetAttributeCommand).HasForeignKey<AssetAttributeCommand>(x => x.AssetAttributeId);
        }
    }
}