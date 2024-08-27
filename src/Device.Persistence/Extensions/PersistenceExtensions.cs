using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using Device.Persistence.DbContext;
using Device.Application.Repository;
using Device.Persistence.Repository;
using AHI.Infrastructure.SharedKernel.Extension;
using Device.Application.DbConnections;
using Device.Persistence.DbConnections;
using IDbConnectionFactory = Device.Application.DbConnections.IDbConnectionFactory;
using Device.Application.Repositories;
using AHI.Infrastructure.Service.Tag.Extension;
namespace Device.Persistence.Extensions
{
    public static class PersistenceExtensions
    {
        public static void AddPersistenceService(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddDbContext<DeviceDbContext>((service, option) =>
            {
                var configuration = service.GetRequiredService(typeof(IConfiguration)) as IConfiguration;
                var tenantContext = service.GetService(typeof(ITenantContext)) as ITenantContext;
                var connectionString = configuration["ConnectionStrings:Default"].BuildConnectionString(configuration, tenantContext.ProjectId);
                option.UseNpgsql(connectionString);
            });

            serviceCollection.AddDbContext<ReadOnlyDeviceDbContext>((service, option) =>
            {
                var configuration = service.GetRequiredService(typeof(IConfiguration)) as IConfiguration;
                var tenantContext = service.GetService(typeof(ITenantContext)) as ITenantContext;

                var replicaConStrCfg = configuration["ConnectionStrings:ReadOnly"];
                if (string.IsNullOrEmpty(replicaConStrCfg))
                    replicaConStrCfg = configuration["ConnectionStrings:Default"];

                var connectionString = replicaConStrCfg.BuildConnectionString(configuration, tenantContext.ProjectId);
                option.UseNpgsql(connectionString);
            });

            serviceCollection.AddScoped<IDbConnectionFactory, WriteDbConnectionFactory>();
            serviceCollection.AddScoped<IWriteConnectionFactory, WriteDbConnectionFactory>();
            serviceCollection.AddScoped<IReadDbConnectionFactory, ReadDbConnectionFactory>();
            serviceCollection.AddScoped<IDbConnectionResolver, DbConnectionResolver>();

            serviceCollection.AddScoped<IAssetRepository, AssetPersistenceRepository>();
            serviceCollection.AddScoped<IAssetAttributeRepository, AssetAttributePersistenceRepository>();
            serviceCollection.AddScoped<IDeviceRepository, DevicePersistenceRepository>();
            serviceCollection.AddScoped<IDeviceTemplateRepository, DeviceTemplatePersistenceRepository>();
            serviceCollection.AddScoped<ITemplateKeyTypesRepository, TemplateKeyTypesPersistenceRepository>();
            serviceCollection.AddScoped<IHealthCheckMethodRepository, HealthCheckMethodPersistenceRepository>();
            serviceCollection.AddScoped<IValidTemplateRepository, ValidTemplatePersistenceRepository>();
            serviceCollection.AddScoped<IAssetAttributeAliasRepository, AssetAttributeAliasPersistenceRepository>();
            serviceCollection.AddScoped<IAssetTemplateRepository, AssetTemplatePersistenceRepository>();
            serviceCollection.AddScoped<IUomRepository, UomPersistenceRepository>();
            serviceCollection.AddScoped<IDeviceMetricSnapshotRepository, DeviceMetricSnapshotPersistenceRepository>();
            serviceCollection.AddScoped<IAssetAttributeTemplateRepository, AssetAttributeTemplatePersistenceRepository>();
            serviceCollection.AddScoped<IAssetUnitOfWork, AssetUnitOfWork>();
            serviceCollection.AddScoped<IDeviceUnitOfWork, DeviceUnitOfWork>();
            serviceCollection.AddScoped<IAssetTemplateUnitOfWork, AssetTemplateUnitOfWork>();
            serviceCollection.AddScoped<IAssetTimeSeriesRepository, AssetTimeSeriesRepository>();
            serviceCollection.AddScoped<IAssetRuntimeTimeSeriesRepository, AssetRuntimeTimeSeriesRepository>();
            serviceCollection.AddScoped<IAssetAliasTimeSeriesRepository, AssetAliasTimeSeriesRepository>();
            serviceCollection.AddScoped<IAssetSnapshotRepository, AssetSnapshotRepository>();
            serviceCollection.AddScoped<ITimeRangeAssetSnapshotRepository, TimeRangeAssetSnapshotRepository>();
            serviceCollection.AddScoped<IAssetHistoricalSnapshotRepository, AssetHistoricalSnapshotRepository>();
            serviceCollection.AddScoped<IAssetRuntimeHistoricalSnapshotRepository, AssetRuntimeHistoricalSnapshotRepository>();
            serviceCollection.AddScoped<IAssetAliasHistoricalSnapshotRepository, AssetAliasHistoricalSnapshotRepository>();
            serviceCollection.AddScoped<IAssetIntegrationTimeSeriesRepository, AssetIntegrationTimeSeriesRepository>();
            serviceCollection.AddScoped<IAssetIntegrationHistoricalSnapshotRepository, AssetIntegrationHistoricalSnapshotRepository>();
            serviceCollection.AddScoped<IAssetAliasSnapshotRepository, AssetAliasSnapshotRepository>();
            serviceCollection.AddScoped<ITemplateDetailRepository, TemplateDetailPersistenceRepository>();
            serviceCollection.AddScoped<IBlockExecutionRepository, BlockExecutionRepository>();
            serviceCollection.AddScoped<IFunctionBlockExecutionRepository, FunctionBlockExecutionRepository>();
            serviceCollection.AddScoped<IBlockFunctionUnitOfWork, BlockFunctionUnitOfWork>();
            serviceCollection.AddScoped<IDeviceUnitOfWork, DeviceUnitOfWork>();
            serviceCollection.AddScoped<IUomUnitOfWork, UomUnitOfWork>();
            serviceCollection.AddScoped<IAssetAttributeSnapshotRepository, AssetAttributeSnapshotRepository>();
            serviceCollection.AddScoped<IFunctionBlockRepository, FunctionBlockPersistenceRepository>();
            serviceCollection.AddScoped<IDeviceMetricTimeseriesRepository, DeviceMetricTimeseriesRepository>();
            serviceCollection.AddScoped<IBlockCategoryRepository, BlockCategoryRepository>();
            serviceCollection.AddScoped<IFunctionBlockTemplateRepository, FunctionBlockTemplateRepository>();
            serviceCollection.AddScoped<IBlockSnippetRepository, BlockSnippetRepository>();
            serviceCollection.AddScoped<IDeviceSignalQualityRepository, DeviceSignalQualityRepository>();
            serviceCollection.AddReadRepositories();
            serviceCollection.AddEntityTagRepository(typeof(DeviceDbContext));
        }

        private static void AddReadRepositories(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScopedWithActivator<IReadAssetAttributeAliasRepository, AssetAttributeAliasPersistenceRepository>(typeof(ReadOnlyDeviceDbContext), typeof(IReadDbConnectionFactory));
            serviceCollection.AddScopedWithActivator<IReadAssetAttributeRepository, AssetAttributePersistenceRepository>(typeof(ReadOnlyDeviceDbContext), typeof(IReadDbConnectionFactory));
            serviceCollection.AddScopedWithActivator<IReadAssetAttributeTemplateRepository, AssetAttributeTemplatePersistenceRepository>(typeof(ReadOnlyDeviceDbContext));
            serviceCollection.AddScopedWithActivator<IReadAssetRepository, AssetPersistenceRepository>(typeof(ReadOnlyDeviceDbContext), typeof(IReadDbConnectionFactory));
            serviceCollection.AddScopedWithActivator<IReadAssetTemplateRepository, AssetTemplatePersistenceRepository>(typeof(ReadOnlyDeviceDbContext));
            serviceCollection.AddScopedWithActivator<IReadDeviceRepository, DevicePersistenceRepository>(typeof(ReadOnlyDeviceDbContext), typeof(IReadDbConnectionFactory));
            serviceCollection.AddScopedWithActivator<IReadDeviceTemplateRepository, DeviceTemplatePersistenceRepository>(typeof(ReadOnlyDeviceDbContext), typeof(IReadDbConnectionFactory));
            serviceCollection.AddScopedWithActivator<IReadFunctionBlockRepository, FunctionBlockPersistenceRepository>(typeof(ReadOnlyDeviceDbContext));
            serviceCollection.AddScopedWithActivator<IReadBlockSnippetRepository, BlockSnippetRepository>(typeof(ReadOnlyDeviceDbContext));
            serviceCollection.AddScopedWithActivator<IReadBlockCategoryRepository, BlockCategoryRepository>(typeof(ReadOnlyDeviceDbContext));
            serviceCollection.AddScopedWithActivator<IReadUomRepository, UomPersistenceRepository>(typeof(ReadOnlyDeviceDbContext));
            serviceCollection.AddScopedWithActivator<IReadFunctionBlockRepository, FunctionBlockPersistenceRepository>(typeof(ReadOnlyDeviceDbContext));
            serviceCollection.AddScopedWithActivator<IReadFunctionBlockExecutionRepository, FunctionBlockExecutionRepository>(typeof(ReadOnlyDeviceDbContext));
            serviceCollection.AddScopedWithActivator<IReadFunctionBlockTemplateRepository, FunctionBlockTemplateRepository>(typeof(ReadOnlyDeviceDbContext));
            serviceCollection.AddScopedWithActivator<IReadTemplateKeyTypesRepository, TemplateKeyTypesPersistenceRepository>(typeof(ReadOnlyDeviceDbContext));
            serviceCollection.AddScopedWithActivator<IReadAssetAttributeSnapshotRepository, AssetAttributeSnapshotRepository>(typeof(ReadOnlyDeviceDbContext));
            serviceCollection.AddScopedWithActivator<IReadDeviceMetricSnapshotRepository, DeviceMetricSnapshotPersistenceRepository>(typeof(ReadOnlyDeviceDbContext));
            serviceCollection.AddScopedWithActivator<IReadValidTemplateRepository, ValidTemplatePersistenceRepository>(typeof(ReadOnlyDeviceDbContext));
        }

        private static void AddScopedWithActivator<TService, TImplementation>(this IServiceCollection serviceCollection, params Type[] inputTypes) where TService : class where TImplementation : class, TService
        {
            serviceCollection.AddScoped<TService, TImplementation>(pro => pro.CreateInstance<TImplementation>(inputTypes));
        }

        private static TImplementation CreateInstance<TImplementation>(this IServiceProvider provider, params Type[] inputTypes)
        {
            var inputs = inputTypes.Select(provider.GetRequiredService);
            return ActivatorUtilities.CreateInstance<TImplementation>(provider, inputs.ToArray());
        }
    }
}