using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration
{
    public class AssetAttributeStaticMappingConfiguration : IEntityTypeConfiguration<Domain.Entity.AssetAttributeStaticMapping>
    {
        public void Configure(EntityTypeBuilder<Domain.Entity.AssetAttributeStaticMapping> builder)
        {
            // configure the model.
            builder.ToTable("asset_attribute_static_mapping");
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.AssetId).HasColumnName("asset_id");
            builder.Property(e => e.AssetAttributeTemplateId).HasColumnName("asset_attribute_template_id");
            builder.Property(e => e.Value).HasColumnName("value");
            builder.Property(e => e.CreatedUtc).HasColumnName("created_utc");
            builder.Property(e => e.UpdatedUtc).HasColumnName("updated_utc");
            builder.Property(e => e.IsOverridden).HasColumnName("is_overridden");
            builder.Property(e => e.SequentialNumber).HasColumnName("sequential_number");
        }
    }
}
