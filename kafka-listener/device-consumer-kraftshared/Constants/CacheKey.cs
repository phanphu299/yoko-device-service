namespace Device.Consumer.KraftShared.Constant
{
    public static class CacheKey
    {
        public const string ASSET_INFORMATION = "{0}_{1}_processing_asset_{2}_information"; // podName, projectId, assetId
        public const string ALIAS_FAILED = "{0}_processing_asset_{1}_attribute_{2}_failed"; // projectId, assetId, attributeId
        public const string ALIAS = "{0}_{1}_processing_asset_{2}_attribute_{3}"; // podName, projectId, assetId, attributeId
    }
}