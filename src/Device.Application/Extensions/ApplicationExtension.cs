using Microsoft.Extensions.DependencyInjection;
using MediatR;
using System.Reflection;
using Device.Application.Service;
using Device.Application.Service.Abstraction;
using AHI.Infrastructure.Bus.ServiceBus.Extension;
using AHI.Infrastructure.Service.Extension;
using AHI.Infrastructure.Interceptor.Extension;
using AHI.Infrastructure.Interceptor.Abstraction;
using AHI.Infrastructure.Interceptor;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using Device.Application.Constant;
using Device.Application.Pipelines.ValidatorPipelines;
using FluentValidation;
using AHI.Infrastructure.MultiTenancy.Http.Handler;
using AHI.Infrastructure.Cache.Redis.Extension;
using AHI.Infrastructure.Validation.Extension;
using AHI.Infrastructure.OpenTelemetry;
using Prometheus;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using AHI.Infrastructure.Service.Dapper.Extension;
using AHI.Infrastructure.Audit.Extension;
using Device.Application.Asset.Validation;
using Device.Application.AssetTemplate.Validation;
using System.Collections.Generic;
using AHI.Infrastructure.Cache.Redis;
using Device.Application.Enum;
using Device.Application.Validation;
using AHI.Infrastructure.Service.Tag.Enum;
using AHI.Infrastructure.Service.Tag.Extension;

namespace Device.ApplicationExtension.Extension
{
    public static class ApplicationExtension
    {
        private const string SERVICE_NAME = "device-service";

        public static void AddApplicationServices(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<DeviceBackgroundService>();
            serviceCollection.AddHostedService(sp => sp.GetRequiredService<DeviceBackgroundService>());

            serviceCollection.AddApplicationValidator();
            serviceCollection.AddFrameworkServices();
            serviceCollection.AddDapperFrameworkServices();
            serviceCollection.AddEntityTagService(DatabaseType.Postgresql);
            serviceCollection.AddMediatR(typeof(ApplicationExtension).GetTypeInfo().Assembly);
            serviceCollection.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestValidationBehavior<,>));
            serviceCollection.AddScoped<IAssetService, AssetService>();
            serviceCollection.AddScoped<IAssetAttributeService, AssetAttributeService>();
            serviceCollection.AddScoped<IDeviceTemplateService, DeviceTemplateService>();
            serviceCollection.AddScoped<IDeviceService, DeviceService>();
            serviceCollection.AddScoped<ITemplateKeyTypesService, TemplateKeyTypesService>();
            serviceCollection.AddScoped<IHealthCheckMethodService, HealthCheckMethodService>();
            serviceCollection.AddScoped<IAssetQueryService, AssetQueryService>();
            serviceCollection.AddScoped<IRegressionAnalysis, RegressionAnalysis>();

            serviceCollection.AddScoped<IAssetTemplateService, AssetTemplateService>();
            serviceCollection.AddScoped<IAssetAttributeTemplateService, AssetAttributeTemplateService>();

            serviceCollection.AddScoped<NotificationService>();
            serviceCollection.AddScoped<IAssetTableService, AssetTableService>();
            serviceCollection.AddScoped<IValidTemplateService, ValidTemplateService>();
            serviceCollection.AddScoped<IUomService, UomService>();
            serviceCollection.AddScoped<IFileEventService, FileEventService>();

            serviceCollection.AddScoped<IConfigurationService, ConfigurationService>();
            serviceCollection.AddScoped<IEntityLockService, EntityLockService>();

            serviceCollection.AddScoped<IFunctionBlockService, FunctionBlockService>();
            serviceCollection.AddScoped<IFunctionBlockTemplateService, FunctionBlockTemplateService>();

            serviceCollection.AddScoped<IBlockCategoryService, BlockCategoryService>();
            serviceCollection.AddScoped<IFunctionBlockTemplateService, FunctionBlockTemplateService>();
            serviceCollection.AddScoped<IBlockVariable, BlockVariable>();
            serviceCollection.AddScoped<IBlockSnippetService, BlockSnippetService>();
            serviceCollection.AddScoped<IAssetCommandHistoryHandler, AssetCommandHistoryHandler>();
            serviceCollection.AddScoped<IUserService, UserService>();
            serviceCollection.AddScoped<IExportNotificationService, ExportNotificationService>();

            serviceCollection.AddServiceBus();
            serviceCollection.AddInterceptor();

            // IMPORTANT: need to override the compiler service to add more dll/assembly
            serviceCollection.AddSingleton<ICompilerService>(service =>
            {
                var languageService = service.GetRequiredService<ILanguageService>();
                var compilerService = new CompilerService(languageService);
                var dllNames = new string[]
                {
                    "AHI.Infrastructure.Interceptor",
                    "Device.Application",
                    "Device.Domain",
                    "System.Net.Http"
                };
                foreach (var dll in dllNames)
                {
                    var assembly = MetadataReference.CreateFromFile(Assembly.Load(dll).Location) as MetadataReference;
                    compilerService.AddMetadataReference(assembly);
                }

                return compilerService;
            });

            serviceCollection.AddHttpClient(HttpClientNames.CONFIGURATION, (service, client) =>
            {
                var configuration = service.GetRequiredService<IConfiguration>();
                client.BaseAddress = new Uri(configuration["Api:Configuration"]);
            }).AddHttpMessageHandler<ClientCrendetialAuthentication>().UseHttpClientMetrics();

            serviceCollection.AddHttpClient(HttpClientNames.BROKER, (service, client) =>
            {
                var configuration = service.GetRequiredService<IConfiguration>();
                client.BaseAddress = new Uri(configuration["Api:Broker"]);
            }).AddHttpMessageHandler<ClientCrendetialAuthentication>().UseHttpClientMetrics();

            serviceCollection.AddHttpClient(HttpClientNames.ALARM_SERVICE, (service, client) =>
            {
                var configuration = service.GetRequiredService<IConfiguration>();
                client.BaseAddress = new Uri(configuration["Api:Alarm"]);
            }).AddHttpMessageHandler<ClientCrendetialAuthentication>().UseHttpClientMetrics();

            serviceCollection.AddHttpClient(HttpClientNames.PROJECT, (service, client) =>
            {
                var configuration = service.GetRequiredService<IConfiguration>();
                client.BaseAddress = new Uri(configuration["Api:Project"]);
            }).AddHttpMessageHandler<ClientCrendetialAuthentication>().UseHttpClientMetrics();

            serviceCollection.AddHttpClient(HttpClientNames.TAG_SERVICE, (service, client) =>
            {
                var configuration = service.GetRequiredService<IConfiguration>();
                client.BaseAddress = new Uri(configuration["Api:Tag"]);
            }).AddHttpMessageHandler<ClientCrendetialAuthentication>().UseHttpClientMetrics();

            serviceCollection.AddHttpClient(HttpClientNames.IDENTITY, (service, client) =>
            {
                var configuration = service.GetRequiredService<IConfiguration>();
                client.BaseAddress = new Uri(configuration["Authentication:Authority"]);
            }).AddHttpMessageHandler<ClientCrendetialAuthentication>();

            serviceCollection.AddHttpClient(HttpClientNames.STORAGE, (service, client) =>
            {
                var configuration = service.GetRequiredService<IConfiguration>();
                client.BaseAddress = new Uri(configuration["Api:Storage"]);
            }).AddHttpMessageHandler<ClientCrendetialAuthentication>().UseHttpClientMetrics();

            serviceCollection.AddHttpClient(HttpClientNames.DEVICE_FUNCTION, (service, client) =>
            {
                var configuration = service.GetRequiredService<IConfiguration>();
                client.BaseAddress = new Uri(configuration["Function:Device"]);
            }).AddHttpMessageHandler<ClientCrendetialAuthentication>().UseHttpClientMetrics();
            serviceCollection.AddHttpClient(HttpClientNames.SCHEDULER_SERVICE, (service, client) =>
            {
                var configuration = service.GetRequiredService<IConfiguration>();
                client.BaseAddress = new Uri(configuration["Api:Scheduler"]);
            }).AddHttpMessageHandler<ClientCrendetialAuthentication>().UseHttpClientMetrics();
            serviceCollection.AddHttpClient(HttpClientNames.EVENT_SERVICE, (service, client) =>
            {
                var configuration = service.GetRequiredService<IConfiguration>();
                client.BaseAddress = new Uri(configuration["Api:Event"]);
            }).AddHttpMessageHandler<ClientCrendetialAuthentication>().UseHttpClientMetrics();
            serviceCollection.AddHttpClient(HttpClientNames.BROKER_FUNCTION, (service, client) =>
            {
                var configuration = service.GetRequiredService<IConfiguration>();
                client.BaseAddress = new Uri(configuration["Function:Broker"]);
            }).AddHttpMessageHandler<ClientCrendetialAuthentication>().UseHttpClientMetrics();

            serviceCollection.AddHttpClient(HttpClientNames.ASSET_MEDIA, (service, client) =>
            {
                var configuration = service.GetRequiredService<IConfiguration>();
                client.BaseAddress = new Uri(configuration["Api:AssetMedia"]);
            }).AddHttpMessageHandler<ClientCrendetialAuthentication>().UseHttpClientMetrics();

            serviceCollection.AddHttpClient(HttpClientNames.ENTITY_SERVICE, (service, client) =>
            {
                var configuration = service.GetRequiredService<IConfiguration>();
                client.BaseAddress = new Uri(configuration["Api:Entity"]);
            }).AddHttpMessageHandler<ClientCrendetialAuthentication>().UseHttpClientMetrics();

            serviceCollection.AddHttpClient(HttpClientNames.ASSET_TABLE, (service, client) =>
            {
                var configuration = service.GetRequiredService<IConfiguration>();
                client.BaseAddress = new Uri(configuration["Api:AssetTable"]);
            }).AddHttpMessageHandler<ClientCrendetialAuthentication>().UseHttpClientMetrics();

            serviceCollection.AddHttpClient(HttpClientNames.USER_FUNCTION, (service, client) =>
            {
                var configuration = service.GetRequiredService<IConfiguration>();
                client.BaseAddress = new Uri(configuration["Function:User"]);
            }).AddHttpMessageHandler<ClientCrendetialAuthentication>().UseHttpClientMetrics();

            serviceCollection.AddRedisCache();
            serviceCollection.AddScoped<ISystemContext, SystemContext>();

            serviceCollection.AddScoped<IAssetTimeSeriesService, AssetTimeSeriesService>();
            serviceCollection.AddScoped<IAssetHistoricalSnapshotService, AssetHistoricalSnapshotService>();
            serviceCollection.AddScoped<IAssetSnapshotService, AssetSnapshotService>();
            serviceCollection.AddScoped<DynamicAssetTemplateAttributeHandler>();
            serviceCollection.AddScoped<IntegrationAssetTemplateAttributeHandler>();
            serviceCollection.AddScoped<StaticAssetTemplateAttributeHandler>();
            serviceCollection.AddScoped<RuntimeAssetTemplateAttributeHandler>();
            serviceCollection.AddScoped<RuntimeDeviceTemplateAttributeHandler>();
            serviceCollection.AddScoped<CommandAssetTemplateAttributeHandler>();
            serviceCollection.AddScoped<AliasAssetTemplateAttributeHandler>();

            serviceCollection.AddScoped<IAssetTemplateAttributeHandler>(service =>
            {
                var aliasTemplateHander = service.GetRequiredService<AliasAssetTemplateAttributeHandler>();
                var staticTemplateHander = service.GetRequiredService<StaticAssetTemplateAttributeHandler>();
                var runtimeTemplateHander = service.GetRequiredService<RuntimeAssetTemplateAttributeHandler>();
                var dynamicTemplateHander = service.GetRequiredService<DynamicAssetTemplateAttributeHandler>();
                var integrationTemplateHander = service.GetRequiredService<IntegrationAssetTemplateAttributeHandler>();
                var commandTemplateHandler = service.GetRequiredService<CommandAssetTemplateAttributeHandler>();

                runtimeTemplateHander.SetNextHandler(commandTemplateHandler);
                staticTemplateHander.SetNextHandler(runtimeTemplateHander);
                integrationTemplateHander.SetNextHandler(staticTemplateHander);
                dynamicTemplateHander.SetNextHandler(integrationTemplateHander);
                commandTemplateHandler.SetNextHandler(aliasTemplateHander);
                return dynamicTemplateHander;
            });

            serviceCollection.AddScoped<Application.Asset.Validation.StaticAttributeValidator>();
            serviceCollection.AddScoped<Application.Asset.Validation.DynamicAttributeValidator>();
            serviceCollection.AddScoped<Application.Asset.Validation.RuntimeAttributeValidator>();
            serviceCollection.AddScoped<Application.Asset.Validation.AliasAttributeValidator>();
            serviceCollection.AddScoped<Application.Asset.Validation.CommandAttributeValidator>();
            serviceCollection.AddScoped<Application.Asset.Validation.IntegrationAttributeValidator>();

            serviceCollection.AddScoped<IAssetAttributeValidator>(service =>
            {
                var staticAttributeValidator = service.GetRequiredService<Application.Asset.Validation.StaticAttributeValidator>();
                var dynamicAttributeValidator = service.GetRequiredService<Application.Asset.Validation.DynamicAttributeValidator>();
                var runtimeAttributeValidator = service.GetRequiredService<Application.Asset.Validation.RuntimeAttributeValidator>();
                var aliasAttributeValidator = service.GetRequiredService<Application.Asset.Validation.AliasAttributeValidator>();
                var commandAttributeValidator = service.GetRequiredService<Application.Asset.Validation.CommandAttributeValidator>();
                var intergrationAttributeValidator = service.GetRequiredService<Application.Asset.Validation.IntegrationAttributeValidator>();

                staticAttributeValidator.SetNextValidator(dynamicAttributeValidator);
                dynamicAttributeValidator.SetNextValidator(runtimeAttributeValidator);
                runtimeAttributeValidator.SetNextValidator(aliasAttributeValidator);
                aliasAttributeValidator.SetNextValidator(commandAttributeValidator);
                commandAttributeValidator.SetNextValidator(intergrationAttributeValidator);

                return staticAttributeValidator;
            });

            serviceCollection.AddScoped<Application.AssetTemplate.Validation.StaticAttributeValidator>();
            serviceCollection.AddScoped<Application.AssetTemplate.Validation.AliasAttributeValidator>();
            serviceCollection.AddScoped<Application.AssetTemplate.Validation.DynamicAttributeValidator>();
            serviceCollection.AddScoped<Application.AssetTemplate.Validation.RuntimeAttributeValidator>();
            serviceCollection.AddScoped<Application.AssetTemplate.Validation.CommandAttributeValidator>();
            serviceCollection.AddScoped<Application.AssetTemplate.Validation.IntegrationAttributeValidator>();
            serviceCollection.AddScoped<IAssetTemplateAttributeValidator>(service =>
            {
                var staticAttributeValidator = service.GetRequiredService<Application.AssetTemplate.Validation.StaticAttributeValidator>();
                var aliasAttributeValidator = service.GetRequiredService<Application.AssetTemplate.Validation.AliasAttributeValidator>();
                var dynamicAttributeValidator = service.GetRequiredService<Application.AssetTemplate.Validation.DynamicAttributeValidator>();
                var runtimeAttributeValidator = service.GetRequiredService<Application.AssetTemplate.Validation.RuntimeAttributeValidator>();
                var commandAttributeValidator = service.GetRequiredService<Application.AssetTemplate.Validation.CommandAttributeValidator>();
                var intergrationAttributeValidator = service.GetRequiredService<Application.AssetTemplate.Validation.IntegrationAttributeValidator>();

                staticAttributeValidator.SetNextValidator(dynamicAttributeValidator);
                dynamicAttributeValidator.SetNextValidator(runtimeAttributeValidator);
                runtimeAttributeValidator.SetNextValidator(commandAttributeValidator);
                commandAttributeValidator.SetNextValidator(intergrationAttributeValidator);
                intergrationAttributeValidator.SetNextValidator(aliasAttributeValidator);

                return staticAttributeValidator;
            });

            serviceCollection.AddScoped<IDictionary<ValidationType, IAttributeValidator>>(service =>
            {
                var assetAttributeValidator = service.GetRequiredService<Application.Asset.Validation.IAssetAttributeValidator>();
                var assetTemplateAttributeValidator = service.GetRequiredService<Application.AssetTemplate.Validation.IAssetTemplateAttributeValidator>();
                return new Dictionary<ValidationType, IAttributeValidator>
                {
                    { ValidationType.Asset, assetAttributeValidator },
                    { ValidationType.AssetTemplate, assetTemplateAttributeValidator }
                };
            });
            serviceCollection.AddScoped<DynamicAssetAttributeHandler>();
            serviceCollection.AddScoped<IntegrationAssetAttributeHandler>();
            serviceCollection.AddScoped<StaticAssetAttributeHandler>();
            serviceCollection.AddScoped<RuntimeAssetAttributeHandler>();
            serviceCollection.AddScoped<AliasAssetAttributeHandler>();
            serviceCollection.AddScoped<CommandAssetAttributeHandler>();
            serviceCollection.AddScoped<IAssetAttributeHandler>(service =>
            {
                var aliasAttributeHander = service.GetRequiredService<AliasAssetAttributeHandler>();
                var staticAttributeHander = service.GetRequiredService<StaticAssetAttributeHandler>();
                var runtimeAttributeHander = service.GetRequiredService<RuntimeAssetAttributeHandler>();
                var dynamicAttributeHander = service.GetRequiredService<DynamicAssetAttributeHandler>();
                var integrationAttributeHander = service.GetRequiredService<IntegrationAssetAttributeHandler>();
                var commandAttributeHandler = service.GetRequiredService<CommandAssetAttributeHandler>();
                runtimeAttributeHander.SetNextHandler(aliasAttributeHander);
                staticAttributeHander.SetNextHandler(runtimeAttributeHander);
                integrationAttributeHander.SetNextHandler(staticAttributeHander);
                dynamicAttributeHander.SetNextHandler(integrationAttributeHander);
                aliasAttributeHander.SetNextHandler(commandAttributeHandler);
                return dynamicAttributeHander;
            });

            serviceCollection.AddScoped<DynamicAssetAttributeMappingHandler>();
            serviceCollection.AddScoped<IntegrationAssetAttributeMappingHandler>();
            serviceCollection.AddScoped<StaticAssetAttributeMappingHandler>();
            serviceCollection.AddScoped<RuntimeAssetAttributeMappingHandler>();
            serviceCollection.AddScoped<CommandAssetAttributeMappingHandler>();
            serviceCollection.AddScoped<AliasAssetAttributeMappingHandler>();

            serviceCollection.AddScoped<IAttributeMappingHandler>(service =>
            {
                var staticAttributeHander = service.GetRequiredService<StaticAssetAttributeMappingHandler>();
                var runtimeAttributeHander = service.GetRequiredService<RuntimeAssetAttributeMappingHandler>();
                var dynamicAttributeHander = service.GetRequiredService<DynamicAssetAttributeMappingHandler>();
                var integrationAttributeHander = service.GetRequiredService<IntegrationAssetAttributeMappingHandler>();
                var commandAttributeHandler = service.GetRequiredService<CommandAssetAttributeMappingHandler>();
                var aliasAttributeHander = service.GetRequiredService<AliasAssetAttributeMappingHandler>();

                commandAttributeHandler.SetNextHandler(aliasAttributeHander);
                staticAttributeHander.SetNextHandler(runtimeAttributeHander);
                integrationAttributeHander.SetNextHandler(staticAttributeHander);
                dynamicAttributeHander.SetNextHandler(integrationAttributeHander);
                runtimeAttributeHander.SetNextHandler(commandAttributeHandler);
                return dynamicAttributeHander;
            });

            serviceCollection.AddArchiveValidations();

            serviceCollection.AddScoped<IDeviceFunction, DeviceFunction>();
            serviceCollection.AddScoped<IBlockEngine, BlockEngine>();
            serviceCollection.AddScoped<IBlockExecution>(serviceProvider =>
            {
                var aggregateAssetTableExecution = new AggregateAssetTableDataBlockExecution(null, serviceProvider);
                var queryAssetTableExecution = new QueryAssetTableDataBlockExecution(aggregateAssetTableExecution, serviceProvider);
                var singleAttributeExecution = new QueryLastValueSingleAttributeBlockExecution(queryAssetTableExecution, serviceProvider);
                var nearestAttributeExecution = new QueryNearestValueSingleAttributeBlockExecution(singleAttributeExecution, serviceProvider);
                var lastTimeDiffAttributeExecution = new QueryLastTimeDiffAttributeValueBlockExecution(nearestAttributeExecution, serviceProvider);
                var lastValueDiffAttributeExecution = new QueryLastValueDiffAttributeValueBlockExecution(lastTimeDiffAttributeExecution, serviceProvider);
                var timeDiff2PointsAttributeExecution = new QueryTimeDiff2PointsAttributeValueBlockExecution(lastValueDiffAttributeExecution, serviceProvider);
                var valueDiff2PointsAttributeExecution = new QueryValueDiff2PointsAttributeValueBlockExecution(timeDiff2PointsAttributeExecution, serviceProvider);
                var aggregateSingledAttributeExecution = new AggregateSingleAttributeValueBlockExecution(valueDiff2PointsAttributeExecution, serviceProvider);
                var durationSingledAttributeExecution = new QueryDurationAttributeValueBlockExecution(aggregateSingledAttributeExecution, serviceProvider);
                var countSingledAttributeExecution = new QueryCountAttributeValueBlockExecution(durationSingledAttributeExecution, serviceProvider);
                return countSingledAttributeExecution;
            });

            serviceCollection.AddScoped<IBlockWriter>(serviceProvider =>
            {
                var assetTableDelete = new AssetTableBlockDelete(null, serviceProvider);
                var assetTableWriter = new AssetTableBlockWriter(assetTableDelete, serviceProvider);
                var singleAttributeWriter = new SingleAttributeBlockWriter(assetTableWriter, serviceProvider);
                return singleAttributeWriter;
            });

            serviceCollection.AddScoped<IBlockTriggerHandler>(service =>
            {
                var assetAttributeTriggerHandler = new AssetAttributeBlockTriggerHandler(null, service);
                var schedulerTriggerHandler = new SchedulerBlockTriggerHandler(assetAttributeTriggerHandler, service);
                return schedulerTriggerHandler;
            });

            serviceCollection.AddScoped<IFunctionBlockWriterHandler>(service =>
            {
                var assetTableOutputHandler = new AssetTableFunctionBlockWriterHandler(null, service);
                var assetAttributeOutputHandler = new AssetAttributeFunctionBlockWriterHandler(assetTableOutputHandler, service);
                return assetAttributeOutputHandler;
            });

            serviceCollection.AddSingleton<IFunctionBlockExecutionResolver, FunctionBlockExecutionResolver>();
            serviceCollection.AddScoped<IFunctionBlockExecutionService, FunctionBlockExecutionService>();
            serviceCollection.AddScoped<IAssetAssemblyService, AssetAssemblyService>();
            serviceCollection.AddScoped<IAssetTemplateAssemblyService, AssetTemplateAssemblyService>();
            serviceCollection.AddScoped<ITokenService, TokenService>();
            serviceCollection.AddScoped<IAlarmRuleService, AlarmRuleService>();
            serviceCollection.AddScoped<IEventForwardingService, EventForwardingService>();
            serviceCollection.AddMemoryCache();
            serviceCollection.AddAuditLogService();
            serviceCollection.AddNotification();

            // for debugging purpose
            serviceCollection.AddOtelTracingService(SERVICE_NAME, typeof(ApplicationExtension).Assembly.GetName().Version.ToString());
            // for production, no need to output to console.
            // will adapt with open telemetry collector in the future.

            serviceCollection.AddLogging(builder =>
            {
                builder.AddOpenTelemetry(option =>
                {
                    option.SetResourceBuilder(
                        ResourceBuilder.CreateDefault()
                            .AddService(SERVICE_NAME, typeof(ApplicationExtension).Assembly.GetName().Version.ToString()));
                    //option.AddConsoleExporter();
                    option.AddOtlpExporter(oltp => { oltp.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf; });
                });
            });
            // Analytic api
            serviceCollection.AddScoped<IAssetAnalyticService, AssetAnalyticService>();

            // signal quality
            serviceCollection.AddScoped<IDeviceSignalQualityService, DeviceSignalQualityService>();
        }

        public static void AddApplicationValidator(this IServiceCollection serviceCollection)
        {
            // Dynamic validation registration.
            serviceCollection.AddDynamicValidation();

            // All the validator object should be added into DI
            var assemblyType = typeof(ApplicationExtension).GetTypeInfo();
            var validators = assemblyType.Assembly.DefinedTypes.Where(x => x.IsClass && !x.IsAbstract && typeof(IValidator).IsAssignableFrom(x)).ToArray();

            foreach (var validator in validators)
            {
                // Validator is an instance of abstract validator.
                if (validator.BaseType != null && validator.BaseType.IsGenericType &&
                    validator.BaseType.GetGenericTypeDefinition() == typeof(AbstractValidator<>))
                {
                    var validatorType =
                        typeof(IValidator<>).MakeGenericType(validator.BaseType.GetGenericArguments()[0]);
                    serviceCollection.AddSingleton(validatorType, validator);
                }
            }
        }
    }
}