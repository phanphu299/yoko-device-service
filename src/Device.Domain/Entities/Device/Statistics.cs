using System;

namespace Device.Domain.Entity
{
    public class Statistics
    {
        public Guid AssetId { get; set; }
        public Guid AttributeId { get; set; }
        public string DeviceId { get; set; }
        public string MetricKey { get; set; }
        public double STDev { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public double Mean { get; set; }
        public double Q1_Inc { get; set; }
        public double Q2_Inc { get; set; }
        public double Q3_Inc { get; set; }
         public double Q1_Exc { get; set; }
        public double Q2_Exc { get; set; }
        public double Q3_Exc { get; set; }    
    }
}
