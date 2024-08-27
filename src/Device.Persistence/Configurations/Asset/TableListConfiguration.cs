using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Device.Persistence.Configuration
{
    public class AssetTableListConfiguration : IEntityTypeConfiguration<Domain.Entity.AssetTableList>
    {
        public void Configure(EntityTypeBuilder<Domain.Entity.AssetTableList> builder)
        {
            // configure the model.
            builder.ToTable("asset_table_list");
            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.TableName).HasColumnName("table_name");
            builder.Property(e => e.AssetPath).HasColumnName("asset_path");
            builder.Property(e => e.Enabled).HasColumnName("enabled");
            builder.Property(e => e.CreatedUtc).HasColumnName("created_utc");
            builder.Property(e => e.UpdatedUtc).HasColumnName("updated_utc");
            builder.Property(e => e.Deleted).HasColumnName("deleted");
        }
    }
}
