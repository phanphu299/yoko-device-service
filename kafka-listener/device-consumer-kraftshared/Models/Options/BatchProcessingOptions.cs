namespace Device.Consumer.KraftShared.Models.Options
{
    public class BatchProcessingOptions
    {
        public int MaxQueueSize { get; set; }
        public int MaxWorker { get; set; }
        public int SnapshotMaxWorker {get;set;}
        public int UpsertSize { get; set; }
        public int InsertSize { get; set; }
        public int MaxOpenConnection { get; set; }
        public int AutoCommitInterval { get; set; }
        public int MaxChunkSize { get; set; }
        public int RedisMaxChunkSize {get;set;}
        public int MaxTransformChunkSize { get; set; }
        public int SyncInterval { get; set; }
        public bool EnabledBackgroundSnapshotSync { get; set; }
        public bool EnabledBackgroundSnapshotDBSync { get;set;}
        public bool EnabledCompareRedisSnapshots { get; set; }
        public bool EnabledPullAssetTriggerOnce { get; set; }
        public PreloadDataOptions PreloadData { get; set; }
    }

    public class PreloadDataOptions
    {
        public bool Enabled { get; set; }
        public int LoadDeviceInformations { get; set; }
        public int LoadAssetAttributes { get; set; }
        public int LoadAssetTriggers { get; set; }
        public int LoadAssetAttributeAlias { get; set; }
    }
}
