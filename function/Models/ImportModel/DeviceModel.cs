using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using AHI.Device.Function.Model.ImportModel.Converter;

namespace AHI.Device.Function.Model.ImportModel
{
    public class DeviceModel : FileParser.Model.ImportModel
    {
        public string Id { get; set; }
        public string Name { get; set; }

        [JsonConverter(typeof(JsonStringTrimmer))]
        public string Tags { get; set; }

        public string Template { get; set; }
        public Guid? TemplateId { get; set; }
        public int? RetentionDays { get; set; }
        public Guid? BrokerId { get; set; }
        public string BrokerType { get; set; }
        public string BrokerProjectId { get; set; }
        public int? SasTokenDuration { get; set; }
        public int? TokenDuration { get; set; }
        public string BrokerName { get; set; }
        public bool HasBinding { get; set; }
        public string CreatedBy { get; set; }
        public string TelemetryTopic { get; set; }
        public string CommandTopic { get; set; }
        public bool? HasCommand { get; set; }
    }
}