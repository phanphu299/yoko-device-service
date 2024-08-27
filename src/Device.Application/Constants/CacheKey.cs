namespace Device.Application.Constant
{
    public static class CacheKey
    {
        // The common hash key pattern: "{categoryName}:{projectId}:{subId}"
        // categoryName: required
        // projectId, subId: optional
        // For example: "functions_block_execution:214c3121-97d9-4729-1983-08da7047a63d:e7f174bd-4453-4247-8742-2052c785ca9c"

        /// <summary>
        /// {0}: templateId
        /// Used in device service, function block service
        /// </summary>
        public const string FUNCTION_BLOCK_TEMPLATE_HASH_KEY = "functions_block_template:{0}"; // templateId
        public const string FUNCTION_BLOCK_TEMPLATE_HASH_FIELD = "{0}"; // projectId

        /// <summary>
        /// {0}: projectId, {1}: Id
        /// Used in device service, function block service
        /// </summary>
        public const string FUNCTION_BLOCK = "functions_block_{0}_{1}"; // projectId, Id

        /// <summary>
        /// {0}: projectId
        /// Used in device service, function block service
        /// </summary>
        public const string FUNCTION_BLOCK_EXECUTION_HASH_KEY = "functions_block_execution:{0}"; // projectId
        public const string FUNCTION_BLOCK_EXECUTION_HASH_FIELD = "{0}"; // executionId

        /// <summary>
        /// {0}: projectId
        /// Used in device service, function block service, asset media service, asset table service
        /// </summary>
        public const string ASSET_HASH_KEY = "assets:{0}"; // projectId
        public const string ASSET_HASH_FIELD = "{0}"; // assetId

        /// <summary>
        /// {0}: projectId
        /// Used in device service
        /// </summary>
        public const string ASSET_SNAPSHOT_HASH_KEY = "asset_snapshot:{0}"; // projectId
        public const string ASSET_SNAPSHOT_HASH_FIELD = "{0}"; // assetId

        /// <summary>
        /// {0}: projectId
        /// Used in device service
        /// </summary>
        public const string FULL_ASSET_HASH_KEY = "full_asset_snapshot:{0}"; // projectId
        public const string FULL_ASSET_HASH_FIELD = "{0}"; // assetId

        /// <summary>
        /// {0}: projectId
        /// Used in device service, function block service
        /// </summary>
        public const string ALL_ALIAS_REFERENCE_ATTRIBUTES_HASH_KEY = "all_alias_reference_attributes:{0}"; // projectId
        public const string ALL_ALIAS_REFERENCE_ATTRIBUTES_HASH_FIELD = "{0}_{1}"; // rootAttributeId, aliasAttributeId

        /// <summary>
        /// {0}: projectId
        /// Used in device service, function block service
        /// </summary>
        public const string ALIAS_REFERENCE_ID_HASH_KEY = "alias_reference_id:{0}"; // projectId
        public const string ALIAS_REFERENCE_ID_HASH_FIELD = "{0}"; // attributeId

        /// <summary>
        /// {0}: projectId
        /// Used in device service
        /// </summary>
        public const string PROCESSING_ASSET_HASH_KEY = "processing_asset:{0}"; // projectId

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

        /// <summary>
        /// {0}: projectId
        /// Used in device service
        /// </summary>
        public const string PROCESSING_ASSET_IDS_HASH_KEY = "processing_asset_ids:{0}"; // projectId

        /// <summary>
        /// {0}: projectId
        /// Used in device service
        /// </summary>
        public const string ASSET_RELATED_RUNTIME_HASH_KEY = "asset_related_runtime_key:{0}"; // projectId

        /// <summary>
        /// {0}: projectId
        /// Used in device service
        /// </summary>
        public const string ATTRIBUTE_HASH_KEY = "attribute:{0}"; // projectId

        /// <summary>
        /// {0}: projectId, {1}: deviceId
        /// Used in device service
        /// </summary>
        public const string DEVICE_STATUS_ONLINE_KEY = "device-service_projectId_{0}_deviceId_{1}_status_online"; // projectId, deviceId

        /// <summary>
        /// {0}: projectId
        /// Used in device service
        /// </summary>
        public const string MQTT_USERNAME_HASH_KEY = "mqtt_username:{0}"; // userName
        public const string MQTT_USERNAME_FIELD_KEY = "password";

        /// <summary>
        /// {0}: projectId
        /// Used in device service
        /// </summary>
        public const string MQTT_USERNAME_ACL_HASH_KEY = "mqtt_acl_username:{0}"; // userName
        public const string MQTT_USERNAME_ACL_FIELD_KEY = "{0}"; // topic

        /// <summary>
        /// {0}: projectId
        /// Used in device service, function block service
        /// </summary>
        public const string DEVICE_SIGNAL_QUALITY_CODES = "device-service_projectId_{0}_device_signal_quality_codes"; // projectId

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
