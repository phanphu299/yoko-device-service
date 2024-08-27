using System;
using System.Collections.Generic;
using AHI.Device.Function.Constant;
using Function.Extension;
using static AHI.Device.Function.Constant.ErrorMessage;

namespace AHI.Device.Function.Model.ImportModel.Attribute
{
    public class AttributeTemplate : FileParser.Model.ImportModel
    {
        public AttributeTemplate()
        {
        }
        public AttributeTemplate(Guid? id, string attributeType, Guid? channelId, string channelMarkup, string deviceMarkup, string deviceTemplate)
        {
            AttributeId = id;
            AttributeType = attributeType;
            ChannelId = channelId;
            ChannelMarkup = channelMarkup;
            DeviceMarkup = deviceMarkup;
            DeviceTemplate = deviceTemplate;
        }

        public Guid? AttributeId { get; set; } = Guid.NewGuid();
        public string AttributeName { get; set; }

        public AssetAttributeType? Type { get; private set; }
        public string AttributeType
        {
            get => Type?.ToString()?.ToLower();
            set
            {
                if (Enum.TryParse<AssetAttributeType>(value, true, out var type))
                    Type = type;
                else
                {
                    if (string.IsNullOrWhiteSpace(value))
                        return;

                    var exception = new ArgumentException(ParseValidation.PARSER_INVALID_DATA);
                    exception.Data["validationInfo"] = new Dictionary<string, object>
                    {
                        { ErrorMessage.ErrorProperty.ERROR_PROPERTY_NAME, ErrorMessage.ErrorProperty.AttributeTemplate.TYPE },
                        { ErrorMessage.ErrorProperty.ERROR_PROPERTY_VALUE, value }
                    };
                    throw exception;
                }
            }
        }

        private string _deviceTemplate;
        public string DeviceTemplate
        {
            get => _deviceTemplate;
            set
            {
                if (Type == AssetAttributeType.DYNAMIC || Type == AssetAttributeType.COMMAND || Type == AssetAttributeType.INTEGRATION)
                    _deviceTemplate = value;
            }
        }

        public Guid? DeviceTemplateId { get; set; }

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

        public Guid? ChannelId { get; set; }

        private string _channelMarkup;
        public string ChannelMarkup
        {
            get => _channelMarkup;
            set
            {
                if (Type == AssetAttributeType.INTEGRATION)
                    _channelMarkup = value;
            }
        }

        private string _device;
        public string Device
        {
            get => _device;
            set
            {
                if (Type == AssetAttributeType.INTEGRATION)
                    _device = value;
            }
        }

        private string _deviceMarkup;
        public string DeviceMarkup
        {
            get => _deviceMarkup;
            set
            {
                if (Type == AssetAttributeType.DYNAMIC || Type == AssetAttributeType.INTEGRATION || Type == AssetAttributeType.COMMAND)
                    _deviceMarkup = value;
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

        //public string DataType { get; set; }

        public string Uom { get; set; }
        public int? UomId { get; set; }
        public string DecimalPlace { get; set; }
        public string ThousandSeparator { get; set; }
        public Guid? TriggerAssetAttributeId { get; set; }
        public string TriggerAssetAttribute { get; set; }
        private string _enabledExpression;
        public string EnabledExpression
        {
            get => _enabledExpression;
            set
            {
                if (Type == AssetAttributeType.RUNTIME)
                    _enabledExpression = value;
            }
        }
        private string _expression;
        public string Expression
        {
            get => _expression;
            set
            {
                if (Type == AssetAttributeType.RUNTIME)
                    _expression = value;
            }
        }
        public Uom UomDetail { get; set; }
        public string MetricName { get; set; }
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
                    ChannelMarkup = null;
                    DeviceTemplateId = null;
                    DeviceTemplate = null;
                    Device = null;
                    DeviceMarkup = null;
                    Metric = null;
                    break;
                case nameof(Device):
                    DeviceTemplateId = null;
                    DeviceTemplate = null;
                    Metric = null;
                    DeviceMarkup = null;
                    break;
                case nameof(Metric):
                    Metric = null;
                    break;
                case nameof(Uom):
                    Uom = null;
                    break;
                case nameof(EnabledExpression):
                    EnabledExpression = FormatDefaultConstants.ATTRIBUTE_RUNTIME_ENABLE_EXPRESSION_DEFAULT;
                    Expression = null;
                    TriggerAssetAttribute = null;
                    break;
            }
        }
    }
}
