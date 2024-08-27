using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration
{
    public class AssetAttributeTemplateRuntimeConfiguration : IEntityTypeConfiguration<Domain.Entity.AssetAttributeRuntimeTemplate>
    {
        public void Configure(EntityTypeBuilder<Domain.Entity.AssetAttributeRuntimeTemplate> builder)
        {
            // configure the model.
            builder.ToTable("asset_attribute_template_runtimes");
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.AssetAttributeTemplateId).HasColumnName("asset_attribute_template_id");
            //builder.Property(e => e.MarkupName).HasColumnName("markup_name");
            //builder.Property(e => e.TriggerAssetTemplateId).HasColumnName("trigger_asset_template_id");
            builder.Property(e => e.TriggerAttributeId).HasColumnName("trigger_attribute_id");
            builder.Property(e => e.EnabledExpression).HasColumnName("enabled_expression");
            builder.Property(e => e.Expression).HasColumnName("expression");
            builder.Property(e => e.ExpressionCompile).HasColumnName("expression_compile");

            builder.Property(e => e.CreatedUtc).HasColumnName("created_utc");
            builder.Property(e => e.UpdatedUtc).HasColumnName("updated_utc");

            builder.HasMany(x => x.AssetAttributeRuntimeMappings).WithOne(x => x.AssetAttributeRuntimeTemplate)
                .HasPrincipalKey(x => new { x.AssetAttributeTemplateId })
                .HasForeignKey(x => x.AssetAttributeTemplateId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);

        }
    }
}
