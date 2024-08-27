
using Device.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration.Device
{
    public class TemplateKeyTypesConfiguration : IEntityTypeConfiguration<TemplateKeyType>
    {
        public void Configure(EntityTypeBuilder<TemplateKeyType> builder)
        {
            // configure the model.
            builder.ToTable("template_key_types");
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.Name).HasColumnName("name");
            builder.Property(e => e.Deleted).HasColumnName("deleted");
            builder.HasMany(e => e.TemplateDetails).WithOne(e => e.TemplateKeyType).HasForeignKey(e => e.KeyTypeId);
        }
    }
}
