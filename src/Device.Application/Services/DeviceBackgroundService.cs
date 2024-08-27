using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AHI.Infrastructure.Audit.Service.Abstraction;
using AHI.Infrastructure.Cache.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.SharedKernel.Abstraction;
using Device.Application.Asset.Command;
using Device.Application.Block.Command;
using Device.Application.BlockTemplate.Query;
using Device.Application.Constant;
using Device.Application.Device.Command;
using Device.Application.EntityLock.Command;
using Device.Application.Extension;
using Device.Application.Models;
using Device.Application.Repository;
using Device.Application.Service.Abstraction;
using Device.ApplicationExtension.Extension;

namespace Device.Application.Service
{
    public class DeviceBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Channel<QueueMessage> _channel;
        private readonly ILoggerAdapter<DeviceBackgroundService> _logger;

        public DeviceBackgroundService(IServiceProvider serviceProvider, ILoggerAdapter<DeviceBackgroundService> logger)
        {
            _channel = Channel.CreateUnbounded<QueueMessage>();
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var queueMessage = await _channel.Reader.ReadAsync(stoppingToken);
                try
                {
                    switch (queueMessage.Command)
                    {
                        case DelayNotificationMessage delayNotification:
                            _ = HandleNotificationMesssageAsync(queueMessage.TenantContext, delayNotification);
                            break;
                        case CleanAssetCache cleanAssetCache:
                            _ = ClearAssetCacheAsync(queueMessage.TenantContext, cleanAssetCache);
                            break;
                        case CleanTopicCache cleanTopicCache:
                            _ = CleanTopicCacheAsync(queueMessage.TenantContext, cleanTopicCache);
                            break;
                        case CleanFunctionBlockCache cleanFunctionBlockCache:
                            _ = ClearFunctionBlockCacheAsync(queueMessage.TenantContext, cleanFunctionBlockCache);
                            break;
                        case StoreObjectCache storeObjectCache:
                            _ = StoreObjectCacheAsync(storeObjectCache);
                            break;
                        case CleanDeviceCache cleanDeviceCache:
                            _ = ClearDeviceCacheAsync(queueMessage.TenantContext, cleanDeviceCache);
                            break;
                        case UpdateFunctionBlockTemplate updateFunctionBlockTemplate:
                            _ = UpdateRelatedBlockExecutionAsync(queueMessage.TenantContext, updateFunctionBlockTemplate);
                            break;
                        case UpdateFunctionBlock updateFunctionBlock:
                            _ = UpdateBlockTemplatesAndBlockExecutionsAsync(queueMessage.TenantContext, updateFunctionBlock.Id);
                            break;
                        case AddDeviceToCache newDevice:
                            _ = HandleAddDeviceToCache(queueMessage.TenantContext, newDevice.DeviceId);
                            break;
                        default:
                            break;
                    }
                }
                catch (System.Exception exc)
                {
                    _logger.LogError(exc, exc.Message);
                }
            }
        }

        private async Task HandleAddDeviceToCache(ITenantContext tenantContext, string deviceId)
        {
            if (tenantContext == null)
            {
                throw new ArgumentNullException(nameof(tenantContext));
            }
            try
            {
                var deviceRepository = _serviceProvider.GetRequiredService<IReadDeviceRepository>();
                var cache = _serviceProvider.GetRequiredService<ICache>();
                var deviceInfo = await deviceRepository.GetDeviceInformationAsync(tenantContext.ProjectId, deviceId);
                await cache.SetHashByKeyAsync(CacheKey.DeviceInfoPattern.GetCacheKey(tenantContext.ProjectId), deviceInfo.DeviceId.ToString(), deviceInfo);
            }
            catch (System.Exception ex)
            {
                _logger.LogError("HandleAddDeviceToCache got exception: {ex}", ex);
            }
        }

        public async Task QueueAsync(ITenantContext tenantContext, object command)
        {
            if (tenantContext == null)
            {
                throw new ArgumentNullException(nameof(tenantContext));
            }
            await _channel.Writer.WriteAsync(new QueueMessage(tenantContext, command));
        }

        public async Task SendNotificationMesssageAsync(ITenantContext tenantContext, int timeout, BaseEntityLock payload)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }
            if (tenantContext == null)
            {
                throw new ArgumentNullException(nameof(tenantContext));
            }
            await _channel.Writer.WriteAsync(new QueueMessage(tenantContext, new DelayNotificationMessage(timeout, payload)));
        }

        public async Task StoreObjectCacheAsync(StoreObjectCache command)
        {
            if (command.Value == null)
            {
                throw new ArgumentNullException(nameof(command));
            }
            var cache = _serviceProvider.GetRequiredService<ICache>();
            await cache.StoreAsync(command.Key, command.Value);
        }

        private async Task ClearFunctionBlockCacheAsync(ITenantContext tenantContext, CleanFunctionBlockCache command)
        {
            // delete all caches related to the uom
            using (var scope = _serviceProvider.CreateNewScope(tenantContext))
            {
                var scopeTenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                scopeTenantContext.CopyFrom(tenantContext);
                var cache = scope.ServiceProvider.GetRequiredService<ICache>();
                var blockTemplateUnitOfWork = scope.ServiceProvider.GetRequiredService<IBlockFunctionUnitOfWork>();
                var templateIds = await blockTemplateUnitOfWork.ReadFunctionBlocks
                                                                .AsQueryable()
                                                                .AsNoTracking()
                                                                .Where(x => command.Ids.Contains(x.Id))
                                                                .SelectMany(x => x.BlockTemplateMappings.Select(x => x.BlockTemplateId))
                                                                .ToListAsync();
                var executionIds = await blockTemplateUnitOfWork.ReadFunctionBlockExecutions
                                                                .AsQueryable()
                                                                .AsNoTracking()
                                                                .Where(x => (x.TemplateId != null && templateIds.Contains(x.TemplateId.Value)) || (x.FunctionBlockId != null && command.Ids.Contains(x.FunctionBlockId.Value)))
                                                                .Select(x => x.Id)
                                                                .ToListAsync();
                var tasks = command.Ids.Select(x =>
                {
                    var key = CacheKey.FUNCTION_BLOCK.GetCacheKey(tenantContext.ProjectId, x);
                    return cache.DeleteAsync(key);
                }).ToList();

                tasks.AddRange(templateIds.Select(templateId =>
                {
                    var hashKey = CacheKey.FUNCTION_BLOCK_TEMPLATE_HASH_KEY.GetCacheKey(templateId);
                    var hashField = CacheKey.FUNCTION_BLOCK_TEMPLATE_HASH_FIELD.GetCacheKey(tenantContext.ProjectId);
                    cache.DeleteHashByKeyAsync(hashKey, hashField);

                    return Task.FromResult(true);
                }));

                tasks.AddRange(executionIds.Select(async executionId =>
                {
                    var hashKey = CacheKey.FUNCTION_BLOCK_EXECUTION_HASH_KEY.GetCacheKey(tenantContext.ProjectId);
                    var hashField = CacheKey.FUNCTION_BLOCK_EXECUTION_HASH_FIELD.GetCacheKey(executionId);
                    await cache.DeleteHashByKeyAsync(hashKey, hashField);
                    return true;
                }));

                await Task.WhenAll(tasks);
            }
        }

        private async Task ClearAssetCacheAsync(ITenantContext tenantContext, CleanAssetCache command)
        {
            // delete all caches related to the assetId
            using (var scope = _serviceProvider.CreateNewScope(tenantContext))
            {
                var scopeTenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                scopeTenantContext.CopyFrom(tenantContext);
                var notificationService = scope.ServiceProvider.GetService(typeof(INotificationService)) as INotificationService;
                var cache = scope.ServiceProvider.GetRequiredService<ICache>();
                var deviceFunction = scope.ServiceProvider.GetRequiredService<IDeviceFunction>();

                // delete all processing cache
                string assetHashKey = CacheKey.ASSET_HASH_KEY.GetCacheKey(tenantContext.ProjectId);
                string assetSnapshotKeyPrefix = CacheKey.ASSET_SNAPSHOT_HASH_KEY.GetCacheKey(tenantContext.ProjectId);
                string assetFullKeyPrefix = CacheKey.FULL_ASSET_HASH_KEY.GetCacheKey(tenantContext.ProjectId);
                var processingKeyPrefix = CacheKey.PROCESSING_ASSET_HASH_KEY.GetCacheKey(tenantContext.ProjectId);
                var assetAttributeDevicePrefix = CacheKey.PROCESSING_DEVICE_HASH_KEY.GetCacheKey(tenantContext.ProjectId);
                var processingFailedKeyPrefix = CacheKey.PROCESSING_FAILED_HASH_KEY.GetCacheKey(tenantContext.ProjectId);
                var assetIdsKeyPrefix = CacheKey.PROCESSING_ASSET_IDS_HASH_KEY.GetCacheKey(tenantContext.ProjectId);

                if (command.OnlyCleanAssetDetail)
                {
                    await cache.ClearHashAsync(assetHashKey);
                }
                else
                {
                    await cache.ClearHashAsync(assetHashKey);
                    await cache.ClearHashAsync(assetSnapshotKeyPrefix);
                    await cache.ClearHashAsync(assetFullKeyPrefix);
                    await cache.ClearHashAsync(processingKeyPrefix);
                    await cache.ClearHashAsync(assetAttributeDevicePrefix);
                    await cache.ClearHashAsync(processingFailedKeyPrefix);
                    await cache.ClearHashAsync(assetIdsKeyPrefix);
                }

                if (command.AssetAttributes != null && command.AssetAttributes.Any())
                {
                    var attributes = command.AssetAttributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_RUNTIME);
                    await deviceFunction.CalculateRuntimeAsync(attributes);
                    var assetToNotifyIds = command.AssetAttributes.Select(x => x.AssetId).Distinct();
                    var notificationTasks = assetToNotifyIds.Select(x =>
                   {
                       return notificationService.SendAssetNotifyAsync(new AssetNotificationMessage(x, NotificationType.ASSET, null));
                   });
                    await Task.WhenAll(notificationTasks);
                }
            }
        }

        private async Task UpdateRelatedBlockExecutionAsync(ITenantContext tenantContext, UpdateFunctionBlockTemplate blockTemplate)
        {
            // Only refreshing BE if BT changed the Trigger/ Diagram (For example, if only BT's Name changed, do not refresh)
            if (blockTemplate.RequiredBlockExecutionRefreshing)
            {
                using (var scope = _serviceProvider.CreateNewScope(tenantContext))
                {
                    var scopeTenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                    scopeTenantContext.CopyFrom(tenantContext);
                    var blockExecutionService = scope.ServiceProvider.GetRequiredService<IFunctionBlockExecutionService>();
                    await blockExecutionService.RefreshBlockExecutionByTemplateIdAsync(blockTemplate.Id, blockTemplate.HasDiagramChanged);
                }
            }
        }

        private async Task UpdateBlockTemplatesAndBlockExecutionsAsync(ITenantContext tenantContext, Guid functionBlockId)
        {
            using (var scope = _serviceProvider.CreateNewScope(tenantContext))
            {
                var scopeTenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                scopeTenantContext.CopyFrom(tenantContext);
                var blockTemplateService = scope.ServiceProvider.GetRequiredService<IFunctionBlockTemplateService>();
                var blockExecutionService = scope.ServiceProvider.GetRequiredService<IFunctionBlockExecutionService>();
                var cache = scope.ServiceProvider.GetRequiredService<ICache>();

                var blockTemplateIds = await blockTemplateService.UpdateBlockTemplateContentsAsync(functionBlockId);
                await blockExecutionService.UpdateBlockExecutionStatusAsync(filter: f => blockTemplateIds.Any(templateId => templateId == f.TemplateId.Value),
                                                                            conditionToChangeStatus: b => b.Status.IsRunning(),
                                                                            targetStatus: BlockExecutionStatusConstants.RUNNING_OBSOLETE);
                await blockExecutionService.UpdateBlockExecutionStatusAsync(filter: f => f.FunctionBlockId == functionBlockId,
                                                                            conditionToChangeStatus: b => b.Status.IsRunning(),
                                                                            targetStatus: BlockExecutionStatusConstants.RUNNING_OBSOLETE);

                var blockTemplateHashKeys = blockTemplateIds.Select(id => CacheKey.FUNCTION_BLOCK_TEMPLATE_HASH_KEY.GetCacheKey(id));
                var tasks = blockTemplateHashKeys.Select(key => cache.DeleteHashByKeyAsync(key, CacheKey.FUNCTION_BLOCK_TEMPLATE_HASH_FIELD.GetCacheKey(tenantContext.ProjectId)));

                await Task.WhenAll(tasks);
            }
        }

        private async Task CleanTopicCacheAsync(ITenantContext tenantContext, CleanTopicCache cmd)
        {
            var topics = cmd.Topics;
            if (topics == null || !topics.Any())
                return;

            topics = topics.Where(x => !string.IsNullOrEmpty(x)).ToList();
            using (var scope = _serviceProvider.CreateNewScope(tenantContext))
            {
                var scopeTenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                scopeTenantContext.CopyFrom(tenantContext);
                var cache = scope.ServiceProvider.GetRequiredService<ICache>();

                var assetAttributeDeviceHashKey = CacheKey.PROCESSING_DEVICE_HASH_KEY.GetCacheKey(tenantContext.ProjectId);

                List<string> fields = await cache.GetHashFieldsByKeyAsync(assetAttributeDeviceHashKey);
                List<string> deleteFields = new List<string>();

                foreach (var topic in topics)
                {
                    if (fields != null && fields.Count > 0)
                    {
                        deleteFields.AddRange(fields.Where(f => f.Contains(topic)));
                    }
                }

                if (deleteFields.Count > 0)
                {
                    await cache.DeleteHashByKeysAsync(assetAttributeDeviceHashKey, deleteFields);
                }
            }
        }

        private async Task ClearDeviceCacheAsync(ITenantContext tenantContext, CleanDeviceCache command)
        {
            // delete all caches related to the assetId
            using (var scope = _serviceProvider.CreateNewScope(tenantContext))
            {
                var scopeTenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                scopeTenantContext.CopyFrom(tenantContext);
                var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                var enableCacheCleanup = Convert.ToBoolean(configuration["EnableCacheCleanup"] ?? "true");
                if (enableCacheCleanup)
                {
                    var cache = scope.ServiceProvider.GetRequiredService<ICache>();
                    var tasks = new List<Task>();

                    var deviceMetricHashKey = CacheKey.PROCESSING_DEVICE_HASH_KEY.GetCacheKey(tenantContext.ProjectId);
                    List<string> fields = await cache.GetHashFieldsByKeyAsync(deviceMetricHashKey);
                    List<string> deleteFields = new List<string>();

                    foreach (var deviceId in command.DeviceIds)
                    {
                        var key = CacheKey.DEVICE_STATUS_ONLINE_KEY.GetCacheKey(tenantContext.ProjectId, deviceId);
                        tasks.Add(cache.DeleteAsync(key));

                        if (fields != null && fields.Count > 0)
                        {
                            deleteFields.AddRange(fields.Where(f => f.Contains(deviceId)));
                        }
                    }

                    if (deleteFields.Count > 0)
                    {
                        await cache.DeleteHashByKeysAsync(deviceMetricHashKey, deleteFields);
                    }

                    await Task.WhenAll(tasks);
                }
                else
                {
                    _logger.LogTrace($"Cache cleanup is disabled.");
                }
            }
        }

        private async Task HandleNotificationMesssageAsync(ITenantContext tenantContext, DelayNotificationMessage command)
        {
            await Task.Delay(command.Timeout * 1000);
            using (var scope = _serviceProvider.CreateScope())
            {
                var scopeTenantContext = scope.ServiceProvider.GetService(typeof(ITenantContext)) as ITenantContext;
                scopeTenantContext.CopyFrom(tenantContext);
                var notificationService = scope.ServiceProvider.GetService(typeof(INotificationService)) as INotificationService;
                await notificationService.SendLockNotifyAsync(new LockNotificationMessage($"{command.Payload.TargetId}", command.Payload));
            }
        }

        private class QueueMessage
        {
            public ITenantContext TenantContext { get; set; }
            public object Command { get; set; }

            public QueueMessage(ITenantContext tenantContext, object command)
            {
                TenantContext = tenantContext;
                Command = command;
            }
        }

        private class DelayNotificationMessage
        {
            public int Timeout { get; set; }
            public BaseEntityLock Payload { get; set; }

            public DelayNotificationMessage(int timeout, BaseEntityLock payload)
            {
                Timeout = timeout;
                Payload = payload;
            }
        }
    }
}