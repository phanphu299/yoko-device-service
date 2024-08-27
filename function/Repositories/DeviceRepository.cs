using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using Dapper;
using AHI.Infrastructure.Cache.Abstraction;
using System;
using AHI.Device.Function.Model;
using AHI.Device.Function.Constant;
using System.Linq;
using AHI.Infrastructure.Repository.Abstraction;
using AHI.Infrastructure.Repository.Abstraction.ReadOnly;
using Function.Helper;
using Microsoft.Extensions.Logging;
using Function.Extension;
using AHI.Infrastructure.Service.Tag.Model;
using AHI.Infrastructure.Service.Tag.Service.Abstraction;

namespace AHI.Infrastructure.Repository
{
    public class DeviceRepository : IDeviceRepository, IReadOnlyDeviceRepository
    {
        private readonly ICache _cache;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        protected readonly ILogger<DeviceRepository> _logger;
        private readonly ITagService _tagService;

        public DeviceRepository(
            ICache cache,
            ILogger<DeviceRepository> logger,
            IDbConnectionFactory dbConnectionFactory,
            ITagService tagService
        )
        {
            _logger = logger;
            _dbConnectionFactory = dbConnectionFactory;
            _cache = cache;
            _tagService = tagService;
        }

        public async Task<IEnumerable<AssetRuntimeTrigger>> GetAssetTriggerAsync(string projectId, string deviceId)
        {
            // need to cache the data for device.
            string assetAttributeRelevantToDeviceIdHashField = CacheKey.PROCESSING_DEVICE_HASH_FIELD.GetCacheKey(deviceId, "asset_related_runtime_trigger_with_metric");
            string assetAttributeRelevantToDeviceIdKeyHashKey = CacheKey.PROCESSING_DEVICE_HASH_KEY.GetCacheKey(projectId);

            var runtimeAssetIds = await _cache.GetHashByKeyAsync<IEnumerable<AssetRuntimeTrigger>>(assetAttributeRelevantToDeviceIdKeyHashKey, assetAttributeRelevantToDeviceIdHashField);
            if (runtimeAssetIds == null)
            {
                using (var dbConnection = GetDbConnection(projectId))
                {
                    var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);

                    // find_all_asset_trigger_by_device_id_refactor requires the write connection
                    runtimeAssetIds = await retryStrategy.ExecuteAsync(async () =>
                        await dbConnection.QueryAsync<AssetRuntimeTrigger>(@"select distinct asset_id as AssetId
                                                                                , attribute_id as AttributeId
                                                                                , trigger_asset_id as TriggerAssetId
                                                                                , trigger_attribute_id as TriggerAttributeId
                                                                                , metric_key as MetricKey
                                                                                from find_all_asset_trigger_by_device_id_refactor(@DeviceId)",
                        new
                        {
                            DeviceId = deviceId
                        }, commandTimeout: 300)
                    );
                    dbConnection.Close();
                }

                await _cache.SetHashByKeyAsync(assetAttributeRelevantToDeviceIdKeyHashKey, assetAttributeRelevantToDeviceIdHashField, runtimeAssetIds);
            }
            else
            {
                // nothing change. no need to update
                _logger.LogDebug("GetAssetTriggerAsync - Cache hit, no change");
            }

            return runtimeAssetIds;
        }

        public async Task<IEnumerable<Guid>> GetAssetIdsAsync(string projectId, string deviceId)
        {
            _logger.LogTrace($"[GetAssetIdsAsync] - Start processing for Project {projectId}...");
            try
            {
                var assetRelatedToDeviceIdHashField = CacheKey.PROCESSING_DEVICE_HASH_FIELD.GetCacheKey(deviceId, "asset_related");
                var assetRelatedToDeviceIdKeyHashKey = CacheKey.PROCESSING_DEVICE_HASH_KEY.GetCacheKey(projectId);

                var assetIds = await _cache.GetHashByKeyAsync<IEnumerable<Guid>>(assetRelatedToDeviceIdKeyHashKey, assetRelatedToDeviceIdHashField);
                if (assetIds == null)
                {
                    using (var dbConnection = GetDbConnection(projectId))
                    {
                        var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);

                        // find_all_asset_related_to_device_refactor requires the write connection
                        assetIds = await retryStrategy.ExecuteAsync(async () =>
                            await dbConnection.QueryAsync<Guid>(@"select distinct asset_id from find_all_asset_related_to_device_refactor(@DeviceId)", new
                            {
                                DeviceId = deviceId
                            })
                        );
                        dbConnection.Close();
                    }

                    await _cache.SetHashByKeyAsync(assetRelatedToDeviceIdKeyHashKey, assetRelatedToDeviceIdHashField, assetIds);
                }
                else
                {
                    // nothing change. no need to update
                    _logger.LogDebug($"[GetAssetIdsAsync][{projectId}] - Cache hit, no change...");
                }

                _logger.LogTrace($"[GetAssetIdsAsync][{projectId}] - Completed for Device {deviceId}!");
                return assetIds;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, $"[GetAssetIdsAsync][{projectId}] - Failed for Device {deviceId}!");
                return default;
            }
        }

        public async Task<IEnumerable<string>> GetDeviceMetricKeyAsync(string projectId)
        {
            _logger.LogInformation($"GetDeviceMetricKeyAsync - projectId={projectId}");

            var deviceMetricKeyDeviceHashField = CacheKey.PROCESSING_DEVICE_HASH_FIELD.GetCacheKey(string.Empty, "metric_device_id_key");
            var deviceMetricKeyDeviceIdHashKey = CacheKey.PROCESSING_DEVICE_HASH_KEY.GetCacheKey(projectId);

            var deviceIdKeys = await _cache.GetHashByKeyAsync<IEnumerable<string>>(deviceMetricKeyDeviceIdHashKey, deviceMetricKeyDeviceHashField);

            if (deviceIdKeys == null)
            {
                using (var dbConnection = GetDbConnection(projectId))
                {
                    var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);
                    // get device Id
                    deviceIdKeys = await retryStrategy.ExecuteAsync(async () =>
                        await dbConnection.QueryAsync<string>("select distinct key from v_template_deviceId")
                    );
                    dbConnection.Close();
                }

                await _cache.SetHashByKeyAsync(deviceMetricKeyDeviceIdHashKey, deviceMetricKeyDeviceHashField, deviceIdKeys);
            }
            else
            {
                // nothing change. no need to update
                _logger.LogDebug("GetDeviceMetricKeyAsync - Cache hit, no change");
            }

            return deviceIdKeys;
        }

        public async Task<DeviceInformation> GetDeviceInformationAsync(string projectId, string[] deviceIds)
        {
            _logger.LogInformation($"GetDeviceInformationAsync - projectId={projectId}, deviceIds={string.Join("_", deviceIds)}");

            var deviceMetricHashField = CacheKey.PROCESSING_DEVICE_HASH_FIELD.GetCacheKey(string.Join("_", deviceIds), "device_metrics");
            var deviceMetricNotFoundHashField = CacheKey.PROCESSING_DEVICE_HASH_FIELD.GetCacheKey(string.Join("_", deviceIds), "device_metrics_not_found");
            var deviceMetricHashKey = CacheKey.PROCESSING_DEVICE_HASH_KEY.GetCacheKey(projectId);

            if (await _cache.GetHashByKeyInStringAsync(deviceMetricHashKey, deviceMetricNotFoundHashField) == "1")
            {
                // not found
                return null;
            }

            var deviceInformation = await _cache.GetHashByKeyAsync<DeviceInformation>(deviceMetricHashKey, deviceMetricHashField);

            if (deviceInformation == null)
            {
                using (var dbConnection = GetDbConnection(projectId))
                {
                    var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);
                    var reader = await retryStrategy.ExecuteAsync(async () =>
                        await dbConnection.QueryMultipleAsync(
                        $@" select id as DeviceId, 
                                   retention_days as RetentionDays,
                                   enable_health_check as EnableHealthCheck
                            from devices 
                            where id = ANY(@DeviceIds);
                                           
                            select metric_key as MetricKey, 
                                   expression_compile as ExpressionCompile,
                                   data_type as DataType, 
                                   value as Value, 
                                   metric_type as MetricType
                            from v_device_metrics
                            where device_id = ANY(@DeviceIds);",
                        new { DeviceIds = deviceIds },
                        commandTimeout: 600)
                    );

                    deviceInformation = await reader.ReadFirstOrDefaultAsync<DeviceInformation>();
                    if (deviceInformation != null)
                    {
                        deviceInformation.Metrics = await reader.ReadAsync<DeviceMetricDataType>();
                    }

                    dbConnection.Close();
                }

                await _cache.SetHashByKeyAsync(deviceMetricHashKey, deviceMetricHashField, deviceInformation);
                if (deviceInformation == null)
                {
                    await _cache.SetHashByKeyAsync(deviceMetricHashKey, deviceMetricNotFoundHashField, "1");
                }
            }
            else
            {
                // nothing change. no need to update
                _logger.LogDebug("GetDeviceInformationAsync - Cache hit, no change");
            }

            return deviceInformation;
        }

        public async Task<IEnumerable<DeviceInformation>> GetDeviceInformationsWithTopicNameAsync(string projectId, string topicName, string brokerType)
        {
            _logger.LogInformation($"GetDeviceInformationsWithTopicNameAsync - projectId={projectId}, topicName={topicName}");

            var deviceMetricHashField = CacheKey.PROCESSING_DEVICE_HASH_FIELD.GetCacheKey($"topic_{topicName}", "device_metrics");
            var deviceMetricNotFoundHashField = CacheKey.PROCESSING_DEVICE_HASH_FIELD.GetCacheKey($"topic_{topicName}", "device_metrics_not_found");
            var deviceMetricHashKey = CacheKey.PROCESSING_DEVICE_HASH_KEY.GetCacheKey(projectId);

            if (await _cache.GetHashByKeyInStringAsync(deviceMetricHashKey, deviceMetricNotFoundHashField) == "1")
            {
                // not found
                return null;
            }

            IEnumerable<DeviceInformation> deviceInformations = await _cache.GetHashByKeyAsync<IEnumerable<DeviceInformation>>(deviceMetricHashKey, deviceMetricHashField);
            if (deviceInformations == null)
            {
                using var dbConnection = GetDbConnection(projectId);

                var sql = $@"
                            SELECT
		                            d.id as DeviceId,
	                                d.retention_days as RetentionDays,
	                                d.enable_health_check as EnableHealthCheck,
	                                vd.metric_key as MetricKey,
	                                vd.expression_compile as ExpressionCompile,
	                                vd.data_type as datatype,
	                                vd.value as Value,
	                                vd.metric_type as MetricType
                            FROM public.devices as d
                            INNER JOIN v_device_metrics as vd ON d.id = vd.device_id
                            WHERE d.telemetry_topic = @TopicName AND d.device_content like @BrokerType;
                            ";

                var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);
                var result = await retryStrategy.ExecuteAsync(async () =>
                    await dbConnection.QueryAsync<DeviceInformation, DeviceMetricDataType, DeviceInformation>(
                        sql,
                        (device, metric) =>
                        {
                            device.Metrics = device.Metrics.Append(metric);
                            return device;
                        },
                        new
                        {
                            TopicName = topicName,
                            BrokerType = string.Format($"%{brokerType}%")
                        },
                        commandTimeout: 600,
                        splitOn: "DeviceId, MetricKey")
                );

                dbConnection.Close();

                if (result.Any())
                {
                    deviceInformations = result.GroupBy(d => d.DeviceId).Select(g =>
                    {
                        var groupedDevice = g.First();
                        groupedDevice.Metrics = g.Select(d => d.Metrics.First()).ToList();
                        return groupedDevice;
                    });
                }
                else
                {
                    await _cache.SetHashByKeyAsync(deviceMetricHashKey, deviceMetricNotFoundHashField, "1");
                }

                await _cache.SetHashByKeyAsync(deviceMetricHashKey, deviceMetricHashField, deviceInformations);
            }
            else
            {
                // nothing change. no need to update
                _logger.LogDebug($"{nameof(GetDeviceInformationsWithTopicNameAsync)} - Cache hit, no change");
            }

            return deviceInformations;
        }

        public async Task<IEnumerable<MetricSeriesDto>> GetMetricNumericsAsync(string deviceIdFromFile)
        {
            using var dbConnection = GetDbConnection();
            var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);
            var metricNumerics = await retryStrategy.ExecuteAsync(async () =>
                await dbConnection.QueryAsync<MetricSeriesDto>($@"
                    select _ts as Timestamp, device_id as DeviceId, metric_key as MetricKey, value as Value
                    from(
                        select _ts, device_id, metric_key, value,
                        row_number() over(partition  by metric_key order by _ts desc) as row_number
                        from(select _ts, device_id, metric_key, value
                                from device_metric_series
                                where device_id = @DeviceId and _ts <= current_timestamp
                                order by _ts desc
                        ) tempGrp
                    ) tempSerie where row_number = 1", new { DeviceId = deviceIdFromFile })
            );

            dbConnection.Close();
            return metricNumerics;
        }

        public async Task<IEnumerable<MetricSeriesTextDto>> GetMetricTextsAsync(string deviceIdFromFile)
        {
            using var dbConnection = GetDbConnection();
            var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);
            var metricTexts = await retryStrategy.ExecuteAsync(async () =>
                await dbConnection.QueryAsync<MetricSeriesTextDto>($@"
                    select _ts as Timestamp, device_id as DeviceId, metric_key as MetricKey, value as Value
                    from(
                        select _ts, device_id, metric_key, value,
                        row_number() over(partition  by metric_key order by _ts desc) as row_number
                        from(select _ts, device_id, metric_key, value
                                from device_metric_series_text
                                where device_id = @DeviceId and _ts <= current_timestamp
                                order by _ts desc
                        ) tempGrp
                    ) tempSerie where row_number = 1", new { DeviceId = deviceIdFromFile }));

            dbConnection.Close();
            return metricTexts;
        }

        public async Task<IEnumerable<DeviceMetricDataType>> GetMetricDataTypesAsync(string deviceIdFromFile)
        {
            using var dbConnection = GetDbConnection();
            var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);
            var dataTypes = await retryStrategy.ExecuteAsync(async () =>
                await dbConnection.QueryAsync<DeviceMetricDataType>(
                    $@" select metric_key as MetricKey
                            , expression_compile as ExpressionCompile
                            , data_type as DataType
                            , value as Value
                            , metric_type as MetricType 
                        from v_device_metrics 
                        where device_id = @DeviceId
                        ",
                    new
                    {
                        DeviceId = deviceIdFromFile
                    }, commandTimeout: 600)
            );
            dbConnection.Close();
            return dataTypes;
        }

        public async Task<IEnumerable<(string MetricKey, string DataType, DateTime? Timestamp)>> GetActiveDeviceMetricsWithTimestampsAsync(string projectId, string deviceId, IList<string> deviceMetrics)
        {
            using (var connection = GetDbConnection(projectId))
            {
                var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);
                var metrics = await retryStrategy.ExecuteAsync(async () =>
                    await connection.QueryAsync<(string MetricKey, string DataType, DateTime? Timestamp)>(
                        $@"SELECT distinct metric_key, data_type, _ts as timestamp
                            FROM v_device_metrics
                            WHERE device_id = @deviceId
                            AND metric_key = ANY(@metricKeys)",
                        new
                        {
                            deviceId = deviceId,
                            metricKeys = deviceMetrics
                        }, commandTimeout: 600)
                );
                connection.Close();
                return metrics;
            }
        }

        public async Task<IEnumerable<(string MetricKey, string DataType)>> GetActiveDeviceMetricsAsync(string projectId, string deviceId, IList<string> deviceMetrics)
        {
            using (var connection = GetDbConnection(projectId))
            {
                var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);
                var metrics = await retryStrategy.ExecuteAsync(async () =>
                    await connection.QueryAsync<(string MetricKey, string DataType)>(
                        $@"SELECT distinct metric_key, data_type
                        FROM v_device_metrics_enable
                        WHERE device_id = @deviceId
                        AND metric_key = ANY(@metricKeys)",
                        new
                        {
                            deviceId = deviceId,
                            metricKeys = deviceMetrics
                        }, commandTimeout: 600)
                );
                connection.Close();
                return metrics;
            }
        }

        private IDbConnection GetDbConnection(string projectId = null) => _dbConnectionFactory.CreateConnection(projectId);
    }

    public class MetricSeriesDto
    {
        public DateTime Timestamp { get; set; }
        public string DeviceId { get; set; }
        public string MetricKey { get; set; }
        public double Value { get; set; }
        public int RetentionDays { get; set; }
    }

    public class MetricSeriesTextDto
    {
        public DateTime Timestamp { get; set; }
        public string DeviceId { get; set; }
        public string MetricKey { get; set; }
        public string Value { get; set; }
        public int RetentionDays { get; set; }
    }
}