namespace Device.Consumer.KraftShared.Constants
{
    public class IngestionRedisCacheKeys
    {
        /// <summary>
        /// device_infos:{projectId}
        /// //hash field = device_id, hash value = json deviceInfo
        /// </summary>
        public const string DeviceInfoPattern = "device_infos:{0}";
        /// <summary>
        /// asset_runtime_triggers:{projectId}
        ///  //hash field = attributeId, hash value = asset runtime trigger 
        /// </summary>
        public const string AssetRuntimeTriggerPattern = "asset_runtime_triggers:{0}";
        /// <summary>
        /// asset_attributes:{projectId}
        /// //hash field = attributeId, hash value = asset attributes
        /// </summary>
        public const string AssetAttributesPattern = "asset_attributes:{0}";
        /// <summary>
        /// asset_snapshots:{projectId}:{assetId}
        /// //hash field = attributeId, hash value = asset snapshot
        /// </summary>
        public const string AssetAttributeRuntimeSnapshotsPattern = "asset_attribute_runtime_snapshots:{0}:{1}";
        /// <summary>
        /// atrribute_alias:{projectId}:{assetId}
        /// //hash field = attributeId, hash value = asset alias
        /// </summary>
        public const string AttributeAliasPattern = "atrribute_alias:{0}:{1}";
        /// <summary>
        /// atrribute_alias_mapping:{projectId}
        ///  //hash field = aliasAttributeId, hash value = target attributeId linked by alias
        /// </summary>
        public const string AliasAttributeMappingPattern = "atrribute_alias_mapping:{0}";
        /// <summary>
        /// asset_device_id:{projectId}:{deviceId}
        /// //hash field = assetId, hash value = asset info
        /// </summary>
        public const string AssetsDeviceIdPattern = "asset_device_id:{0}:{1}";
        /// <summary>
        /// link_asset_device_id:{projectId}
        /// //hash field = deviceId, hash value = array of assetIds
        /// </summary>
        public const string LinkAssetsDeviceIdPattern = "link_asset_device_id:{0}";
        /// <summary>
        /// asset_snapshots:{projectId}:{assetId}
        /// //hash field = attributeId, hash value = asset snapshot
        /// </summary>
        public const string AssetAttributeSnapshotsPattern = "asset_attribute_snapshots:{0}:{1}";

        /// <summary>
        /// device_snapshots:{projectId}:{deviceId}
        /// //hash field = metricKey, hash value = metric value
        /// </summary>
        public const string DeviceMetricSnapshotsPattern = "device_snapshots:{0}:{1}";
        /// <summary>
        /// asset_infos:{projectId}
        /// //hash field = asset_id, hash value = json assetings
        /// </summary>
        public const string AssetInfosPattern = "asset_infos:{0}";
    }
}
