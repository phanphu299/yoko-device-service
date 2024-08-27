using Function.Extension;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Numerics;
using AHI.Device.Function.Constant;
using System.Linq.Expressions;

namespace AHI.Device.Function.Model.ExportModel
{
    public class DeviceTemplate
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public ICollection<ImportExportTagDto> Tags { get; set; }
        public ICollection<TemplatePayload> Payloads { get; private set; }
        public ICollection<TemplateBinding> Bindings { get; private set; }

        public DeviceTemplate()
        {
            Payloads = new List<TemplatePayload>();
            Bindings = new List<TemplateBinding>();
            Tags = new List<ImportExportTagDto>();
        }
    }

    public class TemplatePayload
    {
        [JsonIgnore]
        public Guid TemplateId { get; set; }
        public int Id { get; set; }
        public string JsonPayload { get; set; }
        public ICollection<TemplateDetail> Details { get; private set; }

        public TemplatePayload()
        {
            Details = new List<TemplateDetail>();
        }
    }

    public class TemplateDetail
    {
        public int TemplatePayloadId { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public int KeyTypeId { get; set; }
        public string KeyType { get; set; }
        public int DataTypeId { get; set; }
        public string DataType { get; set; }
        public string Expression { get; set; }
        public bool Enabled { get; set; }
    }

    public class TemplateDetailDto
    {
        static Func<TemplateDetailDto, TemplateDetail> Converter = Projection.Compile();
        public int TemplatePayloadId { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public int KeyTypeId { get; set; }
        public string KeyType { get; set; }
        public int DataTypeId { get; set; }
        public string DataType { get; set; }
        public string Expression { get; set; }
        public bool Enabled { get; set; }
        public Guid DetailId { get; set; }

        private static Expression<Func<TemplateDetailDto, TemplateDetail>> Projection
        {
            get
            {
                return model => new TemplateDetail
                {
                    TemplatePayloadId = model.TemplatePayloadId,
                    Key = model.Key,
                    KeyType = model.KeyType,
                    Name = model.Name,
                    KeyTypeId = model.KeyTypeId,
                    DataTypeId = model.DataTypeId,
                    DataType = model.DataType,
                    Expression = model.Expression,
                    Enabled = model.Enabled
                };
            }
        }

        public static TemplateDetail ConvertToTemplateDetail(TemplateDetailDto model)
        {
            return Converter(model);
        }
    }

    public class TemplateBinding
    {
        [JsonIgnore]
        public Guid TemplateId { get; set; }
        public string Key { get; set; }
        public int DataTypeId { get; set; }
        public string DataType { get; set; }
        public object DefaultValue { get; private set; }
        [JsonIgnore]
        public string DefaultValueString { get; set; }

        public void ConvertDefaultValue()
        {
            if (DefaultValue != null)
                return;

            DefaultValue = DataType switch
            {
                DataTypeConstants.TYPE_BOOLEAN => bool.Parse(DefaultValueString),
                DataTypeConstants.TYPE_INTEGER => int.Parse(DefaultValueString),
                DataTypeConstants.TYPE_DOUBLE => double.Parse(DefaultValueString),
                _ => DefaultValueString
            };

            if (DefaultValue is double && ((double)DefaultValue).IsInteger(15))
            {
                var value = (double)DefaultValue;
                try
                {
                    var valueAsInt = Convert.ToInt64(value);
                    DefaultValue = valueAsInt;
                }
                catch
                {
                    var valueAsBigInt = BigInteger.Parse(value.ToString());
                    DefaultValue = valueAsBigInt;
                }
            }
        }
    }
}
