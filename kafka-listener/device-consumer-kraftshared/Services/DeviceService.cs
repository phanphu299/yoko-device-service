using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Device.Consumer.KraftShared.Model;
using Device.Consumer.KraftShared.Service.Abstraction;
using System;
using AHI.Infrastructure.SharedKernel.Extension;
using AHI.Infrastructure.SharedKernel.Abstraction;
using AHI.Infrastructure.Cache.Abstraction;
using AHI.Infrastructure.Interceptor.Abstraction;
using Device.Consumer.KraftShared.Extensions;
using Device.Consumer.KraftShared.Constant;
using Microsoft.Extensions.Caching.Memory;
using Device.Consumer.KraftShared.Repositories.Abstraction;
using AHI.Infrastructure.Exception;
using Device.Consumer.KraftShared.Repositories.Abstraction.ReadOnly;
namespace Device.Consumer.KraftShared.Services
{
    public class DeviceService : IDeviceService
    {
        private readonly IConfiguration _configuration;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ILoggerAdapter<DeviceService> _logger;
        private readonly ICache _cache;
        private readonly IMemoryCache _memoryCache;
        private readonly IRuntimeAttributeService _runtimeAttributeService;
        private readonly IDynamicResolver _dynamicResolver;
        //private readonly ITenantContext _tenantContext;
        private readonly IDeviceHeartbeatService _deviceHeartbeatService;
        private readonly IReadOnlyDeviceRepository _readOnlyDeviceRepository;
        private const int SIGNAL_QUALITY_CODE_GOOD = 192;
        private readonly IDeviceRepository _deviceRepository;

        public static string[] RESERVE_KEYS = new[] { "tenantId", "subscriptionId", "projectId", "deviceId", "integrationId" };
        public DeviceService(IConfiguration configuration,
            IDbConnectionFactory dbConnectionFactory,
            ILoggerAdapter<DeviceService> logger,
            ICache cache,
            IRuntimeAttributeService runtimeAttributeService,
            IDynamicResolver dynamicResolver,
            IMemoryCache memoryCache,
            IDeviceHeartbeatService deviceHeartbeatService,
            IReadOnlyDeviceRepository readOnlyDeviceRepository,
            IDeviceRepository deviceRepository)
        {
            _configuration = configuration;
            _dbConnectionFactory = dbConnectionFactory;
            _logger = logger;
            _cache = cache;
            _runtimeAttributeService = runtimeAttributeService;
            _dynamicResolver = dynamicResolver;
            _memoryCache = memoryCache;
            _deviceHeartbeatService = deviceHeartbeatService;
            _readOnlyDeviceRepository = readOnlyDeviceRepository;
            _deviceRepository = deviceRepository;
        }

        public async Task<(long, DateTime?, IEnumerable<Guid>)> ProcessEventAsync(IngestionMessage message)
        {
            // var tenantId = metricDict[Constant.MetricPayload.TENANT_ID].ToString();
            // var subscriptionId = metricDict[Constant.MetricPayload.SUBSCRIPTION_ID].ToString();
            var projectId = message.ProjectId;
            //_tenantContext.RetrieveFromString(tenantId, subscriptionId, projectId);
            // var connectionString = _configuration["ConnectionStrings:Default"].BuildConnectionString(_configuration, projectId);
            IEnumerable<Guid> assetIds = new List<Guid>();
            DateTime? deviceUpdateUtc = null;
            long unixTimestamp;
            // using (var dbConnection = new NpgsqlConnection(connectionString))
            // {
            var (deviceId, timestamp, updatedUtc, metrics) = await UpdateDeviceSnapshotAsync(projectId, message.RawData);
            deviceUpdateUtc = updatedUtc ?? DateTime.UtcNow;
            unixTimestamp = timestamp;
            if (!string.IsNullOrEmpty(deviceId))
            {
                var runtimeAssetIds = await _deviceRepository.GetAssetTriggerAsync(projectId, deviceId);
                var runtimeAffectedIds = await _runtimeAttributeService.CalculateRuntimeValueAsync(projectId, deviceUpdateUtc.Value, runtimeAssetIds.Where(ra => metrics.Contains(ra.MetricKey) || ra.MetricKey == null));
                assetIds = await _deviceRepository.GetAssetIdsAsync(projectId, deviceId);
                assetIds = assetIds.Union(runtimeAffectedIds).Distinct();
                //     dbConnection.Close();
                // }
            }
            return (unixTimestamp, deviceUpdateUtc, assetIds);
        }
        private async Task<(string, long, DateTime?, string[])> UpdateDeviceSnapshotAsync(string projectId, IDictionary<string, object> metricDict)
        {
            var activityId = Guid.NewGuid();
            // _logger.LogDebug($"[{activityId}] UpdateDeviceSnapshotAsync - Updating Device's snapshot for project {projectId}...");
            // _logger.LogTrace($"[{activityId}] UpdateDeviceSnapshotAsync - metricDict details: {metricDict.ToJson()}");
            var hashValue = $"{projectId}_processing_device_metric_{string.Join("_", metricDict.Select(x => $"{x.Key}{x.Value}"))}".CalculateMd5Hash();
            var cacheHit = _memoryCache.Get<string>(hashValue);
            if (cacheHit != null)
            {
                // nothing change. no need to update
                // _logger.LogDebug($"[{activityId}] UpdateDeviceSnapshotAsync -Cache hit, no change, system will complete the request!");
                return (null, 0, null, null);
            }
            _memoryCache.Set(hashValue, "cached", TimeSpan.FromHours(2));

            // the device metric key hardly change, implement memory cache should be sufficient
            var deviceIdKeys = await _readOnlyDeviceRepository.GetDeviceMetricKeyAsync(projectId);
            if (!deviceIdKeys.Any())
            {
                // _logger.LogDebug($"[{activityId}] UpdateDeviceSnapshotAsync - No key define!");
                return (null, 0, null, null);
            }
            // the case:
            // multiple deviceId match in database
            // need to loop all the possible deviceId and find the right one
            var deviceIds = (from d in deviceIdKeys
                             join metric in metricDict on d.ToLowerInvariant() equals metric.Key.ToLowerInvariant()
                             select metric.Value.ToString()
                            ).ToArray();
            // foreach (var uniqueKey in deviceIdKeys)
            // {
            //     if (metricDict.ContainsKey(uniqueKey))
            //     {
            //         var posibleDeviceId = metricDict[uniqueKey].ToString();
            //         deviceIds.Add(posibleDeviceId);
            //     }
            // }
            if (deviceIds.Length == 0)
            {
                _logger.LogDebug($"[{activityId}] UpdateDeviceSnapshotAsync - Can not process message because no key is matched, key list [{string.Join(",", deviceIdKeys)}]");
                return (null, 0, null, null);
            }
            // _logger.LogDebug($"[{activityId}] UpdateDeviceSnapshotAsync - Possible DeviceIds {string.Join(",", deviceIds)}");
            //TODO: We will supporting multiple device ingestion later, currently, only processing for the first returned device.
            var deviceInformation = await _readOnlyDeviceRepository.GetDeviceInformationAsync(projectId, deviceIds);
            if (deviceInformation == null || !deviceInformation.Metrics.Any())
            {
                _logger.LogError($"Ingestion_Error Possible DeviceIds {string.Join(",", deviceIds)} not found!. Project: {projectId}!");
                return (null, 0, null, null);
            };
            // tracking the device heart beat
            // consider to move the logic into another queue.
            // _logger.LogDebug($"[{activityId}] UpdateDeviceSnapshotAsync - Device information {deviceInformation.ToJson()}");
            if (deviceIds.Count() > 1)
            {
                // _logger.LogDebug($"[{activityId}] UpdateDeviceSnapshotAsync - Only process 1 device from {deviceIds.Count()} devices from the Request's Message.");
            }

            if (deviceInformation.EnableHealthCheck)
            {
                try
                {
                    // tracking the device heart beat
                    await _deviceHeartbeatService.TrackingHeartbeatAsync(projectId, deviceInformation.DeviceId);
                }
                catch (Exception exc)
                {
                    _logger.LogError("Ingestion_Error {data} - {ex}" , $"[{activityId}] UpdateDeviceSnapshotAsync - TrackingHeartbeatAsync failed - keep processing...", exc);
                }
            }

            var timestampKeys = deviceInformation.Metrics.Where(x => x.MetricType == TemplateKeyTypes.TIMESTAMP).Select(x => x.MetricKey);
            //var timestampKeys = await _deviceRepository.GetTimestampKeyAsync(projectId, deviceId);

            string timestamp = (from ts in timestampKeys
                                join metric in metricDict on ts.ToLowerInvariant() equals metric.Key.ToLowerInvariant()
                                select metric.Value.ToString()
                                ).FirstOrDefault();

            // foreach (var uniqueKey in timestampKeys)
            // {
            //     if (metricDict.ContainsKey(uniqueKey))
            //     {
            //         timestamp = metricDict[uniqueKey].ToString();
            //         break;
            //     }
            // }

            // In case metrics are sent in array timestamp will be null
            if (!long.TryParse(timestamp, out var unixTimestamp) && !string.IsNullOrEmpty(timestamp))
            {
                var deviceTimestamp = timestamp.CutOffFloatingPointPlace().UnixTimeStampToDateTime().CutOffNanoseconds();
                unixTimestamp = (new DateTimeOffset(deviceTimestamp)).ToUnixTimeMilliseconds();
            };
            var metrics = FlattenMetrics(metricDict, deviceInformation, unixTimestamp);
            // var metricValues = metrics.Select(x => new
            // {
            //     MetricKey = x.MetricKey,
            //     Value = x.Value,
            //     Timestamp = x.Timestamp,
            //     UnixTimestamp = x.UnixTimestamp,
            //     DeviceId = deviceInformation.DeviceId
            // });
            var snapshotMetrics = metrics.GroupBy(x => x.MetricKey).Select(x =>
            {
                return x.OrderByDescending(m => m.UnixTimestamp).First();
                //var lastestSnapshot = x.OrderByDescending(m => m.UnixTimestamp).First();
                //return new MetricValue(deviceInformation.DeviceId, x.Key, lastestSnapshot.UnixTimestamp, lastestSnapshot.Timestamp, lastestSnapshot.Value, lastestSnapshot.DataType);
            });

            // var fromDeviceData = (from att in snapshotMetrics
            //                       join dm in deviceInformation.Metrics on att.MetricKey equals dm.MetricKey into gj
            //                       from subMetric in gj.DefaultIfEmpty()
            //                       select new DeviceMetricDataType()
            //                       {
            //                           MetricKey = att.MetricKey,
            //                           Value = att.Value?.ToString(),
            //                           DataType = subMetric?.DataType ?? "text",
            //                           ExpressionCompile = subMetric?.ExpressionCompile,
            //                           MetricType = subMetric?.MetricType
            //                       });

            // var fromDatabaseData = (from dm in deviceInformation.Metrics
            //                         join att in snapshotMetrics on dm.MetricKey equals att.MetricKey
            //                         select dm);

            var fromDeviceMetrics = snapshotMetrics.Select(x => x.MetricKey);
            var values = deviceInformation.Metrics.Where(x => !fromDeviceMetrics.Contains(x.MetricKey)).ToDictionary(x => x.MetricKey, y => ParseValue(y.DataType, y.Value));

            // override from device data
            foreach (var metric in snapshotMetrics)
            {
                values[metric.MetricKey] = ParseValue(metric.DataType, metric.Value);
            }

            var calculatedMetrics = CalculateRuntimeValue(deviceInformation, unixTimestamp, values);
            snapshotMetrics = snapshotMetrics.Union(calculatedMetrics);
            var snapshotValues = metrics.Union(calculatedMetrics);
            // foreach (var snapshotValue in values)
            // {
            //     if (!metricDict.ContainsKey(snapshotValue.Key))
            //     {
            //         snapshotValues.Add(new MetricValue(deviceInformation.DeviceId, snapshotValue.Key, unixTimestamp, DateTime.UtcNow, snapshotValue.Value.ToString()));
            //     }
            // }
            IEnumerable<StoreValue> numericValues;
            IEnumerable<StoreValue> textValues;
            try
            {
                numericValues = snapshotValues.Where(dm => DataTypeExtensions.IsNumericTypeSeries(dm.DataType)).Select(att => new StoreValue { Timestamp = att.Timestamp, DeviceId = att.DeviceId, MetricKey = att.MetricKey, Value = ParseValueToStore(att.DataType, att.Value, true), SignalQualityCode = SIGNAL_QUALITY_CODE_GOOD, RetentionDays = deviceInformation.RetentionDays }).OrderBy(x => x.Timestamp);
                textValues = snapshotValues.Where(dm => DataTypeExtensions.IsTextTypeSeries(dm.DataType)).Select(att => new StoreValue { Timestamp = att.Timestamp , DeviceId = att.DeviceId, MetricKey = att.MetricKey, Value = ParseValueToStore(att.DataType, att.Value, true), SignalQualityCode = SIGNAL_QUALITY_CODE_GOOD, RetentionDays = deviceInformation.RetentionDays }).OrderBy(x => x.Timestamp);
            }
            catch (EntityParseException e)
            {
                _logger.LogError("Ingestion_Error {data} - {ex}" , $"[{activityId}] UpdateDeviceSnapshotAsync - Parsing Snapshot value failed!", e);
                return (null, 0, null, null);
            }
            using (var dbConnection = _dbConnectionFactory.CreateConnection(projectId))
            {
                if (snapshotMetrics.Any())
                {

                    await dbConnection.ExecuteAsync($@"INSERT INTO device_metric_snapshots(_ts, device_id, metric_key, value) 
                                                VALUES(@Timestamp, @DeviceId, @MetricKey, @Value)
                                                ON CONFLICT (device_id, metric_key) 
                                                DO UPDATE SET _ts = EXCLUDED._ts, value = EXCLUDED.value WHERE device_metric_snapshots._ts < EXCLUDED._ts;
                                                ", snapshotMetrics);
                }
                if (numericValues.Any())
                {
                    await dbConnection.ExecuteAsync($@"
                                                INSERT INTO device_metric_series(_ts, device_id, metric_key, value, signal_quality_code, retention_days)
                                                VALUES (@Timestamp, @DeviceId, @MetricKey, @Value,@SignalQualityCode, @RetentionDays );
                                                ", numericValues);
                }

                if (textValues.Any())
                {
                    await dbConnection.ExecuteAsync($@"
                                                INSERT INTO device_metric_series_text(_ts, device_id, metric_key, value, signal_quality_code, retention_days)
                                                VALUES (@Timestamp, @DeviceId, @MetricKey, @Value, @SignalQualityCode, @RetentionDays);
                                                ", textValues);
                }
                await dbConnection.CloseAsync();
            }
            var lastestSnapshot = snapshotMetrics.First();
            // _logger.LogDebug($"[{activityId}] UpdateDeviceSnapshotAsync - Completed!");
            return (deviceInformation.DeviceId, lastestSnapshot.UnixTimestamp, lastestSnapshot.Timestamp, snapshotMetrics.Select(x => x.MetricKey).Distinct().ToArray());
        }

        private IEnumerable<MetricValue> CalculateRuntimeValue(DeviceInformation deviceInformation, long unixTimestamp, IDictionary<string, object> values)
        {
            var calculatedMetrics = new List<MetricValue>();
            foreach (var deviceMetric in deviceInformation.Metrics.Where(x => x.MetricType == TemplateKeyTypes.AGGREGATION))
            {
                try
                {
                    var value = _dynamicResolver.ResolveInstance("return true;", deviceMetric.ExpressionCompile).OnApply(values);
                    var valueAsString = value?.ToString();
                    var val = ParseValue(deviceMetric.DataType, valueAsString);
                    values[deviceMetric.MetricKey] = val;
                    calculatedMetrics.Add(new MetricValue(deviceInformation.DeviceId, deviceMetric.MetricKey, unixTimestamp, valueAsString, deviceMetric.DataType));
                }
                catch (System.Exception exc)
                {
                    _logger.LogError("Ingestion_Error CalculateRuntimeValue {ex}" , exc);
                }
            }
            return calculatedMetrics;
        }
        public static object ParseValueToStore(string dataType, object value, bool throwException = false)
        {
            if (value == null)
            {
                return 0.0;
            }
            switch (dataType)
            {
                case DataTypeConstants.TYPE_DOUBLE:
                    {
                        if (double.TryParse(value.ToString(), out var doubleValue) && !double.IsInfinity(doubleValue))
                        {
                            return doubleValue;
                        }
                        else if (throwException)
                        {
                            throw new EntityParseException($"Cannot parse {value} to {dataType}");
                        }
                        else
                            break;
                    }
                case DataTypeConstants.TYPE_INTEGER:
                    {
                        //need parse to double first cause string like 1.000000000000 cant parse to int
                        if (double.TryParse(value.ToString(), out var intValue) && int.MinValue <= intValue && intValue <= int.MaxValue)
                        {
                            return (int)Math.Round(intValue);
                        }
                        else if (throwException)
                        {
                            throw new EntityParseException($"Cannot parse {value} to {dataType}");
                        }
                        else
                            break;
                    }
                case DataTypeConstants.TYPE_BOOLEAN:
                    {
                        if (bool.TryParse(value.ToString(), out var boolValue))
                        {
                            return boolValue ? 1 : 0;
                        }
                        else if (throwException)
                        {
                            throw new EntityParseException($"Cannot parse {value} to {dataType}");
                        }
                        else
                            break;
                    }
                default:
                    return value.ToString();
            }
            return 0.0;
        }
        public static object ParseValue(string dataType, string val)
        {
            object value = val;
            if (dataType == DataTypeConstants.TYPE_DOUBLE)
            {
                if (double.TryParse(val, out var output))
                {
                    value = output;
                }
                else
                {
                    value = (double)0.0;
                }
            }
            else if (dataType == DataTypeConstants.TYPE_INTEGER)
            {
                // in this case: 29.00121 -> can be failed
                if (Int32.TryParse(val, out var output))
                {
                    value = output;
                }
                // fallback to double and then cast to integer
                else if (double.TryParse(val, out var doubleValue))
                {
                    value = (int)Math.Round(doubleValue);
                }
                else
                {
                    value = 0;
                }
            }
            else if (dataType == DataTypeConstants.TYPE_BOOLEAN)
            {
                if (bool.TryParse(val, out var output))
                {
                    value = output;
                }
                else
                {
                    value = false;
                }
            }
            return value;
        }
        private IEnumerable<MetricValue> FlattenMetrics(IDictionary<string, object> metricDict, DeviceInformation deviceInformation, long? timestamp)
        {
            var metricValues = new List<MetricValue>();
            var deviceId = deviceInformation.DeviceId;
            var metrics = (from metric in metricDict
                           join m in deviceInformation.Metrics on metric.Key.ToLowerInvariant() equals m.MetricKey.ToLowerInvariant()
                           where !RESERVE_KEYS.Contains(metric.Key) && m.MetricType != TemplateKeyTypes.TIMESTAMP
                           select new { Key = m.MetricKey, metric.Value, DataType = m.DataType }
                            );
            foreach (var m in metrics)
            {
                if (m.Value.TryParseJArray<IEnumerable<string[]>>(out var result))
                {
                    foreach (var data in result)
                    {
                        var ts = data[0];
                        var value = data[1];
                        if (long.TryParse(ts, out var unixTimestamp))
                        {
                            var metricValue = new MetricValue(deviceId, m.Key, unixTimestamp, value, m.DataType);
                            metricValues.Add(metricValue);
                        }
                    }
                }
                // signal value must have timestamp
                else if (timestamp > 0)
                {
                    var metricValue = new MetricValue(deviceId, m.Key, timestamp.Value, m.Value.ToString(), m.DataType);
                    metricValues.Add(metricValue);
                }
            }
            return metricValues;
        }

        private class StoreValue
        {
            public DateTime Timestamp { get; set; }
            public string DeviceId { get; set; }
            public string MetricKey { get; set; }
            public object Value { get; set; }
            public int SignalQualityCode { get; set; }
            public int RetentionDays { get; set; }
        }
    }
}
