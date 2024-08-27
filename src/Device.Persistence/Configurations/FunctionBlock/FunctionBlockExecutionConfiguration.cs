using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration
{
    public class FunctionBlockExecutionConfiguration : IEntityTypeConfiguration<Domain.Entity.FunctionBlockExecution>
    {
        public void Configure(EntityTypeBuilder<Domain.Entity.FunctionBlockExecution> builder)
        {
            builder.ToTable("function_block_executions");
            builder.HasQueryFilter(x => !x.Deleted);
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.Name).HasColumnName("name");
            //builder.Property(e => e.InputContent).HasColumnName("input_content");
            builder.Property(e => e.TemplateId).HasColumnName("template_id");
            builder.Property(e => e.FunctionBlockId).HasColumnName("function_block_id");
            //builder.Property(e => e.Content).HasColumnName("content");
            builder.Property(e => e.JobId).HasColumnName("job_id");
            builder.Property(e => e.DiagramContent).HasColumnName("diagram_content");
            builder.Property(e => e.TriggerType).HasColumnName("trigger_type");
            builder.Property(e => e.TriggerContent).HasColumnName("trigger_content");
            builder.Property(e => e.Deleted).HasColumnName("deleted");
            builder.Property(e => e.CreatedUtc).HasColumnName("created_utc");
            builder.Property(e => e.UpdatedUtc).HasColumnName("updated_utc");
            builder.Property(e => e.Status).HasColumnName("status");
            builder.Property(e => e.ExecutedUtc).HasColumnName("executed_utc");
            builder.Property(e => e.ExecutionContent).HasColumnName("execution_content");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.ResourcePath).HasColumnName("resource_path");
            builder.Property(e => e.RunImmediately).HasColumnName("run_immediately");
            builder.Property(e => e.TriggerAssetMarkup).HasColumnName("trigger_asset_markup");
            builder.Property(e => e.TriggerAssetId).HasColumnName("trigger_asset_id");
            builder.Property(e => e.TriggerAttributeId).HasColumnName("trigger_attribute_id");
            builder.Property(e => e.Version).HasColumnName("version");
            builder.HasMany(e => e.Mappings).WithOne(x => x.BlockExecution).HasForeignKey(x => x.BlockExecutionId).OnDelete(DeleteBehavior.Cascade);
            builder.HasQueryFilter(e => !e.Deleted);
        }
    }
}
