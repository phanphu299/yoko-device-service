namespace Device.Consumer.KraftShared.Model
{
    public static class KafkaDefaultConfig
    {
        public const int AutoIntervalCommit = 500;
        public const int BatchSizeInBytes = 1000000; // 1Kb;
    }
}
