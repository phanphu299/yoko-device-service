using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration.Asset
{
    public class FunctionBlockCategoryConfiguration : IEntityTypeConfiguration<Domain.Entity.FunctionBlockCategory>
    {
        public void Configure(EntityTypeBuilder<Domain.Entity.FunctionBlockCategory> builder)
        {
            builder.ToTable("function_block_categories");
            builder.HasQueryFilter(x => !x.Deleted);
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.Name).HasColumnName("name");
            builder.Property(e => e.ParentId).HasColumnName("parent_category_id");
            builder.Property(e => e.CreatedUtc).HasColumnName("created_utc");
            builder.Property(e => e.UpdatedUtc).HasColumnName("updated_utc");
            builder.Property(e => e.Deleted).HasColumnName("deleted");
            builder.Property(e => e.System).HasColumnName("system");
            builder.HasMany(x => x.FunctionBlocks).WithOne(x => x.Categories).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(x => x.Children).WithOne(x => x.Parent).HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
