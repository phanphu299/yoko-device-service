using Confluent.Kafka;

namespace Device.Consumer.KraftShared.Model
{
    public class KafkaProducerOption
    {
        public Acks AckMode { get; set; } = Acks.Leader;
        public double? Linger { get; set; } = 100;
        public int BatchSize { get; set; }
        public string TopicName { get; set; }
    }
}
