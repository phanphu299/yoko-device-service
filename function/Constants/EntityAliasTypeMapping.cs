using System.Collections.Generic;

namespace AHI.Device.Function.Constant
{
    public static class EntityAliasTypeMapping
    {
        public const string ASSET = "asset";
        public const string ASSETTEMPLATE = "assetTemplate";
        public const string DEVICE = "device";
        public const string DEVICETEMPLATE = "deviceTemplate";
        private static readonly IDictionary<string, string> _entityAliasTypeMapping = new Dictionary<string, string>() {
            {IOEntityType.ASSET_TEMPLATE, ASSETTEMPLATE},
            {IOEntityType.DEVICE_TEMPLATE, DEVICETEMPLATE}
        };

        public static string getAliasType(string entityType) => _entityAliasTypeMapping[entityType];
    }
}