using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration.Asset
{
    public class FunctionBlockNodeMappingConfiguration : IEntityTypeConfiguration<Domain.Entity.FunctionBlockNodeMapping>
    {
        public void Configure(EntityTypeBuilder<Domain.Entity.FunctionBlockNodeMapping> builder)
        {
            builder.ToTable("function_block_execution_node_mappings");
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.BlockExecutionId).HasColumnName("block_execution_id");
            builder.Property(e => e.BlockTemplateNodeId).HasColumnName("block_template_node_id");
            builder.Property(e => e.AssetId).HasColumnName("asset_id");
            builder.Property(e => e.AssetName).HasColumnName("asset_name");
            builder.Property(e => e.TargetName).HasColumnName("target_name");
            builder.Property(e => e.AssetMarkupName).HasColumnName("asset_markup_name");
            builder.Property(e => e.Value).HasColumnName("value");
        }
    }
}
