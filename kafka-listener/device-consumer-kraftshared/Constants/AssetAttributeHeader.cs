using System.Collections.Generic;

namespace Device.Consumer.KraftShared.Constant
{
    public static class AssetAttributeHeader
    {
        public const string ATTRIBUTE_NAME = "AttributeName";
        public const string ATTRIBUTE_TYPE = "AttributeType";
        public const string DEVICE_ID = "DeviceId";
        public const string CHANNEL = "Channel";
        public const string METRIC = "Metric";
        public const string VALUE = "Value";
        public const string DATA_TYPE = "DataType";
        public const string ALIAS_ASSET = "AliasAsset";
        public const string ALIAS_ATTRIBUTE = "AliasAttribute";
        public const string ENABLED_EXPRESSION = "EnabledExpression";
        public const string EXPRESSION = "Expression";
        public const string TRIGGER_ATTRIBUTE = "TriggerAttribute";
        public const string UOM = "Uom";
        public const string DECIMAL_PLACES = "DecimalPlace";
        public const string THOUSAND_SEPARATOR = "ThousandSeparator";

        private static readonly IDictionary<string, string> _propertyMapping = new Dictionary<string, string>() {
            {AssetAttributeHeader.ATTRIBUTE_NAME, ErrorMessage.ErrorProperty.AssetAttribute.ATTRIBUTE_NAME},
            {AssetAttributeHeader.ATTRIBUTE_TYPE, ErrorMessage.ErrorProperty.AssetAttribute.ATTRIBUTE_TYPE},
            {AssetAttributeHeader.DEVICE_ID, ErrorMessage.ErrorProperty.AssetAttribute.DEVICE_ID},
            {AssetAttributeHeader.CHANNEL, ErrorMessage.ErrorProperty.AssetAttribute.CHANNEL},
            {AssetAttributeHeader.METRIC, ErrorMessage.ErrorProperty.AssetAttribute.METRIC},
            {AssetAttributeHeader.ALIAS_ASSET, ErrorMessage.ErrorProperty.AssetAttribute.ALIAS_ASSET},
            {AssetAttributeHeader.ALIAS_ATTRIBUTE, ErrorMessage.ErrorProperty.AssetAttribute.ALIAS_ATTRIBUTE},
            {AssetAttributeHeader.VALUE, ErrorMessage.ErrorProperty.AssetAttribute.VALUE},
            {AssetAttributeHeader.DATA_TYPE, ErrorMessage.ErrorProperty.AssetAttribute.DATA_TYPE},
            {AssetAttributeHeader.ENABLED_EXPRESSION, ErrorMessage.ErrorProperty.AssetAttribute.ENABLED_EXPRESSION},
            {AssetAttributeHeader.EXPRESSION, ErrorMessage.ErrorProperty.AssetAttribute.EXPRESSION},
            {AssetAttributeHeader.TRIGGER_ATTRIBUTE, ErrorMessage.ErrorProperty.AssetAttribute.TRIGGER_ATTRIBUTE},
            {AssetAttributeHeader.UOM, ErrorMessage.ErrorProperty.AssetAttribute.UOM},
            {AssetAttributeHeader.DECIMAL_PLACES, ErrorMessage.ErrorProperty.AssetAttribute.DECIMAL_PLACES},
            {AssetAttributeHeader.THOUSAND_SEPARATOR, ErrorMessage.ErrorProperty.AssetAttribute.THOUSAND_SEPARATOR}
        };

        public static string getPropertyName(string prop) => _propertyMapping[prop];
    }
}
