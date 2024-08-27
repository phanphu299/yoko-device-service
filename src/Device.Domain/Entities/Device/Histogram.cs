using System;

namespace Device.Domain.Entity
{
    public class Histogram
    {
        public Guid AssetId { get; set; }
        public Guid AttributeId { get; set; }
        public string DeviceId { get; set; }
        public string MetricKey { get; set; }
        public int TotalBin { get; set; }
        public double ValueFrom { get; set; }
        public double ValueTo { get; set; }
        public int[] Items { get; set; }
    }
}