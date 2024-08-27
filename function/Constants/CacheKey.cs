namespace AHI.Device.Function.Constant
{
    public static class CacheKey
    {
        // The common hash key pattern: "{categoryName}:{projectId}:{subId}"
        // categoryName: required
        // projectId, subId: optional
        // For example: "functions_block_execution:214c3121-97d9-4729-1983-08da7047a63d:e7f174bd-4453-4247-8742-2052c785ca9c"

        /// <summary>
        /// {0}: projectId
        /// Used in device service
        /// </summary>
        public const string PROCESSING_ASSET_HASH_KEY = "processing_asset:{0}"; // projectId
        public const string PROCESSING_ASSET_HASH_FIELD = "{0}_{1}"; // assetId, attributeId
        public const string ASSET_INFORMATION_HASH_FIELD = "{0}"; // assetId
        public const string ASSET_HASH_KEY = "assets:{0}"; // projectId

        /// <summary>
        /// {0}: projectId
        /// Used in device service
        /// </summary>
        public const string PROCESSING_DEVICE_HASH_KEY = "processing_device:{0}"; // projectId
        public const string PROCESSING_DEVICE_HASH_FIELD = "{0}_{1}"; // deviceId, sub key

        /// <summary>
        /// {0}: projectId
        /// Used in device service
        /// </summary>
        public const string PROCESSING_FAILED_HASH_KEY = "processing_failed:{0}"; // projectId
        public const string PROCESSING_FAILED_HASH_FIELD = "{0}_{1}"; // assetId, attributeId

        /// <summary>
        /// {0}: projectId
        /// Used in device service
        /// </summary>
        public const string PROCESSING_ASSET_IDS_HASH_KEY = "processing_asset_ids:{0}"; // projectId
        public const string PROCESSING_ASSET_IDS_HASH_FIELD = "{0}_{1}"; // assetIds, sub key

        /// <summary>
        /// {0}: projectId, {1}: deviceId
        /// Used in device service
        /// </summary>
        public const string DEVICE_STATUS_ONLINE_KEY = "device-service_projectId_{0}_deviceId_{1}_status_online"; // projectId, deviceId

        /// <summary>
        /// {0}: assetId
        /// Used in device service, function block service
        /// </summary>
        public const string FUNCTION_BLOCK_TRIGGER_KEY = "functionblock_trigger:{0}"; // assetId


        #region CacheKeys For Device Consumer
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
        #endregion
    }
}