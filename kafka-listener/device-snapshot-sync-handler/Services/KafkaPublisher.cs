
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;
using Device.Consumer.KraftShared.Service.Abstraction;
using Microsoft.Extensions.Logging;

namespace Device.Consumer.SnapshotSyncHandler.Services
{
    public class KafkaPublisher : IPublisher
    {
        private readonly IProducer<string, byte[]> _publisher;
        private readonly ILogger<KafkaPublisher> _logger;

        public KafkaPublisher(IProducer<string, byte[]> publisher, ILogger<KafkaPublisher> logger)
        {
            _publisher = publisher;
            _logger = logger;
        }

        public Task SendAsync<T>(T message, string topicName) where T : class
        {
           _publisher.Produce(topicName, new Message<string, byte[]>()
            {
                Value = JsonSerializer.SerializeToUtf8Bytes(message)
           }, HandleReport);
            return Task.CompletedTask;
        }

        public Task SendAsync<T>(T message, string topicName, string key) where T : class
        {
            throw new System.NotImplementedException();
        }

        private void HandleReport(DeliveryReport<string, byte[]> report)
        {
            if(report.Error != null && report.Error.IsError)
                _logger.LogInformation("Handle report: time={time} topic={topic},error={error}", report.Timestamp, report.Topic, report.Error != null ? JsonSerializer.Serialize(report.Error) : string.Empty);
        }
    }
}
