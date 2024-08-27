using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration
{
    public class AssetAttributeTemplateConfiguration : IEntityTypeConfiguration<Domain.Entity.AssetAttributeTemplate>
    {
        public void Configure(EntityTypeBuilder<Domain.Entity.AssetAttributeTemplate> builder)
        {
            // configure the model.
            builder.ToTable("asset_attribute_templates");
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.AssetTemplateId).HasColumnName("asset_template_id");
            builder.Property(e => e.Name).HasColumnName("name");
            builder.Property(e => e.Value).HasColumnName("value");
            // builder.Property(e => e.Expression).HasColumnName("expression");
            // builder.Property(e => e.EnabledExpression).HasColumnName("enabled_expression");
            builder.Property(e => e.AttributeType).HasColumnName("attribute_type");
            builder.Property(e => e.DataType).HasColumnName("data_type");
            builder.Property(e => e.CreatedUtc).HasColumnName("created_utc");
            builder.Property(e => e.UpdatedUtc).HasColumnName("updated_utc");
            builder.Property(e => e.UomId).HasColumnName("uom_id");
            //builder.Property(e => e.ExpressionCompile).HasColumnName("expression_compile");
            builder.Property(e => e.DecimalPlace).HasColumnName("decimal_place");
            builder.Property(e => e.ThousandSeparator).HasColumnName("thousand_separator");
            builder.Property(e => e.SequentialNumber).HasColumnName("sequential_number");
            builder.HasOne(x => x.AssetAttributeDynamic).WithOne(x => x.AssetAttribute).HasForeignKey<Domain.Entity.AssetAttributeDynamicTemplate>(x => x.AssetAttributeTemplateId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(x => x.AssetAttributeIntegration).WithOne(x => x.AssetAttribute).HasForeignKey<Domain.Entity.AssetAttributeTemplateIntegration>(x => x.AssetAttributeTemplateId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.AssetAttributeDynamicMappings).WithOne(x => x.AssetAttributeTemplate).HasForeignKey(x => x.AssetAttributeTemplateId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(x => x.AssetAttributeIntegrationMappings).WithOne(x => x.AssetAttributeTemplate).HasForeignKey(x => x.AssetAttributeTemplateId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(x => x.AssetAttributeStaticMappings).WithOne(x => x.AssetAttributeTemplate).HasForeignKey(x => x.AssetAttributeTemplateId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(x => x.AssetAttributeRuntimeMappings).WithOne(x => x.AssetAttributeTemplate).HasForeignKey(x => x.AssetAttributeTemplateId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(x => x.AssetAttributeCommandMappings).WithOne(x => x.AssetAttributeTemplate).HasForeignKey(x => x.AssetAttributeTemplateId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(x => x.AssetAttributeAliasMappings).WithOne(x => x.AssetAttributeTemplate).HasForeignKey(x => x.AssetAttributeTemplateId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.AssetTemplateId, x.Name }).IsUnique();
        }
    }
}
