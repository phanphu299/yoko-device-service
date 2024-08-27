using Device.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration
{
    public class AssetAttributeAliasConfiguration : IEntityTypeConfiguration<Domain.Entity.AssetAttributeAlias>
    {
        public void Configure(EntityTypeBuilder<Domain.Entity.AssetAttributeAlias> builder)
        {
            // configure the model.
            builder.ToTable("asset_attribute_alias");
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.AssetAttributeId).HasColumnName("asset_attribute_id");
            builder.Property(e => e.AliasAssetId).HasColumnName("alias_asset_id");
            builder.Property(e => e.AliasAttributeId).HasColumnName("alias_attribute_id");
            builder.Ignore(e => e.AliasAssetName);//.HasColumnName("alias_asset_name");
            builder.Ignore(x => x.AliasAttributeName);//.HasColumnName("alias_attribute_name");
            builder.Property(e => e.CreatedUtc).HasColumnName("created_utc");
            builder.Property(e => e.UpdatedUtc).HasColumnName("updated_utc");
            // builder.HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => x.ParentId);
            builder.HasOne(x => x.AssetAttribute).WithOne(x => x.AssetAttributeAlias).HasForeignKey<AssetAttributeAlias>(x => x.AssetAttributeId);
        }
    }
}
