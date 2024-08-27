using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Confluent.Kafka;
using Device.Consumer.KraftShared.Helpers;
using Device.Consumer.KraftShared.Model;
using Device.Consumer.KraftShared.Models;
using Device.Consumer.KraftShared.Models.Options;
using Device.Consumer.KraftShared.Service.Abstraction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Device.Consumer.KraftShared.Abstraction
{
    public abstract class OrderingConsumerBackgroundService : BaseBackgroundService<string, byte[]>
    {
        protected readonly ConcurrentDictionary<TopicPartition, bool> _topicPartitionHistory;
        protected readonly ILogger<OrderingConsumerBackgroundService> _logger;
        /// <summary>
        /// key pattern = "topic_partition"
        /// </summary>
        protected readonly ConcurrentDictionary<TopicPartition, TopicPartitionOffset> _topicPartitionOffset;
        protected readonly ConcurrentDictionary<TopicPartition, ConcurrentQueue<ConsumeResult<string, byte[]>>> _projectMessageQueue;
        protected readonly KafkaOption _kafkaOption;
        protected readonly BatchProcessingOptions _batchOptions;
        protected readonly ParallelOptions _parallelOptions;
        protected readonly IRedisDatabase _cache;
        protected readonly ILockFactory _lockFactory;
        protected readonly ConcurrentDictionary<TopicPartition, ILock> _semaphoreSlims;
        protected readonly System.Timers.Timer _autoProcessTimer;
        protected bool _inprocessingPeriodically;
        protected JsonSerializerOptions _jsonSerializerOptions = new()
        {
            Converters = { new EmptyStringConverter() },
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public OrderingConsumerBackgroundService(
            ILogger<OrderingConsumerBackgroundService> logger,
            ChannelProvider<string, byte[]> channelProvider,
            KafkaPartitionsHandler<string, byte[]> kafkaHandler,
            IConfiguration configuration
            , IRedisDatabase cache
            , ILockFactory lockFactory
            ) : base(logger, channelProvider, kafkaHandler, configuration)
        {
            _logger = logger;
            _kafkaOption = configuration.GetSection("Kafka").Get<KafkaOption>();
            _batchOptions = configuration.GetSection("BatchProcessing").Get<BatchProcessingOptions>();
            _semaphoreSlims = new();
            _lockFactory = lockFactory;
            _projectMessageQueue = new();
            _topicPartitionOffset = new();
            _topicPartitionHistory = new();
            _cache = cache;
            _parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = _batchOptions.MaxWorker };

            if (_kafkaOption != null && _kafkaOption.TimerTriggerEnabled)
            {
                _autoProcessTimer = new System.Timers.Timer(_batchOptions.AutoCommitInterval);
                _autoProcessTimer.Elapsed += async (sender, e) => await HandleMessagePeriodicallyAsync(sender, e);
                _autoProcessTimer.AutoReset = true;
                _autoProcessTimer.Enabled = true;
                //Prevent Garbage collection release timer
                GC.KeepAlive(_autoProcessTimer);
            }
        }

        public override async Task ProcessAsync(ConsumeResult<string, byte[]> msg, CancellationToken cancellationToken)
        {
            try
            {
                //logging for counting kafka messages
                //step 1. prepare topic details
                var topicPartitionKey = msg.TopicPartition;
                PreprocessPartition(topicPartitionKey);

                // step 2. store latest current offset
                await EnqueueLatestMessage(topicPartitionKey, msg);

                if (_projectMessageQueue[topicPartitionKey].Count < _kafkaOption.Consumer!.BatchSize)
                    return;

                try
                {
                    await ProcessAsync(topicPartitionKey);
                }
                catch (Confluent.Kafka.KafkaException kex)
                {
                    _logger.LogError("Ingestion_Error ProcessMessagesAsync KafkaException {kex}", kex);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Ingestion_Error ProcessMessagesAsync fail {ex}", ex);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Ingestion_Error ProcessAsync.HandleMessageReceivedAsync fail {ex}", ex);
            }
        }

        public override async Task ProcessEndOfQueueAsync(ConsumeResult<string, byte[]> consumeResult, CancellationToken cancellationToken)
        {
            try
            {
                var topicPartitionKey = consumeResult.TopicPartition;
                if (consumeResult.Message != null && consumeResult.Message.Value != null && consumeResult.Message.Value.Length > 0)
                {
                    PreprocessPartition(topicPartitionKey);
                    _projectMessageQueue[topicPartitionKey].Enqueue(consumeResult);
                }

                if (!_projectMessageQueue.ContainsKey(topicPartitionKey) || _projectMessageQueue[topicPartitionKey].Count == 0)
                    return;// no need to process empty topic

                await ProcessAsync(consumeResult.TopicPartition);
            }
            catch (Exception ex)
            {
                _logger.LogError("Ingestion_Error ProcessAsync.HandleMessageReceivedAsync fail {ex}", ex);
            }
        }

        public async virtual Task ProcessAsync(TopicPartition topicPartitionKey)
        {
            var watch = Stopwatch.StartNew();
            
            await HandleMultipleMessages(topicPartitionKey);
            ///Manual commit behavior require to increase offset manually
            ///Read comment at: https://github.com/confluentinc/confluent-kafka-dotnet/issues/1380
            ///Code at https://github.com/confluentinc/confluent-kafka-dotnet/blob/25f320a672b4324d732304cb4efa2288867b320c/src/Confluent.Kafka/Consumer.cs#L369
            var currentOffset = _topicPartitionOffset[topicPartitionKey];
            var commitOffset = _kafkaOption.IsPartitionEOFEnabled ? currentOffset.Offset : currentOffset.Offset + 1;
            CommitTopicPartitionOffsets([new TopicPartitionOffset(currentOffset.TopicPartition, commitOffset)]);
            ResumeTopicPartitions([topicPartitionKey]);
            watch.Stop();
            _logger.LogInformation("***completed ProcessMessagesAsync: {topicPartitionKey}, took {ms} ms***", topicPartitionKey, watch.ElapsedMilliseconds);
        }

        public abstract Task HandleMultipleMessages(TopicPartition topicPartitionKey);
        
        public override void OnTopicPartitionRemovedCallback(List<TopicPartitionOffset> tpos)
        {
            try
            {
                _logger.LogInformation("OnTopicPartitionRemovedCallback triggered: {tpos}", tpos);
                ClearResoucesForRebalance(tpos.Select(t => t.TopicPartition).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError("Ingestion_Error OnTopicPartitionRemovedCallback tpos:{tpos} --- ex: {ex}", tpos, ex);
            }
        }

        public override void OnTopicPartitionReAssignCallback(List<TopicPartition> tps)
        {
            try
            {
                _logger.LogInformation("OnTopicPartitionReAssignCallback tps: {tps}", tps);

                if (tps.Any())
                {
                    var cleanList = new List<TopicPartition>();
                    foreach (var tp in tps)
                    {
                        if (_topicPartitionHistory.ContainsKey(tp) && _topicPartitionHistory[tp] == true)
                        {
                            cleanList.Add(tp);
                        }
                    }
                    ClearResoucesForRebalance(cleanList);
                    ResumeTopicPartitions(cleanList);
                    _logger.LogInformation("OnTopicPartitionReAssignCallback resumed tps: {cleanList}", cleanList);
                }
            }
            catch (Confluent.Kafka.TopicPartitionException kex)
            {
                _logger.LogError("Ingestion_Error OnTopicPartitionReAssignCallback tps: {tps} ---- kex: {kex}", tps, kex);
            }
            catch (Exception ex)
            {
                _logger.LogError("Ingestion_Error OnTopicPartitionReAssignCallback tps: {tps} ---- ex: {ex}", tps, ex);
            }
        }

        #region separate partition
        /// <summary>
        /// Check if current topic partition doesn't have Queues batch messages, then create new one.
        /// </summary>
        /// <param name="topicPartitionKey"></param>
        /// <returns></returns>
        private void PreprocessPartition(TopicPartition topicPartitionKey)
        {
            if (_projectMessageQueue.ContainsKey(topicPartitionKey))
                return;
            _projectMessageQueue[topicPartitionKey] = new();
            _topicPartitionHistory[topicPartitionKey] = true;
            _logger.LogInformation("***Init _projectMessageQueue: {topicPartitionKey}***", topicPartitionKey);
        }

        private async Task EnqueueLatestMessage(TopicPartition topicPartitionKey, ConsumeResult<string, byte[]> msg)
        {
            var semaphore = _semaphoreSlims.GetOrAdd(topicPartitionKey, _lockFactory.Create(topicPartitionKey.GetHashCode().ToString()));
            await semaphore.WaitAsync();
            _topicPartitionOffset[topicPartitionKey] = msg.TopicPartitionOffset;
            _projectMessageQueue[topicPartitionKey].Enqueue(msg);
            semaphore.Release();
        }

        private async Task HandleMessagePeriodicallyAsync(object sender, ElapsedEventArgs e)
        {
            //ignore if there previous execution wasn't complete yet.
            if (_inprocessingPeriodically)
            {
                _logger.LogInformation("HandleMessagePeriodicallyAsync in running state, ignore...");
                return;
            }
            var watch = Stopwatch.StartNew();
            _inprocessingPeriodically = true;
            await Parallel.ForEachAsync(_projectMessageQueue.Keys, _parallelOptions, async (topicPartitionKey, ct) =>
            {
                try
                {
                    await ProcessAsync(topicPartitionKey);
                }
                catch (Confluent.Kafka.KafkaException kex)
                {
                    _logger.LogError("Ingestion_Error ProcessMessagesAsync KafkaException {kex}", kex);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Ingestion_Error ProcessMessagesAsync fail {ex}", ex);
                }

            });
            _inprocessingPeriodically = false;
            watch.Stop();
            _logger.LogDebug("HandleMessagePeriodicallyAsync took {watch} ms", watch.ElapsedMilliseconds);
        }

        private void ClearResoucesForRebalance(List<TopicPartition> tps)
        {
            foreach (TopicPartition topicPartition in tps)
            {
                ClearResouceForRebalance(topicPartition);
            }
        }
        private void ClearResouceForRebalance(TopicPartition topicPartition)
        {
            _projectMessageQueue.Remove(topicPartition, out _);
            _topicPartitionOffset.Remove(topicPartition, out _);
            _logger.LogInformation("ClearResouceForRebalance deleted successfully: {topicPartition}", topicPartition);
        }

        #endregion
    }
}
