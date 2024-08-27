using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Confluent.Kafka;
using Device.Consumer.KraftShared.Abstraction;
using Device.Consumer.KraftShared.Model;
using Device.Consumer.KraftShared.Models;
using Device.Consumer.KraftShared.Models.Options;
using Device.Consumer.KraftShared.Services.HealthCheck;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Device.Consumer.Kraft.Processor
{
    public sealed class IngestionBackgroundService : BaseBackgroundService<Null, byte[]>
    {
        private readonly ConcurrentDictionary<TopicPartition, bool> _topicPartitionHistory;
        private readonly ILogger<IngestionBackgroundService> _logger;
        private readonly IngestionProcessor _processor;
        /// <summary>
        /// key pattern = "topic_partition"
        /// </summary>
        private readonly ConcurrentDictionary<TopicPartition, ConcurrentQueue<ConsumeResult<Null, byte[]>>> _projectMessageQueue;
        /// <summary>
        /// For storing current total bytes of a topic, to decide keeping or bring _projectMessageQueue to process.
        /// </summary>
        private readonly ConcurrentDictionary<TopicPartition, double> _projectMessageQueueInBytes;
        /// <summary>
        /// 
        /// </summary>
        //private readonly ConcurrentDictionary<string, ConcurrentBag<string>> _topicMapping;
        /// <summary>
        /// key pattern = "topic_partition"
        /// </summary>
        private readonly ConcurrentDictionary<TopicPartition, TopicPartitionOffset> _topicPartitionOffset;
        /// <summary>
        /// Determine if any project already triggered calculation to avoid conflict.
        /// key = projectId
        /// value = inprocessing
        /// </summary>
        private readonly ConcurrentDictionary<TopicPartition, bool> _projectInProcessing;
        private readonly KafkaOption _kafkaOption;
        private readonly BatchProcessingOptions _batchOptions;
        private readonly System.Timers.Timer _autoProcessTimer;
        private readonly ParallelOptions _parallelOptions;
        private readonly ILockFactory _lockFactory;
        private readonly ConcurrentDictionary<TopicPartition, ILock> _semaphoreSlims;

        private const string LEAVE_GROUP_MESSAGE = "Broker: Specified group generation id is not valid";

        private readonly KafkaHealthCheckService _kafkaHealthCheck;
        public IngestionBackgroundService(
            ILogger<IngestionBackgroundService> logger,
            ChannelProvider<Null, byte[]> channelProvider,
            KafkaPartitionsHandler<Null, byte[]> kafkaHandler,
            IngestionProcessor processor,
            IConfiguration configuration,
            ILockFactory lockFactory,
            KafkaHealthCheckService kafkaHealthCheck
            ) : base(logger, channelProvider, kafkaHandler, configuration)
        {
            _logger = logger;
            _processor = processor;
            _kafkaOption = configuration.GetSection("Kafka").Get<KafkaOption>();
            _batchOptions = configuration.GetSection("BatchProcessing").Get<BatchProcessingOptions>();
            _semaphoreSlims = new();
            _lockFactory = lockFactory;
            _parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = _batchOptions.MaxWorker };

            _projectMessageQueue = new();
            _projectMessageQueueInBytes = new();
            _projectInProcessing = new();
            _topicPartitionOffset = new();
            _topicPartitionHistory = new();
            _kafkaHealthCheck = kafkaHealthCheck;
            //Prevent Garbage collection release timer

            if (_kafkaOption.TimerTriggerEnabled)
            {
                _autoProcessTimer = new System.Timers.Timer(_batchOptions.AutoCommitInterval);
                _autoProcessTimer.Elapsed += async (sender, e) => await HandleMessagePeriodicallyAsync(sender, e);
                _autoProcessTimer.AutoReset = true;
                _autoProcessTimer.Enabled = true;
                GC.KeepAlive(_autoProcessTimer);
            }
        }

        public override string TopicName => _kafkaOption.Consumer.IngestionTopicName ?? "ingestion-exchange";
        public override string GroupId => $"consumer_group_{TopicName}";
        public override async Task ProcessAsync(ConsumeResult<Null, byte[]> msg, CancellationToken cancellationToken)
        {
            try
            {
                //step 1. prepare topic details
                var topicPartitionKey = msg.TopicPartition;
                PreprocessPartition(topicPartitionKey);

                // step 2. store latest current offset
                await EnqueueLatestMessageAsync(topicPartitionKey, msg);
                if (_projectMessageQueue[topicPartitionKey].Count < _kafkaOption.Consumer!.BatchSize)
                    return;

                //step 3.2 else start execution.
                await ProcessMessagesAsync(topicPartitionKey);
            }
            catch (Exception ex)
            {
                _logger.LogError("Ingestion_Error ProcessAsync.HandleMessageReceivedAsync fail {ex}", ex);
            }
        }

        public override async Task ProcessEndOfQueueAsync(ConsumeResult<Null, byte[]> consumeResult, CancellationToken cancellationToken)
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
                    return; //due to empty message queue => no need to process;

                await ProcessMessagesAsync(topicPartitionKey);
            }
            catch (Exception ex)
            {
                _logger.LogError("Ingestion_Error ProcessEndOfQueueAsync fail {ex}", ex);
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
            _projectMessageQueueInBytes[topicPartitionKey] = 0;
            _topicPartitionHistory[topicPartitionKey] = true;
            _logger.LogInformation("***Init _projectMessageQueue: {topicPartitionKey}***", topicPartitionKey);
        }

        private async Task EnqueueLatestMessageAsync(TopicPartition topicPartitionKey, ConsumeResult<Null, byte[]> msg)
        {
            var semaphore = _semaphoreSlims.GetOrAdd(topicPartitionKey, _lockFactory.Create(topicPartitionKey.GetHashCode().ToString()));
            await semaphore.WaitAsync();
            _topicPartitionOffset[topicPartitionKey] = msg.TopicPartitionOffset;
            _projectMessageQueue[topicPartitionKey].Enqueue(msg);
            semaphore.Release();


        }
        /// <summary>
        /// One problem here is Periodcally keep trigger, mean while message didn't completed previous processed yet
        /// So Khoa decided to ignore incase global _inprocessingPeriodically = true;
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private async Task HandleMessagePeriodicallyAsync(object sender, ElapsedEventArgs e)
        {
            //ignore if there previous execution wasn't complete yet.
            var watch = Stopwatch.StartNew();
            await Parallel.ForEachAsync(_projectMessageQueue.Keys, _parallelOptions, async (topicPartitionKey, ct) =>
            {
                await ProcessMessagesAsync(topicPartitionKey);
            });
            watch.Stop();
        }

        private async Task<(ConsumeResult<Null, byte[]>[] messages, TopicPartitionOffset currentOffset)> GetMessagesByTopicPartitionAsync(TopicPartition topicPartitionKey)
        {
            if (!_projectMessageQueue.ContainsKey(topicPartitionKey))
                return (Array.Empty<ConsumeResult<Null, byte[]>>(), default);

            try
            {
                //step 1. Lock current topic partition to avoid multiple thread execute same topic partition.
                var splock = _semaphoreSlims.GetOrAdd(topicPartitionKey, _lockFactory.Create(topicPartitionKey.GetHashCode().ToString()));
                await splock.WaitAsync();
                if (_projectInProcessing.ContainsKey(topicPartitionKey) && _projectInProcessing[topicPartitionKey] == true)
                {
                    //step 1.1 already in processing by another thread ==> ignore
                    _logger.LogInformation("topicPartitionKey {topicPartitionKey} is in processing", topicPartitionKey);
                    splock.Release();
                    return (Array.Empty<ConsumeResult<Null, byte[]>>(), default);
                }
                var currentOffset = _topicPartitionOffset[topicPartitionKey];
                //step 2. postpond consume message for current topic partition
                PauseTopicPartitions([currentOffset.TopicPartition]);
                //step 3. dequeue messages of current topic partition, then clear
                var messages = _projectMessageQueue[topicPartitionKey].ToArray();
                //step 3.1 ignore process if message is empty
                if (messages.Length == 0)
                {
                    _logger.LogDebug("ProcessMessagesAsync {topicPartitionKey} messages.Length = 0 ===> ignore...", topicPartitionKey);
                    splock.Release();
                    ResumeTopicPartitions([currentOffset.TopicPartition]);
                    return (Array.Empty<ConsumeResult<Null, byte[]>>(), default);
                }
                _projectMessageQueue[topicPartitionKey].Clear();
                _projectMessageQueueInBytes[topicPartitionKey] = 0;
                return (messages, currentOffset);
            }
            catch (Exception ex)
            {
                _logger.LogError("Ingestion_Error GetMessagesByTopicPartitionAsync failed ex: {ex}", ex);
                return (Array.Empty<ConsumeResult<Null, byte[]>>(), default);
            }
        }

        /// <summary>
        /// To handle commit offset and resume partition listening.
        /// </summary>
        /// <param name="currentOffset"></param>
        /// <returns></returns>
        private void PostProcessPartition(TopicPartitionOffset currentOffset)
        {
            //step 5. Commit currentOffset
            ///Manual commit behavior require to increase offset manually
            ///Read comment at: https://github.com/confluentinc/confluent-kafka-dotnet/issues/1380
            ///Code at https://github.com/confluentinc/confluent-kafka-dotnet/blob/25f320a672b4324d732304cb4efa2288867b320c/src/Confluent.Kafka/Consumer.cs#L369
            var commitOffset = _kafkaOption.IsPartitionEOFEnabled ? currentOffset.Offset : currentOffset.Offset + 1;
            try
            {
                CommitTopicPartitionOffsets(new[] { new TopicPartitionOffset(currentOffset.TopicPartition, commitOffset) });
            }
            catch (Exception ex)
            {
                _logger.LogError("Ingestion_Error PostProcessPartition.CommitTopicPartitionOffsets failed currentOffset: {currentOffset} --- ex: {ex}", currentOffset, ex);
            }
            try
            {
                ResumeTopicPartitions([currentOffset.TopicPartition]);
            }
            catch (Exception ex)
            {
                _logger.LogError("Ingestion_Error PostProcessPartition.ResumeTopicPartitions failed TopicPartition: {TopicPartition} --- ex: {ex}", currentOffset.TopicPartition, ex);
            }
        }

        /// <summary>
        /// execute when queue is full either periodically triggered
        /// </summary>
        /// <param name="topicPartitionKey">key pattern = "topic_partition"</param>
        /// <param name="topicName">key standing for a project</param>
        /// <returns></returns>
        private async Task ProcessMessagesAsync(TopicPartition topicPartitionKey)
        {
            try
            {
                ///https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Diagnostics/Stopwatch.cs
                (var messages, var currentOffset) = await GetMessagesByTopicPartitionAsync(topicPartitionKey);
                if (messages.Length == 0)
                    return;
                _projectInProcessing[topicPartitionKey] = true;
                await _processor.ProcessAsync(messages, currentOffset);
                PostProcessPartition(currentOffset);
            }
            catch (Confluent.Kafka.KafkaException kex)
            {
                var errorCode = kex.Error.Code;
                _logger.LogError("Ingestion_Error ProcessMessagesAsync topicPartitionKey: {topicPartitionKey} KafkaException: {kex} ERROR_CODE: {errorCode}", topicPartitionKey, kex, errorCode);
                ClearResouceForRebalance(topicPartitionKey);

                // if (kex.Message.Contains(LEAVE_GROUP_MESSAGE, StringComparison.OrdinalIgnoreCase))
                // {
                //     _logger.LogError("Ingestion_Error Consumer Error, Triggering to restart");
                //     _kafkaHealthCheck.KafkaCompleted = true;
                // }
            }
            catch (Exception ex)
            {
                _logger.LogError("Ingestion_Error ProcessMessagesAsync topicPartitionKey: {topicPartitionKey} -- ex: {ex}", topicPartitionKey, ex);
            }
            finally
            {
                _projectInProcessing[topicPartitionKey] = false;
                ResumeTopicPartitions([topicPartitionKey]);
            }
        }

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
            _projectMessageQueueInBytes.Remove(topicPartition, out _);
            _topicPartitionOffset.Remove(topicPartition, out _);
            _logger.LogInformation("ClearResouceForRebalance deleted successfully: {topicPartition}", topicPartition);
        }


        #endregion


    }
}
