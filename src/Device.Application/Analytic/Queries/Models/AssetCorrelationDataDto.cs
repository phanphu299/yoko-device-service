using System;
using System.Collections.Generic;
using Device.Application.Historical.Query;

namespace Device.Application.Analytic.Query.Model
{
    public class AssetCorrelationDataDto
    {
        public Guid AssetId { get; set; }
        public string AssetName { get; set; }
        public string AssetNormalizeName { get; set; }
        public string Aggregate { get; set; }
        public string TimeGrain { get; set; }
        public long? Start { get; set; }
        public long? End { get; set; }
        public HistoricalDataType QueryType { get; set; }
        public string RequestType { get; set; }
        public IDictionary<string, string> Statics { get; set; }
        public string TimezoneOffset { get; set; }
        public string GapfillFunction { get; set; }
        public double[][] Correlations { get; set; }
        public IEnumerable<Guid> NoDataAttributeIds { get; set; }
        public AssetCorrelationDataDto()
        {
            //Attributes = new List<AttributeCorrelationDataDto>();
            // Correlations = new List<double[]>();
        }
    }
}
