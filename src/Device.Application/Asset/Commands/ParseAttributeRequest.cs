using System;

namespace Device.Application.Asset.Command
{
    public class ParseAttributeRequest : ValidatAttributeRequest
    {
        public string Name { get; set; }
        public string MarkupName { get; set; }
        public string MetricName { get; set; }
        public string ExpressionRuntime { get; set; } 
        public string DeviceMarkupName { get; set; } 
        public string IntegrationMarkupName { get; set; }
        public Guid? IntegrationId { get; set; }
        public Guid? TemplateAttributeId { get; set; }
        public DateTime? UpdatedUtc { get; set; }
    }
}
