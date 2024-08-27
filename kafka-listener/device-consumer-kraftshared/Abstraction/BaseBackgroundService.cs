using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Device.Consumer.KraftShared.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

namespace Device.Consumer.KraftShared.Abstraction
{
    public abstract class BaseBackgroundService<TK, TD> : BackgroundService
    {
        private readonly ChannelProvider<TK, TD> _channelProvider;
        private readonly KafkaPartitionsHandler<TK, TD> _kafkaHandler;
        private readonly IConfiguration _configuration;
        private readonly KafkaOption _kafkaOption;
        private readonly ILogger<BaseBackgroundService<TK, TD>> _logger;
        private readonly ConcurrentDictionary<TopicPartition, bool> _topicInPausedState;
        private IConsumer<TK, TD> _consumer;

        protected BaseBackgroundService(
            ILogger<BaseBackgroundService<TK, TD>> logger,
            ChannelProvider<TK, TD> channelProvider,
            KafkaPartitionsHandler<TK, TD> kafkaHandler,
            IConfiguration configuration)
        {
            _channelProvider = channelProvider;
            _kafkaHandler = kafkaHandler;
            _configuration = configuration;
            _logger = logger;
            _kafkaOption = configuration.GetSection("Kafka").Get<KafkaOption>();
            _topicInPausedState = new();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();
            _consumer = CreateConsumer();
            _consumer.Subscribe(TopicName);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    //step 1. consume from kafka
                    var consumeResult = _consumer.Consume(stoppingToken);
                    if (_kafkaOption.IsPartitionEOFEnabled && consumeResult.IsPartitionEOF)
                    {
                        await ProcessEndOfQueueAsync(consumeResult, stoppingToken);
                        continue;
                    }

                    // _consumer.StoreOffset(consumeResult);

                    //step 2. write to channel
                    var channelWriter = _channelProvider.GetChannelWriter(_consumer, consumeResult.TopicPartition, ProcessAsync, stoppingToken);
                    await channelWriter.WriteAsync(consumeResult, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Ingestion_Error BaseBackgroundService failed, got error: {ex}", ex);
                    //should be exit consumer then mark as failure
                    _consumer.Unsubscribe();
                    _consumer.Dispose();
                    break;
                }
            }
            _channelProvider.MarkKafkaCompleted();
        }

        private IConsumer<TK, TD> CreateConsumer()
        {
            var option = _configuration.GetRequiredSection("Kafka").Get<KafkaOption>();
            _logger.LogInformation("KafkaCreateConsumer: Config={option}", JsonSerializer.Serialize(option));
            if (option == null)
                throw new System.Exception("KafkaConfig has not value");

            var config = new ConsumerConfig
            {
                BootstrapServers = option.BootstrapServers,
                GroupId = GroupId,
                PartitionAssignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky,
                EnableAutoOffsetStore = false,
                EnableAutoCommit = false,
                AutoCommitIntervalMs = option?.Consumer?.AutoCommitInterval ?? KafkaDefaultConfig.AutoIntervalCommit,
                FetchMaxBytes = option?.Consumer?.FetchMaxBytes ?? KafkaDefaultConfig.BatchSizeInBytes,
                MessageMaxBytes = option?.Consumer?.MessageMaxBytes,
                EnablePartitionEof = _kafkaOption.IsPartitionEOFEnabled //this config will allow consumer consume latest message status from broker.
            };
            if (option.IsAuthenticationEnabled)
            {
                if (option.AuthenticationType == KafkaAuthenticationType.Plaintext)
                {
                    config.SslEndpointIdentificationAlgorithm = option.KafkaAuthentication.SslEndpointIdentificationAlgorithm;
                    config.SecurityProtocol = SecurityProtocol.SaslPlaintext;
                    config.SaslMechanism = option.KafkaAuthentication.SaslMechanism;
                    config.SaslUsername = option.KafkaAuthentication.Username;
                    config.SaslPassword = option.KafkaAuthentication.Password;
                }
                else
                {
                    config.SslCaLocation = option.KafkaAuthentication?.SslCaLocation;
                    config.SslCaCertificateStores = option.KafkaAuthentication.SslCertificateLocation;
                    config.SecurityProtocol = SecurityProtocol.Ssl;
                    config.SslKeystorePassword = option.KafkaAuthentication.SslKeystorePassword;
                }
            }
            var consumer = new ConsumerBuilder<TK, TD>(config)
                .SetPartitionsAssignedHandler(_kafkaHandler.PartitionsAssignedHandler)
                .SetPartitionsLostHandler(_kafkaHandler.PartitionsLostHandler)
                .SetPartitionsRevokedHandler(_kafkaHandler.PartitionsRevokedHandler)
                .SetErrorHandler(_kafkaHandler.SetErrorHandler)
                .Build();
            Action<List<TopicPartitionOffset>> onTopicFail = OnTopicPartitionRemovedCallback;
            _kafkaHandler.SetCallbackAction(onTopicFail);
            Action<List<TopicPartition>> onReAssign = OnTopicPartitionReAssignCallback;
            _kafkaHandler.SetCallbackAction(onReAssign);
            return consumer;
        }

        public abstract string TopicName { get; }
        public abstract string GroupId { get; }
        public abstract Task ProcessAsync(ConsumeResult<TK, TD> consumeResult, CancellationToken cancellationToken);
        public abstract Task ProcessEndOfQueueAsync(ConsumeResult<TK, TD> consumeResult, CancellationToken cancellationToken);
        public abstract void OnTopicPartitionRemovedCallback(List<TopicPartitionOffset> tpos);
        public abstract void OnTopicPartitionReAssignCallback(List<TopicPartition> tps);
        protected virtual void PauseTopicPartitions(IEnumerable<TopicPartition> tps)
        {
            foreach (var tp in tps)
                _topicInPausedState[tp] = true;

            this._consumer.Pause(tps);
        }

        protected virtual void ResumeTopicPartitions(IEnumerable<TopicPartition> tps)
        {
            var listTopicToResumes = new List<TopicPartition>();
            foreach (var tp in tps)
            {
                if (!(_topicInPausedState.ContainsKey(tp) && _topicInPausedState[tp] == true))
                {
                    _logger.LogError("Ingestion_Error Topic Partition {tp} doesn't in consuming list", tp);
                    continue;
                }
                listTopicToResumes.Add(tp);
                _topicInPausedState[tp] = false;
            }
            if (listTopicToResumes.Any())
                this._consumer.Resume(listTopicToResumes);
        }

        protected virtual void CommitTopicPartitionOffsets(IEnumerable<TopicPartitionOffset> tps)
        {
            if (tps.Any())
                this._consumer.Commit(tps);
        }

        protected virtual void RemoveTopicPartitionTrack(TopicPartition tp)
        {
            _topicInPausedState.TryRemove(tp, out _);
        }
    }
}
