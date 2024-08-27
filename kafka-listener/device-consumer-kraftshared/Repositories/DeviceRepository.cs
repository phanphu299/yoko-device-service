using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Abstraction;
using Dapper;
using Device.Consumer.KraftShared.Constant;
using Device.Consumer.KraftShared.Constants;
using Device.Consumer.KraftShared.Helpers;
using Device.Consumer.KraftShared.Model;
using Device.Consumer.KraftShared.Models.MetricModel;
using Device.Consumer.KraftShared.Models.Options;
using Device.Consumer.KraftShared.Repositories.Abstraction;
using Device.Consumer.KraftShared.Repositories.Abstraction.ReadOnly;
using Device.Consumer.KraftShared.Service.Model;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Pipelines.Sockets.Unofficial.Arenas;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Device.Consumer.KraftShared.Repositories
{
    public class DeviceRepository : IDeviceRepository, IReadOnlyDeviceRepository
    {

        private readonly BatchProcessingOptions _batchOptions;
        private readonly IRedisDatabase _cache;
        //private readonly IMemoryCache _memoryCache;
        private readonly string _podName;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ILoggerAdapter<DeviceRepository> _logger;
        private readonly ParallelOptions _parallelOptions;
        public DeviceRepository(
            IConfiguration configuration,
            IRedisDatabase cache,
            //IMemoryCache memoryCache,
            ILoggerAdapter<DeviceRepository> logger,
            IDbConnectionFactory dbConnectionFactory,
            IOptions<BatchProcessingOptions> options
        )
        {
            _batchOptions = options.Value;
            _logger = logger;
            _dbConnectionFactory = dbConnectionFactory;
            _cache = cache;
            //_memoryCache = memoryCache;
            _podName = configuration["PodName"] ?? "device-function";
            _parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 5 };
        }

        public async Task<IEnumerable<AssetRuntimeTrigger>> GetAssetTriggerAsync(string projectId, string deviceId, bool forceToReload = false)
        {
            // need to cache the data for device.
            //var assetAttributeRelevantToDeviceIdKey = $"{projectId}_processing_device_{deviceId}_asset_related_runtime_trigger_with_metric";
            var assetAttributeRelevantToDeviceIdKey = string.Format(IngestionRedisCacheKeys.AssetRuntimeTriggerPattern, projectId);
            IEnumerable<AssetRuntimeTrigger> runtimeAssetIds = null;
            if(!forceToReload)
            {
                runtimeAssetIds = await _cache.HashGetAsync<IEnumerable<AssetRuntimeTrigger>>(assetAttributeRelevantToDeviceIdKey, deviceId);
                //var runtimeAssetIds = JsonSerializer.Deserialize<IEnumerable<AssetRuntimeTrigger>>(data);
                if (runtimeAssetIds != null)
                    return runtimeAssetIds;
            }

            _logger.LogInformation("GetAssetTriggerAsync for projectId: {projectId}, deviceId: {deviceId}", projectId, deviceId);
            using var dbConnection = GetDbConnection(projectId);
            if (dbConnection.State != ConnectionState.Open)
                dbConnection.Open();

            runtimeAssetIds = await dbConnection.QueryAsync<AssetRuntimeTrigger>(@"select distinct asset_id as AssetId
                                                                                , attribute_id as AttributeId
                                                                                , trigger_asset_id as TriggerAssetId
                                                                                , trigger_attribute_id as TriggerAttributeId
                                                                                , metric_key as MetricKey
                                                                                from find_all_asset_trigger_by_device_id_refactor(@DeviceId)",
                    new
                    {
                        DeviceId = deviceId
                    }, commandTimeout: 10000);
            await _cache.HashSetAsync<IEnumerable<AssetRuntimeTrigger>>(assetAttributeRelevantToDeviceIdKey, deviceId, runtimeAssetIds);
            return runtimeAssetIds;
        }

        public async Task<IEnumerable<Guid>> GetAssetIdsAsync(string projectId, string deviceId)
        {
            var assetRelatedToDeviceIdKey = string.Format(IngestionRedisCacheKeys.LinkAssetsDeviceIdPattern, projectId);
            var assetIds = await _cache.HashGetAsync<IEnumerable<Guid>>(assetRelatedToDeviceIdKey, deviceId);
            if (assetIds != null)
                return assetIds;

            using var dbConnection = GetDbConnection(projectId);
            var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);
            assetIds = await retryStrategy.ExecuteAsync(async () =>
                await dbConnection.QueryAsync<Guid>(@"select distinct asset_id from find_all_asset_related_to_device_refactor(@DeviceId)", new
                {
                    DeviceId = deviceId
                })
            );
            await _cache.HashSetAsync<IEnumerable<Guid>>(assetRelatedToDeviceIdKey, deviceId, assetIds);
            return assetIds;
        }

        public async Task<IEnumerable<string>> GetDeviceMetricKeyAsync(string projectId)
        {
            // _logger.LogInformation($"GetDeviceMetricKeyAsync - projectId={projectId}");
            var deviceMetricKeyDeviceId = $"{projectId}_processing_device_metric_device_id_key";
            var deviceIdKeys = await _cache.GetAsync<IEnumerable<string>>(deviceMetricKeyDeviceId);
            if (deviceIdKeys != null)
                return deviceIdKeys;
            using (var dbConnection = GetDbConnection(projectId))
            {
                var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);
                // get device Id
                deviceIdKeys = await retryStrategy.ExecuteAsync(async () => await dbConnection.QueryAsync<string>("select distinct key from v_template_deviceId"));
                dbConnection.Close();
            }

            await _cache.SetAddAsync(deviceMetricKeyDeviceId, deviceIdKeys);
            return deviceIdKeys;
        }

        public async Task<DeviceInformation> GetDeviceInformationAsync(string projectId, string[] deviceIds)
        {
            // _logger.LogInformation($"GetDeviceInformationAsync - projectId={projectId}, deviceIds={string.Join("_", deviceIds)}");
            var devicesRedisHashKey = string.Format(IngestionRedisCacheKeys.DeviceInfoPattern, projectId);
            var deviceMetricKey = $"{deviceIds.First()}";
            var deviceInformation = await _cache.HashGetAsync<DeviceInformation>(devicesRedisHashKey, deviceMetricKey);
            // var cacheHit = await _cache.StringGetAsync(deviceMetricKey);

            if (deviceInformation != null)
                return deviceInformation;
            using (var dbConnection = GetDbConnection())
            {

                var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);
                var reader = await retryStrategy.ExecuteAsync(async () =>
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
                            where device_id = ANY(@DeviceIds);", new { DeviceIds = deviceIds }, commandTimeout: 10000)
                );
                deviceInformation = await reader.ReadFirstOrDefaultAsync<DeviceInformation>();
                if (deviceInformation != null)
                    deviceInformation.Metrics = await reader.ReadAsync<DeviceMetricDataType>();

                dbConnection.Close();

                if (deviceInformation != null)
                    await _cache.HashSetAsync<DeviceInformation>(devicesRedisHashKey, deviceMetricKey, deviceInformation);

                return deviceInformation;
            }
        }

        public async Task<IEnumerable<DeviceInformation>> GetDeviceInformationsWithTopicNameAsync(string projectId, string topicName, string brokerType)
        {
            // _logger.LogInformation($"GetDeviceInformationsWithTopicNameAsync - projectId={projectId}, topicName={topicName}");
            //var deviceMetricKey = $"{_podName}_{projectId}_processing_device_topic_{topicName}_device_metrics";
            //var deviceMetricNotFoundKey = $"{_podName}_{projectId}_processing_device_topic_{topicName}_device_metrics_not_found";
            //var cacheHit = await _cache.HashGetAsync<string>("GetDeviceInformation", deviceMetricKey);

            var devicesRedisHashKey = $"DeviceInformationByTopic:{projectId}";
            var deviceTopicMetricKey = $"{topicName}_device_metrics";
            var deviceInformations = await _cache.HashGetAsync<IEnumerable<DeviceInformation>>(devicesRedisHashKey, deviceTopicMetricKey);

            if (deviceInformations != null)
                return deviceInformations;

            using (var dbConnection = GetDbConnection())
            {


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

                var result = await dbConnection.QueryAsync<DeviceInformation, DeviceMetricDataType, DeviceInformation>(
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
                        splitOn: "DeviceId, MetricKey");

                deviceInformations = result.GroupBy(d => d.DeviceId).Select(g =>
                {
                    var groupedDevice = g.First();
                    groupedDevice.Metrics = g.Select(d => d.Metrics.First()).ToList();
                    return groupedDevice;
                });
            }

            await _cache.HashSetAsync<IEnumerable<DeviceInformation>>(devicesRedisHashKey, deviceTopicMetricKey, deviceInformations);
            return deviceInformations;
        }

        public async Task<IEnumerable<DeviceMetricDataType>> GetMetricDataTypesAsync(string deviceIdFromFile)
        {
            using (var dbConnection = GetDbConnection())
            {
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
                return dataTypes;
            }
        }

        public async Task<IEnumerable<(string MetricKey, string DataType)>> GetActiveDeviceMetricsAsync(string deviceId, IList<string> deviceMetrics)
        {
            using (var dbConnection = GetDbConnection())
            {
                var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);
                var metrics = await retryStrategy.ExecuteAsync(async () =>
                    await dbConnection.QueryAsync<(string MetricKey, string DataType)>(
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
                dbConnection.Close();
                return metrics;
            }
        }

        private IDbConnection GetDbConnection(string projectId = null) => _dbConnectionFactory.CreateConnection(projectId);

        public async Task<IEnumerable<DeviceInformation>> GetProjectDevicesAsync(string projectId, bool forceToReload = false)
        {
            //TODO: should we always load all again? Or just load missing devices from cache?
            var result = new List<DeviceInformation>();
            var notfoundIds = new List<string>();
            var hashKey = string.Format(IngestionRedisCacheKeys.DeviceInfoPattern, projectId);
            if (!forceToReload)
            {
                var devices = await _cache.HashGetAllAsync<DeviceInformation>(hashKey);
                if (devices.Any())
                {
                    _logger.LogInformation("Device already exists in cache");
                    return devices.Values.Select(x => x);
                }
            }

            var sql = @"SELECT d.id as DeviceId, 
                        d.retention_days as RetentionDays,
                        d.enable_health_check as EnableHealthCheck
                        from devices as d order by d.id asc;

                        SELECT 
                        dvm.metric_key as MetricKey,
                        dvm.expression_compile as ExpressionCompile,
                        dvm.data_type as DataType, 
                        dvm.metric_type as MetricType,
                        dvm.device_id as SourceDeviceId
                        FROM v_device_metrics as dvm;

            ;";

            IEnumerable<DeviceInformation> devicesFromDb;
            using (var dbConnection = GetDbConnection(projectId))
            {
                using (var data = await dbConnection.QueryMultipleAsync(sql))
                {
                    devicesFromDb = await data.ReadAsync<DeviceInformation>();
                    var metrics = await data.ReadAsync<DeviceMetricDataType>();
                    foreach (var dInfoDb in devicesFromDb)
                        dInfoDb.Metrics = metrics.Where(i => i.SourceDeviceId == dInfoDb.DeviceId);
                }
            }

            //step 3. append to redis, then to memorycache
            foreach (var dInfoDb in devicesFromDb)
                await _cache.HashSetAsync<DeviceInformation>(hashKey, $"{dInfoDb.DeviceId}", dInfoDb);

            return devicesFromDb;
        }
        public async Task<IDictionary<string, IEnumerable<AssetAttribute>>> GetProjectAssetAttributesAsync(string projectId, IDictionary<string, IEnumerable<AssetRuntimeTrigger>> assetRuntimeTriggers)
        {
            var dictResult = new Dictionary<string, IEnumerable<AssetAttribute>>();

            foreach (var deviceId in assetRuntimeTriggers.Keys)
            {
                var assetIds = assetRuntimeTriggers[deviceId].Select(x => x.AssetId).ToList();
                //TODO: this list is very long, need to redefined.
                //var assetAttributeKey = $"{_podName}_{projectId}_processing_assets_ids_{string.Join(',', assetIds)}_attributes_trigger"; // OLD

                var assetAttributeRedisKey = string.Format(IngestionRedisCacheKeys.AssetAttributesPattern, projectId);//hash field = AssetId, hash value = assetAttributes
                var notfoundAttributes = new List<Guid>();// to query 1 times from database for missing asset
                var result = new List<AssetAttribute>();
                foreach (var assetId in assetIds)
                {
                    var assetAttributeMemoryKey = $"{assetAttributeRedisKey}:{assetId}";//hash field = AssetId, hash value = assetAttributes
                    //step 1. get from memcache

                    //step 2. get from redis cache
                    var att = await _cache.HashGetAsync<IEnumerable<AssetAttribute>>(assetAttributeRedisKey, assetId.ToString());
                    if (att != null)
                    {
                        result.AddRange(att);
                        continue;
                    }
                    else
                        notfoundAttributes.Add(assetId);
                }

                if (!notfoundAttributes.Any())
                {
                    dictResult.Add(deviceId, result);
                    continue;
                }


                var smallbatches = notfoundAttributes.Chunk(_batchOptions.MaxChunkSize).ToArray();
                ConcurrentBag<AssetAttribute> assetAttributes = new();
                foreach (var batch in smallbatches)
                {
                    //step 3. get from database for all missing assets
                    using (var dbConnection = GetDbConnection(projectId))
                    {
                        var dbresult = await dbConnection.QueryAsync<AssetAttribute>(@"select
                                                                                aa.asset_id as AssetId
                                                                                , aa.attribute_id as AttributeId
                                                                                , aa.data_type as DataType
                                                                                , aa.attribute_type as AttributeType
                                                                                , aa.expression_compile as Expression
                                                                                , aa.enabled_expression as EnabledExpression
                                                                                , aart.trigger_asset_id as TriggerAssetId
                                                                                , aart.trigger_attribute_id as TriggerAttributeId
                                                                            from v_asset_attributes aa
                                                                            left join asset_attribute_runtime_triggers aart on aart.asset_id = aa.asset_id and aart.attribute_id = aa.attribute_id
                                                                            where aa.asset_id = ANY(@AssetIds)
                                                                            order by aa.created_utc, aa.sequential_number", new { AssetIds = batch.ToArray() });
                        foreach (var item in dbresult)
                            assetAttributes.Add(item);
                    }
                }

                // step 3.1 because get batch, so now must to group each by assetId
                var assetAttributesGroups = assetAttributes.GroupBy(i => i.AssetId).Select(g => new { AssetId = g.Key, Attributes = g.ToArray() });
                foreach (var group in assetAttributesGroups)
                {
                    //step 4. set memcache

                    //step 5. set redis cache
                    await _cache.HashSetAsync<IEnumerable<AssetAttribute>>(assetAttributeRedisKey, group.AssetId.ToString(), group.Attributes);

                    //step 6. append to result list
                    result.AddRange(group.Attributes);
                }
                dictResult.Add(deviceId, result);
            }

            return dictResult;
        }

        public async Task<IDictionary<string, IEnumerable<AssetRuntimeTrigger>>> GetProjectAssetRuntimeTriggersAsync(string projectId, IEnumerable<string> deviceIds, bool forceToReload = false)
        {
            var results = new ConcurrentDictionary<string, IEnumerable<AssetRuntimeTrigger>>();
            var notfoundDeviceIds = new ConcurrentBag<string>();
            var deviceIdSmallbatches = deviceIds.Chunk(_batchOptions.MaxChunkSize).ToArray();
            var redisKey = string.Format(IngestionRedisCacheKeys.AssetRuntimeTriggerPattern, projectId);
                    
            foreach (var deviceId in deviceIds)
            {
                if (!forceToReload)
                {
                    var runtimeAssetTriggers = await _cache.HashGetAsync<IEnumerable<AssetRuntimeTrigger>>(redisKey, deviceId);
                    if (runtimeAssetTriggers != null)
                    {
                        results.TryAdd(deviceId, runtimeAssetTriggers);
                        continue;
                    }
                }
                notfoundDeviceIds.Add(deviceId);
            }

            if (notfoundDeviceIds.IsEmpty)
                return results;

            var smallbatches = notfoundDeviceIds.Chunk(_batchOptions.MaxChunkSize).ToArray();
            var watch = System.Diagnostics.Stopwatch.StartNew();
            foreach (var batch in smallbatches)
            {
                var watch2 = System.Diagnostics.Stopwatch.StartNew();
                var queryTasks = batch.Select(deviceId => Task.Run(async () =>
                  {
                      using var dbConnection = GetDbConnection(projectId);
                      //step 4. get from db
                      var assetRuntimes = await dbConnection.QueryAsync<AssetRuntimeTrigger>(@"select distinct asset_id as AssetId
                                                                                , attribute_id as AttributeId
                                                                                , trigger_asset_id as TriggerAssetId
                                                                                , trigger_attribute_id as TriggerAttributeId
                                                                                , metric_key as MetricKey
                                                                                from find_all_asset_trigger_by_device_id_refactor(@DeviceId)",
                          new { DeviceId = deviceId }, commandTimeout: 3000);

                      if (assetRuntimes != null && assetRuntimes.Any())
                      {
                          results.TryAdd(deviceId, assetRuntimes);
                          // set redis cache
                          await _cache.HashSetAsync<IEnumerable<AssetRuntimeTrigger>>(redisKey, deviceId, assetRuntimes);
                      }
                  }));
                await Task.WhenAll(queryTasks);
                watch2.Stop();
                _logger.LogInformation($"QueryAsync<AssetRuntimeTrigger>  smallbatch {batch.Count()} items tooks {watch2.ElapsedMilliseconds} ms");
            }

            watch.Stop();
            _logger.LogInformation($"QueryAsync<AssetRuntimeTrigger> {notfoundDeviceIds.Count} items, run for {watch.ElapsedMilliseconds} ms");
            return results;
        }

        public async Task<IEnumerable<DeviceAttributeSnapshot>> GetProjectDeviceAttributeSnapshotsAsync(string projectId, IEnumerable<AssetAttribute> assetsAttributes)
        {
            var result = new List<DeviceAttributeSnapshot>();
            var notfoundAttributeIds = new ConcurrentBag<Guid>();
            var attributeIds = assetsAttributes.Select(i => i.AttributeId).ToArray();

            foreach (var attribute in assetsAttributes)
            {

                // step 1. 
                //var assetAttributeRelevantToDeviceIdKey = $"{projectId}_processing_device_{deviceId}_asset_related_runtime_trigger_with_metric"; //old hash key
                var redisKey = string.Format(IngestionRedisCacheKeys.AssetAttributeRuntimeSnapshotsPattern, projectId, attribute.AssetId);

                // step 2. load from Memcache


                // step 3. load from redisCache
                var snapshots = await _cache.HashGetAsync<DeviceAttributeSnapshot>(redisKey, attribute.AttributeId.ToString());
                if (snapshots != null)
                {
                    result.Add(snapshots);
                    continue;
                }
                //else set to notfound then query from db
                notfoundAttributeIds.Add(attribute.AttributeId);
            }

            if (!notfoundAttributeIds.Any())
            {
                return result;
            }
            var smallbatches = notfoundAttributeIds.Chunk(_batchOptions.MaxChunkSize).ToArray();
            //step 4. get from db
            ConcurrentBag<DeviceAttributeSnapshot> attributeSnapshots = new();
            foreach (var batch in smallbatches)
            {
                using (var dbConnection = GetDbConnection(projectId))
                {
                    var dbresult = await dbConnection.QueryAsync<DeviceAttributeSnapshot>(@"select attribute_id as AttributeId
                                    , value as Value
                                    , _ts as Timestamp
                                    , data_type as DataType
                                    , device_id as DeviceId
                                    , metric_key as MetricKey
                                    , attribute_type as AttributeType
                                    , asset_id as AssetId
                                    from v_asset_attribute_snapshots
                                    where attribute_id = ANY(@AttributeIds)",
                    new { AttributeIds = batch }, commandTimeout: 10000);
                    foreach (var item in dbresult)
                        attributeSnapshots.Add(item);
                }
            }

            // step 3.1 because get batch, so now must to group each by attribute_id
            var attributeSnapshotsGroups = attributeSnapshots.GroupBy(i => i.AttributeId).Select(g => new { AttributeId = g.Key, Snapshots = g.ToArray() });

            foreach (var group in attributeSnapshotsGroups)
            {
                var redisKey = string.Format(IngestionRedisCacheKeys.AssetAttributeRuntimeSnapshotsPattern, projectId, group.AttributeId);

                //step 4. set memcache

                //step 5. set redis cache
                await _cache.HashSetAsync<DeviceAttributeSnapshot>(redisKey, group.AttributeId.ToString(), group.Snapshots.OrderByDescending(i => i.Timestamp).FirstOrDefault());

                //step 6. append to result list
                result.AddRange(group.Snapshots);
            }

            return result;
        }

        public async Task<IEnumerable<DeviceAttributeSnapshot>> GetProjectDeviceAttributeSnapshotsAsync(string projectId, IEnumerable<AssetAttribute> assetsAttributes, IEnumerable<(Guid, Guid)> aliasMapping)
        {
            var result = new List<DeviceAttributeSnapshot>();
            var attributeIds = assetsAttributes.Select(i => i.AttributeId).Union(aliasMapping.Select(i => i.Item1).Distinct()).ToArray();
            var dict = assetsAttributes.Select(i => new { i.AssetId, i.AttributeId }).Distinct().ToDictionary(i => i.AttributeId, i=> i.AssetId);

            //approach1: get all snapshots by assetId,
            //cons: some attributes bring back meanwhile that isn't necessary.
            //var assetIds = dict.Values.ToArray();
            //foreach (var assetId in assetIds)
            //{
            //    var redisKey = string.Format(IngestionRedisCacheKeys.AssetAttributeRuntimeSnapshotsPattern, projectId, assetId);
            //    var daSnapshots = await _cache.HashGetAllAsync<DeviceAttributeSnapshot>(redisKey);
            //    result.AddRange(daSnapshots.Values);
            //}
            //IEnumerable<Guid> notfoundAttributeIds = attributeIds.Except(result.Select(i=>i.AttributeId));

            //approach2: get by attributeId directly.
            List<Guid> notfoundAttributeIds = new List<Guid>();
            foreach (var attributeId in attributeIds)
            {

                // step 1. 
                //var assetAttributeRelevantToDeviceIdKey = $"{projectId}_processing_device_{deviceId}_asset_related_runtime_trigger_with_metric"; //old hash key
                if (dict.ContainsKey(attributeId))
                {
                    var redisKey = string.Format(IngestionRedisCacheKeys.AssetAttributeRuntimeSnapshotsPattern, projectId, dict[attributeId]);
                    // step 2. load from Memcache


                    // step 3. load from redisCache
                    var snapshots = await _cache.HashGetAsync<DeviceAttributeSnapshot>(redisKey, attributeId.ToString());
                    if (snapshots != null)
                    {
                        result.Add(snapshots);
                        continue;
                    }
                }
                //else set to notfound then query from db
                notfoundAttributeIds.Add(attributeId);
            }

            if (!notfoundAttributeIds.Any())
            {
                return result;
            }

            var smallbatches = notfoundAttributeIds.Chunk(_batchOptions.MaxChunkSize).ToArray();
            //step 4. get from db
            ConcurrentBag<DeviceAttributeSnapshot> attributeSnapshots = new();

            foreach (var batch in smallbatches)
            {
                using (var dbConnection = GetDbConnection(projectId))
                {
                    var dbresult = await dbConnection.QueryAsync<DeviceAttributeSnapshot>(@"select attribute_id as AttributeId
                                    , value as Value
                                    , _ts as Timestamp
                                    , data_type as DataType
                                    , device_id as DeviceId
                                    , metric_key as MetricKey
                                    , attribute_type as AttributeType
                                    , asset_id as AssetId
                                    from v_asset_attribute_snapshots
                                    where attribute_id = ANY(@AttributeIds)",
                    new { AttributeIds = batch }, commandTimeout: 10000);
                    foreach (var item in dbresult)
                        attributeSnapshots.Add(item);
                }

            }

            // step 3.1 because get batch, so now must to group each by attribute_id
            var attributeSnapshotsGroups = attributeSnapshots.GroupBy(i => i.AttributeId).Select(g => new { AttributeId = g.Key, Snapshots = g.ToArray() });

            foreach (var group in attributeSnapshotsGroups)
            {
                var redisKey = string.Format(IngestionRedisCacheKeys.AssetAttributeRuntimeSnapshotsPattern, projectId, group.AttributeId);

                //step 4. set memcache

                //step 5. set redis cache
                await _cache.HashSetAsync<DeviceAttributeSnapshot>(redisKey, group.AttributeId.ToString(), group.Snapshots.OrderByDescending(i => i.Timestamp).FirstOrDefault());

                //step 6. append to result list
                result.AddRange(group.Snapshots);
            }

            return result;
        }

        public async Task<IEnumerable<AttributeSnapshot>> GetProjectAttributeSnapshotsAsync(string projectId, IEnumerable<AssetAttribute> assetsAttributes)
        {
            var result = new List<AttributeSnapshot>();
            var notfoundAttributeIds = new List<Guid>();
            var attributeIds = assetsAttributes.Select(i => i.AttributeId).ToArray();

            foreach (var attribute in assetsAttributes)
            {

                // step 1. 
                //var assetAttributeRelevantToDeviceIdKey = $"{projectId}_processing_device_{deviceId}_asset_related_runtime_trigger_with_metric"; //old hash key
                var redisKey = string.Format(IngestionRedisCacheKeys.AssetAttributeRuntimeSnapshotsPattern, projectId, attribute.AssetId);

                // step 2. load from Memcache


                // step 3. load from redisCache
                var snapshots = await _cache.HashGetAsync<AttributeSnapshot>(redisKey, attribute.AttributeId.ToString());
                if (snapshots != null)
                {
                    result.Add(snapshots);
                    continue;
                }
                //else set to notfound then query from db
                notfoundAttributeIds.Add(attribute.AttributeId);
            }

            if (!notfoundAttributeIds.Any())
            {
                return result;
            }

            //step 4. get from db
            IEnumerable<AttributeSnapshot> attributeSnapshots;
            using (var dbConnection = GetDbConnection(projectId))
            {
                attributeSnapshots = await dbConnection.QueryAsync<AttributeSnapshot>(@"select attribute_id as AttributeId
                                    , value as Value
                                    , _ts as Timestamp
                                    , data_type as DataType
                                    , device_id as DeviceId
                                    , metric_key as MetricKey
                                    , 
                                    from v_asset_attribute_snapshots
                                    where attribute_id = ANY(@AttributeIds)",
                    new { AttributeIds = notfoundAttributeIds }, commandTimeout: 10000);
            }


            // step 3.1 because get batch, so now must to group each by attribute_id
            var attributeSnapshotsGroups = attributeSnapshots.GroupBy(i => i.AttributeId).Select(g => new { AttributeId = g.Key, Snapshots = g.ToArray() });

            foreach (var group in attributeSnapshotsGroups)
            {
                var redisKey = string.Format(IngestionRedisCacheKeys.AssetAttributeRuntimeSnapshotsPattern, projectId, group.AttributeId);

                //step 4. set memcache

                //step 5. set redis cache
                await _cache.HashSetAsync<AttributeSnapshot>(redisKey, group.AttributeId.ToString(), group.Snapshots.OrderByDescending(i => i.Timestamp).FirstOrDefault());

                //step 6. append to result list
                result.AddRange(group.Snapshots);
            }

            return result;
        }


        public async Task<IDictionary<string, IEnumerable<AttributeSnapshot>>> GetProjectAttributeSnapshotsAsync(string projectId, IDictionary<string, IEnumerable<AssetAttribute>> assetsAttributesDict)
        {
            var dictResult = new Dictionary<string, IEnumerable<AttributeSnapshot>>();
            foreach (var deviceId in assetsAttributesDict.Keys)
            {
                var assetsAttributes = assetsAttributesDict[deviceId];
                var result = new List<AttributeSnapshot>();
                var notfoundAttributeIds = new List<Guid>();
                var attributeIds = assetsAttributes.Select(i => i.AttributeId).ToArray();

                foreach (var attribute in assetsAttributes)
                {

                    // step 1. 
                    //var assetAttributeRelevantToDeviceIdKey = $"{projectId}_processing_device_{deviceId}_asset_related_runtime_trigger_with_metric"; //old hash key
                    var redisKey = string.Format(IngestionRedisCacheKeys.AssetAttributeRuntimeSnapshotsPattern, projectId, attribute.AssetId);

                    // step 2. load from Memcache


                    // step 3. load from redisCache
                    var runtimeAssetTriggers = await _cache.HashGetAsync<AttributeSnapshot>(redisKey, attribute.AttributeId.ToString());
                    if (runtimeAssetTriggers != null)
                    {
                        result.Add(runtimeAssetTriggers);
                        continue;
                    }
                    //else set to notfound then query from db
                    notfoundAttributeIds.Add(attribute.AttributeId);
                }

                if (!notfoundAttributeIds.Any())
                {
                    dictResult.Add(deviceId, result);
                    continue;
                }

                //step 4. get from db
                IEnumerable<AttributeSnapshot> attributeSnapshots;
                using (var dbConnection = GetDbConnection(projectId))
                {
                    attributeSnapshots = await dbConnection.QueryAsync<AttributeSnapshot>(@"select attribute_id as AttributeId
                                    , value as Value
                                    , _ts as Timestamp
                                    , data_type as DataType
                                    from v_asset_attribute_snapshots
                                    where attribute_id = ANY(@AttributeIds)",
                        new { AttributeIds = notfoundAttributeIds }, commandTimeout: 10000);
                }

                // step 3.1 because get batch, so now must to group each by attribute_id
                var attributeSnapshotsGroups = attributeSnapshots.GroupBy(i => i.AttributeId).Select(g => new { AttributeId = g.Key, Snapshots = g.ToArray() });

                foreach (var group in attributeSnapshotsGroups)
                {
                    var redisKey = string.Format(IngestionRedisCacheKeys.AssetAttributeRuntimeSnapshotsPattern, projectId, group.AttributeId);

                    //step 4. set memcache

                    //step 5. set redis cache
                    await _cache.HashSetAsync<AttributeSnapshot>(redisKey, group.AttributeId.ToString(), group.Snapshots.OrderByDescending(i => i.Timestamp).FirstOrDefault());

                    //step 6. append to result list
                    result.AddRange(group.Snapshots);
                }

                dictResult.Add(deviceId, result);
            }

            return dictResult;
        }


        public async Task<IEnumerable<(Guid, Guid)>> GetProjectAttributeAliasMappingAsync(string projectId, IEnumerable<AssetAttribute> assetAttributes, IEnumerable<AssetRuntimeTrigger> assetRuntimeTriggers)
        {
            var assetIds = assetRuntimeTriggers.Select(x => x.AssetId).ToList();
            var aliasMapping = new List<(Guid, Guid)>();
            var notfoundAlias = new List<Guid>();
            foreach (var alias in assetAttributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_ALIAS))
            {
                //var aliasFailedKey = $"{projectId}_processing_asset_{alias.AssetId}_attribute_{alias.AttributeId}_failed"; // old key
                //var aliasKey = $"{_podName}_{projectId}_processing_asset_{alias.AssetId}_attribute_{alias.AttributeId}"; //old key
                var target = await GetProjectTargetAttributeIdsAsync(projectId, alias.AttributeId);
                if (target.target != Guid.Empty && target.source != Guid.Empty)
                    aliasMapping.Add(target);
                else
                {
                    //_logger.LogError("Ingestion_Error Not found alias for alias {alias}", alias.AttributeId);

                    notfoundAlias.Add(alias.AttributeId);
                }
            }

            if (notfoundAlias.Count > 0)
            {
                using (var dbConnection = GetDbConnection(projectId))
                {
                    var redisKey = string.Format(IngestionRedisCacheKeys.AliasAttributeMappingPattern, projectId);
                    foreach (var alias in notfoundAlias)
                    {
                        var targetAliasAttributeId = await dbConnection.QuerySingleOrDefaultAsync<Guid>("select attribute_id from find_root_alias_asset_attribute(@AliasAttributeId) order by alias_level desc limit 1", new { AliasAttributeId = alias }, commandTimeout: 2);
                        if(targetAliasAttributeId == Guid.Empty)
                        {
                            //_logger.LogError("Ingestion_Error Not found alias for alias {alias}", alias.AttributeId);
                            continue;
                        }
                        await _cache.HashSetAsync<Guid>(redisKey, alias.ToString(), targetAliasAttributeId);
                        aliasMapping.Add((targetAliasAttributeId, alias));
                    }
                }
            }
            return aliasMapping.Distinct();
        }

        public async Task<(Guid target, Guid source)> GetProjectTargetAttributeIdsAsync(string projectId, Guid aliasAttributeId)
        {
            var redisKey = string.Format(IngestionRedisCacheKeys.AliasAttributeMappingPattern, projectId);
            //step 1. get from cache

            //step 2. get from redis
            var targetId = await _cache.HashGetAsync<Guid>(redisKey, aliasAttributeId.ToString());
            if (targetId != Guid.Empty)
            {
                return (targetId, aliasAttributeId);
            }

            //step 3. get from db
            using (var dbConnection = GetDbConnection(projectId))
            {
                targetId = await dbConnection.QuerySingleOrDefaultAsync<Guid>(
                   @"select attribute_id
                from find_root_alias_asset_attribute(@AliasAttributeId)
                order by alias_level desc
                limit 1", new { AliasAttributeId = aliasAttributeId }, commandTimeout: 10000);
            }


            //step 4. set to memcache

            //step 5. set to redis
            await _cache.HashSetAsync<Guid>(redisKey, aliasAttributeId.ToString(), targetId);
            return (targetId, aliasAttributeId);
        }


        public async Task<IEnumerable<(Guid, Guid)>> GetProjectAttributeAliasAsync(string projectId, IEnumerable<AssetAttribute> assetAttributes)
        {
            var aliasMapping = new List<(Guid, Guid)>();
            var notfoundIds = new List<AssetAttribute>();
            var attributesTypeAlias = assetAttributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_ALIAS);
            foreach (var alias in attributesTypeAlias)
            {
                //var aliasFailedKey = $"{projectId}_processing_asset_{alias.AssetId}_attribute_{alias.AttributeId}_failed"; //old key
                //var aliasKey = $"{_podName}_{projectId}_processing_asset_{alias.AssetId}_attribute_{alias.AttributeId}";//old key

                //key of memcache contains hashfield, redis isn't
                var aliasRedisKey = string.Format(IngestionRedisCacheKeys.AttributeAliasPattern, projectId, alias.AssetId);
                //var aliasMemCacheKey = $"{aliasRedisKey}:{alias.AttributeId}";

                //step 1. get from memoryCache BY memoryCacheKey


                //step 2. get from redis
                var targetAliasAttributeIdString = await _cache.HashGetAsync<string>(aliasRedisKey, alias.AttributeId.ToString());
                if (!string.IsNullOrEmpty(targetAliasAttributeIdString))
                {
                    aliasMapping.Add((Guid.Parse(targetAliasAttributeIdString), alias.AttributeId));
                    continue;
                }

                notfoundIds.Add(alias);
            }

            if (!notfoundIds.Any())
            {
                return aliasMapping;
            }

            //step 3. get from database
            using (var dbConnection = GetDbConnection(projectId))
            {
                foreach (var alias in notfoundIds)
                {
                    var aliasRedisKey = string.Format(IngestionRedisCacheKeys.AttributeAliasPattern, projectId, alias.AssetId);
                    var aliasKey = $"{aliasRedisKey}:{alias.AttributeId}";

                    var targetAliasAttributeId = await dbConnection.QuerySingleOrDefaultAsync<Guid>(
                    "select attribute_id from find_root_alias_asset_attribute(@AliasAttributeId) order by alias_level desc limit 1",
                    new { AliasAttributeId = alias.AttributeId }, commandTimeout: 200);
                    var targetAliasAttributeIdString = targetAliasAttributeId.ToString();

                    //step 4. save to memoryCache

                    //step 5. save to redis
                    await _cache.HashSetAsync<string>(aliasRedisKey, alias.AttributeId.ToString(), targetAliasAttributeIdString);

                    aliasMapping.Add((Guid.Parse(targetAliasAttributeIdString), alias.AttributeId));
                }
            }

            return aliasMapping;
        }


        public async Task LoadAllNecessaryResourcesAsync(string projectId)
        {
            var hasMigrated = await _cache.GetAsync<bool?>("LoadAllNecessaryResourcesAsync_Triggered");
            if (hasMigrated != null && hasMigrated == true)
            {
                _logger.LogInformation("All Cache are existing projectId: {projectId}.", projectId);
                return;
            }
            _logger.LogInformation("Not found any cache, start migration projectId: {projectId}", projectId);
            var watch = System.Diagnostics.Stopwatch.StartNew();

            var watch2 = System.Diagnostics.Stopwatch.StartNew();
            var devices = await GetProjectDevicesAsync(projectId, true);
            watch2.Stop();
            _logger.LogInformation($"GetProjectDevicesAsync finished, took :{watch2.ElapsedMilliseconds} ms");

            watch2 = System.Diagnostics.Stopwatch.StartNew();
            //TODO: only get first 1000 devices for testing, instead of all, due to perforcement issues
            var deviceIds = devices.Select(i => i.DeviceId).Take(_batchOptions.PreloadData.LoadDeviceInformations).ToArray();
            var assetRuntimeTriggersDict = await GetProjectAssetRuntimeTriggersAsync(projectId, deviceIds, true);
            watch2.Stop();
            _logger.LogInformation($"GetProjectAssetRuntimeTriggersAsync finished, size {assetRuntimeTriggersDict.Count()} took :{watch2.ElapsedMilliseconds} ms");

            System.Diagnostics.Stopwatch.StartNew();
            var assetAttributesDict = await GetProjectAssetAttributesAsync(projectId, assetRuntimeTriggersDict);
            watch2.Stop();
            _logger.LogInformation($"GetProjectAssetAttributesAsync finished, took :{watch2.ElapsedMilliseconds} ms");

            foreach (var deviceId in assetAttributesDict.Keys)
            {
                var assetAttributes = assetAttributesDict[deviceId];
                var assetRuntimeTriggers = assetRuntimeTriggersDict[deviceId];
                var runtimeAttributes = (from attribute in assetAttributes
                                         join trigger in assetRuntimeTriggers on new { attribute.AssetId, attribute.AttributeId, attribute.TriggerAssetId, attribute.TriggerAttributeId }
                                         equals new { trigger.AssetId, trigger.AttributeId, trigger.TriggerAssetId, trigger.TriggerAttributeId }
                                         where attribute.AttributeType == AttributeTypeConstants.TYPE_RUNTIME && attribute.EnabledExpression == true
                                         select attribute).Distinct();

                var aliasMapping = await GetProjectAttributeAliasAsync(projectId, assetAttributes);
            }
            watch2.Stop();
            _logger.LogInformation($"GetProjectAttributeAliasAsync finished, took :{watch2.ElapsedMilliseconds} ms");

            watch.Stop();
            _logger.LogInformation($"LoadAllNecessaryResourcesAsync took :{watch.ElapsedMilliseconds} ms");
            _logger.LogInformation("All Cache has been loaded to memory and redis.");
        }

        public async Task<IEnumerable<AssetAttribute>> GetAssetAttributesAsync(string projectId, string deviceId, IEnumerable<AssetRuntimeTrigger> triggers)
        {
            var assetIds = triggers.Select(x => x.AssetId).ToList();
            //TODO: this list is very long, need to redefined.
            //var assetAttributeKey = $"{_podName}_{projectId}_processing_assets_ids_{string.Join(',', assetIds)}_attributes_trigger"; // OLD

            var assetAttributeRedisKey = string.Format(IngestionRedisCacheKeys.AssetAttributesPattern, projectId);//hash field = AssetId, hash value = assetAttributes
            var notfoundAttributes = new List<Guid>();// to query 1 times from database for missing asset
            var result = new List<AssetAttribute>();
            foreach (var assetId in assetIds)
            {
                //step 2. get from redis cache
                var att = await _cache.HashGetAsync<IEnumerable<AssetAttribute>>(assetAttributeRedisKey, assetId.ToString());
                if (att != null)
                    result.AddRange(att);
                else
                    notfoundAttributes.Add(assetId);
            }

            if (!notfoundAttributes.Any())
            {
                return result;
            }

            IEnumerable<AssetAttribute> assetAttributes;
            //step 3. get from database for all missing assets
            using (var dbConnection = GetDbConnection(projectId))
            {
                assetAttributes = await dbConnection.QueryAsync<AssetAttribute>(@"select
                                                                                aa.asset_id as AssetId
                                                                                , aa.attribute_id as AttributeId
                                                                                , aa.data_type as DataType
                                                                                , aa.attribute_type as AttributeType
                                                                                , aa.expression_compile as Expression
                                                                                , aa.enabled_expression as EnabledExpression
                                                                                , aart.trigger_asset_id as TriggerAssetId
                                                                                , aart.trigger_attribute_id as TriggerAttributeId
                                                                            from v_asset_attributes aa
                                                                            left join asset_attribute_runtime_triggers aart on aart.asset_id = aa.asset_id and aart.attribute_id = aa.attribute_id
                                                                            where aa.asset_id = ANY(@AssetIds)
                                                                            order by aa.created_utc, aa.sequential_number", new { AssetIds = notfoundAttributes.ToArray() });
            }


            if (!(assetAttributes != null))
            {
                _logger.LogError("Ingestion_Error Not found any asset Attributes for ids: ", notfoundAttributes);
                return result;
            }

            // step 3.1 because get batch, so now must to group each by assetId
            var assetAttributesGroups = assetAttributes.GroupBy(i => i.AssetId).Select(g => new { AssetId = g.Key, Attributes = g.ToArray() });
            foreach (var group in assetAttributesGroups)
            {
                //var assetAttributeMemoryKey = $"{assetAttributeRedisKey}:{group.AssetId}";//hash field = AssetId, hash value = assetAttributes

                //step 4. set memcache

                //step 5. set redis cache
                await _cache.HashSetAsync<IEnumerable<AssetAttribute>>(assetAttributeRedisKey, group.AssetId.ToString(), group.Attributes);

                //step 6. append to result list
                result.AddRange(group.Attributes);
            }

            return assetAttributes;
        }
    }

    public class MetricSerieDto
    {
        public DateTime Timestamp { get; set; }
        public string DeviceId { get; set; }
        public string MetricKey { get; set; }
        public double Value { get; set; }
        public int RetentionDays { get; set; }
    }

    public class MetricSerieTextDto
    {
        public DateTime Timestamp { get; set; }
        public string DeviceId { get; set; }
        public string MetricKey { get; set; }
        public string Value { get; set; }
        public int RetentionDays { get; set; }
    }
}
