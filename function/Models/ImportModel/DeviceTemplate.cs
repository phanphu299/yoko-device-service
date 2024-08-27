using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using AHI.Device.Function.Model.ImportModel.Converter;

namespace AHI.Device.Function.Model.ImportModel
{
    public class DeviceTemplate : FileParser.Model.ImportModel
    {
        [JsonConverter(typeof(JsonStringTrimmer))]
        public string Name { get; set; }

        public int TotalMetric { get; set; }

        public string CreatedBy { get; set; }

        public ICollection<TemplatePayload> Payloads { get; set; }

        public ICollection<TemplateBinding> Bindings { get; set; }

        public ICollection<ImportExportTagDto> Tags { get; set; }

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
        public Guid? TemplateId { get; set; }
        [JsonConverter(typeof(JsonStringTrimmer))]
        public string JsonPayload { get; set; }
        public ICollection<TemplateDetail> Details { get; set; }
        public TemplatePayload()
        {
            Details = new List<TemplateDetail>();
        }
    }

    public class TemplateDetail
    {
        [JsonIgnore]
        public int? TemplatePayloadId { get; set; }
        public string Key { get; set; }
        [JsonConverter(typeof(JsonStringTrimmer))]
        public string Name { get; set; }
        [JsonIgnore]
        public int? KeyTypeId { get; set; }
        [JsonConverter(typeof(JsonStringTrimmer))]
        public string KeyType { get; set; }
        [JsonConverter(typeof(JsonStringTrimmer))]
        public string DataType { get; set; }
        [JsonRequired]
        public bool Enabled { get; set; }
        [JsonConverter(typeof(JsonStringTrimmer))]
        public string Expression { get; set; }
        [JsonConverter(typeof(JsonStringTrimmer))]
        public string ExpressionCompile { get; set; }
        [JsonConverter(typeof(JsonStringTrimmer))]
        public Guid DetailId { get; set; } = Guid.NewGuid();
    }

    public class TemplateBinding
    {
        [JsonIgnore]
        public Guid? TemplateId { get; set; }
        [JsonConverter(typeof(JsonStringTrimmer))]
        public string Key { get; set; }
        [JsonIgnore]
        public int? DataTypeId { get; set; }
        [JsonConverter(typeof(JsonStringTrimmer))]
        public string DataType { get; set; }
        public object DefaultValue { get; set; }
    }
}
