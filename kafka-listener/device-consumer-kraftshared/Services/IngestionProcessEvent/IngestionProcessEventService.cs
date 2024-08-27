using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.Interceptor.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;
using Confluent.Kafka;
using Dapper;
using Device.Consumer.KraftShared.Constant;
using Device.Consumer.KraftShared.Constants;
using Device.Consumer.KraftShared.Extensions;
using Device.Consumer.KraftShared.Helpers;
using Device.Consumer.KraftShared.Model;
using Device.Consumer.KraftShared.Models.MetricModel;
using Device.Consumer.KraftShared.Models.Options;
using Device.Consumer.KraftShared.Models.QueryModel;
using Device.Consumer.KraftShared.Repositories.Abstraction;
using Device.Consumer.KraftShared.Repositories.Abstraction.ReadOnly;
using Device.Consumer.KraftShared.Service.Abstraction;
using Device.Consumer.KraftShared.Service.Model;
using Device.Consumer.KraftShared.Services.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using NPOI.HSSF.Record;
using NPOI.SS.Formula.Functions;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Device.Consumer.KraftShared.Service
{
    //ProjectId
    //TODO: Enable again once AHI library upgrade to .NET 8
    public sealed class IngestionProcessEventService : IIngestionProcessEventService
    {
        private const int SIGNAL_QUALITY_CODE_GOOD = 192;
        private readonly IRedisDatabase _cache;
        private readonly IReadOnlyAssetRepository _readOnlyAssetRepository;
        private readonly IDeviceRepository _deviceRepository;
        private readonly IFowardingNotificationService _forwardingNotificationService;

        private readonly BatchProcessingOptions _batchOptions;
        private readonly IConfiguration _configuration;
        private readonly IReadOnlyDeviceRepository _readOnlyDeviceRepository;
        private readonly ILogger<IngestionProcessEventService> _logger;
        private readonly IDynamicResolver _dynamicResolver;
        private readonly string[] RESERVE_KEYS = ["tenantId", "subscriptionId", "projectId", "deviceId", "integrationId"];
        private readonly IDbConnectionResolver _dbConnectionResolver;
        private readonly IBackgroundTaskQueue _bgTaskQueue;
        private readonly ParallelOptions _parallelOptions;
        private readonly ConcurrentDictionary<(string, DateTime), IEnumerable<MetricValue>> _snapshotDicts = new();
        private IDictionary<string, IEnumerable<AssetRuntimeTrigger>> _datAssetRuntimeTriggers;
        private readonly GeneralOptions _generalOptions;
        /// <summary>
        /// Key = projectId <br/>
        /// Sub Key = deviceid
        /// </summary>
        private ConcurrentDictionary<string, ConcurrentDictionary<string, DeviceInformation>> _deviceInformations;

        public IngestionProcessEventService(
            IDeviceRepository deviceRepository,
            IReadOnlyDeviceRepository readOnlyDeviceRepository,
            IReadOnlyAssetRepository readOnlyAssetRepository,
            IConfiguration configuration,
            ILogger<IngestionProcessEventService> logger,
            IDynamicResolver dynamicResolver,
            IDbConnectionResolver dbConnectionResolver,
            IRedisDatabase cache,
            IBackgroundTaskQueue bgTaskQueue,
            IOptions<GeneralOptions> generalOptions,
            IFowardingNotificationService forwardingNotificationService
            )
        {
            _configuration = configuration;
            _deviceRepository = deviceRepository;
            _readOnlyDeviceRepository = readOnlyDeviceRepository;
            _readOnlyAssetRepository = readOnlyAssetRepository;
            _logger = logger;
            _dynamicResolver = dynamicResolver;
            _dbConnectionResolver = dbConnectionResolver;
            _forwardingNotificationService = forwardingNotificationService;
            _cache = cache;
            _deviceInformations = new ConcurrentDictionary<string, ConcurrentDictionary<string, DeviceInformation>>();
            var batchOptions = _configuration.GetSection("BatchProcessing").Get<BatchProcessingOptions>();
            if (batchOptions is null)
                _batchOptions = new BatchProcessingOptions() { MaxOpenConnection = 20, UpsertSize = 3000, MaxWorker = 50, AutoCommitInterval = 1000, MaxChunkSize = 200, MaxQueueSize = 2000 };
            else
                _batchOptions = batchOptions;
            _bgTaskQueue = bgTaskQueue;
            _parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = _batchOptions.MaxWorker };
            _generalOptions = generalOptions.Value;
        }

        /// <summary>
        /// ProcessEventAsync handle batch messages
        /// </summary>
        /// <param name="message">list of messages has same TenantId, SubscriptionId, ProjectId</param>
        /// <returns></returns>
        public async Task ProcessEventAsync(IEnumerable<IngestionMessage> messages,
            IEnumerable<DeviceInformation> deviceInfos)
        {
            try
            {
                var projectId = messages.First().ProjectId;
                if (!_deviceInformations.ContainsKey(projectId))
                {
                    _deviceInformations[projectId] = new ConcurrentDictionary<string, DeviceInformation>(deviceInfos.ToDictionary(i => i.DeviceId));
                }

                await HandleBatchAsync(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ingestion_Error Unprocessable HandleBatchAsync {ex.Message}");
            }

        }

        /// <summary>
        /// HandleBatchAsync all messsages in a btach always has same TenantId, SubscriptionId, ProjectId
        /// </summary>
        /// <param name="messages"></param>
        /// <returns></returns>
        public async Task HandleBatchAsync(IEnumerable<IngestionMessage> messages)
        {
            var watchA = Stopwatch.StartNew();
            //all messsages in a btach always has same TenantId, SubscriptionId, ProjectId
            var projectId = messages.First().ProjectId;
            var tenantId = messages.First().TenantId;
            var subscriptionId = messages.First().SubscriptionId;
            var metrictsHasIntegrationId = messages.Where(i => i.RawData.ContainsKey(MetricPayload.INTEGRATION_ID));
            var remainingMetricts = messages.Where(i => !i.RawData.ContainsKey(MetricPayload.INTEGRATION_ID));
                        
            _logger.LogInformation($"#### HandleBatchAsync begin size {messages.Count()}");
            //handle sub batch contains the rest
            if (remainingMetricts.Any())
            {
                _logger.LogInformation($"CalculateDeviceMetricAsync begin size:{remainingMetricts.Count()}");
                //var watch = Stopwatch.StartNew();
                await CalculateDeviceMetricAsync(remainingMetricts);
                //watch.Stop();
                //_logger.LogInformation($"CalculateDeviceMetricAsync finish size {remainingMetricts.Count()} took : {watch.ElapsedMilliseconds} ms");
                _logger.LogInformation($"CalculateDeviceMetricAsync finish size {remainingMetricts.Count()} ms");
            }

            //handle sub batch contains integration_id
            if (metrictsHasIntegrationId.Any())
            {
                _logger.LogInformation($"CalculateRuntimeMetricAsync begin size: {metrictsHasIntegrationId.Count()}");
                //var watch = Stopwatch.StartNew();
                await CalculateRuntimeMetricAsync(metrictsHasIntegrationId, projectId);
                //watch.Stop();
                //_logger.LogInformation($"CalculateRuntimeMetricAsync finish size {metrictsHasIntegrationId.Count()}  took: {watch.ElapsedMilliseconds} ms");
                _logger.LogInformation($"CalculateRuntimeMetricAsync finish size {metrictsHasIntegrationId.Count()}  ms");
            }
            if(!_batchOptions.EnabledPullAssetTriggerOnce)
            {
                var deviceIds = _snapshotDicts.Keys.Select(i => i.Item1).ToList();
                _logger.LogInformation("*** Begin HandleBatchAsync GetAssetRuntimeTriggersAsync from deviceRepository *** ");
                _datAssetRuntimeTriggers = await _readOnlyDeviceRepository.GetProjectAssetRuntimeTriggersAsync(projectId, deviceIds);
                _logger.LogInformation("*** Completed HandleBatchAsync GetAssetRuntimeTriggersAsync from deviceRepository *** ");

            } else {
                var assetAttributeRelevantToDeviceIdKey = string.Format(IngestionRedisCacheKeys.AssetRuntimeTriggerPattern, projectId);

                _datAssetRuntimeTriggers = await _cache.HashGetAllAsync<IEnumerable<AssetRuntimeTrigger>>(assetAttributeRelevantToDeviceIdKey);
            }

            _logger.LogInformation($"CalculateRuntimeAttributeAsync start size: {messages.Count()}");
            var watch2 = Stopwatch.StartNew();
            await CalculateRuntimeAttributeAsync(tenantId, subscriptionId, projectId, messages);
            watch2.Stop();
            _logger.LogInformation($"CalculateRuntimeAttributeAsync finish size: {messages.Count()}  took {watch2.ElapsedMilliseconds} ms");

            watchA.Stop();
            _logger.LogInformation($"#### HandleBatchAsync finish size {messages.Count()} took: {watchA.ElapsedMilliseconds} ms");
        }

        /// <summary>
        /// GetListDeviceInformationAsync return list device's info and it's metrict messages
        /// single messages could return multiple device's info
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="projectId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<DeviceInformation>> GetListDeviceInformationAsync(IEnumerable<IngestionMessage> messages, string projectId)
        {
            var result = new List<DeviceInformation>();
            var deviceIds = new List<string>();
            //assign message to accordingly type
            foreach (var message in messages)
            {
                if (message.RawData.ContainsKey(MetricPayload.DEVICE_ID) && !string.IsNullOrEmpty(message.RawData[MetricPayload.DEVICE_ID]?.ToString()))
                {
                    deviceIds.Add(message.RawData[MetricPayload.DEVICE_ID].ToString());
                }
                else if (message.RawData.ContainsKey(MetricPayload.TOPIC_NAME) && !string.IsNullOrEmpty(message.RawData[MetricPayload.TOPIC_NAME]?.ToString()))
                {
                    var deviceInformations = await GetDeviceInformationsWithTopicNameAsync(message);
                    if (deviceInformations is not null && deviceInformations.Any())
                        result.AddRange(deviceInformations);
                }
                else
                {
                    var deviceInformation = await GetDeviceInformationFromMetricKeyAsync(projectId, message);
                    if (deviceInformation is not null)
                        result.Add(deviceInformation);
                }
            }

            //query 1 times
            var devicesInfos = await GetMultipleDeviceInformationAsync(projectId, deviceIds);
            if (devicesInfos is not null && devicesInfos.Any())
                result.AddRange(devicesInfos);
            return result;
        }


        private async Task<IEnumerable<DeviceInformation>> GetDeviceInformationByIngestionMessageAsync(IngestionMessage message, string projectId)
        {
            var result = new List<DeviceInformation>();

            if (message.RawData.ContainsKey(MetricPayload.DEVICE_ID) && !string.IsNullOrEmpty(message.RawData[MetricPayload.DEVICE_ID]?.ToString()))
            {
                var deviceInfo = await GetDeviceInformationAsync(projectId, message.RawData[MetricPayload.DEVICE_ID].ToString());
                if (deviceInfo != null)
                    result.Add(deviceInfo);
            }
            else if (message.RawData.ContainsKey(MetricPayload.TOPIC_NAME) && !string.IsNullOrEmpty(message.RawData[MetricPayload.TOPIC_NAME]?.ToString()))
            {
                var deviceInformations = await GetDeviceInformationsWithTopicNameAsync(message);
                if (deviceInformations is not null && deviceInformations.Any())
                    result.AddRange(deviceInformations);
            }
            else
            {
                var deviceInformation = await GetDeviceInformationFromMetricKeyAsync(projectId, message);
                if (deviceInformation is not null)
                    result.Add(deviceInformation);
            }

            return result;
        }

        /// <summary>
        /// GetListDeviceInformationAsync return list device's info and it's metrict messages
        /// single messages could return multiple device's info
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="projectId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<DeviceInformation>> GetListDeviceInformationAsync(IngestionMessage message, string projectId)
        {
            return await GetDeviceInformationByIngestionMessageAsync(message, projectId);
        }

        private async Task<DeviceInformation?> GetDeviceInformationFromMetricKeyAsync(string projectId, IngestionMessage message)
        {
            if (string.IsNullOrWhiteSpace(projectId) || !Guid.TryParse(projectId, out Guid projectIdGuid))
            {
                _logger.LogInformation("ProjectId is invalid.");
                return null;
            }
            // the device metric key hardly change, implement memory cache should be sufficient
            var deviceIdKeys = await _readOnlyDeviceRepository.GetDeviceMetricKeyAsync(projectId);
            if (!deviceIdKeys.Any())
            {
                _logger.LogInformation("No key define");
                return null;
            }


            //// the case:
            //// multiple deviceId match in database
            //// need to loop all the possible deviceId and find the right one
            var deviceIds = (from d in deviceIdKeys
                             join metric in message.RawData on d.ToLowerInvariant() equals metric.Key.ToLowerInvariant()
                             select metric.Value.ToString()
                            );
            if (deviceIds.Count() == 0)
            {
                _logger.LogInformation($"Can not process message because no key is matched, key list [{string.Join(",", deviceIdKeys)}]");
                return null;
            }
            _logger.LogInformation($"Possible DeviceIds {string.Join(",", deviceIds)}");

            //Q: Is there anycase to falling this type of messages.
            var deviceInformations = await GetMultipleDeviceInformationAsync(projectId, deviceIds);
            return deviceInformations.FirstOrDefault();
        }


        private async Task<IEnumerable<DeviceInformation>> GetDeviceInformationsWithTopicNameAsync(IngestionMessage message)
        {
            var projectId = message.ProjectId;
            var topicName = message.TopicName;
            var brokerType = message.RawData[MetricPayload.BROKER_TYPE]?.ToString() ?? string.Empty;
            IEnumerable<DeviceInformation> deviceInformation = await _readOnlyDeviceRepository.GetDeviceInformationsWithTopicNameAsync(projectId, topicName, brokerType);

            return deviceInformation;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="deviceInformation"></param>
        /// <returns></returns>
        private IEnumerable<MetricValue> GetTotalSnapshotMetrics(IngestionMessage message, DeviceInformation deviceInformation)
        {
            if (deviceInformation == null || !deviceInformation.Metrics.Any())
            {
                _logger.LogError("Ingestion_Error GetTotalSnapshotMetrics device no infos msg: {message}", message);
                return Enumerable.Empty<MetricValue>();
            }
            var timestampKeys = deviceInformation.Metrics
                                    .Where(x => x.MetricType == TemplateKeyTypes.TIMESTAMP)
                                    .Select(x => x.MetricKey.ToLowerInvariant())
                                    .FirstOrDefault();
            var unixTimestamp = GetUnixTimestamp(timestampKeys, message);

            var metrics = FlattenMetrics(message, deviceInformation, unixTimestamp, timestampKeys);

            if (metrics != null)
            {

                var projectId = message.ProjectId;
                var calculatedMetrics = GetCalculateMetrics(metrics, deviceInformation, unixTimestamp, projectId);

                metrics = metrics.Union(calculatedMetrics);
            }

            return metrics;
        }

        private static long GetUnixTimestamp(string timestampKeys, IngestionMessage message)
        {
            message.RawData.TryGetValue(timestampKeys, out var objTimestamp);
            string timestamp = objTimestamp.ToString();
            if (!long.TryParse(timestamp, out var unixTimestamp) && !string.IsNullOrEmpty(timestamp))
            {
                var deviceTimestamp = timestamp.CutOffFloatingPointPlace().UnixTimeStampToDateTime().CutOffNanoseconds();
                unixTimestamp = new DateTimeOffset(deviceTimestamp).ToUnixTimeMilliseconds();
            }

            return unixTimestamp;
        }

        private IDbConnection GetReadOnlyDbConnection(string projectId) => _dbConnectionResolver.CreateConnection(projectId, true);

        private IEnumerable<MetricValue> FlattenMetrics(IngestionMessage message, DeviceInformation deviceInformation, long? timestamp, string timestampKeys)
        {
            if (!timestamp.HasValue || timestamp.Value < 0)
            {
                _logger.LogError("Ingestion_Error FlattenMetrics DeviceId: {deviceId} - no timestamp", deviceInformation.DeviceId);
                return [];
            }
            
            var deviceMetric = deviceInformation.Metrics.ToDictionary(x => x.MetricKey.ToLowerInvariant());
            var deviceId = deviceInformation.DeviceId;
            return message.RawData.Where(
                x => x.Value != null 
                && !RESERVE_KEYS.Contains(x.Key)
                && !x.Key.Equals(timestampKeys, StringComparison.InvariantCultureIgnoreCase)
                && deviceMetric.ContainsKey(x.Key.ToLowerInvariant())
            ).Select(x=> new MetricValue
            (
                deviceId, 
                deviceMetric[x.Key.ToLowerInvariant()].MetricKey,
                timestamp.Value,
                x.Value?.ToString() ?? string.Empty,
                deviceMetric[x.Key.ToLowerInvariant()].DataType
            ));
        }

        private static object ParseValue(string dataType, string val)
        {
            object value = val;
            switch (dataType)
            {
                case DataTypeConstants.TYPE_BOOLEAN:
                    value = bool.TryParse(val, out var output) && output;
                    break;
                case DataTypeConstants.TYPE_DOUBLE:
                    value = double.TryParse(val, out var output2) ? output2 : (double)0.0;
                    if (double.TryParse(value.ToString(), out var doubleValue))
                        return doubleValue;
                    break;
                case DataTypeConstants.TYPE_INTEGER:
                    value = Int32.TryParse(val, out var output3) ? output3 : double.TryParse(val, out var doubleOutput) ? (int)Math.Round(doubleOutput) : 0;
                    if (double.TryParse(value.ToString(), out var intValue))
                        return (int)Math.Round(intValue);
                    break;
                default:
                    return value?.ToString() ?? string.Empty;
            }


            return value;
        }

        private static object ParseValueToStore(string dataType, object value)
        {
            if (value == null)
                return 0.0;

            switch (dataType)
            {
                case DataTypeConstants.TYPE_BOOLEAN:
                    if (bool.TryParse(value.ToString(), out var boolValue))
                        return boolValue ? 1 : 0;
                    break;
                case DataTypeConstants.TYPE_DOUBLE:

                    if (double.TryParse(value.ToString(), out var doubleValue))
                        return doubleValue;
                    break;
                case DataTypeConstants.TYPE_INTEGER:
                    if (double.TryParse(value.ToString(), out var intValue))
                        return (int)Math.Round(intValue);
                    break;
                default:
                    return value.ToString();
            }

            //nothing comeout.
            return 0.0;

        }

        private IEnumerable<MetricValue> GetCalculateMetrics(IEnumerable<MetricValue> snapshotMetrics, DeviceInformation deviceInformation, long unixTimestamp, string projectId)
        {

            if (snapshotMetrics == null || !snapshotMetrics.Any())
                return [];
            // need to check aggregation
            if (deviceInformation.Metrics.Any(x => x.MetricType == TemplateKeyTypes.AGGREGATION))
            {
                var fromDeviceMetrics = snapshotMetrics.Select(x => x.MetricKey);
                var values = deviceInformation.Metrics
                                                .Where(x => !fromDeviceMetrics.Contains(x.MetricKey))
                                                .ToDictionary(x => x.MetricKey, y => ParseValue(y.DataType, y.Value));

                // override from device data
                foreach (var metric in snapshotMetrics)
                    values[metric.MetricKey] = ParseValue(metric.DataType, metric.Value);

                return CalculateRuntimeValue(deviceInformation, unixTimestamp, values);
            }

            return [];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="deviceInformation"></param>
        /// <param name="unixTimestamp"></param>
        /// <param name="values"></param>
        /// <returns></returns>
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
                catch (Exception exc)
                {
                    _logger.LogError("Ingestion_Error CalculateRuntimeValue deviceInformation: {deviceInformation} - ex: {ex}", deviceInformation, exc);
                }
            }
            return calculatedMetrics.AsEnumerable();
        }

        public async Task CalculateDeviceMetricAsync(IEnumerable<IngestionMessage> ingestionMessages)
        {
            var finalSnapshotMetrics = new MetricList<MetricValue>();
            var finalNumbericValues = new MetricList<DeviceMetricSeries>();
            var finalTextValues = new MetricList<DeviceMetricSeries>();
            var projectId = ingestionMessages.FirstOrDefault()?.ProjectId ?? string.Empty;
            var tenantId = ingestionMessages.FirstOrDefault()?.TenantId ?? string.Empty;
            var subscriptionId = ingestionMessages.FirstOrDefault()?.SubscriptionId ?? string.Empty;
            if (string.IsNullOrEmpty(projectId))
            {
                _logger.LogError("Ingestion_Error Invalid ingestion messages, missing projectID", ingestionMessages.FirstOrDefault());             
            }

            _logger.LogInformation($"Caching CalculateDeviceMetricAsync begin transforming data size: {ingestionMessages.Count()}");
            var watch = Stopwatch.StartNew();
            //foreach(var message in ingestionMessages)
            //    await TransformMetric(message, projectId, finalSnapshotMetrics, finalNumbericValues, finalTextValues);
            var batchMetrics = ingestionMessages.Chunk(_batchOptions.MaxTransformChunkSize);
            foreach (var optimizeMetrics in batchMetrics)
            {
                var taskList = optimizeMetrics.Select(metric => TransformMetric(metric, projectId, finalSnapshotMetrics, finalNumbericValues, finalTextValues));
                await Task.WhenAll(taskList);
            }

            watch.Stop();
            _logger.LogInformation($"Caching CalculateDeviceMetricAsync finish metricList size: {ingestionMessages.Count()} took: {watch.ElapsedMilliseconds} ms");

            try
            {
                StoreRedisDeviceMetricsSnapshots(tenantId, subscriptionId, projectId, finalSnapshotMetrics.AsEnumerable()); // fire and forgot
                await Task.WhenAll(
                    BulkInsertDeviceMetricsSeriesTextAsync(projectId, finalTextValues.AsEnumerable()),
                    BulkInsertDeviceMetricsSeriesAsync(projectId, finalNumbericValues.AsEnumerable())
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ingestion_Error CalculateDeviceMetricAsync - ex: {ex.Message}");
            }
        }

        private async Task TransformMetric(
            IngestionMessage message,
            string projectId,
            MetricList<MetricValue> finalSnapshotMetrics,
            MetricList<DeviceMetricSeries> finalNumbericValues,
            MetricList<DeviceMetricSeries> finalTextValues)
        {
            var deviceId = message.RawData[MetricPayload.DEVICE_ID].ToString();
            if (!string.IsNullOrEmpty(deviceId))
            {
                _deviceInformations[projectId].TryGetValue(deviceId, out DeviceInformation device);
                device ??= await GetDeviceInformationAsync(projectId, deviceId);
                try
                {
                    var snapshotValues = GetTotalSnapshotMetrics(message, device);
                    if (snapshotValues == null || !snapshotValues.Any())
                        return;
                    finalSnapshotMetrics.Add(snapshotValues);
                    finalNumbericValues.Add(snapshotValues.Where(
                        dm => DataTypeConstants.NUMBERIC_TYPES.Contains(dm.DataType))
                        .Select(att => new DeviceMetricSeries
                        {
                            Timestamp = att.Timestamp,
                            DeviceId = att.DeviceId,
                            MetricKey = att.MetricKey,
                            Value = ParseValueToStore(att.DataType, att.Value),
                            SignalQualityCode = SIGNAL_QUALITY_CODE_GOOD,
                            RetentionDays = device.RetentionDays
                        }));
                    finalTextValues.Add(snapshotValues.Where(
                        dm => DataTypeConstants.TEXT_TYPES.Contains(dm.DataType))
                        .Select(att => new DeviceMetricSeries
                        {
                            Timestamp = att.Timestamp,
                            DeviceId = att.DeviceId,
                            MetricKey = att.MetricKey,
                            Value = ParseValueToStore(att.DataType, att.Value),
                            SignalQualityCode = SIGNAL_QUALITY_CODE_GOOD,
                            RetentionDays = device.RetentionDays
                        }));
                    // add snapshot value for next step   
                    var latestSnapshot = snapshotValues.First();
                    _snapshotDicts.TryAdd((deviceId, latestSnapshot.Timestamp), snapshotValues);

                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Ingestion_Error TransformMetric failure: metric={metric}", message.ToJson());
                }
            }
        }
        #region Private Methods

        private async Task<DeviceInformation> GetDeviceInformationAsync(string projectId, string deviceId)
        {
            //step 1. get from currentInstance to avoid memory racing
            if (_deviceInformations[projectId].ContainsKey(deviceId))
                return _deviceInformations[projectId][deviceId];

            //step 2. get from redis
            var deviceMetricKey = $"{deviceId}";
            var hashKey = string.Format(IngestionRedisCacheKeys.DeviceInfoPattern, projectId);
            var deviceInformation = await _cache.HashGetAsync<DeviceInformation>(hashKey, deviceMetricKey);
            if (deviceInformation != null)
            {
                _deviceInformations[projectId][deviceId] = deviceInformation;
                return deviceInformation;
            }
            using (var dbConnection = _dbConnectionResolver.CreateConnection(projectId))
            {
                var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);
                using var reader = await retryStrategy.ExecuteAsync(async () =>
                    await dbConnection.QueryMultipleAsync(
                    @" select id as DeviceId, 
                                   retention_days as RetentionDays,
                                   enable_health_check as EnableHealthCheck
                            from devices 
                            where id = @DeviceId;
                                           
                            select metric_key as MetricKey, 
                                   expression_compile as ExpressionCompile,
                                   data_type as DataType, 
                                   value as Value, 
                                   metric_type as MetricType
                            from v_device_metrics
                            where device_id = @DeviceId;", new { DeviceId = deviceId }, commandTimeout: 600)
                );
                deviceInformation = await reader.ReadFirstOrDefaultAsync<DeviceInformation>();
                if (deviceInformation != null)
                    deviceInformation.Metrics = await reader.ReadAsync<DeviceMetricDataType>();

                await _cache.HashSetAsync<DeviceInformation>(hashKey, deviceMetricKey, deviceInformation);
                _deviceInformations[projectId][deviceId] = deviceInformation;
                dbConnection.Close();
            }

            return deviceInformation;
        }

        /// <summary>
        /// GetMultipleDeviceInformationAsync Get From Current Context
        /// else if not have, get from memoryCache
        /// else if not have, get from redisCache
        /// else if not have, get from database, then set to redis, memoryCache, and instance cache
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="deviceIds"></param>
        /// <returns></returns>
        private async Task<IEnumerable<DeviceInformation>> GetMultipleDeviceInformationAsync(string projectId, IEnumerable<string> deviceIds)
        {
            var deviceInfos = new List<DeviceInformation>();
            var notfoundInCacheIds = new List<string>();
            var hashKey = string.Format(IngestionRedisCacheKeys.DeviceInfoPattern, projectId);

            //step 1. Get From Cache, if not exist, add to waiting list to query from database.
            foreach (var id in deviceIds)
            {
                var deviceMetricKey = $"{id}";

                //try memcache
                if (_deviceInformations[projectId].ContainsKey(id))
                {
                    deviceInfos.Add(_deviceInformations[projectId][id]);
                    continue;
                }

                //try rediscache
                var info = await _cache.HashGetAsync<DeviceInformation>(hashKey, deviceMetricKey);
                if (info != null)
                {
                    _deviceInformations[projectId][id] = info;
                    deviceInfos.Add(info);
                    continue;
                }

                //append to notfound
                notfoundInCacheIds.Add(id);
            }

            if (!notfoundInCacheIds.Any())
                return deviceInfos;

            //step 2. Get from database
            using (var dbConnection = _dbConnectionResolver.CreateConnection(projectId))
            {

                var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);
                using var reader = await retryStrategy.ExecuteAsync(async () =>
                    await dbConnection.QueryMultipleAsync(
                    @" select id as DeviceId, 
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
                    commandTimeout: 1000)
                );

                var devicesFromDb = await reader.ReadAsync<DeviceInformation>();

                //if no new data from db, then return current result
                if (!(devicesFromDb != null && devicesFromDb.Any()))
                    return deviceInfos;

                //Q: Should We  use EagerLoading instead of foreach Reading?
                foreach (var dInfoDb in devicesFromDb)
                    dInfoDb.Metrics = await reader.ReadAsync<DeviceMetricDataType>();

                //Close dbConnection to avoid exhausted pool.
                dbConnection.Close();


                //step 3. append to redis, then to memorycache
                foreach (var dInfoDb in devicesFromDb)
                {
                    var deviceMetricKey = $"{dInfoDb.DeviceId}";
                    await _cache.HashSetAsync<DeviceInformation>(hashKey, deviceMetricKey, dInfoDb);
                    _deviceInformations[projectId][dInfoDb.DeviceId] = dInfoDb;
                    deviceInfos.Add(dInfoDb);
                }

            }
            return deviceInfos;
        }

        private void StoreRedisDeviceMetricsSnapshots(string tenantId, string subscriptionId,  string projectId, IEnumerable<MetricValue> snapshotMetricsInput)
        {
            if (_batchOptions.EnabledBackgroundSnapshotSync)
            {
                try
                {
                    _logger.LogInformation($"StoreRedisDeviceMetricsSnapshotsAsync2  projectId {projectId} enters total: {snapshotMetricsInput.Count()}");
                    _bgTaskQueue.ExecuteStoreDeviceSnapshotBackgroundAsync(tenantId, subscriptionId, projectId, snapshotMetricsInput);
                    _logger.LogInformation($"#### StoreRedisDeviceMetricsSnapshotsAsync2  finish. projectId {projectId} size: {snapshotMetricsInput.Count()} ");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ingestion_Error StoreRedisDeviceMetricsSnapshotsAsync2   projectId: {projectId} -- error: {ex}", projectId, ex);
                }
            }
        }
        private async Task BulkInsertDeviceMetricsSeriesAsync(string projectId, IEnumerable<DeviceMetricSeries> numericValues)
        {
            if (!numericValues.Any())
                return;
            var watch = Stopwatch.StartNew();
            using (var dbConnection = _dbConnectionResolver.CreateConnection(projectId))
            {
                if (dbConnection.State != ConnectionState.Open)
                    await dbConnection.OpenAsync();

                var chunks = numericValues.Chunk(_batchOptions.InsertSize);
                foreach (var chunk in chunks)
                {
                    Func<NpgsqlBinaryImporter, Task> writeToSTDIN = async (NpgsqlBinaryImporter writer) =>
                    {
                        foreach (var record in chunk)
                        {
                            await writer.StartRowAsync();
                            await writer.WriteAsync(record.DeviceId, NpgsqlTypes.NpgsqlDbType.Varchar);
                            await writer.WriteAsync(record.MetricKey, NpgsqlTypes.NpgsqlDbType.Varchar);
                            await writer.WriteAsync(double.Parse(record.Value.ToString()), NpgsqlTypes.NpgsqlDbType.Double);
                            await writer.WriteAsync(record.Timestamp, NpgsqlTypes.NpgsqlDbType.Timestamp);
                            await writer.WriteAsync(record.RetentionDays, NpgsqlTypes.NpgsqlDbType.Integer);
                            await writer.WriteAsync(record.SignalQualityCode, NpgsqlTypes.NpgsqlDbType.Smallint);
                        }
                    };

                    (var affected, var err) = await dbConnection.BulkInsertWithWriterAsync<DeviceMetricSeries>(
                     "device_metric_series(device_id, metric_key, value, _ts, retention_days, signal_quality_code)",
                     writeToSTDIN, _logger);
                    if (!string.IsNullOrEmpty(err))
                        _logger.LogError("Ingestion_Error {err}", err);
                    else
                        _logger.LogInformation($"BulkInsertDeviceMetricsSeriesAsync projectId: {projectId}, {affected}/{numericValues.Count()} numericValues records has been upserted successfully");
                }

            }
            watch.Stop();
            _logger.LogInformation($"#### BulkInsertDeviceMetricsSeriesAsync finish. took: {watch.ElapsedMilliseconds} ms");
            //_logger.LogInformation($"#### BulkInsertDeviceMetricsSeriesAsync finish.");
        }

        private async Task BulkInsertDeviceMetricsSeriesTextAsync(string projectId, IEnumerable<DeviceMetricSeries> textValues)
        {
            if (!textValues.Any())
                return;
            var watch = Stopwatch.StartNew();
            using (var dbConnection = _dbConnectionResolver.CreateConnection(projectId))
            {
                if (dbConnection.State != ConnectionState.Open)
                    dbConnection.Open();

                var chunks = textValues.Chunk(_batchOptions.InsertSize);
                foreach (var chunk in chunks)
                {
                    Func<NpgsqlBinaryImporter, Task> writeToSTDIN = async (NpgsqlBinaryImporter writer) =>
                    {
                        foreach (var record in chunk)
                        {
                            await writer.StartRowAsync();
                            await writer.WriteAsync(record.DeviceId, NpgsqlTypes.NpgsqlDbType.Varchar);
                            await writer.WriteAsync(record.MetricKey, NpgsqlTypes.NpgsqlDbType.Varchar);
                            await writer.WriteAsync(record.Value, NpgsqlTypes.NpgsqlDbType.Text);
                            await writer.WriteAsync(record.Timestamp, NpgsqlTypes.NpgsqlDbType.Timestamp);
                            await writer.WriteAsync(record.RetentionDays, NpgsqlTypes.NpgsqlDbType.Integer);
                            await writer.WriteAsync(record.SignalQualityCode, NpgsqlTypes.NpgsqlDbType.Smallint);
                        }
                    };

                    (var affected, var err) = await dbConnection.BulkInsertWithWriterAsync<DeviceMetricSeries>(
                        "device_metric_series_text(device_id, metric_key, value, _ts, retention_days, signal_quality_code)",
                        writeToSTDIN, _logger);
                    if (!string.IsNullOrEmpty(err))
                        _logger.LogError("Ingestion_Error {err}", err);
                    else
                        _logger.LogInformation($"BulkInsertDeviceMetricsSeriesTextAsync projectId: {projectId}, {affected}/{textValues.Count()} textValues records has been inserted successfully");
                }
            }
            watch.Stop();
            _logger.LogInformation($"#### BulkInsertDeviceMetricsSeriesTextAsync finish. {textValues.Count()} records took: {watch.ElapsedMilliseconds} ms");
            //_logger.LogInformation($"#### BulkInsertDeviceMetricsSeriesTextAsync finish. {textValues.Count()} records");
        }

        #endregion

        #region Attribute Runtimes

        /// <summary>
        /// CalculateRuntimeAttributeAsync always received batch message has same TenantId, SubscriptionId, ProjectId
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task CalculateRuntimeAttributeAsync(string tenantId, string subscriptionId, string projectId, IEnumerable<IngestionMessage> ingesMessages)
        {
            if (_datAssetRuntimeTriggers == null || !_datAssetRuntimeTriggers.Values.SelectMany(c => c).Any())
            {
                _logger.LogInformation($"CalculateRuntimeAttributeAsync: No Any Asset Attribute Trigger, Trigger for related assets");
                if (_generalOptions.EnabledForwarding && _snapshotDicts.Keys.Count > 0)
                {
                    var tasks = _snapshotDicts.Select(data => _forwardingNotificationService.ForwardingNotificationAssetMessageAsync(tenantId, subscriptionId, projectId, data.Key.Item1, data.Value.Last().UnixTimestamp));
                    await Task.WhenAll(tasks);
                }
            }
            else
            {
                _logger.LogInformation($"CalculateRuntimeAttributeAsync: begin size {ingesMessages.Count()}");
                //var watch = Stopwatch.StartNew();
                var watch2 = Stopwatch.StartNew();
                var runtimeData = await GetAllDataAssetRuntime(ingesMessages, projectId);
                watch2.Stop();
                _logger.LogInformation($"CalculateRuntimeAttributeAsync GetAllDataAssetRuntime {ingesMessages.Count()} messages tooks {watch2.ElapsedMilliseconds} ms");

                //for alias, key = deviceId
                var batchAssetAttributes = runtimeData.SelectMany(i => i.AssetAttributes).DistinctBy(i => i.Key).ToDictionary(pair => pair.Key, pair => pair.Value);
                //for alias, key = deviceId
                var batchAssetRuntimeTriggers = runtimeData.SelectMany(i => i.AssetRuntimeTriggers).DistinctBy(i => i.Key).ToDictionary(pair => pair.Key, pair => pair.Value);

                var runtimeValues = runtimeData.SelectMany(i => i.AssetRuntimeValues.SelectMany(c => c.Value));
                try
                {
                    var listDeviceIds = ingesMessages.Where(i => i.RawData.ContainsKey(MetricPayload.DEVICE_ID)).Select(i => i.RawData[MetricPayload.DEVICE_ID].ToString()).Distinct();
                    StoreRedisSnapshotAttributes(tenantId, subscriptionId, projectId, runtimeValues); // fire and forgot
                    await Task.WhenAll(
                        StoreSnapshotNumbericsAsync(projectId, runtimeValues),
                        StoreSnapshotTextsAsync(projectId, runtimeValues));
                    if (_generalOptions.EnabledForwarding && runtimeData.Length > 0)
                    {
                        var tasks = runtimeData.Select(data => _forwardingNotificationService.ForwardingNotificationAssetMessageAsync(data.MetricDict, data.DeviceInformations, data.DataUnixTimestamps, data.AssetRuntimeValues));
                        await Task.WhenAll(tasks);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Ingestion_Error CalculateRuntimeAttributeAsync {ex.Message}", ex);
                }
                //watch.Stop();
                //_logger.LogInformation($"## CalculateRuntimeAttributeAsync finish {ingesMessages.Count()} messages, took: {watch.ElapsedMilliseconds} ms");
                _logger.LogInformation($"## CalculateRuntimeAttributeAsync finish {ingesMessages.Count()} messages");
            }
        }

        /// <summary>
        /// StoreAliasKeyToRedisAsync handle single IngestionMessages
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="listDeviceInformation"></param>
        /// <param name="dataAssetAttributes"></param>
        /// <param name="dataAssetRuntimeTriggers"></param>
        /// <returns></returns>
        private async Task StoreAliasKeyToRedisAsync(string projectId,
            IEnumerable<string> listDeviceId,
            Dictionary<string, IEnumerable<AssetAttribute>> dataAssetAttributes,
            Dictionary<string, IEnumerable<AssetRuntimeTrigger>> dataAssetRuntimeTriggers)
        {
            var watch = Stopwatch.StartNew();
            var dbConnection = GetReadOnlyDbConnection(projectId);
            foreach (var deviceId in listDeviceId)
            {

                if (!dataAssetRuntimeTriggers.ContainsKey(deviceId))
                {
                    //_logger.LogWarning($"Ingestion_Error assetRuntimeTriggers is empty. Device: {deviceId}");
                    continue;
                }

                var assetRuntimeTriggers = dataAssetRuntimeTriggers[deviceId];
                var assetRuntimeTriggerIds = assetRuntimeTriggers.Select(x => x.AssetId).ToList();
                var assetAttributes = dataAssetAttributes[deviceId];
                var aliasAttributes = assetAttributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_ALIAS);
                //var assetAttributeKey = $"{projectId}_processing_assets_ids_{string.Join(',', assetIds)}_attributes_trigger";
                //var cacheHit = await _cache.GetAsync<string>(redisAssetAttributeKey);
                var redisKey = string.Format(IngestionRedisCacheKeys.AliasAttributeMappingPattern, projectId);
                var needToStoreRedis = new List<Guid> ();
                foreach (var alias in aliasAttributes)
                {
                    try
                    {
                        var targetAttributeId = await _cache.HashGetAsync<Guid>(redisKey, alias.AttributeId.ToString());
                        if (targetAttributeId != Guid.Empty)
                        {
                            //already exist no changes, following old logic
                            continue;
                        }
                        needToStoreRedis.Add(alias.AttributeId);

                    }
                    catch (System.Exception exc)
                    {
                        // await _cache.AddAsync<string>(aliasFailedKey, exc.Message, expiresIn: TimeSpan.FromDays(1));
                        _logger.LogError(exc, $"Ingestion_Error StoreAliasKeyToRedisAsync {exc.Message}");
                    }
                }

                foreach (var aliasId in needToStoreRedis)
                {
                    var targetAliasAttributeId = await dbConnection.QuerySingleOrDefaultAsync<Guid>("select attribute_id from find_root_alias_asset_attribute(@AliasAttributeId) order by alias_level desc limit 1", new { AliasAttributeId = aliasId }, commandTimeout: 10);
                    if(targetAliasAttributeId == Guid.Empty)
                    {
                        _logger.LogError("Ingestion_Error projectId: {projectId} - deviceId: {deviceId} - AliasId: {aliasId} not found", projectId, deviceId, aliasId);
                        continue;
                    }
                    await _cache.HashSetAsync<Guid>(redisKey, aliasId.ToString(), targetAliasAttributeId);
                }
                
            }
            watch.Stop();
            _logger.LogInformation($"#### StoreAliasKeyToRedisAsync finish for {listDeviceId.Count()} devices, took: {watch.ElapsedMilliseconds} ms");
        }


        private void StoreRedisSnapshotAttributes(string tenantId, string subscriptionId, string projectId, IEnumerable<RuntimeValueObject> runtimeValues)
        {
            try
            {
                _bgTaskQueue.ExecuteStoreAttributeRuntimeSnapshotBackgroundAsync(tenantId, subscriptionId, projectId, runtimeValues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ingestion_Error StoreRedisSnapshotAttributesAsync {ex.Message}");
            }
        }

        private async Task StoreSnapshotNumbericsAsync(string projectId, IEnumerable<RuntimeValueObject> runtimeValues)
        {
            try
            {
                // numberic value
                var numbericValues = runtimeValues.Where(x => DataTypeConstants.NUMBERIC_TYPES.Contains(x.DataType));

                if (!numbericValues.Any())
                    return;
                //var watch = Stopwatch.StartNew();

                using (var dbConnection = _dbConnectionResolver.CreateConnection(projectId))
                {
                    if (dbConnection.State != ConnectionState.Open)
                        dbConnection.Open();

                    var chunks = numbericValues.Chunk(_batchOptions.InsertSize);
                    foreach (var chunk in chunks)
                    {
                        Func<NpgsqlBinaryImporter, Task> writeToSTDIN = async (NpgsqlBinaryImporter writer) =>
                        {
                            foreach (var record in chunk)
                            {
                                await writer.StartRowAsync();
                                await writer.WriteAsync(record.Timestamp, NpgsqlTypes.NpgsqlDbType.Timestamp);
                                await writer.WriteAsync(record.AssetId, NpgsqlTypes.NpgsqlDbType.Uuid);
                                await writer.WriteAsync(record.AttributeId, NpgsqlTypes.NpgsqlDbType.Uuid);
                                await writer.WriteAsync(record.Value, NpgsqlTypes.NpgsqlDbType.Double);
                                await writer.WriteAsync(record.RetentionDays, NpgsqlTypes.NpgsqlDbType.Integer);
                            }
                        };

                        (var affected, var err) = await dbConnection.BulkInsertWithWriterAsync<RuntimeValueObject>("asset_attribute_runtime_series(_ts, asset_id, asset_attribute_id, value, retention_days)", writeToSTDIN, _logger);

                        if (!string.IsNullOrEmpty(err))
                            _logger.LogError("Ingestion_Error StoreSnapshotNumbericsAsync {err}", err);
                        else
                            _logger.LogInformation($"StoreSnapshotNumbericsAsync {affected}/{numbericValues.Count()} numbericValues records has been Insert successfully");
                    }
                }

                //watch.Stop();
                //_logger.LogInformation($"#### StoreSnapshotNumbericsAsync finish took: {watch.ElapsedMilliseconds} ms");
                _logger.LogInformation($"#### StoreSnapshotNumbericsAsync finish.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ingestion_Error StoreSnapshotNumbericsAsync {ex.Message}");
            }
        }

        private async Task StoreSnapshotTextsAsync(string projectId, IEnumerable<RuntimeValueObject> runtimeValues)
        {
            try
            {
                // text value
                var textValues = runtimeValues.Where(x => DataTypeConstants.TEXT_TYPES.Contains(x.DataType)).Select(x => new RuntimeValueObject
                {
                    Timestamp = x.Timestamp,
                    AssetId = x.AssetId,
                    AttributeId = x.AttributeId,
                    Value = x.Value.ToString(), // should be a string
                    DataType = x.DataType,
                    RetentionDays = x.RetentionDays,
                });

                if (!textValues.Any())
                    return;

                //var watch = Stopwatch.StartNew();
                using (var dbConnection = _dbConnectionResolver.CreateConnection(projectId))
                {
                    if (dbConnection.State != ConnectionState.Open)
                        dbConnection.Open();

                    var chunks = textValues.Chunk(_batchOptions.InsertSize);
                    foreach (var chunk in chunks)
                    {
                        Func<NpgsqlBinaryImporter, Task> writeToSTDIN = async (NpgsqlBinaryImporter writer) =>
                        {
                            foreach (var record in chunk)
                            {
                                await writer.StartRowAsync();
                                await writer.WriteAsync(record.Timestamp, NpgsqlTypes.NpgsqlDbType.Timestamp);
                                await writer.WriteAsync(record.AssetId, NpgsqlTypes.NpgsqlDbType.Uuid);
                                await writer.WriteAsync(record.AttributeId, NpgsqlTypes.NpgsqlDbType.Uuid);
                                await writer.WriteAsync(record.Value, NpgsqlTypes.NpgsqlDbType.Varchar);
                                await writer.WriteAsync(record.RetentionDays, NpgsqlTypes.NpgsqlDbType.Integer);
                            }
                        };

                        (var affected, var err) = await dbConnection.BulkInsertWithWriterAsync<RuntimeValueObject>(" asset_attribute_runtime_series_text(_ts, asset_id, asset_attribute_id, value, retention_days)", writeToSTDIN, _logger);
                        if (!string.IsNullOrEmpty(err))
                            _logger.LogError("Ingestion_Error StoreSnapshotTextsAsync {err}", err);
                        else
                            _logger.LogInformation($"StoreSnapshotTextsAsync projectId: {projectId}, {affected}/{textValues.Count()} textValues records has been upserted successfully");
                    }
                }
                //watch.Stop();
                //_logger.LogInformation($"#### StoreSnapshotTextsAsync finish  ElapsedMilliseconds: {watch.ElapsedMilliseconds}");
                _logger.LogInformation($"#### StoreSnapshotTextsAsync finish.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ingestion_Error StoreSnapshotTextsAsync {ex.Message}");
            }
        }

        private async Task<IEnumerable<RuntimeValueObject>> GetRuntimeValueAsync(string projectId, string deviceId, IEnumerable<AssetRuntimeTrigger> assetRuntimeTriggers, IEnumerable<AssetAttribute> assetAttributes, DateTime timestamp)
        {
            // get runtime value
            var runtimeAttributeValues = new ConcurrentBag<RuntimeValueObject>();
            var runtimeAttributes = (from attribute in assetAttributes
                                     join trigger in assetRuntimeTriggers
                                        on new { attribute.AssetId, attribute.AttributeId, attribute.TriggerAssetId, attribute.TriggerAttributeId }
                                        equals new { trigger.AssetId, trigger.AttributeId, trigger.TriggerAssetId, trigger.TriggerAttributeId }
                                     where attribute.AttributeType == AttributeTypeConstants.TYPE_RUNTIME && attribute.EnabledExpression == true
                                     select attribute).Distinct();
            _snapshotDicts.TryGetValue((deviceId, timestamp), out var snapshotMetrics);

            if(snapshotMetrics == null) throw new InvalidOperationException("SnapshotMetrics can't null");

            var dictionary = await GetDictionaryAsync(assetAttributes, projectId, assetRuntimeTriggers, snapshotMetrics);
            var concurrentDict = new ConcurrentDictionary<string,object>(dictionary);
            //var watch = Stopwatch.StartNew();
            var tasks = new List<Task>();
            foreach (var attribute in runtimeAttributes)
            {
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        var value = _dynamicResolver.ResolveInstance("return true;", attribute.Expression).OnApply(dictionary);
                        concurrentDict[attribute.AttributeId.ToString()] = value;
                        runtimeAttributeValues.Add(new RuntimeValueObject
                        {
                            AssetId = attribute.AssetId,
                            AttributeId = attribute.AttributeId,
                            DataType = attribute.DataType,
                            Value = value,
                            Timestamp = timestamp,
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Ingestion_Error GetRuntimeValueAsync failed attributeId: {attributeUd}, exMsg: {exMsg}", attribute.AttributeId, ex.Message);
                    }
                }));
            }
            await Task.WhenAll(tasks);
            //watch.Stop();
            //_logger.LogInformation("GetRuntimeValueAsync CalculateRuntimeMetricDict tooks: {ms} ms", watch.ElapsedMilliseconds);
            var assetInfos = new List<AssetInformation>();
            var uniquesRuntimeAttributes = runtimeAttributes.Select(x => x.AssetId).Distinct();
            foreach (var assetId in uniquesRuntimeAttributes)
            {
                var assetInfo = await _readOnlyAssetRepository.GetAssetInformationsAsync(projectId, assetId);
                assetInfos.Add(assetInfo);
            }

            var values = runtimeAttributeValues.Select(x => new RuntimeValueObject
            {
                Timestamp = timestamp,
                AssetId = x.AssetId,
                AttributeId = x.AttributeId,
                Value = ParseValueToStore(x.DataType, x.Value),
                DataType = x.DataType,
                RetentionDays = assetInfos.First(i => i.AssetId == x.AssetId).RetentionDays
            });

            return values;
        }

        /// <summary>
        /// //TODO: Need to determind what's exactly logic behind GetDictionaryAsync
        /// 
        /// GetDictionaryAsync 
        /// </summary>
        /// <param name="assetAttributes"></param>
        /// <param name="dbConnection"></param>
        /// <param name="projectId"></param>
        /// <param name="assetRuntimeTriggers"></param>
        /// <returns></returns>
        private async Task<Dictionary<string, object>> GetDictionaryAsync(IEnumerable<AssetAttribute> assetAttributes, string projectId, IEnumerable<AssetRuntimeTrigger> assetRuntimeTriggers, IEnumerable<MetricValue> snapshotValues)
        {
            var watch = Stopwatch.StartNew();
            var aliasMapping = await _readOnlyDeviceRepository.GetProjectAttributeAliasMappingAsync(projectId, assetAttributes, assetRuntimeTriggers);
            var snapshots = await _readOnlyDeviceRepository.GetProjectDeviceAttributeSnapshotsAsync(projectId, assetAttributes, aliasMapping); //ToList because need to modify data
            watch.Stop();
            _logger.LogInformation("GetRuntimeValueAsync_GetDictionaryAsync Get Alias and snapshot tooks: {ms} ms", watch.ElapsedMilliseconds);
            // from dynamicAttributes, get DeviceId & it's MetricKey (use metrickey, deviceId for getting it's snapshot)

            foreach (var attrubute in snapshots.Where(i => i.AttributeType == AttributeTypeConstants.TYPE_DYNAMIC || i.AttributeType == AttributeTypeConstants.TYPE_ALIAS))
            {
                if (string.IsNullOrEmpty(attrubute.MetricKey))
                {
                    _logger.LogWarning("aliasAttribute has null metricKey: {attributeId}", attrubute.AttributeId);
                    continue;
                }
                var metricValue = snapshotValues.FirstOrDefault(i => i.MetricKey == attrubute.MetricKey);
                if (metricValue is null)
                    continue;

                attrubute.Value = metricValue.Value;
                attrubute.Timestamp = metricValue.Timestamp;
            }

            var attributeSnapshots = snapshots.SelectMany(x =>
            {
                object value = ParseValue(x.DataType, x.Value?.ToString());
                Guid attributeId = x.AttributeId;
                return aliasMapping.Where(x => x.Item1 == attributeId).Select(mapping => (mapping.Item2, Value: value, x.Timestamp));
            });

            var dictionary = new Dictionary<string, object>();
            foreach (var attribute in assetAttributes)
            {
                var snapshot = snapshots.Where(x => x.AttributeId == attribute.AttributeId).Select(t => (t.Value, t.Timestamp)).FirstOrDefault();
                if (snapshot.Value != null &&
                    (string.Equals(attribute.AttributeType, AttributeTypeConstants.TYPE_STATIC, StringComparison.InvariantCultureIgnoreCase)
                    || snapshot.Timestamp.HasValue
                    ))
                {
                    object value = ParseValue(attribute.DataType, snapshot.Value?.ToString());
                    dictionary[attribute.AttributeId.ToString()] = value;
                }
                else
                {
                    var aliasSnapshot = attributeSnapshots.Where(x => x.Item1 == attribute.AttributeId).Select(t => (t.Value, t.Timestamp)).FirstOrDefault();
                    if (aliasSnapshot.Value != null && aliasSnapshot.Timestamp.HasValue)
                    {
                        dictionary[attribute.AttributeId.ToString()] = aliasSnapshot.Value;
                    }
                    else
                    {
                        _logger.LogError($"Ingestion_Error Snapshot notfound projectId: {projectId} - assetId: {attribute.AssetId} - attributeId: {attribute.AttributeId}");

                        continue;
                        // cannot find the snapshot for this attribute, can cause the issue with runtime attribute
                                            }
                }
            }

            return dictionary;
        }


        /// <summary>
        /// GetAllDataAssetRuntime Each ingestionMessage return a AllDataAssetRuntimeObject
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="projectId"></param>
        /// <returns></returns>
        private async Task<GetAllDataAssetRuntimeQueryResult[]> GetAllDataAssetRuntime(IEnumerable<IngestionMessage> messages, string projectId)
        {
            var result = new ConcurrentBag<GetAllDataAssetRuntimeQueryResult>();
            var messageFilters = messages.Where(x => {
                var deviceId = x.RawData[MetricPayload.DEVICE_ID].ToString();
                 _datAssetRuntimeTriggers.TryGetValue(deviceId, out var trigger);

                 return trigger != null && trigger.Any();
            });

            // No need to run in case there is no asset trigger
            await Parallel.ForEachAsync(messageFilters, _parallelOptions, async (message, ct) =>
            {
                var deviceId = message.RawData[MetricPayload.DEVICE_ID].ToString();
                if (!string.IsNullOrEmpty(deviceId))
                {
                    _deviceInformations[projectId].TryGetValue(deviceId, out DeviceInformation deviceInformation);
                    deviceInformation ??= await GetDeviceInformationAsync(projectId, deviceId);
                    var timestampKeys = deviceInformation.Metrics.Where(x => x.MetricType == TemplateKeyTypes.TIMESTAMP).Select(x => x.MetricKey);
                    var timestamp = (from ts in timestampKeys
                                     join metric in message.RawData on ts.ToLowerInvariant() equals metric.Key.ToLowerInvariant()
                                     select metric.Value.ToString()
                        ).FirstOrDefault();
                    if (timestamp != null)
                    {
                        var deviceTimestamp = DateTimeExtensions.CutOffNanoseconds(timestamp.CutOffFloatingPointPlace().UnixTimeStampToDateTime());
                        var dataUnixTimestamps = new Dictionary<string, long>();
                        _datAssetRuntimeTriggers.TryGetValue(deviceId, out var assetRuntimeTriggers);

                        //watch6.Stop();
                        //_logger.LogInformation("GetAllDataAssetRuntime GetDeviceInformationAsync and timestamp tooks: {ms} ms", watch6.ElapsedMilliseconds);

                        if (!message.RawData.ContainsKey(MetricPayload.INTEGRATION_ID))
                        {
                            _snapshotDicts.TryGetValue((deviceId, deviceTimestamp), out var snapshotMetrics);
                            if (snapshotMetrics != null)
                            {
                                var latestSnapshot = snapshotMetrics.First();

                                deviceTimestamp = latestSnapshot.Timestamp;
                                // save UnixTimestamp for notify
                                _ = dataUnixTimestamps.TryAdd(deviceId, latestSnapshot.UnixTimestamp);
                            }
                        }

                        result.Add(await CalculateAssetAttributeRuntimeValue(message, projectId, deviceId, deviceTimestamp, dataUnixTimestamps, assetRuntimeTriggers));
                    }
                }
            });
            return [.. result];
        }

        private async Task<GetAllDataAssetRuntimeQueryResult> CalculateAssetAttributeRuntimeValue
        (
            IngestionMessage message,
            string projectId,
            string deviceId,
            DateTime deviceTimestamp,
            Dictionary<string, long> dataUnixTimestamps,
            IEnumerable<AssetRuntimeTrigger> assetRuntimeTriggers
        )
        {

            var dataAssetAttributes = new Dictionary<string, IEnumerable<AssetAttribute>>();
            var dataAssetRuntimeTriggers = new Dictionary<string, IEnumerable<AssetRuntimeTrigger>>();
            var dataRuntimeValues = new Dictionary<string, IEnumerable<RuntimeValueObject>>();

            if (assetRuntimeTriggers.Any())
            {
                //save asset runtime trigger to dictionary
                _ = dataAssetRuntimeTriggers.TryAdd(deviceId, assetRuntimeTriggers);
                //var watch = Stopwatch.StartNew();
                var assetAttributes = await _deviceRepository.GetAssetAttributesAsync(projectId, deviceId, assetRuntimeTriggers);
                //watch.Stop();
                //_logger.LogInformation("GetAllDataAssetRuntime GetAssetAttributesAsync tooks: {ms} ms", watch.ElapsedMilliseconds);

                //var watch2 = Stopwatch.StartNew();
                //save runtime value to dictionary
                var values = await GetRuntimeValueAsync(projectId, deviceId, assetRuntimeTriggers, assetAttributes, deviceTimestamp);
                    //watch2.Stop();
                    //_logger.LogInformation("GetAllDataAssetRuntime GetRuntimeValueAsync tooks: {ms} ms", watch2.ElapsedMilliseconds);
                    _ = dataRuntimeValues.TryAdd(deviceId, values);

                    // save assets attributes to dictionary
                    _ = dataAssetAttributes.TryAdd(deviceId, assetAttributes);
                }

                return new GetAllDataAssetRuntimeQueryResult
                {
                    MetricDict = message,
                    AssetAttributes = dataAssetAttributes,
                    AssetRuntimeTriggers = dataAssetRuntimeTriggers,
                    AssetRuntimeValues = dataRuntimeValues,
                    DataUnixTimestamps = dataUnixTimestamps
                };
        }

        public async Task<List<MetricValuesObject>> CalculateRuntimeMetricAsync(IEnumerable<IngestionMessage> ingesMessages, string batchProjectId)
        {
            var upsertData = new List<MetricValuesObject>();
            foreach (var message in ingesMessages)
            {
                var tenantId = message.TenantId;
                var subscriptionId = message.SubscriptionId;
                var projectId = message.ProjectId;
                var integrationString = message.RawData[MetricPayload.INTEGRATION_ID].ToString();
                var deviceId = message.RawData[MetricPayload.DEVICE_ID].ToString();
                if (integrationString == null)
                {
                    _logger.LogDebug($"device and integrationId required");
                    continue;
                }
                var deviceInfos = await GetDeviceInformationAsync(projectId, deviceId);
                // _logger.LogDebug($"IntegrationId {integrationString}/DeviceId {deviceInformation.DeviceId}");
                //Q: what is the purpose of this 
                //var hash = $"{tenantId}_{subscriptionId}_{projectId}_processing_device_external_{string.Join("_", message.RawData.Select(x => $"{x.Key}_{x.Value}"))}".CalculateMd5Hash().ToLowerInvariant();
                //var cacheHit = await _cache.GetAsync<string>(hash);
                //if (cacheHit != null)
                //{
                //    //nothing change. no need to update
                //    _logger.LogInformation($"Cache hit, no change, system will complete the request deviceId: {deviceId}");
                //    return upsertData;
                //}

                //await _cache.AddAsync(hash, "cached", TimeSpan.FromDays(1));
                // _logger.LogDebug($"Integration Id {integrationString}");
                var integrationId = Guid.Parse(integrationString.ToString());
                var timestampKeys = deviceInfos.Metrics.Where(x => x.MetricType == TemplateKeyTypes.TIMESTAMP).Select(x => x.MetricKey);
                var timestamp = (from ts in timestampKeys
                                 join metric in message.RawData on ts.ToLowerInvariant() equals metric.Key.ToLowerInvariant()
                                 select metric.Value.ToString()
                    ).FirstOrDefault();
                var deviceTimestamp = timestamp.CutOffFloatingPointPlace().UnixTimeStampToDateTime().CutOffNanoseconds();
                //_tenantContext.RetrieveFromString(tenantId, subscriptionId, projectId); why do we need this?
                upsertData.AddRange(message.RawData.Where(x => !RESERVE_KEYS.Contains(x.Key)).Select(x => new MetricValuesObject
                {
                    Timestamp = deviceTimestamp,
                    Value = x.Value.ToString(),
                    IntegrationId = integrationId,
                    DeviceId = deviceInfos.DeviceId,
                    MetricId = x.Key
                }));
            }

            //upsert only 1 time.
            _logger.LogInformation($"#### Upsert CalculateRuntimeMetricAsync begin. timestamp: {((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds()}");
            //var watchA = Stopwatch.StartNew();

            using (var dbConnection = _dbConnectionResolver.CreateConnection(batchProjectId))
            {
                if (dbConnection.State != ConnectionState.Open)
                    dbConnection.Open();

                var chunks = upsertData.Chunk(_batchOptions.UpsertSize);
                foreach (var chunk in chunks)
                {
                    Func<NpgsqlBinaryImporter, Task> writeToSTDIN = async (NpgsqlBinaryImporter writer) =>
                    {
                        foreach (var item in upsertData)
                        {
                            await writer.StartRowAsync();
                            await writer.WriteAsync(item.Timestamp, NpgsqlDbType.Timestamp);
                            await writer.WriteAsync(item.Value, NpgsqlDbType.Text);
                            await writer.WriteAsync(item.IntegrationId, NpgsqlDbType.Uuid);
                            await writer.WriteAsync(item.DeviceId, NpgsqlDbType.Varchar);
                            await writer.WriteAsync(item.MetricId, NpgsqlDbType.Varchar);
                        }
                    };

                    (var affected, var err) = await dbConnection.BulkUpsertAsync(
                   "device_metric_external_snapshots",
                   "(_ts, value, integration_id, device_id, metric_key)",
                   "(integration_id, device_id, metric_key)",
                   "UPDATE SET _ts = EXCLUDED._ts, value = EXCLUDED.value WHERE device_metric_external_snapshots._ts < EXCLUDED._ts",
                   writeToSTDIN, _logger);
                    if (!string.IsNullOrEmpty(err))
                        _logger.LogError("Ingestion_Error CalculateRuntimeMetricAsync {err}", err);
                    else
                        _logger.LogInformation($"CalculateRuntimeMetricAsync {affected} / {upsertData.Count} records has been upserted successfully");
                }
            }

            //watchA.Stop();
            //_logger.LogInformation($"#### Upsert CalculateRuntimeMetricAsync finish .ElapsedMilliseconds: {watchA.ElapsedMilliseconds}");
            return upsertData;
        }

        #endregion
    }

    class MetricList<T>
    {
        private readonly List<T> _list = [];
        private readonly SemaphoreSlim _sync = new(1,1);
        public List<T> ToList()
        {
            return [.. _list];
        }

        public IEnumerable<T> AsEnumerable()
        {
            return _list.AsEnumerable();
        }

        public List<T> ToListClone()
        {
            return [.. _list];
        }
        public void Add(IEnumerable<T> values)
        {
            _sync.Wait();
            _list.AddRange(values);
            _sync.Release();
        }

        public void Add2(IEnumerable<T> values)
        {
            foreach (var item in values)
                _list.Add(item);
        }
    }
}
