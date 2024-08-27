using System.Collections.Generic;
using Function.Extension;
using Newtonsoft.Json;

namespace AHI.Device.Function.Model.ExportModel
{
    public class Uom
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Lookup { get; set; }
        public double? RefFactorValue { get; set; }
        public string RefFactor => RefFactorValue.ToNumberString();
        public double? RefOffsetValue { get; set; }
        public string RefOffset => RefOffsetValue?.ToNumberString();
        public double? CanonicalFactorValue { get; set; }
        public string CanonicalFactor => CanonicalFactorValue?.ToNumberString();
        public double? CanonicalOffsetValue { get; set; }
        public string CanonicalOffset => CanonicalOffsetValue?.ToNumberString();
        public string Abbreviation { get; set; }
        public int? RefId { get; set; }
        public string RefName { get; set; }
        public string Tags { get; set; }
    }
}