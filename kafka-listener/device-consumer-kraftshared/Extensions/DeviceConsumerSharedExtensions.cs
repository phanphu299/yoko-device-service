using System;
using System.Collections.Generic;
using AHI.Infrastructure.Audit.Extension;
using AHI.Infrastructure.DataCompression.Extension;
using AHI.Infrastructure.MultiTenancy.Extension;
using AHI.Infrastructure.MultiTenancy.Http.Handler;
using AHI.Infrastructure.SharedKernel;
using Device.Consumer.KraftShared.Constant;
using Device.Consumer.KraftShared.Repositories;
using Device.Consumer.KraftShared.Repositories.Abstraction;
using Device.Consumer.KraftShared.Repositories.Abstraction.ReadOnly;
using Device.Consumer.KraftShared.Service;
using Device.Consumer.KraftShared.Service.Abstraction;
using Device.Consumer.KraftShared.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Device.Consumer.KraftShared.Extensions
{
    public static class DeviceConsumerSharedExtensions
    {

        public static void AddDeviceConsumerShared(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.AddDataCompression();
            serviceCollection.AddLoggingService();
            serviceCollection.AddScoped<ISystemContext, SystemContext>();
            serviceCollection.AddMultiTenantService();

            serviceCollection.AddScoped<IDeviceService, DeviceService>();
            serviceCollection.AddScoped<IImportNotificationService, ImportNotificationService>();
            serviceCollection.AddScoped<IExportNotificationService, ExportNotificationService>();
            serviceCollection.AddScoped<IAssetNotificationService, AssetNotificationService>();
            serviceCollection.AddScoped<FloatDataProcessor>();
            serviceCollection.AddScoped<IntDataProcessor>();
            serviceCollection.AddScoped<TextDataProcessor>();
            serviceCollection.AddScoped<BooleanDataProcessor>();
            serviceCollection.AddScoped<IStorageService, StorageService>();
            serviceCollection.AddScoped<IIotBrokerService, IotBrokerService>();
            serviceCollection.AddScoped<IProjectService, ProjectService>();
            serviceCollection.AddScoped<IDeviceHeartbeatService, DeviceHeartbeatService>();

            serviceCollection.AddScoped<IEnumerable<IDataProcessor>>(service =>
            {
                return new IDataProcessor[]{
                        service.GetRequiredService<BooleanDataProcessor>(),
                        service.GetRequiredService<IntDataProcessor>(),
                        service.GetRequiredService<FloatDataProcessor>(),
                        service.GetRequiredService<TextDataProcessor>()
                };
            });

            //add db connection
            serviceCollection.AddScoped<IDbConnectionFactory, DbConnectionFactory>();
            serviceCollection.AddScoped<IWriteDbConnectionFactory, WriteDbConnectionFactory>();
            serviceCollection.AddScoped<IReadOnlyDbConnectionFactory, ReadOnlyDbConnectionFactory>();
            serviceCollection.AddScoped<IDbConnectionResolver, DbConnectionResolver>();

            // add read and write repositories
            serviceCollection.AddScoped<IDeviceRepository, DeviceRepository>();
            serviceCollection.AddScoped<IReadOnlyDeviceRepository, DeviceRepository>();
            serviceCollection.AddScoped<IReadOnlyAssetRepository, AssetRepository>();

            serviceCollection.AddScoped<IIngestionProcessEventService, IngestionProcessEventService>();
            serviceCollection.AddScoped<IFowardingNotificationService, FowardingNotificationService>();
            serviceCollection.AddScoped<IFunctionBlockExecutionService, FunctionBlockExecutionService>();

            serviceCollection.AddHttpClient(ClientNameConstant.DEVICE_SERVICE, (service, client) =>
            {
                var configuration = service.GetRequiredService<IConfiguration>();
                client.BaseAddress = new Uri(configuration["Api:Device"]);
            }).AddHttpMessageHandler<ClientCrendetialAuthentication>();

            serviceCollection.AddHttpClient(ClientNameConstant.STORAGE_SERVICE, (service, client) =>
            {
                var configuration = service.GetRequiredService<IConfiguration>();
                client.BaseAddress = new Uri(configuration["Api:Storage"]);
            }).AddHttpMessageHandler<ClientCrendetialAuthentication>();

            serviceCollection.AddHttpClient(ClientNameConstant.CONFIGURATION_SERVICE, (service, client) =>
            {
                var configuration = service.GetRequiredService<IConfiguration>();
                client.BaseAddress = new Uri(configuration["Api:Configuration"]);
            }).AddHttpMessageHandler<ClientCrendetialAuthentication>();

            serviceCollection.AddHttpClient(ClientNameConstant.BROKER_SERVICE, (service, client) =>
            {
                var configuration = service.GetRequiredService<IConfiguration>();
                client.BaseAddress = new Uri(configuration["Api:Broker"]);
            }).AddHttpMessageHandler<ClientCrendetialAuthentication>();

            serviceCollection.AddHttpClient(ClientNameConstant.MASTER_FUNCTION, (service, client) =>
            {
                var configuration = service.GetRequiredService<IConfiguration>();
                client.BaseAddress = new Uri(configuration["Function:Master"]);
            }).AddHttpMessageHandler<ClientCrendetialAuthentication>();

            serviceCollection.AddHttpClient(ClientNameConstant.USER_SERVICE, (service, client) =>
            {
                var configuration = service.GetRequiredService<IConfiguration>();
                client.BaseAddress = new Uri(configuration["Api:User"]);
            }).AddHttpMessageHandler<ClientCrendetialAuthentication>();

            serviceCollection.AddHttpClient(ClientNameConstant.PROJECT_SERVICE, (service, client) =>
            {
                var configuration = service.GetRequiredService<IConfiguration>();
                client.BaseAddress = new Uri(configuration["Api:Project"]);
            }).AddHttpMessageHandler<ClientCrendetialAuthentication>();

            serviceCollection.AddAuditLogService();
            serviceCollection.AddNotification();
            serviceCollection.AddScoped<IRuntimeAttributeService, RuntimeAttributeService>();
            serviceCollection.AddSingleton<IMasterService, MasterService>();
        }
    }
}
