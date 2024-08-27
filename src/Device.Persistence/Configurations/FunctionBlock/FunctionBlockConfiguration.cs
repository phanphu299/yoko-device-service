using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration.Asset
{
    public class FunctionBlockConfiguration : IEntityTypeConfiguration<Domain.Entity.FunctionBlock>
    {
        public void Configure(EntityTypeBuilder<Domain.Entity.FunctionBlock> builder)
        {
            builder.ToTable("function_blocks");
            builder.HasQueryFilter(x => !x.Deleted);
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.Name).HasColumnName("name");
            builder.Property(e => e.BlockContent).HasColumnName("block_content");
            builder.Property(e => e.Type).HasColumnName("type");
            builder.Property(e => e.CategoryId).HasColumnName("category_id");
            // builder.Property(e => e.IsActive).HasColumnName("is_active");
            builder.Property(e => e.CreatedUtc).HasColumnName("created_utc");
            builder.Property(e => e.UpdatedUtc).HasColumnName("updated_utc");
            builder.Property(e => e.Deleted).HasColumnName("deleted");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.ResourcePath).HasColumnName("resource_path");
            builder.Property(e => e.System).HasColumnName("system");
            builder.Property(e => e.Version).HasColumnName("version");
            builder.HasMany(x => x.Bindings).WithOne(x => x.FunctionBlock).HasForeignKey(x => x.FunctionBlockId).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(x => x.BlockTemplateMappings).WithOne(x => x.FunctionBlock).HasForeignKey(x => x.FunctionBlockId).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany<Domain.Entity.FunctionBlockExecution>().WithOne(x => x.FunctionBlock).HasForeignKey(x => x.FunctionBlockId);
        }
    }
}
