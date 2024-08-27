using Device.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration
{
    public class AssetAttributeConfiguration : IEntityTypeConfiguration<AssetAttribute>
    {
        public void Configure(EntityTypeBuilder<AssetAttribute> builder)
        {
            // configure the model.
            builder.ToTable("asset_attributes");
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.AssetId).HasColumnName("asset_id");
            builder.Property(e => e.Name).HasColumnName("name");
            builder.Property(e => e.Value).HasColumnName("value");
            //builder.Property(e => e.Expression).HasColumnName("expression");
            builder.Property(e => e.AttributeType).HasColumnName("attribute_type");
            builder.Property(e => e.DataType).HasColumnName("data_type");
            builder.Property(e => e.CreatedUtc).HasColumnName("created_utc");
            builder.Property(e => e.UpdatedUtc).HasColumnName("updated_utc");
            builder.Property(e => e.UomId).HasColumnName("uom_id");
            // builder.Property(e => e.EnabledExpression).HasColumnName("enabled_expression");
            // builder.Property(e => e.ExpressionCompile).HasColumnName("expression_compile");
            builder.Property(e => e.DecimalPlace).HasColumnName("decimal_place");
            builder.Property(e => e.ThousandSeparator).HasColumnName("thousand_separator");
            // builder.Property(e => e.TriggerAssetId).HasColumnName("trigger_asset_id");
            //builder.Property(e => e.TriggerAttributeId).HasColumnName("trigger_attribute_id");
            builder.Property(e => e.SequentialNumber).HasColumnName("sequential_number");
            builder.HasOne(x => x.AssetAttributeAlias).WithOne(x => x.AssetAttribute).HasForeignKey<AssetAttributeAlias>(x => x.AssetAttributeId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(x => x.AssetAttributeDynamic).WithOne(x => x.AssetAttribute).HasForeignKey<AssetAttributeDynamic>(x => x.AssetAttributeId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(x => x.AssetAttributeIntegration).WithOne(x => x.AssetAttribute).HasForeignKey<AssetAttributeIntegration>(x => x.AssetAttributeId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(x => x.AssetAttributeRuntime).WithOne(x => x.AssetAttribute).HasForeignKey<AssetAttributeRuntime>(x => x.AssetAttributeId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(x => x.AssetAttributeCommand).WithOne(x => x.AssetAttribute).HasForeignKey<AssetAttributeCommand>(x => x.AssetAttributeId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(x => new { x.AssetId, x.Name }).IsUnique();
        }
    }
}
