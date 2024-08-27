using System;
using System.Collections.Generic;
using Device.Application.Analytic.Query.Model;
using Device.Application.Constant;
using Device.Application.Historical.Query;
using Device.Application.Historical.Query.Model;
using MediatR;

namespace Device.Application.Analytic.Query
{
    public class GetAssetAttributeRegressionData : IRequest<RegressionDataDto>
    {
        public string TimezoneOffset { get; set; } = "+00:00";
        public int TimeoutInSecond { get; set; }
        public int? Quality { get; set; }

        public string FitMethod { get; set; }
        public int Order { get; set; }
        public int LimitSample { get; set; }

        public IEnumerable<AssetAttributeRegression> Assets { get; set; }
        public GetAssetAttributeRegressionData()
        {
            Assets = new List<AssetAttributeRegression>();
        }

    }

    public class AssetAttributeRegression
    {
        public int TimeoutInSecond { get; set; }
        public HistoricalDataType RequestType { get; set; }
        public string TimezoneOffset { get; set; }

        public Guid AssetId { get; set; }
        public VARIABLETYPE Variable { get; set; } // 0: dependent variable (y) and 1: independent variable (x).

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
        public AssetAttributeRegression()
        {
            AttributeIds = new List<Guid>();
            Start = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeMilliseconds();
            End = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Aggregate = "avg";
            RequestType = HistoricalDataType.SERIES;
        }
    }
    public class HistoricalDataDtoRegression : HistoricalDataDto
    {
        public VARIABLETYPE Variable { get; set; } // 0: dependent variable (y) and 1: independent variable (x).
    }

    public enum VARIABLETYPE
    {
        DEPENDENTVARIABLE = 0,
        INDEPENDENTVARIABLE = 1
    }
    public class FitingPoint
    {
        public long ts { get; set; }
        public double x { get; set; }
        public double y { get; set; }
        public long lts { get; set; }
        public int? q { get; set; }
    }
}

