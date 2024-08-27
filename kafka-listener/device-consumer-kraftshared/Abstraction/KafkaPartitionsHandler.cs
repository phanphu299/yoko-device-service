using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Device.Consumer.KraftShared.Abstraction
{
    public sealed class KafkaPartitionsHandler<TKey, TValue>
    {
        private readonly ConcurrentDictionary<TopicPartition, bool> _assignedPartition;
        private readonly ILogger<KafkaPartitionsHandler<TKey, TValue>> _logger;
        private readonly ChannelProvider<TKey, TValue> _channelProvider;
        private readonly string _podName;
        private Action<List<TopicPartitionOffset>> OnTopicPartitionRemovedCallback;
        private Action<List<TopicPartition>> OnTopicPartitionReAssignCallback;
        public KafkaPartitionsHandler(ILogger<KafkaPartitionsHandler<TKey, TValue>> logger,
            ChannelProvider<TKey, TValue> channelProvider,
            IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _channelProvider = channelProvider ?? throw new ArgumentNullException(nameof(channelProvider));
            _podName = configuration["PodName"] ?? "device-consumer-kafka";
            _assignedPartition = new();
        }

        public void PartitionsAssignedHandler(IConsumer<TKey, TValue> _, List<TopicPartition> tps)
        {
            try
            {
                ProcessAssignedPartition(tps);
                // OnTopicPartitionReAssignCallback.Invoke(tps);
                // foreach (var tp in tps)
                // {
                //     _logger.LogInformation("TopicPartition assigned: {pod}/{TopicPartition}", _podName, tp);
                //     _channelProvider.CreateTopicPartitionChannel(tp);
                // }
            }
            catch (Exception ex)
            {
                _logger.LogError("Ingestion_Error PartitionsAssignedHandler tps: {TopicPartition}, error: {ex}", tps, ex);
            }
        }

        public void PartitionsLostHandler(IConsumer<TKey, TValue> _, List<TopicPartitionOffset> tpos)
        {
            foreach (var tpo in tpos)
            {
                _logger.LogInformation("TopicPartition lost:  {pod}/{TopicPartition}", _podName, tpo.TopicPartition);
                _channelProvider.CompleteTopicPartitionChannel(tpo.TopicPartition);
            }
        }

        public void PartitionsRevokedHandler(IConsumer<TKey, TValue> consumer, List<TopicPartitionOffset> tpos)
        {
            try
            {
                // consumer.Commit(tpos);
                foreach (var tpo in tpos)
                {
                    _logger.LogInformation("TopicPartition rebalancing:  {pod}/{TopicPartition}", _podName, tpo.TopicPartition);
                    // Need to remove channel
                    // Error if remove during rebalance
                    // _channelProvider.RemoveTopicPartitionWorker(tpo.TopicPartition);
                    // Temporary commit, might we will need to handle task for db

                    // _channelProvider.RemoveTopicPartitionWorker(tpo.TopicPartition);
                }
                OnTopicPartitionRemovedCallback.Invoke(tpos);
            }
            catch (Exception ex)
            {
                _logger.LogError("Ingestion_Error PartitionsRevokedHandler got error:  {TopicPartition}, error: {ex}", tpos, ex);
            }

        }

        public void SetCallbackAction(Action<List<TopicPartitionOffset>> action)
        {
            this.OnTopicPartitionRemovedCallback = action;
        }

        public void SetCallbackAction(Action<List<TopicPartition>> action)
        {
            this.OnTopicPartitionReAssignCallback = action;
        }


        public void SetErrorHandler(IConsumer<TKey, TValue> consumer, Confluent.Kafka.Error error)
        {
            _logger.LogError($"Ingestion_error:: error code: {error.Code} message: {error.Reason}", error);
        }

        /// <summary>
        /// Remove old partition if has been taken by another Pods, then Create TopicPartition Channel 
        /// </summary>
        /// <param name="newPartitions"></param>
        /// <returns></returns>
        private void ProcessAssignedPartition(IEnumerable<TopicPartition> newPartitions)
        {
            //step 1. remove all old partition doesn't in new parititon, (which means already assigned to another POD)
            var outdatedPartition = _assignedPartition.Keys.Except(newPartitions).ToList();
            OnTopicPartitionReAssignCallback.Invoke(outdatedPartition);

            //step 2. remove existing worker , handler that outdated (assigned to another POD).
            foreach (var key in outdatedPartition)
            {
                if (_assignedPartition.TryRemove(key, out var provider))
                    _logger.LogInformation("Removed cosolidated partition topic: {}", key);
                else
                    _logger.LogError("Ingestion_Error Fail to remove cosolidated partition topic: {}", key);
                _channelProvider.RemoveTopicPartitionWorker(key);
            }

            //step 3. if new then start process for create new Worker, Handler
            foreach (var newPartition in newPartitions.Except(_assignedPartition.Keys))
            {
                _logger.LogInformation("TopicPartition assigned: {pod}/{TopicPartition}", _podName, newPartition);
                _channelProvider.CreateTopicPartitionChannel(newPartition);
            }
        }
    }
}
