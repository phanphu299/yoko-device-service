using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration
{
    public class AssetAttributeRuntimeConfiguration : IEntityTypeConfiguration<Domain.Entity.AssetAttributeRuntime>
    {
        public void Configure(EntityTypeBuilder<Domain.Entity.AssetAttributeRuntime> builder)
        {
            // configure the model.
            builder.ToTable("asset_attribute_runtimes");
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.AssetAttributeId).HasColumnName("asset_attribute_id");
            // builder.Property(e => e.TriggerAssetId).HasColumnName("trigger_asset_id");
            // builder.Property(e => e.TriggerAttributeId).HasColumnName("trigger_attribute_id");
            builder.Property(e => e.IsTriggerVisibility).HasColumnName("is_trigger_visibility");
            builder.Property(e => e.EnabledExpression).HasColumnName("enabled_expression");
            builder.Property(e => e.Expression).HasColumnName("expression");
            builder.Property(e => e.ExpressionCompile).HasColumnName("expression_compile");
            builder.Property(e => e.CreatedUtc).HasColumnName("created_utc");
            builder.Property(e => e.UpdatedUtc).HasColumnName("updated_utc");
            builder.HasMany(x => x.Triggers).WithOne(x => x.AssetAttributeRuntime).HasForeignKey(x => x.AttributeId).HasPrincipalKey(x => x.AssetAttributeId);

        }
    }
}
