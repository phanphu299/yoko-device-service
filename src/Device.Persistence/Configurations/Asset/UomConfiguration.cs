using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration
{
    public class UomConfiguration : IEntityTypeConfiguration<Domain.Entity.Uom>
    {
        public void Configure(EntityTypeBuilder<Domain.Entity.Uom> builder)
        {
            // configure the model.
            builder.ToTable("uoms");
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.Name).HasColumnName("name");
            builder.Property(e => e.LookupCode).HasColumnName("lookup_code");
            builder.Property(e => e.RefFactor).HasColumnName("ref_factor");
            builder.Property(e => e.RefOffset).HasColumnName("ref_offset");
            builder.Property(e => e.CreatedUtc).HasColumnName("created_utc");
            builder.Property(e => e.UpdatedUtc).HasColumnName("updated_utc");
            builder.Property(e => e.Deleted).HasColumnName("deleted");
            builder.Property(e => e.CanonicalFactor).HasColumnName("canonical_factor");
            builder.Property(e => e.CanonicalOffset).HasColumnName("canonical_offset");
            builder.Property(e => e.Description).HasColumnName("description");
            builder.Property(e => e.Abbreviation).HasColumnName("abbreviation");
            builder.Property(e => e.RefId).HasColumnName("ref_id");
            builder.Property(e => e.System).HasColumnName("system");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.ResourcePath).HasColumnName("resource_path");
            builder.HasMany(x => x.AssetAttributes).WithOne(x => x.Uom).HasForeignKey(x => x.UomId);
            builder.HasOne(x => x.RefUom).WithMany(x => x.Children).HasForeignKey(x => x.RefId);
            builder.HasQueryFilter(x => !x.Deleted);

            builder.HasMany(x => x.AssetAttributeTemplates).WithOne(x => x.Uom).HasForeignKey(x => x.UomId).OnDelete(DeleteBehavior.SetNull);
            builder.HasMany(x => x.AssetAttributes).WithOne(x => x.Uom).HasForeignKey(x => x.UomId).OnDelete(DeleteBehavior.SetNull);
            builder.HasMany(x => x.EntityTags).WithOne(x => x.Uom).HasForeignKey(x => x.EntityIdInt).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
