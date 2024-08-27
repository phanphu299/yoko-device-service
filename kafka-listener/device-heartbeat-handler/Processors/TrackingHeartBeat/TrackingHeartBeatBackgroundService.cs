using System;
using System.Threading.Tasks;
using Confluent.Kafka;
using Device.Consumer.KraftShared.Abstraction;
using Device.Heartbeat.Handler.Processor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Device.Consumer.KraftShared.Model;
using StackExchange.Redis.Extensions.Core.Abstractions;
using Device.Consumer.KraftShared.Models;
using System.Linq;
using System.Text.Json;

namespace Device.Heartbeat.Handler.Processors.TrackingHeartBeat
{
    public sealed class TrackingHeartBeatBackgroundService : OrderingConsumerBackgroundService
    {
        private readonly TrackingHeartBeatProcessor _trackingHeartBeatProcessor;
        public TrackingHeartBeatBackgroundService(
            ILogger<TrackingHeartBeatBackgroundService> logger,
            ChannelProvider<string, byte[]> channelProvider,
            KafkaPartitionsHandler<string, byte[]> kafkaHandler,
            IConfiguration configuration,
            IRedisDatabase cache,
            ILockFactory lockFactory,
            TrackingHeartBeatProcessor processor
            ) : base(logger, channelProvider, kafkaHandler, configuration, cache, lockFactory)
        {
            _trackingHeartBeatProcessor = processor;
        }

        public override string TopicName => _kafkaOption?.Consumer?.IngestionTopicName ?? "snapshot.metrics.sync";

        public override string GroupId => $"heartbeat_group_{TopicName}";

        public override async Task HandleMultipleMessages(TopicPartition topicPartitionKey)
        {
            //dequeue all
            var splock = _semaphoreSlims.GetOrAdd(topicPartitionKey, _lockFactory.Create(topicPartitionKey.GetHashCode().ToString()));
            await splock.WaitAsync();
            var messages = _projectMessageQueue[topicPartitionKey].ToArray();
            PauseTopicPartitions([topicPartitionKey]);
            _projectMessageQueue[topicPartitionKey].Clear();
            splock.Release();

            // calculate messages
            var extractMessages = messages.Select(e => JsonSerializer.Deserialize<RedisSyncDbMessage>(e.Message.Value, _jsonSerializerOptions))
                                          .Where(i => i != null);
            if (!extractMessages.Any())
            {
                _logger.LogDebug("TopicPartition {topicPartitionKey} has no data to sync, ignore", topicPartitionKey);
                ResumeTopicPartitions([topicPartitionKey]);
                return;
            }
            var groups = extractMessages.Where(x => x.SnapshotType == Consumer.KraftShared.Enums.SnapshotEnum.DeviceMetricSnapshot).GroupBy(x =>
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

            var tasks = groups.Select(x => _trackingHeartBeatProcessor.ProcessAsync(x.TenantId, x.SubscriptionId, x.ProjectId, x.Messages, default));

            await Task.WhenAll(tasks);
            ResumeTopicPartitions([topicPartitionKey]);
        }
    }
}
