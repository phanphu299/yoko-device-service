using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using AHI.Device.Function.Constant;
using AHI.Device.Function.Service.Abstraction;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;
using AHI.Infrastructure.Cache.Abstraction;
using AHI.Infrastructure.Interceptor.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.MultiTenancy.Extension;
using AHI.Infrastructure.Repository.Abstraction;
using AHI.Infrastructure.Repository.Abstraction.ReadOnly;
using AHI.Infrastructure.SharedKernel.Extension;
using Dapper;
using Function.Extension;
using Function.Helper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AHI.Device.Function.Service
{
    public class IntegrationCalculateRuntimeMetricService : IngestionProcessEventService, IIntegrationDeviceCalculateRuntimeMetricService
    {
        private readonly ITenantContext _tenantContext;
        private readonly ICache _cache;

        public IntegrationCalculateRuntimeMetricService(
            IDomainEventDispatcher domainEventDispatcher,
            IReadOnlyDeviceRepository readOnlyDeviceRepository,
            IConfiguration configuration,
            ILogger<IngestionProcessEventService> logger,
            ICache cache,
            ITenantContext tenantContext,
            IDynamicResolver dynamicResolver,
            IDbConnectionResolver dbConnectionResolver,
            IServiceProvider serviceProvider) : base(domainEventDispatcher, readOnlyDeviceRepository, configuration, logger, dynamicResolver, dbConnectionResolver, serviceProvider)
        {
            _tenantContext = tenantContext;
            _cache = cache;
        }

        public async Task CalculateRuntimeMetricAsync(IDictionary<string, object> metricDict)
        {
            var tenantId = metricDict[MetricPayload.TENANT_ID] as string;
            var subscriptionId = metricDict[MetricPayload.SUBSCRIPTION_ID] as string;
            var projectId = metricDict[MetricPayload.PROJECT_ID] as string;
            var integrationString = metricDict[MetricPayload.INTEGRATION_ID];

            if (integrationString == null)
            {
                _logger.LogDebug($"device and integrationId required");
                return;
            }

            var deviceInfos = await GetListDeviceInformationAsync(metricDict, projectId);
            var tasks = deviceInfos.Select(async deviceInformation =>
            {
                _logger.LogDebug($"IntegrationId {integrationString}/DeviceId {deviceInformation.DeviceId}");

                var hash = CacheKey.PROCESSING_DEVICE_HASH_FIELD.GetCacheKey($"{tenantId}_{subscriptionId}_external_{string.Join("_", metricDict.Select(x => $"{x.Key}_{x.Value}"))}".CalculateMd5Hash().ToLowerInvariant(), "");
                var hashPrefix = CacheKey.PROCESSING_DEVICE_HASH_KEY.GetCacheKey(projectId);

                var cacheHit = await _cache.GetHashByKeyInStringAsync(hashPrefix, hash);
                if (cacheHit != null)
                {
                    // nothing change. no need to update
                    _logger.LogDebug("Cache hit, no change, system will complete the request");
                    return;
                }

                await _cache.SetHashByKeyAsync(hashPrefix, hash, "cached");
                _logger.LogDebug($"Integration Id {integrationString}");

                var integrationId = Guid.Parse(integrationString.ToString());
                var timestampKeys = deviceInformation.Metrics.Where(x => x.MetricType == TemplateKeyTypes.TIMESTAMP).Select(x => x.MetricKey);
                var timestamp = (from ts in timestampKeys
                                 join metric in metricDict on ts.ToLowerInvariant() equals metric.Key.ToLowerInvariant()
                                 select metric.Value.ToString()
                    ).FirstOrDefault();
                var deviceTimestamp = timestamp.CutOffFloatingPointPlace().UnixTimeStampToDateTime().CutOffNanoseconds();
                _tenantContext.RetrieveFromString(tenantId, subscriptionId, projectId);

                var values = metricDict.Where(x => !RESERVE_KEYS.Contains(x.Key)).Select(x => new
                {
                    Timestamp = deviceTimestamp,
                    Value = x.Value.ToString(),
                    IntegrationId = integrationId,
                    DeviceId = deviceInformation.DeviceId,
                    MetricId = x.Key
                });

                using (var dbConnection = _dbConnectionResolver.CreateConnection())
                {
                    await dbConnection.OpenAsync();
                    using (var transaction = await dbConnection.BeginTransactionAsync())
                    {
                        try
                        {
                            await dbConnection.ExecuteAsync($@"INSERT INTO device_metric_external_snapshots(_ts, value, integration_id, device_id, metric_key)
                                                VALUES(@Timestamp, @Value, @IntegrationId, @DeviceId, @MetricId)
                                                ON CONFLICT (integration_id, device_id, metric_key)
                                                DO UPDATE SET _ts = EXCLUDED._ts, value = EXCLUDED.value WHERE device_metric_external_snapshots._ts < EXCLUDED._ts;
                                                ", values);

                            var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);
                            await retryStrategy.ExecuteAsync(async () => await transaction.CommitAsync());
                        }
                        catch (DbException ex)
                        {
                            _logger.LogError(ex, $"IntegrationCalculateRuntimeMetric DbException - values = {values.ToJson()}");
                            await transaction.RollbackAsync();
                            throw;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"IntegrationCalculateRuntimeMetric Exception - values = {values.ToJson()}");
                            await transaction.RollbackAsync();
                            throw;
                        }
                        finally
                        {
                            await dbConnection.CloseAsync();
                        }
                    }
                }

            });

            await Task.WhenAll(tasks);
        }
    }
}