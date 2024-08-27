using System;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AHI.Device.Function.Model.ImportModel;
using AHI.Device.Function.Constant;
using AHI.Device.Function.FileParser.Extension;
using AHI.Device.Function.Service;
using AHI.Device.Function.Service.Abstraction;
using AHI.Device.Function.Service.FileImport;
using AHI.Device.Function.Service.FileImport.Abstraction;
using AHI.Infrastructure.MultiTenancy.Extension;
using AHI.Infrastructure.MultiTenancy.Http.Handler;
using AHI.Infrastructure.DataCompression.Extension;
using AHI.Infrastructure.Import.Abstraction;
using AHI.Infrastructure.Interceptor.Extension;
using AHI.Infrastructure.Interceptor.Abstraction;
using AHI.Infrastructure.Interceptor;
using AHI.Infrastructure.Export.Extension;
using AHI.Infrastructure.Cache.Redis.Extension;
using AHI.Infrastructure.OpenTelemetry;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;
using OpenTelemetry.Logs;
using AHI.Infrastructure.Audit.Extension;
using AHI.Infrastructure.UserContext.Extension;
using System.Threading;
using AHI.Infrastructure.Bus.ServiceBus.Extension;
using AHI.Device.Function.Services;
using AHI.Infrastructure.Repository.Abstraction.ReadOnly;
using AHI.Infrastructure.Repository;
using AHI.Infrastructure.Repository.Abstraction;
using Function.Extensions;
using Function.Repositories;
using Function.Services;
using AHI.Infrastructure.Service.Tag.Extension;

[assembly: FunctionsStartup(typeof(AHI.Device.Function.Startup))]
namespace AHI.Device.Function
{
    public class Startup : FunctionsStartup
    {
        public Startup()
        {
            System.Diagnostics.Activity.DefaultIdFormat = System.Diagnostics.ActivityIdFormat.W3C;
            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
        }

        public const string SERVICE_NAME = "device-function";
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // DI goes here
            builder.Services.AddDataCompression();
            builder.Services.AddDataParserServices();
            builder.Services.AddScoped<IDeviceService, DeviceService>();
            builder.Services.AddScoped<IFileImportService, FileImportService>();
            builder.Services.AddScoped<IImportNotificationService, ImportNotificationService>();
            builder.Services.AddScoped<IExportNotificationService, ExportNotificationService>();
            builder.Services.AddScoped<IAssetNotificationService, AssetNotificationService>();
            builder.Services.AddScoped<FloatDataProcessor>();
            builder.Services.AddScoped<IntDataProcessor>();
            builder.Services.AddScoped<TextDataProcessor>();
            builder.Services.AddScoped<BooleanDataProcessor>();
            builder.Services.AddScoped<IAssetAttributeParseService, AssetAttributeParseService>();
            builder.Services.AddScoped<IStorageService, StorageService>();
            builder.Services.AddScoped<IIotBrokerService, IotBrokerService>();
            builder.Services.AddScoped<IProjectService, ProjectService>();
            builder.Services.AddScoped<IEntityTagService, EntityTagService>();
            builder.Services.AddScoped<IDeviceHeartbeatService, DeviceHeartbeatService>();
            builder.Services.AddScoped<ITemplateAttributeParserService, TemplateAttributeParserService>();
            builder.Services.AddScoped<IAssetAttributeTemplateImportService, AssetAttributeTemplateImportService>();
            builder.Services.AddScoped<IAssetTemplateService, AssetTemplateService>();
            builder.Services.AddScoped<IEnumerable<IDataProcessor>>(service =>
            {
                return new IDataProcessor[]{
                        service.GetRequiredService<BooleanDataProcessor>(),
                        service.GetRequiredService<IntDataProcessor>(),
                        service.GetRequiredService<FloatDataProcessor>(),
                        service.GetRequiredService<TextDataProcessor>()
                };
            });

            builder.Services.AddEntityTagService(Infrastructure.Service.Tag.Enum.DatabaseType.Postgresql);
            builder.Services.AddInterceptor();
            // IMPORTANT: need to override the compiler service to add more dll/assembly
            builder.Services.AddSingleton<ICompilerService>(service =>
            {
                var languageService = service.GetRequiredService<ILanguageService>();
                var compilerService = new CompilerService(languageService);
                var dllNames = new string[] {
                    "Function",
                    "AHI.Infrastructure.Interceptor"
                };
                foreach (var dll in dllNames)
                {
                    var assembly = MetadataReference.CreateFromFile(Assembly.Load(dll).Location) as MetadataReference;
                    compilerService.AddMetadataReference(assembly);
                }
                return compilerService;
            });
            builder.Services.AddMultiTenantService();
            builder.Services.AddUserContextService();

            builder.Services.AddScoped<IFileExportService, FileExportService>();
            builder.Services.AddScoped<DeviceTemplateExportHandler>();
            builder.Services.AddScoped<AssetTemplateExportHandler>();
            builder.Services.AddScoped<AssetTemplateAttributeExportHandler>();
            builder.Services.AddScoped<AssetAttributeExportHandler>();
            builder.Services.AddScoped<DeviceExportHandler>();
            builder.Services.AddScoped<UomExportHandler>();
            builder.Services.AddScoped<IDictionary<string, IExportHandler>>(service =>
            {
                var dictionary = new Dictionary<string, IExportHandler>();
                var deviceTemplateExportHandler = service.GetRequiredService<DeviceTemplateExportHandler>();
                var assetTemplateExportHandler = service.GetRequiredService<AssetTemplateExportHandler>();
                var assetTemplateAttributeExportHandler = service.GetRequiredService<AssetTemplateAttributeExportHandler>();
                var assetAttributeHandler = service.GetRequiredService<AssetAttributeExportHandler>();
                var deviceExportHandler = service.GetRequiredService<DeviceExportHandler>();
                var uomExportHandler = service.GetRequiredService<UomExportHandler>();
                dictionary[IOEntityType.DEVICE_TEMPLATE] = deviceTemplateExportHandler;
                dictionary[IOEntityType.ASSET_TEMPLATE] = assetTemplateExportHandler;
                dictionary[IOEntityType.ASSET_TEMPLATE_ATTRIBUTE] = assetTemplateAttributeExportHandler;
                dictionary[IOEntityType.ASSET_ATTRIBUTE] = assetAttributeHandler;
                dictionary[IOEntityType.DEVICE] = deviceExportHandler;
                dictionary[IOEntityType.UOM] = uomExportHandler;
                return dictionary;
            });

            //add db connection
            builder.Services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();
            builder.Services.AddScoped<IWriteDbConnectionFactory, WriteDbConnectionFactory>();
            builder.Services.AddScoped<IReadOnlyDbConnectionFactory, ReadOnlyDbConnectionFactory>();
            builder.Services.AddScoped<IDbConnectionResolver, DbConnectionResolver>();

            // add import repository services
            builder.Services.AddScoped<IImportRepository<DeviceTemplate>, DeviceTemplateRepository>();
            builder.Services.AddScoped<IImportRepository<AssetTemplate>, AssetTemplateRepository>();
            builder.Services.AddScoped<IImportRepository<Uom>, UomRepository>();
            builder.Services.AddScoped<IImportRepository<DeviceModel>, DeviceImportRepository>();
            // add read and write repositories
            builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
            builder.Services.AddScopedWithActivator<IReadOnlyDeviceRepository, DeviceRepository>(typeof(IReadOnlyDbConnectionFactory));
            builder.Services.AddScoped<IReadOnlyAssetRepository, AssetRepository>();

            builder.Services.AddScoped<IDeviceImportService, DeviceImportService>();
            builder.Services.AddScoped<IDeviceTemplateImportService, DeviceTemplateImportService>();
            builder.Services.AddScoped<IAssetTemplateImportService, AssetTemplateImportService>();
            builder.Services.AddScoped<IAssetAttributeImportService, AssetAttributeImportService>();
            builder.Services.AddScoped<IUomImportService, UomImportService>();
            builder.Services.AddScoped<IDataIngestionService, DataIngestionService>();
            builder.Services.AddScoped<ICalculateRuntimeMetricService, CalculateRuntimeMetricService>();
            builder.Services.AddScoped<ICalculateRuntimeAttributeService, CalculateRuntimeAttributeService>();
            builder.Services.AddScoped<IIngestionProcessEventService, IngestionProcessEventService>();
            builder.Services.AddScoped<IIntegrationDeviceCalculateRuntimeMetricService, IntegrationCalculateRuntimeMetricService>();
            builder.Services.AddScoped<IFowardingNotificationService, FowardingNotificationService>();
            builder.Services.AddScoped<IFunctionBlockExecutionService, FunctionBlockExecutionService>();
            builder.Services.AddScoped<IDictionary<Type, IFileImport>>(service =>
            {
                // return the proper type
                var assetTemplate = service.GetRequiredService<IAssetTemplateImportService>();
                var device = service.GetRequiredService<IDeviceImportService>();
                var deviceTemplate = service.GetRequiredService<IDeviceTemplateImportService>();
                var uom = service.GetRequiredService<IUomImportService>();
                return new Dictionary<Type, IFileImport>()
                {
                    {typeof(AssetTemplate), assetTemplate},
                    {typeof(DeviceModel), device},
                    {typeof(DeviceTemplate), deviceTemplate},
                    {typeof(Uom), uom}
                };
            });

            builder.Services.AddScoped<ISystemContext, SystemContext>();
            builder.Services.AddServiceBus();
            builder.Services.AddExportingServices();
            builder.Services.AddHttpClient(ClientNameConstant.DEVICE_SERVICE, (service, client) =>
            {
                var configuration = service.GetRequiredService<IConfiguration>();
                client.BaseAddress = new Uri(configuration["Api:Device"]);
            }).AddHttpMessageHandler<ClientCrendetialAuthentication>();

            builder.Services.AddHttpClient(ClientNameConstant.STORAGE_SERVICE, (service, client) =>
           {
               var configuration = service.GetRequiredService<IConfiguration>();
               client.BaseAddress = new Uri(configuration["Api:Storage"]);
           }).AddHttpMessageHandler<ClientCrendetialAuthentication>();

            builder.Services.AddHttpClient(ClientNameConstant.CONFIGURATION_SERVICE, (service, client) =>
            {
                var configuration = service.GetRequiredService<IConfiguration>();
                client.BaseAddress = new Uri(configuration["Api:Configuration"]);
            }).AddHttpMessageHandler<ClientCrendetialAuthentication>();

            builder.Services.AddHttpClient(ClientNameConstant.BROKER_SERVICE, (service, client) =>
            {
                var configuration = service.GetRequiredService<IConfiguration>();
                client.BaseAddress = new Uri(configuration["Api:Broker"]);
            }).AddHttpMessageHandler<ClientCrendetialAuthentication>();

            builder.Services.AddHttpClient(ClientNameConstant.MASTER_FUNCTION, (service, client) =>
            {
                var configuration = service.GetRequiredService<IConfiguration>();
                client.BaseAddress = new Uri(configuration["Function:Master"]);
            }).AddHttpMessageHandler<ClientCrendetialAuthentication>();

            builder.Services.AddHttpClient(ClientNameConstant.USER_SERVICE, (service, client) =>
            {
                var configuration = service.GetRequiredService<IConfiguration>();
                client.BaseAddress = new Uri(configuration["Api:User"]);
            }).AddHttpMessageHandler<ClientCrendetialAuthentication>();

            builder.Services.AddHttpClient(ClientNameConstant.TAG_SERVICE, (service, client) =>
            {
                var configuration = service.GetRequiredService<IConfiguration>();
                client.BaseAddress = new Uri(configuration["Api:Tag"]);
            }).AddHttpMessageHandler<ClientCrendetialAuthentication>();

            builder.Services.AddHttpClient(ClientNameConstant.PROJECT_SERVICE, (service, client) =>
            {
                var configuration = service.GetRequiredService<IConfiguration>();
                client.BaseAddress = new Uri(configuration["Api:Project"]);
            }).AddHttpMessageHandler<ClientCrendetialAuthentication>();

            builder.Services.AddHttpClient(ClientNameConstant.FUNCTION_BLOCK, (service, client) =>
            {
                var configuration = service.GetRequiredService<IConfiguration>();
                client.BaseAddress = new Uri(configuration["Api:FunctionBlock"]);
            }).AddHttpMessageHandler<ClientCrendetialAuthentication>();

            // download link should be fully qualified URL, don't need to setup BaseAddress
            // download client must not use ClientCrendetialAuthentication handler to avoid Authorization conflict when download from blob storage
            builder.Services.AddHttpClient(ClientNameConstant.DOWNLOAD_CLIENT);

            builder.Services.AddAuditLogService();
            builder.Services.AddNotification();
            builder.Services.AddRedisCache();
            builder.Services.AddMemoryCache();
            builder.Services.AddScoped<IRuntimeAttributeService, RuntimeAttributeService>();
            builder.Services.AddSingleton<IMasterService, MasterService>();
            builder.Services.AddOtelTracingService(SERVICE_NAME, typeof(Startup).Assembly.GetName().Version.ToString());
            builder.Services.AddLogging(builder =>
            {
                builder.AddOpenTelemetry(option =>
               {
                   option.SetResourceBuilder(
                   ResourceBuilder.CreateDefault()
                       .AddService(SERVICE_NAME, typeof(Startup).Assembly.GetName().Version.ToString()));

                   option.AddOtlpExporter(oltp =>
                   {
                       oltp.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                   });
               });
            });
            SetMinThread(builder);
        }

        private const int DEFAULT_MIN_THREAD = 300;
        private static void SetMinThread(IFunctionsHostBuilder builder)
        {
            // set min threads for connecting to redis
            var configuration = builder.GetContext().Configuration;
            int.TryParse(configuration["Dotnet:MinThreads"], out var minThread);
            if (minThread <= 0)
                minThread = DEFAULT_MIN_THREAD;
            ThreadPool.SetMinThreads(minThread, minThread);

        }
    }
}