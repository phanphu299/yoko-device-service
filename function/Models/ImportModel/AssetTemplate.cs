using System;
using System.Linq;
using System.Collections.Generic;
using AHI.Device.Function.FileParser.Model;
using ValidationMessage = AHI.Device.Function.Constant.ErrorMessage.FluentValidation;
using AHI.Device.Function.Constant;

namespace AHI.Device.Function.Model.ImportModel
{
    public enum AssetAttributeType
    {
        STATIC,
        DYNAMIC,
        RUNTIME,
        INTEGRATION,
        COMMAND,
        ALIAS
    }

    public class AssetTemplateValue
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    public class AssetTemplate : ComplexModel
    {
        public string Name { get; set; }
        public ICollection<AssetTemplateAttribute> Attributes { get; set; }
        public string Tags { get; set; }

        public override Type ChildType => typeof(AssetTemplateAttribute);
        public override IEnumerable<object> ChildProperty
        {
            set
            {
                var attributes = new List<AssetTemplateAttribute>();
                if (value != null)
                {
                    attributes.AddRange(value.Where(x => x is AssetTemplateAttribute)
                                             .Select(x => x as AssetTemplateAttribute));
                }
                Attributes = attributes;
            }
        }
    }

    public class AssetTemplateAttribute : TrackModel
    {
        public Guid AssetTemplateId { get; set; }
        public Guid? AttributeId { get; set; }
        public string AttributeName { get; set; }

        public AssetAttributeType? Type { get; private set; }
        public string AttributeType
        {
            get => Type.ToString();
            set
            {
                if (Enum.TryParse<AssetAttributeType>(value, true, out var type))
                    Type = type;
                else
                {
                    var exception = new ArgumentException(ValidationMessage.GENERAL_INVALID_VALUE);
                    exception.Data["validationInfo"] = new Dictionary<string, object>
                    {
                        { "propertyName", "Type" },
                        { "propertyValue", value }
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
                if (Type == AssetAttributeType.DYNAMIC || Type == AssetAttributeType.COMMAND)
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

        private string _dataType = "text";
        public string DataType
        {
            get => _dataType;
            set
            {
                _dataType = value;
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

        public string Uom { get; set; }
        public int? UomId { get; set; }
        public int? DecimalPlace { get; set; }
        public bool? _thousandSeparator;
        public bool? ThousandSeparator
        {
            get => (DataType == DataTypeConstants.TYPE_INTEGER || DataType == DataTypeConstants.TYPE_DOUBLE) ? (_thousandSeparator ?? true) : null;
            set
            {
                _thousandSeparator = value;
            }
        }
        public string TriggerAssetAttribute { get; set; }
        public bool EnabledExpression { get; set; }
        public string Expression { get; set; }
    }
}
