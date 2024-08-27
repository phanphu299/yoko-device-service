using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Device.Application.Historical.Query;
using Device.Application.Uom.Command.Model;

namespace Device.Application.Analytic.Query.Model
{
    public class RegressionDataDto
    {
        public AssetRegressionDataDto IndependenceAsset { get; set; }
         public AssetRegressionDataDto DependenceAsset { get; set; }
        public string Equation { get; set; }
        public IEnumerable<ValuesDics> Coefficients { get; set; }
        public IEnumerable<ValuesDics> GoodnessMeansures { get; set; }
        public IEnumerable<FitingPoint> SamplePlots { get; set; }
    }

    public class ValuesDics
    {
        public string Name { get; set; }
        public double Value { get; set; }
        public ValuesDics(string name, double value)
        {
            Name = name;
            Value = value;
        }

    }


    public class AssetRegressionDataDto
    {
        public Guid AssetId { get; set; }
        public string AssetName { get; set; }
        public string AssetNormalizeName { get; set; }
        //[JsonProperty(PropertyName = "aggregate")]
        public string Aggregate { get; set; }
        // [JsonProperty(PropertyName = "timegrain")]
        public string TimeGrain { get; set; }

        //[JsonProperty(PropertyName = "metrics")]
        public IEnumerable<AssetAttributeRegressionDataDto> Attributes { get; set; }
        // [JsonProperty(PropertyName = "start")]
        public long Start { get; set; }
        //[JsonProperty(PropertyName = "end")]
        public long End { get; set; }
        public HistoricalDataType QueryType { get; set; }
        public string RequestType { get; set; }
        public IDictionary<string, string> Statics { get; set; }
        public string TimezoneOffset { get; set; }        
        
    }
    public class AssetAttributeRegressionDataDto
    {
         public Guid AttributeId { get; set; }
        public string AttributeName { get; set; }
        public string AttributeNameNormalize { get; set; }
        public bool? ThousandSeparator { get; set; }
        public GetSimpleUomDto Uom { get; set; }
        public int? DecimalPlace { get; set; }


        static Func<(Guid, string,bool, string, GetSimpleUomDto, bool?, int?), AssetAttributeRegressionDataDto> Converter = Projection.Compile();

        private static Expression<Func<(Guid attributeId, string dataType, bool isRawData, string attributeName, GetSimpleUomDto uom, bool? thousandSeparator , int? decimalPlace), AssetAttributeRegressionDataDto>> Projection
        {
            get
            {
                return entity => new AssetAttributeRegressionDataDto
                {
                    AttributeId = entity.attributeId,
                    AttributeName = entity.attributeName,
                    Uom = entity.uom,
                    ThousandSeparator = entity.thousandSeparator,
                    DecimalPlace = entity.decimalPlace
                };
            }
        }

        public static AssetAttributeRegressionDataDto Create(Guid attributeId, string dataType, bool isRawData, string attributeName = null, GetSimpleUomDto uom = null, bool? thousandSeparator = null, int? decimalPlace = null)
        {
           
            return Converter((attributeId, dataType, isRawData, attributeName, uom, thousandSeparator, decimalPlace));
        }
      
    }
}
