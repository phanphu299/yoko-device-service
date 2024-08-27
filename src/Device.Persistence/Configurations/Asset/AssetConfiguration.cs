using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration
{
    public class AssetConfiguration : IEntityTypeConfiguration<Domain.Entity.Asset>
    {
        public void Configure(EntityTypeBuilder<Domain.Entity.Asset> builder)
        {
            // configure the model.
            builder.ToTable("assets");
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.Name).HasColumnName("name");
            builder.Property(e => e.ParentAssetId).HasColumnName("parent_asset_id");
            builder.Property(e => e.AssetTemplateId).HasColumnName("asset_template_id");
            builder.Property(e => e.CreatedUtc).HasColumnName("created_utc");
            builder.Property(e => e.UpdatedUtc).HasColumnName("updated_utc");
            builder.Property(e => e.RetentionDays).HasColumnName("retention_days");
            builder.HasOne(x => x.ParentAsset).WithMany(x => x.Children).HasForeignKey(x => x.ParentAssetId).OnDelete(DeleteBehavior.Cascade);
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.ResourcePath).HasColumnName("resource_path");

            builder.HasMany(x => x.Attributes).WithOne(x => x.Asset).HasForeignKey(x => x.AssetId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(x => x.AssetTemplate).WithMany(x => x.Assets).HasForeignKey(x => x.AssetTemplateId);

            builder.HasMany(x => x.Attributes).WithOne(x => x.Asset).HasForeignKey(x => x.AssetId);

            builder.HasMany(x => x.AssetAttributeDynamicMappings).WithOne(x => x.Asset).HasForeignKey(x => x.AssetId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(x => x.AssetAttributeIntegrationMappings).WithOne(x => x.Asset).HasForeignKey(x => x.AssetId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(x => x.AssetAttributeStaticMappings).WithOne(x => x.Asset).HasForeignKey(x => x.AssetId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(x => x.AssetAttributeRuntimeMappings).WithOne(x => x.Asset).HasForeignKey(x => x.AssetId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(x => x.AssetAttributeCommandMappings).WithOne(x => x.Asset).HasForeignKey(x => x.AssetId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.Name, x.ParentAssetId, x.CreatedBy }).IsUnique();

            builder.HasMany(x => x.Triggers).WithOne(x => x.Asset).HasForeignKey(x => x.AssetId).IsRequired(false).OnDelete(DeleteBehavior.Cascade);

            builder.Property(x => x.IsDocument).HasColumnName("is_document");
            builder.HasMany(x => x.EntityTags).WithOne(x => x.Asset).HasForeignKey(x => x.EntityIdGuid).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
