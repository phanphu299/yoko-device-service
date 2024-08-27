using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;
using AHI.Infrastructure.Cache.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;
using Device.Consumer.KraftShared.Constant;
using Device.Consumer.KraftShared.Events;
using Device.Consumer.KraftShared.Extensions;
using Device.Consumer.KraftShared.Model;
using Device.Consumer.KraftShared.Models.MetricModel;
using Device.Consumer.KraftShared.Repositories.Abstraction;
using Device.Consumer.KraftShared.Repositories.Abstraction.ReadOnly;
using Device.Consumer.KraftShared.Service.Abstraction;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace Device.Consumer.KraftShared.Service
{
    //TODO: Enable again once AHI library upgrade to .NET 8
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

        public async Task ForwardingNotificationAssetMessageAsync(IngestionMessage ingestionMessage,
            IEnumerable<DeviceInformation> listDeviceInformation,
            Dictionary<string, long> dataUnixTimestamps, Dictionary<string, IEnumerable<RuntimeValueObject>> dataRuntimeValues)
        {
            var metricDict = ingestionMessage.RawData;
            var tenantId = ingestionMessage.TenantId;
            var subscriptionId = ingestionMessage.SubscriptionId;
            var projectId = ingestionMessage.ProjectId;
            if (metricDict.ContainsKey(MetricPayload.INTEGRATION_ID))
            {
                long unixTimestamp = 0;
                IEnumerable<Guid> assetIds;

                assetIds = await GetAssetIdsAsync(ingestionMessage, listDeviceInformation);
                await Task.WhenAll(SendNotificationAttributeChangeAsync(assetIds, unixTimestamp),
                    SendAssetNotificationMessageAsync(ingestionMessage.TenantId, ingestionMessage.SubscriptionId, ingestionMessage.ProjectId, assetIds),
                    CreateAndSendMessageTriggerFunctionBlock(tenantId, subscriptionId, projectId, assetIds, unixTimestamp));
            }
            else
            {
                var tasks = listDeviceInformation.Select(async deviceInformation =>
                {
                    var deviceId = deviceInformation.DeviceId;

                    var unixTimestamp = dataUnixTimestamps[deviceId];

                    var runtimeValues = dataRuntimeValues.ContainsKey(deviceId) ? dataRuntimeValues[deviceId] : Enumerable.Empty<RuntimeValueObject>();

                    var assetIds = await GetAssetIdsUnitTimestampAsync(ingestionMessage, runtimeValues, deviceId);

                    await Task.WhenAll(SendNotificationAttributeChangeAsync(assetIds, unixTimestamp),
                        SendAssetNotificationMessageAsync(ingestionMessage.TenantId, ingestionMessage.SubscriptionId, ingestionMessage.ProjectId, assetIds),
                        CreateAndSendMessageTriggerFunctionBlock(tenantId, subscriptionId, projectId, assetIds, unixTimestamp));
                });

                await Task.WhenAll(tasks);
            }
        }

        public async Task ForwardingNotificationAssetMessageAsync(string tenantId,
            string subscriptionId,
            string projectId,
            string deviceId,
            long unixTimestamp)
        {
            var assetIds = await _deviceRepository.GetAssetIdsAsync(projectId, deviceId);

            await Task.WhenAll(SendNotificationAttributeChangeAsync(assetIds, unixTimestamp),
                               CreateAndSendMessageTriggerFunctionBlock(tenantId, subscriptionId, projectId, assetIds, unixTimestamp));
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

        public async Task SendAssetNotificationMessageAsync(string tenantId, string subscriptionId, string projectId, IEnumerable<Guid> assetIds)
        {
            _tenantContext.SetTenantId(tenantId);
            _tenantContext.SetSubscriptionId(subscriptionId);
            _tenantContext.SetProjectId(projectId);
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

        public async Task SendAssetNotificationMessageAsync(string tenantId, string subscriptionId, string projectId, string deviceId)
        {
            _tenantContext.SetTenantId(tenantId);
            _tenantContext.SetSubscriptionId(subscriptionId);
            _tenantContext.SetProjectId(projectId);
            var assetIds = await _deviceRepository.GetAssetIdsAsync(projectId, deviceId);
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

        public async Task SendAssetNotificationMessageAsync(string tenantId, string subscriptionId, string projectId, IEnumerable<string> deviceIds)
        {
            _tenantContext.SetTenantId(tenantId);
            _tenantContext.SetSubscriptionId(subscriptionId);
            _tenantContext.SetProjectId(projectId);
            var listAssetId = new List<Guid>();
            foreach (var deviceId in deviceIds)
            {
                var assetIds = await _deviceRepository.GetAssetIdsAsync(projectId, deviceId);

                if (assetIds.Any())
                {
                    listAssetId.AddRange(assetIds);
                }
            }

            if (listAssetId != null && listAssetId.Any())
            {
                var notificationTasks = listAssetId.Select(assetId =>
                {
                    var notificationMessage = new AssetNotificationMessage(Constant.NotificationType.ASSET, assetId);
                    return _notificationService.NotifyAssetAsync(notificationMessage);
                });
                // FE need to load the snapshot when receiving this message.
                await Task.WhenAll(notificationTasks);
            }
        }

        private async Task<IEnumerable<Guid>> GetAssetIdsAsync(
            IngestionMessage ingestionMessage,
            string deviceId)
        {

            var tenantId = ingestionMessage.TenantId;
            var subscriptionId = ingestionMessage.SubscriptionId;

            var projectId = ingestionMessage.ProjectId;

            var metricDict = ingestionMessage.RawData;
            var hash = $"{tenantId}_{subscriptionId}_{projectId}_processing_device_external_{string.Join("_", metricDict.Select(x => $"{x.Key}_{x.Value}"))}".CalculateMd5Hash().ToLowerInvariant();

            var cacheHit = await _cache.GetStringAsync(hash);
            if (cacheHit != null)
            {
                // nothing change. no need to update
                // _logger.LogDebug($"Cache hit, no change, system will complete the request");
                return Enumerable.Empty<Guid>();
            }

            await _cache.StoreAsync(hash, "cached", TimeSpan.FromDays(1));

            var listAssetId = new List<Guid>();

            var assetIds = await _deviceRepository.GetAssetIdsAsync(projectId, deviceId);

            if (assetIds.Any())
            {
                listAssetId.AddRange(assetIds);
            }

            return listAssetId;
        }


        private async Task<IEnumerable<Guid>> GetAssetIdsAsync(IngestionMessage ingestionMessage, IEnumerable<DeviceInformation> listDeviceInformation)
        {

            var tenantId = ingestionMessage.TenantId;
            var subscriptionId = ingestionMessage.SubscriptionId;

            var projectId = ingestionMessage.ProjectId;

            var metricDict = ingestionMessage.RawData;
            var hash = $"{tenantId}_{subscriptionId}_{projectId}_processing_device_external_{string.Join("_", metricDict.Select(x => $"{x.Key}_{x.Value}"))}".CalculateMd5Hash().ToLowerInvariant();

            var cacheHit = await _cache.GetStringAsync(hash);
            if (cacheHit != null)
            {
                // nothing change. no need to update
                // _logger.LogDebug($"Cache hit, no change, system will complete the request");
                return Enumerable.Empty<Guid>();
            }

            await _cache.StoreAsync(hash, "cached", TimeSpan.FromDays(1));

            var listAssetId = new List<Guid>();

            foreach (var device in listDeviceInformation)
            {
                var assetIds = await _deviceRepository.GetAssetIdsAsync(projectId, device.DeviceId);

                if (assetIds.Any())
                {
                    listAssetId.AddRange(assetIds);
                }
            }

            return listAssetId;
        }

        private async Task<IEnumerable<Guid>> GetAssetIdsUnitTimestampAsync(IngestionMessage ingestionMessage, IEnumerable<RuntimeValueObject> runtimeValues, string deviceId)
        {

            var projectId = ingestionMessage.ProjectId;

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

        private async Task CreateAndSendMessageTriggerFunctionBlock(string tenantId, string subscriptionId, string projectId, IEnumerable<Guid> assetIds, long timestamp)
        {
            var triggerAttributes = await GetFunctionBlockTriggerAttributes(tenantId, subscriptionId, projectId, assetIds.ToList());
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

        private async Task<IEnumerable<TriggerAttributeFunctionBlockExecution>> GetFunctionBlockTriggerAttributes(string tenantId, string subscriptionId, string projectId, IList<Guid> assetIds)
        {
            return await _functionBlockExecutionService.FindFunctionBlockExecutionByAssetIds(tenantId, subscriptionId, projectId, assetIds);
        }
    }
}
