using System;
using System.Collections.Generic;

namespace Device.Consumer.KraftShared.Model
{
    public class ParseAssetAttributeMessage
    {
        public Guid ActivityId { get; set; } = Guid.NewGuid();
        public string AssetId { get; set; }
        public string FileName { get; set; }
        public string ObjectType { get; set; }
        public string Upn { get; set; }
        public string DateTimeFormat { get; set; }
        public string DateTimeOffset { get; set; }
        public IEnumerable<UnsaveAttribute> UnsavedAttributes { get; set; } = new List<UnsaveAttribute>();
    }
}
