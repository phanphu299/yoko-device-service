using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Confluent.Kafka;
using Device.Consumer.KraftShared.Models.Options;
using Device.Consumer.KraftShared.Services.HealthCheck;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Device.Consumer.KraftShared.Abstraction;
public sealed class ChannelProvider<TKey, TValue>
{
    private readonly ConcurrentDictionary<TopicPartition, Channel<ConsumeResult<TKey, TValue>>> _channels = new();
    private readonly ConcurrentDictionary<TopicPartition, Task> _workers = new();
    private readonly ILogger<ChannelProvider<TKey, TValue>> _logger;
    private readonly BatchProcessingOptions _batchOptions;
    private readonly KafkaHealthCheckService _kafkaHealthCheck;

    public ChannelProvider(ILogger<ChannelProvider<TKey, TValue>> logger, IOptions<BatchProcessingOptions> batchOptions, KafkaHealthCheckService kafkaHealthCheck)
    {
        _logger = logger;
        _batchOptions = batchOptions.Value;
        _kafkaHealthCheck = kafkaHealthCheck;
    }

    public void CreateTopicPartitionChannel(TopicPartition topicPartition)
    {
        if (_channels.ContainsKey(topicPartition))
        {
            _logger.LogWarning("Ingestion_Error already exists {tp} -- ignore", topicPartition);
            return;
            // _logger.LogWarning("Ingestion_Error Duplicate {tp} -- applying recreation", topicPartition);
            // if (!_channels.TryRemove(topicPartition, out _))
            // {
            //     _logger.LogError("Ingestion_Error Unabled to remove topic {tp} during recreate topic partition");
            // };
        }

        _logger.LogInformation("CreateTopicChannel: Start create New {tp}", topicPartition);
        var channel = Channel.CreateBounded<ConsumeResult<TKey, TValue>>(new BoundedChannelOptions(_batchOptions.MaxQueueSize)
        {
            AllowSynchronousContinuations = true,
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = true,
        });
        var addResult = _channels.TryAdd(topicPartition, channel);
        _logger.LogInformation("CreateTopicChannel: Finish create New {tp},{rs}", topicPartition, addResult);
    }

    public ChannelWriter<ConsumeResult<TKey, TValue>> GetChannelWriter(IConsumer<TKey, TValue> consumer,
        TopicPartition topicPartition,
        Func<ConsumeResult<TKey, TValue>, CancellationToken, Task> processingAction,
        CancellationToken cancellationToken)
    {
        if (!_channels.ContainsKey(topicPartition))
        {
            CreateTopicPartitionChannel(topicPartition);
        }

        var channel = _channels[topicPartition];

        if (_workers.ContainsKey(topicPartition))
            return channel.Writer;

        try
        {
            var topicWorker = new TopicPartitionWorker<TKey, TValue>(consumer, channel.Reader, processingAction);
            _workers.TryAdd(topicPartition, topicWorker.ExecuteAsync(cancellationToken));
        }
        catch (KafkaException ke)
        {
            _logger.LogError($"Ingestion_Error TopicPartitionWorker error: {ke.Message}");
        }

        return channel.Writer;
    }

    public void CompleteTopicPartitionChannel(TopicPartition topicPartition)
    {
        var channel = _channels[topicPartition];
        channel.Writer.Complete();
        var res = _channels.TryRemove(topicPartition, out Channel<ConsumeResult<TKey, TValue>> value);
        _logger.LogInformation("Remove channel: {tp},{rs}", topicPartition, res);
        if (res && _channels.Count == 0)
        {
            //All topic partition has been removed in this POD
            _kafkaHealthCheck.KafkaCompleted = true;
        }
    }

    public void RemoveTopicPartitionWorker(TopicPartition topicPartition)
    {
        if (!_workers.ContainsKey(topicPartition))
            return;

        try
        {
            _workers.TryRemove(topicPartition, out var task);
            if (task is not null)
                task.Wait();
            CompleteTopicPartitionChannel(topicPartition);
        }
        catch (KafkaException ke)
        {
            _logger.LogError($"Ingestion_Error TopicPartitionWorker error: {ke.Message}");
        }
    }

    public void MarkKafkaCompleted()
    {
        _logger.LogWarning("Marking current Consumer as completed and has left the topic.");
        this._kafkaHealthCheck.KafkaCompleted = true;
    }
}
