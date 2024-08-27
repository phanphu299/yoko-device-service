using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration
{
    public class AssetAttributeTemplateDynamicConfiguration : IEntityTypeConfiguration<Domain.Entity.AssetAttributeDynamicTemplate>
    {
        public void Configure(EntityTypeBuilder<Domain.Entity.AssetAttributeDynamicTemplate> builder)
        {
            // configure the model.
            builder.ToTable("asset_attribute_template_dynamics");
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.AssetAttributeTemplateId).HasColumnName("asset_attribute_template_id");
            builder.Property(e => e.DeviceTemplateId).HasColumnName("device_template_id");
            builder.Property(e => e.MarkupName).HasColumnName("markup_name");
            builder.Property(e => e.MetricKey).HasColumnName("metric_key");
            builder.Property(e => e.CreatedUtc).HasColumnName("created_utc");
            builder.Property(e => e.UpdatedUtc).HasColumnName("updated_utc");

            builder.HasMany(x => x.AssetAttributeDynamicMappings).WithOne(x => x.AssetAttributeDynamicTemplate)
                .HasPrincipalKey(x => new { x.AssetAttributeTemplateId })
                .HasForeignKey(x => x.AssetAttributeTemplateId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);

        }
    }
}
