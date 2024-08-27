using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration
{
    public class AssetAttributeTemplateIntegrationConfiguration : IEntityTypeConfiguration<Domain.Entity.AssetAttributeTemplateIntegration>
    {
        public void Configure(EntityTypeBuilder<Domain.Entity.AssetAttributeTemplateIntegration> builder)
        {
            // configure the model.
            builder.ToTable("asset_attribute_template_integrations");
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.AssetAttributeTemplateId).HasColumnName("asset_attribute_template_id");
            builder.Property(e => e.IntegrationMarkupName).HasColumnName("integration_markup_name");
            builder.Property(e => e.IntegrationId).HasColumnName("integration_id");
            builder.Property(e => e.DeviceMarkupName).HasColumnName("device_markup_name");
            builder.Property(e => e.DeviceId).HasColumnName("device_id");
            builder.Property(e => e.MetricKey).HasColumnName("metric_key");
            builder.Property(e => e.CreatedUtc).HasColumnName("created_utc");
            builder.Property(e => e.UpdatedUtc).HasColumnName("updated_utc");

            builder.HasMany(x => x.AssetAttributeIntegrationMappings).WithOne(x => x.AssetAttributeIntegrationTemplate)
                .HasPrincipalKey(x => new { x.AssetAttributeTemplateId })
                .HasForeignKey(x => x.AssetAttributeTemplateId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
