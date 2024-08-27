using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AHI.Device.Function.Constant;
using AHI.Device.Function.Events;
using AHI.Device.Function.Model;
using AHI.Infrastructure.Repository.Abstraction.ReadOnly;
using AHI.Device.Function.Service.Abstraction;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;
using AHI.Infrastructure.Cache.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.Repository.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;
using Dapper;
using Function.Extension;
using Microsoft.Extensions.Logging;
using Function.Extension;
namespace AHI.Device.Function.Service
{
    public class FowardingNotificationService : IFowardingNotificationService
    {
        private readonly ITenantContext _tenantContext;
        private readonly IAssetNotificationService _notificationService;
        private readonly IDomainEventDispatcher _domainEventDispatcher;
        private readonly ICache _cache;
        private readonly IDeviceRepository _deviceRepository;
        private readonly IReadOnlyDeviceRepository _readOnlyDeviceRepository;
        private readonly ILogger<FowardingNotificationService> _logger;
        private readonly IReadOnlyDbConnectionFactory _readOnlyDbConnectionFactory;
        private readonly IFunctionBlockExecutionService _functionBlockExecutionService;

        public FowardingNotificationService(
            ITenantContext tenantContext,
            IAssetNotificationService notificationService,
            IDomainEventDispatcher domainEventDispatcher,
            ICache cache,
            IDeviceRepository deviceRepository,
            ILogger<FowardingNotificationService> logger,
            IReadOnlyDeviceRepository readOnlyDeviceRepository,
            IReadOnlyDbConnectionFactory readOnlyDbConnectionFactory,
            IFunctionBlockExecutionService functionBlockExecutionService)
        {
            _tenantContext = tenantContext;
            _notificationService = notificationService;
            _domainEventDispatcher = domainEventDispatcher;
            _cache = cache;
            _deviceRepository = deviceRepository;
            _logger = logger;
            _readOnlyDeviceRepository = readOnlyDeviceRepository;
            _readOnlyDbConnectionFactory = readOnlyDbConnectionFactory;
            _functionBlockExecutionService = functionBlockExecutionService;
        }

        public async Task ForwardingNotificationAssetMessageAsync(IDictionary<string, object> metricDict, IEnumerable<DeviceInformation> listDeviceInformation, Dictionary<string, long> dataUnixTimestamps, Dictionary<string, IEnumerable<RuntimeValueObject>> dataRuntimeValues)
        {
            if (metricDict.ContainsKey(MetricPayload.INTEGRATION_ID))
            {
                long unixTimestamp = 0;
                IEnumerable<Guid> assetIds;

                assetIds = await GetAssetIdsAsync(metricDict, listDeviceInformation);
                await Task.WhenAll(SendNotificationAttributeChangeAsync(assetIds, unixTimestamp),
                    SendAssetNotificationMessageAsync(assetIds),
                    CreateAndSendMessageTriggerFunctionBlock(assetIds, unixTimestamp));
            }
            else
            {
                var tasks = listDeviceInformation.Select(async deviceInformation =>
                {
                    var deviceId = deviceInformation.DeviceId;

                    var unixTimestamp = dataUnixTimestamps[deviceId];

                    var runtimeValues = dataRuntimeValues.ContainsKey(deviceId) ? dataRuntimeValues[deviceId] : Enumerable.Empty<RuntimeValueObject>();

                    var assetIds = await GetAssetIdsUnitTimestampAsync(metricDict, runtimeValues, deviceId);

                    await Task.WhenAll(SendNotificationAttributeChangeAsync(assetIds, unixTimestamp),
                        SendAssetNotificationMessageAsync(assetIds),
                        CreateAndSendMessageTriggerFunctionBlock(assetIds, unixTimestamp));
                });

                await Task.WhenAll(tasks);
            }
        }

        private async Task SendNotificationAttributeChangeAsync(IEnumerable<Guid> assetIds, long unixTimestamp)
        {
            if (assetIds != null && assetIds.Any())
            {
                // var assetSnapshots = await _assetService.GetSnapshotAsync(unixTimestamp, updatedUtc.Value, assetIds);
                var tasks = assetIds.Select(assetId => { return _domainEventDispatcher.SendAsync(new AssetAttributeChangedEvent(assetId, unixTimestamp, _tenantContext)); });
                // FE need to load the snapshot when receiving this message.
                await Task.WhenAll(tasks);
            }
        }

        private async Task SendAssetNotificationMessageAsync(IEnumerable<Guid> assetIds)
        {
            if (assetIds != null && assetIds.Any())
            {
                var notificationTasks = assetIds.Select(assetId =>
                {
                    var notificationMessage = new AssetNotificationMessage(Constant.NotificationType.ASSET, assetId);
                    return _notificationService.NotifyAssetAsync(notificationMessage);
                });
                // FE need to load the snapshot when receiving this message.
                await Task.WhenAll(notificationTasks);
            }
        }

        private async Task<IEnumerable<Guid>> GetAssetIdsAsync(IDictionary<string, object> metricDict, IEnumerable<DeviceInformation> listDeviceInformation)
        {
            var tenantId = metricDict[Constant.MetricPayload.TENANT_ID] as string;
            var subscriptionId = metricDict[Constant.MetricPayload.SUBSCRIPTION_ID] as string;

            var projectId = metricDict[Constant.MetricPayload.PROJECT_ID] as string;

            var hash = CacheKey.PROCESSING_DEVICE_HASH_FIELD.GetCacheKey($"{tenantId}_{subscriptionId}_external_{string.Join("_", metricDict.Select(x => $"{x.Key}_{x.Value}"))}".CalculateMd5Hash().ToLowerInvariant(), "");
            var hashPrefix = CacheKey.PROCESSING_DEVICE_HASH_KEY.GetCacheKey(projectId);

            var cacheHit = await _cache.GetHashByKeyInStringAsync(hashPrefix, hash);
            if (cacheHit != null)
            {
                // nothing change. no need to update
                _logger.LogDebug("Cache hit, no change, system will complete the request");
                return null;
            }
            await _cache.SetHashByKeyAsync(hashPrefix, hash, "cached");

            var listAssetId = new List<Guid>();

            foreach (var deviceInfo in listDeviceInformation)
            {
                var assetIds = await _deviceRepository.GetAssetIdsAsync(projectId, deviceInfo.DeviceId);

                if (assetIds.Any())
                {
                    listAssetId.AddRange(assetIds);
                }
            }

            return listAssetId;
        }

        private async Task<IEnumerable<Guid>> GetAssetIdsUnitTimestampAsync(IDictionary<string, object> metricDict, IEnumerable<RuntimeValueObject> runtimeValues, string deviceId)
        {
            var projectId = metricDict[Constant.MetricPayload.PROJECT_ID] as string;

            // numeric value
            var numericValues = runtimeValues.Where(x => DataTypeExtensions.IsNumericTypeSeries(x.DataType));

            // text value
            var textValues = runtimeValues.Where(x => DataTypeExtensions.IsTextTypeSeries(x.DataType)).Select(x => new
            {
                Timestamp = x.Timestamp,
                AssetId = x.AssetId,
                AttributeId = x.AttributeId,
                Value = x.Value.ToString(), // should be a string
                DataType = x.DataType,
                RetentionDays = x.RetentionDays,
            });

            var runtimeAffectedIds = numericValues.Select(x => x.AssetId).Union(textValues.Select(x => x.AssetId)).Distinct();

            var assetIds = await _deviceRepository.GetAssetIdsAsync(projectId, deviceId);

            assetIds = assetIds.Union(runtimeAffectedIds).Distinct();

            return assetIds;
        }

        private async Task CreateAndSendMessageTriggerFunctionBlock(IEnumerable<Guid> assetIds, long timestamp)
        {
            try
            {
                List<Guid> assetIdsNotInCache = new List<Guid>();
                List<TriggerAttributeFunctionBlockExecution> triggerAttributes = new List<TriggerAttributeFunctionBlockExecution>();

                foreach (Guid assetId in assetIds)
                {
                    var cacheKey = CacheKey.FUNCTION_BLOCK_TRIGGER_KEY.GetCacheKey(assetId);
                    var triggerByAssetIds = await _cache.GetAsync<List<TriggerAttributeFunctionBlockExecutionByAssetId>>(cacheKey);

                    if (triggerByAssetIds != null)
                    {
                        foreach (TriggerAttributeFunctionBlockExecutionByAssetId triggerByAssetId in triggerByAssetIds)
                        {
                            triggerAttributes.Add(new TriggerAttributeFunctionBlockExecution()
                            {
                                AssetId = assetId,
                                FunctionBlockId = triggerByAssetId.FunctionBlockId,
                                AssetAttributeId = triggerByAssetId.AssetAttributeId
                            });
                        }
                    }
                    else
                    {
                        assetIdsNotInCache.Add(assetId);
                    }
                }

                if (assetIdsNotInCache.Count > 0)
                {
                    List<TriggerAttributeFunctionBlockExecution> triggerAttributesFromDbs = (await GetFunctionBlockTriggerAttributes(assetIdsNotInCache)).ToList();

                    if (triggerAttributesFromDbs.Count > 0)
                    {
                        triggerAttributes.AddRange(triggerAttributesFromDbs);

                        foreach (Guid assetId in assetIdsNotInCache)
                        {
                            var triggerByAssetIds = triggerAttributesFromDbs.Where(x => x.AssetId == assetId)
                                                        .Select(x => new TriggerAttributeFunctionBlockExecutionByAssetId() { AssetAttributeId = x.AssetAttributeId, FunctionBlockId = x.FunctionBlockId })
                                                        .ToList();

                            var cacheKey = CacheKey.FUNCTION_BLOCK_TRIGGER_KEY.GetCacheKey(assetId);
                            await _cache.StoreAsync(cacheKey, triggerByAssetIds);
                        }
                    }
                }

                if (!triggerAttributes.Any())
                    return;

                _logger.LogInformation("Has trigger attrs: {count}", triggerAttributes.Count());

                var messages = triggerAttributes.GroupBy(x => new
                {
                    x.AssetId,
                    x.AssetAttributeId
                }).Select(x => new TriggerFunctionBlockMessage()
                {
                    UnixTimestamp = timestamp,
                    AssetId = x.Key.AssetId,
                    AttributeId = x.Key.AssetAttributeId,
                    FunctionBlockIds = x.Select(x => x.FunctionBlockId),
                    TenantId = _tenantContext.TenantId,
                    SubscriptionId = _tenantContext.SubscriptionId,
                    ProjectId = _tenantContext.ProjectId,
                });

                _logger.LogInformation("Message trigger fbe: {count}", messages.Count());

                var tasks = messages.Select(x => _domainEventDispatcher.SendAsync(x));
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in CreateAndSendMessageTriggerFunctionBlock {ex.Message}");
            }
        }

        private async Task<IEnumerable<TriggerAttributeFunctionBlockExecution>> GetFunctionBlockTriggerAttributes(IList<Guid> assetIds)
        {
            return await _functionBlockExecutionService.FindFunctionBlockExecutionByAssetIds(assetIds);
        }
    }
}