namespace AHI.Device.Function.Constant
{
    public static class BrokerConstants
    {
        public const string EMQX_MQTT = "BROKER_EMQX_MQTT";
        public const string EMQX_COAP = "BROKER_EMQX_COAP";
        public const string IOT_HUB = "BROKER_IOT_HUB";
        public static readonly string[] EMQX_BROKERS = { EMQX_MQTT, EMQX_COAP };
    }
}