using Confluent.Kafka;

namespace Device.Consumer.KraftShared.Model
{
    public enum KafkaAuthenticationType
    {
        Plaintext = 1,
        SSL = 2,
    }

    public class KafkaOption
    {
        public string BootstrapServers { get; set; }
        public KafkaProducerOption Producer { get; set; }
        public KafkaConsumerOption Consumer { get; set; }
        public bool IsAuthenticationEnabled { get; set; }
        public bool TimerTriggerEnabled {get;set;}
        public bool IsPartitionEOFEnabled {get;set;}
        public KafkaAuthenticationType AuthenticationType { get; set; }
        public KafkaAuthenticationOption KafkaAuthentication { get; set; }
    }

    public class KafkaAuthenticationOption
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public SslEndpointIdentificationAlgorithm SslEndpointIdentificationAlgorithm { get; set; }
        public SecurityProtocol SecurityProtocol {get;set;}
        public SaslMechanism SaslMechanism { get;set;}
        public string SslCaLocation {  get; set; }
        public string SslCertificateLocation {  get; set; }
        public string SslKeystorePassword {  get; set; }

    }
}
