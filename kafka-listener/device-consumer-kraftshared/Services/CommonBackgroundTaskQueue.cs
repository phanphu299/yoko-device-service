using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Device.Consumer.KraftShared.Constants;
using Device.Consumer.KraftShared.Enums;
using Device.Consumer.KraftShared.Model;
using Device.Consumer.KraftShared.Models.MetricModel;
using Device.Consumer.KraftShared.Models.Options;
using Device.Consumer.KraftShared.Service.Abstraction;
using Device.Consumer.KraftShared.Services.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pipelines.Sockets.Unofficial.Arenas;
using StackExchange.Redis.Extensions.Core.Abstractions;


namespace Device.Consumer.KraftShared.Services
{
    public class CommonBackgroundTaskQueue : IBackgroundTaskQueue, IDisposable
    {
        private readonly BatchProcessingOptions _batchOptions;
        private readonly IRedisDatabase _cache;
        private readonly IPublisher _publisher;
        private readonly KafkaOption _kafkaOption;
        private readonly ILogger<CommonBackgroundTaskQueue> _logger;
        public CommonBackgroundTaskQueue(
            IOptions<BatchProcessingOptions> options,
            IRedisDatabase cache,
            IPublisher publisher,
            IOptions<KafkaOption> kafkaOptions,
            ILogger<CommonBackgroundTaskQueue> logger)
        {
            _batchOptions = options.Value;
            _cache = cache;
            _logger = logger;
            _publisher = publisher;
            _kafkaOption = kafkaOptions.Value;
        }

        public void ExecuteRedisHashSetFireAndForgetAsync<T>(string redisKey, string hashField, T data)
        {
            Task.Run(() => _cache.HashSetAsync<T>(redisKey, hashField, data, flag: StackExchange.Redis.CommandFlags.FireAndForget));
        }

        public void ExecuteRedisHashSetDictionaryFireAndForgetAsync<T>(string redisKey, IDictionary<string, T> data)
        {
            Task.Run(() => _cache.HashSetAsync<T>(redisKey, data, StackExchange.Redis.CommandFlags.FireAndForget));
        }

        public void ExecuteStoreDeviceSnapshotBackgroundAsync(string tenantId, string subscriptionId, string projectId, IEnumerable<MetricValue> snapshotMetricsInput)
        {
            Task.Run(() => HandleDeviceSnapshotSync(tenantId, subscriptionId, projectId, snapshotMetricsInput));
        }

        public void ExecuteStoreAttributeRuntimeSnapshotBackgroundAsync(string tenantId, string subscriptionId, string projectId, IEnumerable<RuntimeValueObject> runtimeValues)
        {
            Task.Run(() => HandleAttributeSnapshotSync(tenantId, subscriptionId, projectId, runtimeValues));
        }

        private async ValueTask HandleDeviceSnapshotSync(string tenantId, string subscriptionId, string projectId, IEnumerable<MetricValue> snapshotMetricsInput)
        {
            try
            {
                var watcher = Stopwatch.StartNew();
                _logger.LogInformation($"*** Begin HandleDeviceSnapshotSync to Redis project: {projectId} length {snapshotMetricsInput.Count()} ***");
                var updateSnapshotMetrics = snapshotMetricsInput.GroupBy(item => item.DeviceId).Select(g => new { deviceId = g.Key, snapshots = g.ToArray() });
                _logger.LogInformation($"*** HandleDeviceSnapshotSync to Redis project: {projectId} group devices length {updateSnapshotMetrics.Count()} ***");
                var batches = updateSnapshotMetrics.Chunk(_batchOptions.RedisMaxChunkSize);
                var redisKeyPatternDatas = new ConcurrentBag<string>();
                foreach (var batch in batches)
                {
                    var tasks = batch.Select(gr => Task.Run(async () =>
                    {
                        var redisKey = string.Format(IngestionRedisCacheKeys.DeviceMetricSnapshotsPattern, projectId, gr.deviceId);
                        IDictionary<string, MetricValue> oldSnapshots = await _cache.HashGetAllAsync<MetricValue>(redisKey);
                        var upsertSnapshots = new Dictionary<string, MetricValue>();
                        foreach (var snapshot in gr.snapshots)
                        {
                            if (oldSnapshots != null)
                            {
                                oldSnapshots.TryGetValue(snapshot.MetricKey, out var oldSnapshot);
                                if (oldSnapshot != null && oldSnapshot.Timestamp > snapshot.Timestamp)
                                    continue;
                            }
                            upsertSnapshots[snapshot.MetricKey] = snapshot;
                        }
                        if (upsertSnapshots.Keys.Count > 0)
                        {
                            await _cache.HashSetAsync(redisKey, upsertSnapshots, StackExchange.Redis.CommandFlags.FireAndForget);
                            redisKeyPatternDatas.Add(gr.deviceId);
                        }
                    }));

                    await Task.WhenAll(tasks);
                }
                if (!redisKeyPatternDatas.IsEmpty)
                    await _publisher.SendAsync(new RedisSyncDbMessage
                    {
                        TenantId = tenantId,
                        SubscriptionId = subscriptionId,
                        ProjectId = projectId,
                        Data = redisKeyPatternDatas,
                        SnapshotType = SnapshotEnum.DeviceMetricSnapshot
                    }, _kafkaOption.Producer.TopicName, projectId);
                watcher.Stop();
                _logger.LogInformation($"*** End HandleDeviceSnapshotSync to Redis project: {projectId} - {updateSnapshotMetrics.Count()} records tooks {watcher.ElapsedMilliseconds} ms ***");
            }
            catch (Exception ex)
            {
                _logger.LogError("Ingestion_Error HandleDeviceSnapshotSync failed ex: {ex}", ex);
            }
        }

        private async ValueTask HandleAttributeSnapshotSync(string tenantId, string subscriptionId, string projectId, IEnumerable<RuntimeValueObject> runtimeValues)
        {
            try
            {
                _logger.LogInformation($"#### HandleAttributeSnapshotSync begin projectId {projectId} -- {runtimeValues.Count()} records");
                var watch = Stopwatch.StartNew();
                var groupAssetRuntimes = runtimeValues.OrderBy(i => i.Timestamp)
                                                      .GroupBy(g => g.AssetId);
                var redisKeyPatternDatas = new List<string>();
                foreach (var assetsRuntimes in groupAssetRuntimes)
                {
                    _logger.LogInformation($"#### HandleAttributeSnapshotSync begin with asset: {assetsRuntimes.Key}");

                    var redisKey = string.Format(IngestionRedisCacheKeys.AssetAttributeRuntimeSnapshotsPattern, projectId, assetsRuntimes.Key.ToString());
                    var oldRuntimes = await _cache.HashGetAllAsync<RuntimeValueObject>(redisKey);
                    var upsertRuntimes = new Dictionary<string, RuntimeValueObject>();
                    foreach (var item in assetsRuntimes)
                    {
                        oldRuntimes.TryGetValue(item.AttributeId.ToString(), out var oldSnapshot);
                        if (oldSnapshot != null && oldSnapshot.Timestamp > item.Timestamp)
                        {
                            _logger.LogDebug("old snapshot: {data}", System.Text.Json.JsonSerializer.Serialize(oldSnapshot));
                            continue;
                        }
                        upsertRuntimes[item.AttributeId.ToString()] = item;

                        _logger.LogDebug("HandleAttributeSnapshotSync logged items: {redisKey} -- jsonData ={json}", redisKey, item);
                    }
                    if (upsertRuntimes.Keys.Count > 0)
                    {
                        await _cache.HashSetAsync(redisKey, upsertRuntimes, StackExchange.Redis.CommandFlags.FireAndForget);
                        redisKeyPatternDatas.Add(assetsRuntimes.Key.ToString());
                    }
                }
                if (redisKeyPatternDatas.Count > 0)
                    await _publisher.SendAsync(new RedisSyncDbMessage
                    {
                        TenantId = tenantId,
                        SubscriptionId = subscriptionId,
                        ProjectId = projectId,
                        Data = redisKeyPatternDatas,
                        SnapshotType = Enums.SnapshotEnum.RuntimeAttributeSnapshot,
                    }, _kafkaOption.Producer.TopicName, projectId);

                watch.Stop();
                _logger.LogInformation($"#### HandleAttributeSnapshotSync finish {runtimeValues.Count()} records took: {watch.ElapsedMilliseconds} ms");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ingestion_Error HandleAttributeSnapshotSync {ex.Message}");
            }
        }

        private void ClearResources()
        {
            GC.Collect();
        }
        public void Dispose()
        {
            ClearResources();
        }
        ~CommonBackgroundTaskQueue()
        {
            ClearResources();
        }
    }
}
