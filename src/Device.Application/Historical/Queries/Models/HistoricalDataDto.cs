using System;
using System.Collections.Generic;
using System.Linq;
using MessagePack;

namespace Device.Application.Historical.Query.Model
{
    [MessagePackObject]
    public class HistoricalDataDto : HistoricalGeneralDto
    {

        [Key("attributes")]
        public List<AttributeDto> Attributes { get; set; }

        public HistoricalDataDto() { }
        public HistoricalDataDto(long timeStart, long timeEnd, string aggregate, string timegrain, IEnumerable<AttributeDto> metrics)
        {
            Start = timeStart;
            End = timeEnd;
            Aggregate = aggregate;
            TimeGrain = timegrain;
            Attributes = metrics.ToList();
        }

        public HistoricalDataDto(long timeStart, long timeEnd, string aggregate, string timegrain, IEnumerable<AttributeDto> metrics, Guid assetId, string assetName, HistoricalDataType requestType)
            : this(timeStart, timeEnd, aggregate, timegrain, metrics)
        {
            AssetId = assetId;
            AssetName = assetName;
            QueryType = requestType;
        }
    }


    [MessagePackObject]
    public class HistoricalGeneralDto
    {
        [Key("assetId")]
        public Guid AssetId { get; set; }
        [Key("assetName")]
        public string AssetName { get; set; }
        [Key("assetNormalizeName")]
        public string AssetNormalizeName { get; set; }
        //[JsonProperty(PropertyName = "aggregate")]
        [Key("aggregate")]
        public string Aggregate { get; set; }
        // [JsonProperty(PropertyName = "timegrain")]
        [Key("timeGrain")]
        public string TimeGrain { get; set; }

        // [JsonProperty(PropertyName = "start")]
        [Key("start")]
        public long Start { get; set; }
        //[JsonProperty(PropertyName = "end")]
        [Key("end")]
        public long End { get; set; }
        [Key("queryType")]
        public HistoricalDataType QueryType { get; set; }
        [Key("requestType")]
        public string RequestType { get; set; }
        [Key("statics")]
        public IDictionary<string, string> Statics { get; set; }
        [Key("timezoneOffset")]
        public string TimezoneOffset { get; set; }
    }
}
