using System;
using AHI.Infrastructure.Service.Tag.PostgreSql.Configuration;
using Device.Persistence.Configuration;
using Device.Persistence.Configuration.Asset;
using Device.Persistence.Configuration.Device;
using Microsoft.EntityFrameworkCore;

namespace Device.Persistence.DbContext
{
    public class DeviceDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public DbSet<Domain.Entity.Device> Devices { get; set; }
        public DbSet<Domain.Entity.DeviceTemplate> DeviceTemplates { get; set; }
        public DbSet<Domain.Entity.TemplatePayload> TemplatePayloads { get; set; }
        public DbSet<Domain.Entity.TemplateDetail> TemplateDetails { get; set; }
        public DbSet<Domain.Entity.TemplateKeyType> TemplateKeyTypes { get; set; }
        public DbSet<Domain.Entity.HealthCheckMethod> HealthCheckMethods { get; set; }

        public DbSet<Domain.Entity.Asset> Assets { get; set; }
        public DbSet<Domain.Entity.AssetAttribute> AssetAttributes { get; set; }
        public DbSet<Domain.Entity.AssetAttributeAlias> AssetAttributeAlias { get; set; }
        public DbSet<Domain.Entity.AssetAttributeDynamic> AssetAttributeDynamic { get; set; }
        public DbSet<Domain.Entity.AssetAttributeDynamicMapping> AssetAttributeDynamicMapping { get; set; }
        public DbSet<Domain.Entity.AssetAttributeIntegrationMapping> AssetAttributeIntegrationMapping { get; set; }
        public DbSet<Domain.Entity.AssetAttributeStaticMapping> AssetAttributeStaticMapping { get; set; }
        public DbSet<Domain.Entity.AssetAttributeRuntime> AssetAttributeRuntimes { get; set; }
        public DbSet<Domain.Entity.AssetAttributeRuntimeMapping> AssetAttributeRuntimeMapping { get; set; }
        public DbSet<Domain.Entity.AssetAttributeRuntimeTrigger> AssetAttributeRuntimeTriggers { get; set; }
        public DbSet<Domain.Entity.AssetAttributeCommand> AssetAttributeCommands { get; set; }
        public DbSet<Domain.Entity.AssetAttributeCommandHistory> AssetAttributeCommandHistories { get; set; }
        public DbSet<Domain.Entity.AssetAttributeCommandMapping> AssetAttributeCommandMappings { get; set; }
        public DbSet<Domain.Entity.Uom> Uoms { get; set; }
        public DbSet<Domain.Entity.EntityTagDb> EntityTags { get; set; }
        public DbSet<Domain.Entity.DeviceMetricSnapshot> DeviceSignalSnapshots { get; set; }
        public DbSet<Domain.Entity.AttributeSnapshot> AssetAttributeSnapshots { get; set; }
        public DbSet<Domain.Entity.DeviceMetricSnapshotInfo> DeviceMetricSnapshots { get; set; }

        public DbSet<Domain.Entity.AssetTemplate> AssetTemplates { get; set; }
        public DbSet<Domain.Entity.AssetAttributeTemplate> AssetAttributeTemplates { get; set; }

        public DbSet<Domain.Entity.FunctionBlockExecution> FunctionBlockExecutions { get; set; }
        public DbSet<Domain.Entity.FunctionBlockCategory> FunctionBlockCategories { get; set; }
        public DbSet<Domain.Entity.FunctionBlockBinding> FunctionBlockBindings { get; set; }
        public DbSet<Domain.Entity.FunctionBlock> FunctionBlocks { get; set; }
        public DbSet<Domain.Entity.FunctionBlockTemplate> FunctionBlockTemplates { get; set; }
        public DbSet<Domain.Entity.FunctionBlockTemplateNode> FunctionBlockTemplateNodes { get; set; }
        public DbSet<Domain.Entity.FunctionBlockSnippet> FunctionBlockSnippets { get; set; }
        public DbSet<Domain.Entity.FunctionBlockNodeMapping> FunctionBlockNodeMappings { get; set; }
        public DbSet<Domain.Entity.AssetAttributeAliasMapping> AssetAttributeAliasMapping { get; set; }

        public DeviceDbContext(DbContextOptions<DeviceDbContext> options) : base(options)
        {
        }

        protected DeviceDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new DeviceConfiguration());
            modelBuilder.ApplyConfiguration(new DeviceSnapshotConfiguration());

            modelBuilder.ApplyConfiguration(new DeviceTemplateConfiguration());
            modelBuilder.ApplyConfiguration(new TemplatePayloadsConfiguration());
            modelBuilder.ApplyConfiguration(new TemplateDetailsConfiguration());
            modelBuilder.ApplyConfiguration(new TemplateBindingsConfiguration());

            modelBuilder.ApplyConfiguration(new TemplateKeyTypesConfiguration());
            //modelBuilder.ApplyConfiguration(new DeviceMetricConfiguration());

            modelBuilder.ApplyConfiguration(new AssetConfiguration());
            modelBuilder.ApplyConfiguration(new AssetAttributeConfiguration());
            modelBuilder.ApplyConfiguration(new AssetAttributeAliasConfiguration());
            modelBuilder.ApplyConfiguration(new AssetAttributeDynamicConfiguration());
            modelBuilder.ApplyConfiguration(new AssetAttributeRuntimeConfiguration());
            modelBuilder.ApplyConfiguration(new AssetAttributeIntegrationConfiguration());
            modelBuilder.ApplyConfiguration(new AssetAttributeDynamicMappingConfiguration());
            modelBuilder.ApplyConfiguration(new AssetAttributeIntegrationMappingConfiguration());
            modelBuilder.ApplyConfiguration(new AssetAttributeStaticMappingConfiguration());
            modelBuilder.ApplyConfiguration(new AssetAttributeRuntimeMappingConfiguration());
            modelBuilder.ApplyConfiguration(new AssetAttributeRuntimeTriggerConfiguration());
            modelBuilder.ApplyConfiguration(new AssetAttributeCommandConfiguration());
            modelBuilder.ApplyConfiguration(new AssetAttributeCommandMappingConfiguration());
            modelBuilder.ApplyConfiguration(new AssetAttributeCommandHistoryConfiguration());

            modelBuilder.ApplyConfiguration(new ValidTemplateConfiguration());
            modelBuilder.ApplyConfiguration(new AssetTemplateConfiguration());
            modelBuilder.ApplyConfiguration(new AssetAttributeTemplateConfiguration());
            modelBuilder.ApplyConfiguration(new AssetAttributeTemplateDynamicConfiguration());
            modelBuilder.ApplyConfiguration(new AssetAttributeTemplateIntegrationConfiguration());
            modelBuilder.ApplyConfiguration(new AssetAttributeTemplateRuntimeConfiguration());
            modelBuilder.ApplyConfiguration(new AssetAttributeCommandTemplateConfiguration());
            modelBuilder.ApplyConfiguration(new HealthCheckMethodConfiguration());

            modelBuilder.ApplyConfiguration(new AssetTableListConfiguration());

            modelBuilder.ApplyConfiguration(new UomConfiguration());
            modelBuilder.ApplyConfiguration(new DeviceSignalSnapshotConfiguration());

            // snapshot
            modelBuilder.ApplyConfiguration(new AttributeSnapshotConfiguration());
            modelBuilder.ApplyConfiguration(new DeviceMetricSnapshotInfoConfiguration());
            modelBuilder.ApplyConfiguration(new FunctionBlockExecutionConfiguration());
            // function block
            modelBuilder.ApplyConfiguration(new FunctionBlockBindingConfiguration());
            modelBuilder.ApplyConfiguration(new FunctionBlockCategoryConfiguration());
            modelBuilder.ApplyConfiguration(new FunctionBlockConfiguration());
            modelBuilder.ApplyConfiguration(new FunctionBlockTemplateConfiguration());
            modelBuilder.ApplyConfiguration(new FunctionBlockTemplateNodeConfiguration());
            modelBuilder.ApplyConfiguration(new FunctionBlockNodeMappingConfiguration());
            modelBuilder.ApplyConfiguration(new FunctionBlockTemplateOverlayConfiguration());
            modelBuilder.ApplyConfiguration(new FunctionBlockSnippetConfiguration());

            modelBuilder.ApplyConfiguration(new DeviceSignalQualityConfiguration());
            modelBuilder.ApplyConfiguration(new AssetAttributeAliasMappingConfiguration());
            modelBuilder.ApplyConfiguration(new EntityTagConfiguration<Domain.Entity.EntityTagDb>());
        }
    }

    public class ReadOnlyDeviceDbContext : DeviceDbContext
    {
        public ReadOnlyDeviceDbContext(DbContextOptions<ReadOnlyDeviceDbContext> options) : base(options)
        {

        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            throw new Exception("ReadOnlyDBContext has not save-changes method");
        }
    }
}
