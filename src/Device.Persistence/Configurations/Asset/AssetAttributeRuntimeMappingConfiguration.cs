using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration
{
    public class AssetAttributeRuntimeMappingConfiguration : IEntityTypeConfiguration<Domain.Entity.AssetAttributeRuntimeMapping>
    {
        public void Configure(EntityTypeBuilder<Domain.Entity.AssetAttributeRuntimeMapping> builder)
        {
            // configure the model.
            builder.ToTable("asset_attribute_runtime_mapping");
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.AssetId).HasColumnName("asset_id");
            builder.Property(e => e.AssetAttributeTemplateId).HasColumnName("asset_attribute_template_id");
            builder.Property(e => e.EnabledExpression).HasColumnName("enabled_expression");
            builder.Property(e => e.Expression).HasColumnName("expression");
            builder.Property(e => e.ExpressionCompile).HasColumnName("expression_compile");
            builder.Property(e => e.IsTriggerVisibility).HasColumnName("is_trigger_visibility");
            //builder.Property(e => e.TriggerAttributeId).HasColumnName("trigger_attribute_id");
            builder.Property(e => e.CreatedUtc).HasColumnName("created_utc");
            builder.Property(e => e.UpdatedUtc).HasColumnName("updated_utc");
            builder.Property(e => e.SequentialNumber).HasColumnName("sequential_number");
            builder.HasMany(x => x.Triggers).WithOne(x => x.AssetAttributeRuntimeMapping).HasForeignKey(x => x.AttributeId);
        }
    }
}
