using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Device.Application.Historical.Query.Model;

namespace Device.Application.Analytic.Query.Model
{
    public class AssetHistogramDataDto
    {
        public string ProjectId { get; set; }
        public string SubscriptionId { get; set; }
        public Guid AssetId { get; set; }
        public string AssetName { get; set; }
        public string AssetNormalizeName { get; set; }
        public double BinSize { get; set; }
        public IEnumerable<AssetAttributeHistogramDataDto> Attributes { get; set; }
        public long Start { get; set; }
        public long End { get; set; }
        public IDictionary<string, string> Statics { get; set; }
        public string Aggregate { get; set; }
        public string TimeGrain { get; set; }
        public string TimezoneOffset { get; set; }
        public string GapfillFunction { get; set; }
    }
    public class AssetAttributeHistogramDataDto
    {
        public Guid AttributeId { get; set; }
        public string AttributeName { get; set; }
        public string AttributeNameNormalize { get; set; }
        public IEnumerable<HistogramDistribution> Distributions { get; set; }
        public bool? ThousandSeparator { get; set; }
        public string GapfillFunction { get; set; }
        static Func<(AttributeDto Attribute, double BinSize), AssetAttributeHistogramDataDto> Converter = Projection.Compile();
        private static Expression<Func<(AttributeDto Attribute, double BinSize), AssetAttributeHistogramDataDto>> Projection
        {
            get
            {
                return entity => new AssetAttributeHistogramDataDto
                {
                    AttributeId = entity.Attribute.AttributeId,
                    AttributeName = entity.Attribute.AttributeName,
                    AttributeNameNormalize = entity.Attribute.AttributeNameNormalize,
                    ThousandSeparator = entity.Attribute.ThousandSeparator,
                    Distributions = HistogramDistribution.Create(entity.Attribute.Series, entity.BinSize)
                };
            }
        }
        public static AssetAttributeHistogramDataDto Create(AttributeDto entity, double binSize)
        {
            if (entity == null)
                return null;
            return Converter((entity, binSize));
        }


    }
    public class HistogramDistribution
    {
        public double ValueFrom { get; set; }
        public double ValueTo { get; set; }
        public int Count { get; set; }
        public HistogramDistribution(double valueFrom, double valueTo, int count)
        {
            ValueFrom = valueFrom;
            ValueTo = valueTo;
            Count = count;
        }
        public static IEnumerable<HistogramDistribution> Create(IEnumerable<TimeSeriesDto> series, double binSize)
        {
            if (binSize <= 0)
            {
                return Array.Empty<HistogramDistribution>();
            }
            var min = series.Min(x => Convert.ToDouble(x.v));
            var max = series.Max(x => Convert.ToDouble(x.v));
            var minFloor = binSize * (int)(min / binSize);
            var output = new List<HistogramDistribution>();
            for (double i = minFloor; i < max; i += binSize)
            {
                output.Add(new HistogramDistribution(i, i + binSize, series.Where(x => Convert.ToDouble(x.v) >= i && Convert.ToDouble(x.v) < (i + binSize)).Count()));
            }
            return output;
        }
    }
}
