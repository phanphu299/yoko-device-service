
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration.Device
{
    public class ValidTemplateConfiguration : IEntityTypeConfiguration<Domain.Entity.ValidTemplate>
    {
        public void Configure(EntityTypeBuilder<Domain.Entity.ValidTemplate> builder)
        {
            builder.ToTable("v_template_valid");
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.Name).HasColumnName("name");
            builder.Property(e => e.CreatedUtc).HasColumnName("created_utc");
            builder.Property(e => e.UpdatedUtc).HasColumnName("updated_utc");
            builder.Property(e => e.Deleted).HasColumnName("deleted");
        }
    }
}
