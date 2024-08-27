using System;
using System.Collections.Generic;
using AHI.Device.Function.Constant;
using Function.Extension;
using static AHI.Device.Function.Constant.ErrorMessage;

namespace AHI.Device.Function.Model.ImportModel
{
    public class AssetAttribute : FileParser.Model.ImportModel
    {
        public Guid Id { get; set; }
        public string AttributeName { get; set; }
        public AssetAttributeType? Type { get; private set; }
        private string _attributeType;
        public string AttributeType
        {
            get => _attributeType?.ToLower();
            set
            {
                if (string.IsNullOrEmpty(value))
                    return;

                if (Enum.TryParse<AssetAttributeType>(value, true, out var type) && Enum.IsDefined(typeof(AssetAttributeType), type))
                {
                    Type = type;
                    _attributeType = value;
                }
                else
                {
                    var exception = new ArgumentException(ParseValidation.PARSER_INVALID_DATA);
                    exception.Data["validationInfo"] = new Dictionary<string, object>
                    {
                        { "PropertyName", "Type" },
                        { "PropertyValue", value }
                    };
                    throw exception;
                }
            }
        }
        public string DeviceId { get; set; }
        public Guid? ChannelId { get; set; }
        private string _channel;
        public string Channel
        {
            get => _channel;
            set
            {
                if (Type == AssetAttributeType.INTEGRATION)
                    _channel = value;
            }
        }
        private string _metric;
        public string Metric
        {
            get => _metric;
            set
            {
                if (Type == AssetAttributeType.DYNAMIC || Type == AssetAttributeType.INTEGRATION || Type == AssetAttributeType.COMMAND)
                    _metric = value;
            }
        }
        public string _value;
        public string Value
        {
            get => _value;
            set
            {
                if (Type == AssetAttributeType.STATIC)
                    _value = value;
            }
        }
        private string _dataType;
        public string DataType
        {
            get => _dataType;
            set
            {
                if (Type == AssetAttributeType.STATIC)
                {
                    if (string.IsNullOrEmpty(value))
                        return;

                    var validData = DataTypeExtensions.IsDataTypeForAttribute(value);
                    if (!validData)
                    {
                        var exception = new ArgumentException(ParseValidation.PARSER_INVALID_DATA);
                        exception.Data["validationInfo"] = new Dictionary<string, object>
                        {
                            { ErrorMessage.ErrorProperty.ERROR_PROPERTY_NAME, ErrorMessage.ErrorProperty.AssetAttribute.DATA_TYPE },
                            { ErrorMessage.ErrorProperty.ERROR_PROPERTY_VALUE, value }
                        };
                        throw exception;
                    }
                }
                _dataType = value?.ToLower();
            }
        }

        public string AliasAsset { get; set; }
        public string AliasAssetName { get; set; }
        public string AliasAttribute { get; set; }
        public Guid? AliasAttributeId { get; set; }
        public string TriggerAttribute { get; set; }
        public Guid? TriggerAttributeId { get; set; }
        public string EnabledExpression { get; set; }
        public string Expression { get; set; }
        public string Uom { get; set; }
        public int? UomId { get; set; }
        public object UomData { get; set; }
        public string DecimalPlace { get; set; }
        public string ThousandSeparator { get; set; }
        public DateTime? UpdatedUtc { get; set; }
        public bool IsStaticAttribute => string.Equals(AttributeType, AttributeTypeConstants.TYPE_STATIC, StringComparison.InvariantCultureIgnoreCase);
        public bool IsAliasAttribute => string.Equals(AttributeType, AttributeTypeConstants.TYPE_ALIAS, StringComparison.InvariantCultureIgnoreCase);
        public bool IsDynamicAttribute => string.Equals(AttributeType, AttributeTypeConstants.TYPE_DYNAMIC, StringComparison.InvariantCultureIgnoreCase);
        public bool IsRuntimeAttribute => string.Equals(AttributeType, AttributeTypeConstants.TYPE_RUNTIME, StringComparison.InvariantCultureIgnoreCase);
        public bool IsCommandAttribute => string.Equals(AttributeType, AttributeTypeConstants.TYPE_COMMAND, StringComparison.InvariantCultureIgnoreCase);
        public bool IsIntegrationAttribute => string.Equals(AttributeType, AttributeTypeConstants.TYPE_INTEGRATION, StringComparison.InvariantCultureIgnoreCase);

        public void SetDefaultValue(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(Channel):
                    Channel = null;
                    DeviceId = null;
                    Metric = null;
                    break;
                case nameof(DeviceId):
                    DeviceId = null;
                    Metric = null;
                    break;
                case nameof(Metric):
                    Metric = null;
                    break;
                case nameof(AliasAsset):
                    AliasAsset = null;
                    AliasAttribute = null;
                    break;
                case nameof(AliasAttribute):
                    AliasAttribute = null;
                    break;
                case nameof(Uom):
                    Uom = null;
                    break;
                case nameof(EnabledExpression):
                    EnabledExpression = FormatDefaultConstants.ATTRIBUTE_RUNTIME_ENABLE_EXPRESSION_DEFAULT;
                    Expression = null;
                    TriggerAttribute = null;
                    break;
            }
        }
    }
}
