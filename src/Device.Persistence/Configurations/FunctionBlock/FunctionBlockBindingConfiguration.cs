using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration.Asset
{
    class FunctionBlockBindingConfiguration : IEntityTypeConfiguration<Domain.Entity.FunctionBlockBinding>
    {
        public void Configure(EntityTypeBuilder<Domain.Entity.FunctionBlockBinding> builder)
        {
            builder.ToTable("function_block_bindings");
            builder.HasQueryFilter(x => !x.Deleted);
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.Key).HasColumnName("key");
            builder.Property(e => e.DataType).HasColumnName("data_type");
            builder.Property(e => e.DefaultValue).HasColumnName("default_value");
            builder.Property(e => e.BindingType).HasColumnName("binding_type");
            // builder.Property(e => e.AssetTemplateId).HasColumnName("asset_template_id");
            // builder.Property(e => e.AttributeTemplateId).HasColumnName("asset_attribute_id");
            builder.Property(e => e.Description).HasColumnName("description");
            builder.Property(e => e.Deleted).HasColumnName("deleted");
            //builder.Property(e => e.IsInput).HasColumnName("is_input");
            builder.Property(e => e.FunctionBlockId).HasColumnName("function_block_id");
            builder.Property(e => e.CreatedUtc).HasColumnName("created_utc");
            builder.Property(e => e.SequentialNumber).HasColumnName("sequential_number");
            builder.Property(e => e.System).HasColumnName("system");
        }
    }
}
