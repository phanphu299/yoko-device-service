using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration.Asset
{
    public class FunctionBlockTemplateNodeConfiguration : IEntityTypeConfiguration<Domain.Entity.FunctionBlockTemplateNode>
    {
        public void Configure(EntityTypeBuilder<Domain.Entity.FunctionBlockTemplateNode> builder)
        {
            builder.ToTable("function_block_template_nodes");
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.BlockTemplateId).HasColumnName("template_id");
            builder.Property(e => e.FunctionBlockId).HasColumnName("block_id");
            builder.Property(e => e.BlockType).HasColumnName("block_type");
            builder.Property(e => e.SequentialNumber).HasColumnName("sequential_number");
            builder.Property(e => e.Name).HasColumnName("name");
            builder.Property(e => e.AssetMarkupName).HasColumnName("asset_markup_name");
            builder.Property(e => e.TargetName).HasColumnName("target_name");
            builder.Property(e => e.PortId).HasColumnName("port_id");
            builder.HasMany(e => e.Mappings).WithOne(x => x.BlockTemplateNode).HasForeignKey(x => x.BlockTemplateNodeId);
        }
    }
}
