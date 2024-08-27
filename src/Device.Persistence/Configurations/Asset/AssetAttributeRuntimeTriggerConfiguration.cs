using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration
{
    public class AssetAttributeRuntimeTriggerConfiguration : IEntityTypeConfiguration<Domain.Entity.AssetAttributeRuntimeTrigger>
    {
        public void Configure(EntityTypeBuilder<Domain.Entity.AssetAttributeRuntimeTrigger> builder)
        {
            // configure the model.
            builder.ToTable("asset_attribute_runtime_triggers");
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.AssetId).HasColumnName("asset_id");
            builder.Property(e => e.AttributeId).HasColumnName("attribute_id");
            builder.Property(e => e.TriggerAssetId).HasColumnName("trigger_asset_id");
            builder.Property(e => e.TriggerAttributeId).HasColumnName("trigger_attribute_id");
            builder.Property(e => e.IsSelected).HasColumnName("is_selected");
        }
    }
}
