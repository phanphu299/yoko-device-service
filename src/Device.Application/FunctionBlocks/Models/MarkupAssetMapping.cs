using System;

namespace Device.Application.Model
{
    public class MarkupAssetMapping
    {
        public string MarkupName { get; set; }
        public Guid? AssetId { get; set; }
        public string AssetName { get; set; }
    }
}