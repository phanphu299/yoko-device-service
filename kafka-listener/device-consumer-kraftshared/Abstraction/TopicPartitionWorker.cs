using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace Device.Consumer.KraftShared.Abstraction
{
    /// <summary>
    /// Receive message from channel. Invoke processor and autocommit.
    /// For each partition then create on worker.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public sealed class TopicPartitionWorker<TKey, TValue>
    {
        private readonly IConsumer<TKey, TValue> _consumer;
        private readonly ChannelReader<ConsumeResult<TKey, TValue>> _channelReader;
        private readonly Func<ConsumeResult<TKey, TValue>, CancellationToken, Task> _processingAction;
        private TopicPartitionOffset _lastOffset;

        public TopicPartitionWorker(IConsumer<TKey, TValue> consumer,
            ChannelReader<ConsumeResult<TKey, TValue>> channelReader,
            Func<ConsumeResult<TKey, TValue>, CancellationToken, Task> processingAction)
        {
            _consumer = consumer;
            _channelReader = channelReader ?? throw new ArgumentNullException(nameof(channelReader));
            _processingAction = processingAction ?? throw new ArgumentNullException(nameof(processingAction));
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (await _channelReader.WaitToReadAsync(cancellationToken))
            {
                while (_channelReader.TryRead(out var consumeResult))
                {
                    _lastOffset = consumeResult.TopicPartitionOffset;
                    await _processingAction(consumeResult, cancellationToken);
                }
            }
            await GracefulShutdownAsync(_lastOffset.TopicPartition, cancellationToken);
        }

        public async Task GracefulShutdownAsync(TopicPartition topicPartition, CancellationToken cancellationToken)
        {
            Console.WriteLine($"GracefulShutdownAsync topic {topicPartition.Topic} - partition: {topicPartition.Partition}...");
            _consumer.Commit(new[] { new TopicPartitionOffset(_lastOffset.TopicPartition, _lastOffset.Offset) });
            //wait for no more task being execute.
            await _channelReader.Completion;
        }
    }
}
