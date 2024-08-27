using System.Collections.Generic;

namespace AHI.Device.Function.Model
{
    public class AssetTemplateAttributeMessage
    {
        public string FileName { get; set; }
        public string ObjectType { get; set; }
        public string TemplateId { get; set; }
        public string Upn { get; set; }
        public string DateTimeFormat { get; set; }
        public string DateTimeOffset { get; set; }
        public IEnumerable<UnsaveAttribute> UnsavedAttributes { get; set; } = new List<UnsaveAttribute>();
    }
}
