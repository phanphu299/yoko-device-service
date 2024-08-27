using Confluent.Kafka;

namespace Device.Consumer.KraftShared.Model
{
    public class KafkaConsumerOption
    {
        public int? AutoCommitInterval { get; set; }
        public string GroupId { get; set; }
        public string? IngestionTopicName { get; set; }
        public int BatchSize { get; set; }
        public int FetchMaxBytes { get; set; }
        public int MessageMaxBytes { get; set; }
        public int BatchMaxBytes { get; set; }
    }
}
