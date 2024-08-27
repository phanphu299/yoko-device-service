namespace Device.Application.Constant
{
    public class BrokerConstants
    {
        public const string EMQX_MQTT = "BROKER_EMQX_MQTT";
        public const string EMQX_COAP = "BROKER_EMQX_COAP";
        public const string IOT_HUB = "BROKER_IOT_HUB";
        public static readonly string[] EMQX_BROKERS = { EMQX_MQTT, EMQX_COAP };
    }

    public class BrokerContentKeys
    {
        public const string TELEMETRY_TOPIC = "telemetry_topic";
        public const string COMMAND_TOPIC = "command_topic";
    }

    public class BrokerStatus
    {
        public const string ACTIVE = "AC";
    }
}