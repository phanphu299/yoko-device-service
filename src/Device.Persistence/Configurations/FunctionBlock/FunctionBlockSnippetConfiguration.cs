using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration.Asset
{
    public class FunctionBlockSnippetConfiguration : IEntityTypeConfiguration<Domain.Entity.FunctionBlockSnippet>
    {
        public void Configure(EntityTypeBuilder<Domain.Entity.FunctionBlockSnippet> builder)
        {
            builder.ToTable("function_block_snippets");
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.Name).HasColumnName("name");
            builder.Property(e => e.TemplateCode).HasColumnName("template_code");
            builder.Property(e => e.CreatedUtc).HasColumnName("created_utc");
            builder.Property(e => e.UpdatedUtc).HasColumnName("updated_utc");
        }
    }
}
