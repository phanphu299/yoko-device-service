using System;
using System.Collections.Generic;
using AHI.Device.Function.Model.ImportModel;

namespace AHI.Device.Function.Constant
{
    public static class EntityFileMapping
    {
        // mapping between model and the file type supported for file operation on that model
        private static readonly IDictionary<string, string> _entityFileMapping = new Dictionary<string, string>() {
            {IOEntityType.ASSET_TEMPLATE, MimeType.EXCEL},
            {IOEntityType.ASSET_ATTRIBUTE, MimeType.EXCEL},
            {IOEntityType.DEVICE_TEMPLATE, MimeType.JSON},
            {IOEntityType.DEVICE, MimeType.EXCEL},
            {IOEntityType.UOM, MimeType.EXCEL},
            {IOEntityType.ASSET_TEMPLATE_ATTRIBUTE, MimeType.EXCEL}
        };

        // mapping between model and the model specific entity type, use when invoke generic method
        private static readonly IDictionary<string, Type> _entityTypeMapping = new Dictionary<string, Type>() {
            {IOEntityType.ASSET_TEMPLATE, typeof(AssetTemplate)},
            {IOEntityType.ASSET_ATTRIBUTE, typeof(AssetAttribute)},
            {IOEntityType.DEVICE_TEMPLATE, typeof(DeviceTemplate)},
            {IOEntityType.DEVICE, typeof(DeviceModel)},
            {IOEntityType.UOM, typeof(Uom)},
            {IOEntityType.ASSET_TEMPLATE_ATTRIBUTE, typeof(AssetTemplateAttribute)}
        };

        public static string GetMimeType(string entityType) => _entityFileMapping[entityType];
        public static Type GetEntityType(string entityType) => _entityTypeMapping[entityType];
    }
}
