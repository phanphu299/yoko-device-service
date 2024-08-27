using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Device.Consumer.KraftShared.Service.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;
using AHI.Infrastructure.SharedKernel.Abstraction;
using AHI.Infrastructure.Cache.Abstraction;
using Device.Consumer.KraftShared.Model;
using Device.Consumer.KraftShared.Extensions;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.MultiTenancy.Extension;
using Device.Consumer.KraftShared.Constant;
using Device.Consumer.KraftShared.Services;
using Device.Consumer.KraftShared.Repositories.Abstraction;
namespace Device.Consumer.KraftShared.Service
{
    public class IntegrationDeviceService : IIntegrationDeviceService
    {
        private readonly IConfiguration _configuration;
        private readonly ILoggerAdapter<IntegrationDeviceService> _logger;
        private readonly IEnumerable<IDataProcessor> _dataProcessors;
        private readonly IAssetNotificationService _notificationService;
        private readonly ICache _cache;
        private readonly IRuntimeAttributeService _runtimeAttributeService;
        private readonly ITenantContext _tenantContext;
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public IntegrationDeviceService(IConfiguration configuration,
            ILoggerAdapter<IntegrationDeviceService> logger,
            IEnumerable<IDataProcessor> dataProcessors,
            IAssetNotificationService notificationService,
            IRuntimeAttributeService runtimeAttributeService,
            ICache cache,
            ITenantContext tenantContext,
            IDbConnectionFactory dbConnectionFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _dataProcessors = dataProcessors;
            _notificationService = notificationService;
            _cache = cache;
            _runtimeAttributeService = runtimeAttributeService;
            _tenantContext = tenantContext;
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<IEnumerable<Guid>> ProcessEventAsync(IngestionMessage message)
        {
            var tenantId = message.TenantId;
            var subscriptionId = message.SubscriptionId;
            var projectId = message.ProjectId;
            var deviceId = message.RawData[MetricPayload.DEVICE_ID].ToString();
            var integrationString = message.RawData[MetricPayload.INTEGRATION_ID].ToString();
            if (deviceId == null || integrationString == null)
            {
                _logger.LogDebug($"{deviceId} and integrationId required");
                return null;
            }
            _logger.LogDebug($"IntegrationId {integrationString}/DeviceId {deviceId}");
            var hash = $"{tenantId}_{subscriptionId}_{projectId}_processing_device_external_{string.Join("_", message.RawData.Select(x => $"{x.Key}_{x.Value}"))}".CalculateMd5Hash().ToLowerInvariant();
            var cacheHit = await _cache.GetStringAsync(hash, RedisConstants.PROCESSING_DEFAULT_DATABASE);
            if (cacheHit != null)
            {
                // nothing change. no need to update
                _logger.LogDebug($"Cache hit, no change, system will complete the request");
                return null;
            }
            await _cache.StoreAsync(hash, "cached", TimeSpan.FromDays(1), RedisConstants.PROCESSING_DEFAULT_DATABASE);
            _logger.LogDebug($"Integration Id {integrationString}");
            var integrationId = Guid.Parse(integrationString.ToString());
            string timestamp = null;
            if (message.RawData.ContainsKey(MetricPayload.TIMESTAMP))
            {
                timestamp = message.RawData[MetricPayload.TIMESTAMP].ToString();
            }
            var deviceTimestamp = timestamp.CutOffFloatingPointPlace().UnixTimeStampToDateTime().CutOffNanoseconds();
            _tenantContext.RetrieveFromString(tenantId, subscriptionId, projectId);
            // insert into snapshot table
            using (var dbConnection = GetDbConnection(projectId))
            {
                var values = message.RawData.Where(x => !DeviceService.RESERVE_KEYS.Contains(x.Key)).Select(x => new
                {
                    Timestamp = deviceTimestamp,
                    Value = x.Value.ToString(),
                    IntegrationId = integrationId,
                    DeviceId = deviceId,
                    MetricId = x.Key
                });

                await dbConnection.ExecuteAsync($@"INSERT INTO device_metric_external_snapshots(_ts, value, integration_id, device_id, metric_key) 
                                                VALUES(@Timestamp, @Value, @IntegrationId, @DeviceId, @MetricId)
                                                ON CONFLICT (integration_id, device_id, metric_key)
                                                DO UPDATE SET _ts = EXCLUDED._ts, value = EXCLUDED.value WHERE device_metric_external_snapshots._ts < EXCLUDED._ts;
                                                ", values);

                var assetAttributeRelevantToDeviceIdKey = $"{projectId}_processing_device_{deviceId}_asset_related_runtime";
                var runtimeAssetIds = await _cache.GetAsync<IEnumerable<AssetRuntimeTrigger>>(assetAttributeRelevantToDeviceIdKey, RedisConstants.PROCESSING_DEFAULT_DATABASE);
                if (runtimeAssetIds == null)
                {
                    runtimeAssetIds = await dbConnection.QueryAsync<AssetRuntimeTrigger>(@"select distinct asset_id as AssetId
                                                                                            , asset_attribute_id as AttributeId
                                                                                            , trigger_asset_id as TriggerAssetId
                                                                                            , trigger_attribute_id as TriggerAttributeId
                                                                                            from find_all_asset_trigger_by_device_id(@DeviceId)",
                        new
                        {
                            DeviceId = deviceId
                        });
                    if (runtimeAssetIds.Any())
                    {
                        await _cache.StoreAsync(assetAttributeRelevantToDeviceIdKey, runtimeAssetIds, TimeSpan.FromDays(1), RedisConstants.PROCESSING_DEFAULT_DATABASE);
                    }
                }

                await _runtimeAttributeService.CalculateRuntimeValueAsync(_tenantContext.ProjectId, deviceTimestamp, runtimeAssetIds);

                var assetRelatedToDeviceIdKey = $"{projectId}_processing_device_{deviceId}_asset_related";
                var assetIds = await _cache.GetAsync<IEnumerable<Guid>>(assetRelatedToDeviceIdKey, RedisConstants.PROCESSING_DEFAULT_DATABASE);
                if (assetIds == null)
                {
                    assetIds = await dbConnection.QueryAsync<Guid>(@"select distinct asset_id from find_all_asset_related_to_device(@DeviceId)", new
                    {
                        DeviceId = deviceId
                    });
                    if (assetIds.Any())
                    {
                        await _cache.StoreAsync(assetRelatedToDeviceIdKey, assetIds, TimeSpan.FromDays(1), RedisConstants.PROCESSING_DEFAULT_DATABASE);
                    }
                }
                dbConnection.Close();

                // var deleteCacheTasks = assetIds.Select(x =>
                // {
                //     var key = $"{tenantId}_{subscriptionId}_{projectId}_snapshot_asset_{x}_*";
                //     return _cache.DeleteAllKeysAsync(key);
                // });
                // await Task.WhenAll(deleteCacheTasks);
                var tasks = assetIds.Select(assetId =>
                {
                    var notificationMessage = new AssetNotificationMessage(Constant.NotificationType.ASSET, assetId);
                    return _notificationService.NotifyAssetAsync(notificationMessage);
                });
                // FE need to load the snapshot when receiving this message.
                await Task.WhenAll(tasks);
                return assetIds;
            }
        }
        private IDbConnection GetDbConnection(string projectId = null) => _dbConnectionFactory.CreateConnection(projectId);
    }
}
