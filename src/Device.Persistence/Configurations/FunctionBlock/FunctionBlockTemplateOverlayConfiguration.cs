using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration.Asset
{
    public class FunctionBlockTemplateOverlayConfiguration : IEntityTypeConfiguration<Domain.Entity.FunctionBlockTemplateOverlay>
    {
        public void Configure(EntityTypeBuilder<Domain.Entity.FunctionBlockTemplateOverlay> builder)
        {
            builder.ToTable("v_function_block_template_overlay");
            builder.HasKey(e => e.FunctionBlockExecutionId);
            builder.Property(e => e.Name).HasColumnName("name");
            builder.Property(e => e.FunctionBlockExecutionId).HasColumnName("function_block_execution_id");
            builder.HasOne(e => e.FunctionBlockExecution).WithOne(x => x.TemplateOverlay).HasPrincipalKey<Domain.Entity.FunctionBlockTemplateOverlay>(x => x.FunctionBlockExecutionId);
        }
    }
}
