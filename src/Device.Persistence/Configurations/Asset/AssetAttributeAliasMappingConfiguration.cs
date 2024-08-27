using Device.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration
{
    public class AssetAttributeAliasMappingConfiguration : IEntityTypeConfiguration<Domain.Entity.AssetAttributeAliasMapping>
    {
        public void Configure(EntityTypeBuilder<AssetAttributeAliasMapping> builder)
        {
            // configure the model.
            builder.ToTable("asset_attribute_alias_mapping");
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.AssetId).HasColumnName("asset_id");
            builder.Property(e => e.AliasAssetId).HasColumnName("alias_asset_id");
            builder.Property(e => e.AliasAttributeId).HasColumnName("alias_attribute_id");
            builder.Property(e => e.AssetAttributeTemplateId).HasColumnName("asset_attribute_template_id");
            builder.Property(e => e.CreatedUtc).HasColumnName("created_utc");
            builder.Property(e => e.UpdatedUtc).HasColumnName("updated_utc");
            builder.Ignore(e => e.AliasAssetName);
            builder.Ignore(e => e.AliasAttributeName);
            builder.Ignore(e => e.DataType);
            builder.Ignore(e => e.UomId);
            builder.Ignore(e => e.DecimalPlace);
            builder.Ignore(e => e.ThousandSeparator);
        }
    }
}
