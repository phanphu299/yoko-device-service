using System;
using System.Collections.Generic;
using Device.Application.Analytic.Query.Model;
using Device.Application.Constant;
using Device.Application.Historical.Query;
using MediatR;

namespace Device.Application.Analytic.Query
{
    public class GetAssetAttributeStatisticsData : IRequest<IEnumerable<AssetStatisticsDataDto>>
    {
        public string TimezoneOffset { get; set; } = "+00:00";
        public int TimeoutInSecond { get; set; }
        public int? Quality { get; set; }

        public string FitMethod { get; set; }
        public int Order { get; set; }

        public IEnumerable<AssetAttributeStatisticsData> Assets { get; set; }
        public GetAssetAttributeStatisticsData()
        {
            Assets = new List<AssetAttributeStatisticsData>();
        }
    }
    public class AssetAttributeStatisticsData
    {
        public int TimeoutInSecond { get; set; }
        public HistoricalDataType RequestType { get; set; }
        public string TimezoneOffset { get; set; }

        public Guid AssetId { get; set; }

        public string ProjectId { get; set; }
        public string SubscriptionId { get; set; }
        public long Start { get; set; }
        public long End { get; set; }
        public string TimeGrain { get; set; }
        public string Aggregate { get; set; }
        public IDictionary<string, string> Statics { get; set; }
        public IEnumerable<Guid> AttributeIds { get; set; }
        public bool UseCustomTimeRange { get; set; }
        public int PageSize { get; set; }
        public string GapfillFunction { get; set; } = PostgresFunction.TIME_BUCKET_GAPFILL; // valid: time_bucket_gapfill, time_bucket, default: time_bucket_gapfill
        public AssetAttributeStatisticsData() : base()
        {
            AttributeIds = new List<Guid>();
            Start = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeMilliseconds();
            End = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Aggregate = "avg";
            RequestType = HistoricalDataType.SERIES;
        }
    }
}
