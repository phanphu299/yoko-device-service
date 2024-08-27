using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration
{
    public class AssetTemplateConfiguration : IEntityTypeConfiguration<Domain.Entity.AssetTemplate>
    {
        public void Configure(EntityTypeBuilder<Domain.Entity.AssetTemplate> builder)
        {
            // configure the model.
            builder.ToTable("asset_templates");
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.Name).HasColumnName("name");
            builder.Property(e => e.CreatedUtc).HasColumnName("created_utc");
            builder.Property(e => e.UpdatedUtc).HasColumnName("updated_utc");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.ResourcePath).HasColumnName("resource_path");
            builder.HasMany(e => e.Attributes).WithOne(x => x.AssetTemplate).HasForeignKey(x => x.AssetTemplateId).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(x => x.EntityTags).WithOne(x => x.AssetTemplate).HasForeignKey(x => x.EntityIdGuid).OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(e => e.Name).IsUnique();
        }
    }
}
