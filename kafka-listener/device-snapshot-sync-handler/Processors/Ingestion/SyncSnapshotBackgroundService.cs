using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Device.Consumer.KraftShared.Constants;
using Device.Consumer.KraftShared.Extensions;
using Device.Consumer.KraftShared.Model;
using Device.Consumer.KraftShared.Models;
using Device.Consumer.KraftShared.Models.MetricModel;
using Device.Consumer.KraftShared.Repositories.Abstraction;
using Device.Consumer.KraftShared.Service.Abstraction;
using Device.Consumer.KraftShared.Abstraction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Device.Consumer.SnapshotSyncHandler.Processor
{
    public class SyncSnapshotBackgroundService : OrderingConsumerBackgroundService
    {
        private readonly IDbConnectionResolver _dbCnnResolver;
        protected readonly IFowardingNotificationService _forwardingNotificationService;
        public override string TopicName => _kafkaOption.Consumer.IngestionTopicName ?? "snapshot.metrics.sync";

        public override string GroupId => $"sync_group_{TopicName}";

        public SyncSnapshotBackgroundService(
            ILogger<OrderingConsumerBackgroundService> logger,
            ChannelProvider<string, byte[]> channelProvider,
            KafkaPartitionsHandler<string, byte[]> kafkaHandler,
            IConfiguration configuration
            , IRedisDatabase cache
            , ILockFactory lockFactory
            , IFowardingNotificationService forwardingNotificationService,
            IDbConnectionResolver dbCnnResolver
            ) : base(logger, channelProvider, kafkaHandler, configuration, cache, lockFactory)
        {
            _dbCnnResolver = dbCnnResolver;
            _forwardingNotificationService = forwardingNotificationService;
        }

        #region separate partition

        public override async Task HandleMultipleMessages(TopicPartition topicPartitionKey)
        {
            try
            {
                if (!_projectMessageQueue.ContainsKey(topicPartitionKey))
                    return;
                    
                //dequeue all
                var splock = _semaphoreSlims.GetOrAdd(topicPartitionKey, _lockFactory.Create(topicPartitionKey.GetHashCode().ToString()));
                await splock.WaitAsync();
                var messages = _projectMessageQueue[topicPartitionKey].ToArray();
                PauseTopicPartitions([_topicPartitionOffset[topicPartitionKey].TopicPartition]);
                _projectMessageQueue[topicPartitionKey].Clear();
                splock.Release();

                // calculate messages
                var extractMessages = messages.Select(e => JsonSerializer.Deserialize<RedisSyncDbMessage>(e.Message.Value, _jsonSerializerOptions))
                                              .Where(i => i != null);
                if (extractMessages.Count() == 0)
                {
                    _logger.LogDebug("TopicPartition {topicPartitionKey} has no data to sync, ignore", topicPartitionKey);
                    ResumeTopicPartitions([topicPartitionKey]);
                    return;
                }
                var groups = extractMessages.GroupBy(x =>
                new
                {
                    x.TenantId,
                    x.SubscriptionId,
                    x.ProjectId,
                    x.SnapshotType
                }).Select(g => new
                {
                    g.Key.TenantId,
                    g.Key.SubscriptionId,
                    g.Key.ProjectId,
                    g.Key.SnapshotType,
                    Messages = g.SelectMany(x => x.Data).Distinct()
                });

                IEnumerable<Task> tasks = groups.Select(x => x.SnapshotType == KraftShared.Enums.SnapshotEnum.DeviceMetricSnapshot
                ? SyncDeviceMetricSnapshotAsync(x.TenantId, x.SubscriptionId, x.ProjectId, x.Messages, default)
                : SyncAttributeRuntimeSnapshotAsync(x.TenantId, x.SubscriptionId, x.ProjectId, x.Messages, default));

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError("Ingestion_Error HandleMultipleMessages failed tp: {topicPartitionKey} --- ex: {ex}", topicPartitionKey, ex);
            }
            finally
            {
                ResumeTopicPartitions([topicPartitionKey]);
            }
        }

        private async Task SyncAttributeRuntimeSnapshotAsync(string tenantId, string subscriptionId, string projectId, IEnumerable<string> redisKeyPatternDatas, CancellationToken token)
        {
            try
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                //get all runtime values
                var attributeRuntimeValues = new List<RuntimeValueObject>();
                var redisKeyDistint = redisKeyPatternDatas.Distinct().ToArray();
                foreach (var assetId in redisKeyDistint)
                {
                    var redisKey = string.Format(IngestionRedisCacheKeys.AssetAttributeRuntimeSnapshotsPattern, projectId, assetId);
                    var allAttributesRuntimeValues = await _cache.HashGetAllAsync<RuntimeValueObject>(redisKey);
                    foreach (var att in allAttributesRuntimeValues)
                    {
                        attributeRuntimeValues.Add(att.Value);
                    }
                }
                var attributeForSync = attributeRuntimeValues
                    .Where(i => i.AttributeId != Guid.Empty && i.AssetId != Guid.Empty)
                    .OrderByDescending(i => i.Timestamp)
                    .DistinctBy(i => new { i.AssetId, i.AttributeId });
                var assetHasChanges = new List<Guid>();
                using (var dbConnection = _dbCnnResolver.CreateConnection(projectId))
                {
                    if (dbConnection.State != ConnectionState.Open)
                        dbConnection.Open();
                    var writeToSTDIN = async (NpgsqlBinaryImporter writer) =>
                    {
                        foreach (var item in attributeForSync)
                        {
                            try
                            {
                                if (string.IsNullOrEmpty(item.Value?.ToString()))
                                    continue;
                                await writer.StartRowAsync();
                                await writer.WriteAsync(item.Timestamp, NpgsqlDbType.Timestamp);
                                await writer.WriteAsync(item.AssetId, NpgsqlDbType.Uuid);
                                await writer.WriteAsync(item.AttributeId, NpgsqlDbType.Uuid);
                                await writer.WriteAsync(item.Value is string ? (string)item.Value! : item.Value!.ToString(), NpgsqlDbType.Text);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Ingestion_Error SyncAttributeRuntimeSnapshotAsync writeToSTDIN exception projectId: {projectId}, item: {System.Text.Json.JsonSerializer.Serialize(item)}, ex {ex.ToString()}");
                            }
                        }
                    };

                    (var affected, var err) = await dbConnection.BulkUpsertAsync(
                        "asset_attribute_runtime_snapshots",
                        "(_ts, asset_id, asset_attribute_id, value)",
                        "(asset_id, asset_attribute_id)",
                        "UPDATE SET _ts = EXCLUDED._ts, value = EXCLUDED.value WHERE asset_attribute_runtime_snapshots._ts < EXCLUDED._ts",
                        writeToSTDIN, _logger);
                    await dbConnection.CloseAsync();
                    if (!string.IsNullOrEmpty(err))
                        _logger.LogError("Ingestion_Error SyncAttributeRuntimeSnapshotAsync failure: ex={err}", err);
                    else
                        _logger.LogInformation($"SyncAttributeRuntimeSnapshotAsync {affected}/{attributeForSync.Count()} records has been upserted successfully");

                    if (affected > 0)
                    {
                        _ = Task.Run(async () => await _forwardingNotificationService.SendAssetNotificationMessageAsync(tenantId, subscriptionId, projectId, attributeForSync.Select(i => i.AssetId)), token);
                    }
                }
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                _logger.LogInformation($"#### SyncAttributeRuntimeSnapshotAsync finish {attributeRuntimeValues.Count} records  took: {watch.ElapsedMilliseconds} ms");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ingestion_Error SyncAttributeRuntimeSnapshotAsync failed {ex.Message}");
            }
        }

        private async Task SyncDeviceMetricSnapshotAsync(string tenantId, string subscriptionId, string projectId, IEnumerable<string> redisKeyPatternDatas, CancellationToken token)
        {
            try
            {
                if (!redisKeyPatternDatas.Any())
                    return;
                var redisKeyDistint = redisKeyPatternDatas.Distinct();
                var watch = System.Diagnostics.Stopwatch.StartNew();
                //get all runtime values
                var metricValues = new List<MetricValue>();
                foreach (var deviceId in redisKeyDistint)
                {
                    var redisKey = string.Format(IngestionRedisCacheKeys.DeviceMetricSnapshotsPattern, projectId, deviceId);
                    var allMetricValues = await _cache.HashGetAllAsync<MetricValue>(redisKey);
                    foreach (var mt in allMetricValues)
                    {
                        metricValues.Add(mt.Value);
                    }
                }
                using (var dbConnection = _dbCnnResolver.CreateConnection(projectId))
                {
                    if (dbConnection.State != ConnectionState.Open)
                        await dbConnection.OpenAsync();
                    var writeToSTDIN = async (NpgsqlBinaryImporter writer) =>
                    {
                        foreach (var snapshot in metricValues)
                        {
                            try
                            {
                                if (string.IsNullOrEmpty(snapshot.Value?.ToString()))
                                    continue;
                                await writer.StartRowAsync();
                                await writer.WriteAsync(snapshot.Timestamp, NpgsqlDbType.Timestamp);
                                await writer.WriteAsync(snapshot.DeviceId, NpgsqlDbType.Varchar);
                                await writer.WriteAsync(snapshot.MetricKey, NpgsqlDbType.Varchar);
                                await writer.WriteAsync(snapshot.Value is string ? (string)snapshot.Value! : snapshot.Value!.ToString(), NpgsqlDbType.Text);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Ingestion_Error SyncDeviceMetricSnapshotAsync writeToSTDIN exception projectId: {projectId}, snapshot: {System.Text.Json.JsonSerializer.Serialize(snapshot)} , ex {ex.ToString()}");
                            }
                        }
                    };
                    (var affected, var err) = await dbConnection.BulkUpsertAsync(
                        "device_metric_snapshots",
                        "(_ts, device_id, metric_key, value)",
                        "(device_id, metric_key)",
                        "UPDATE SET _ts = EXCLUDED._ts, value = EXCLUDED.value WHERE device_metric_snapshots._ts < EXCLUDED._ts;",
                        writeToSTDIN, _logger);
                    if (!string.IsNullOrEmpty(err))
                    {
                        _logger.LogError("Ingestion_Error Error during SyncDeviceMetricSnapshotAsync.BulkUpsertAsync: {err}", err);
                    }
                    else
                    {
                        _logger.LogInformation("SyncDeviceMetricSnapshotAsync Inserted snashot with record effect: {affected}/{data} records", affected, metricValues.Count);
                    }
                    if (affected > 0)
                    {
                        _ = Task.Run(async () => await _forwardingNotificationService.SendAssetNotificationMessageAsync(tenantId, subscriptionId, projectId, redisKeyDistint), token);
                    }
                }
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                _logger.LogInformation($"#### SyncDeviceMetricSnapshotAsync finish  {metricValues.Count} records   took: {watch.ElapsedMilliseconds} ms");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ingestion_Error SyncDeviceMetricSnapshotAsync failed {ex.Message}");
            }
        }

        #endregion
    }
}
