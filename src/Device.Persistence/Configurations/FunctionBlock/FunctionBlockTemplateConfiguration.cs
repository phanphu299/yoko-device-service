using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration
{
    public class FunctionBlockTemplateConfiguration : IEntityTypeConfiguration<Domain.Entity.FunctionBlockTemplate>
    {
        public void Configure(EntityTypeBuilder<Domain.Entity.FunctionBlockTemplate> builder)
        {
            builder.ToTable("function_block_templates");
            builder.HasQueryFilter(x => !x.Deleted);
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.Name).HasColumnName("name");
            builder.Property(e => e.DesignContent).HasColumnName("design_content");
            builder.Property(e => e.Content).HasColumnName("content");
            builder.Property(e => e.CreatedUtc).HasColumnName("created_utc");
            builder.Property(e => e.UpdatedUtc).HasColumnName("updated_utc");
            builder.Property(e => e.Deleted).HasColumnName("deleted");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.ResourcePath).HasColumnName("resource_path");
            builder.Property(e => e.TriggerType).HasColumnName("trigger_type");
            builder.Property(e => e.TriggerContent).HasColumnName("trigger_content");
            builder.Property(e => e.Version).HasColumnName("version");
            builder.HasMany(x => x.Nodes).WithOne(x => x.BlockTemplate).HasForeignKey(x => x.BlockTemplateId).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(e => e.Executions).WithOne(x => x.Template).HasForeignKey(x => x.TemplateId);
        }
    }
}
